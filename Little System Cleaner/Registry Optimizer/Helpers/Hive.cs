using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Little_System_Cleaner.Registry_Optimizer.Helpers
{
    public class Hive : IDisposable
    {
        private readonly string strHiveName;

        /// <summary>
        /// The file location of the hive that is loaded with Windows
        /// </summary>
        /// <remarks>This file is locked and cannot be removed or changed</remarks>
        private readonly string strHivePath;

        private volatile string strRootKey, strKeyName;

        private volatile string strOldHivePath;

        /// <summary>
        /// Where a backup copy of the hive is saved when RegReplaceKey is called
        /// </summary>
        public string OldHivePath
        {
            get { return strOldHivePath; }
        }

        private volatile string strNewHivePath;

        /// <summary>
        /// The location of the compacted hive (created with RegSaveKey)
        /// </summary>
        public string NewHivePath
        {
            get { return strNewHivePath; }
        }

        private readonly FileInfo _fi = null;
        public FileInfo HiveFileInfo
        {
            get { return _fi; }
        }

        private readonly long lOldHiveSize = 0;
        public long OldHiveSize
        {
            get { return lOldHiveSize; }
        }

        private volatile uint lNewHiveSize = 0;
        public long NewHiveSize
        {
            get { return lNewHiveSize; }
        }

        #region ListView Properties
        public string RegistryHive
        {
            get { return this.HiveFileInfo.Name; }
        }

        public string RegistryHivePath
        {
            get { return this.strHivePath; }
        }

        public string CurrentSize
        {
            get { return Utils.ConvertSizeToString(lOldHiveSize); }
        }

        public string CompactSize
        {
            get { return Utils.ConvertSizeToString(lNewHiveSize); }
        }

        public System.Windows.Controls.Image Image { get; private set; }
        #endregion

        public volatile bool bAnaylzed = false, bCompacted = false;

        private volatile int hKey = 0;
        private bool disposed = false;

        public bool IsValid
        {
            get
            {
                if (this.HiveFileInfo == null)
                    return false;

                if (this.lOldHiveSize == 0)
                    return false;

                if (!File.Exists(this.strHivePath))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Constructor for Hive class
        /// </summary>
        /// <param name="HiveName">Name of Hive (\REGISTRY\USER\...)</param>
        /// <param name="HivePath">Path to Hive (\Device\HarddiskVolumeX\Windows\System32\config\... or C:\Windows\System32\config\...)</param>
        public Hive(string HiveName, string HivePath)
        {
            this.strHiveName = HiveName;

            if (File.Exists(HivePath))
                this.strHivePath = HivePath;
            else
                this.strHivePath = HiveManager.ConvertDeviceToMSDOSName(HivePath);

            try
            {
                this._fi = new FileInfo(this.strHivePath);
                this.lOldHiveSize = GetFileSize(this.strHivePath);
            }
            catch (Exception ex) 
            {
                this._fi = null;
                this.lOldHiveSize = 0;

                System.Diagnostics.Debug.WriteLine("The following error occurred trying to get registry hive information: " + ex.Message); 
            }

            if (this.IsValid)
            {
                // Temporary directory must be on same partition
                char drive = this.strHivePath[0];

                try
                {
                    this.strOldHivePath = HiveManager.GetTempHivePath(drive);
                    this.strNewHivePath = HiveManager.GetTempHivePath(drive);
                }
                catch (Exception ex)
                {
                    this._fi = null;
                    this.lOldHiveSize = 0;

                    System.Diagnostics.Debug.WriteLine("The following error occurred trying to get temporary hive path: " + ex.Message);
                }
            }
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                if (this.hKey != 0)
                    PInvoke.RegCloseKey(hKey);

                hKey = 0;

                if (File.Exists(strOldHivePath))
                    File.Delete(strOldHivePath);

                disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            // Clear HKey
            if (this.hKey != 0)
                PInvoke.RegCloseKey(hKey);

            this.hKey = 0;

            // Remove hives
            if (File.Exists(this.strOldHivePath))
                File.Delete(this.strOldHivePath);

            if (File.Exists(this.strNewHivePath))
                File.Delete(this.strNewHivePath);

            // Reset registry size
            this.lNewHiveSize = 0;

            // Reset analyzed
            this.bAnaylzed = false;
        }

        /// <summary>
        /// Uses Windows RegSaveKeyA API to rewrite registry hive
        /// </summary>
        public void AnalyzeHive()
        {
            try
            {
                if (this.bAnaylzed)
                    throw new Exception("Hive has already been analyzed");

                int nRet = 0, hkey = 0;

                this.strRootKey = this.strHiveName.ToLower();
                this.strKeyName = strRootKey.Substring(strRootKey.LastIndexOf('\\') + 1);

                // Open Handle to registry key
                if (strRootKey.StartsWith(@"\registry\machine"))
                    nRet = PInvoke.RegOpenKeyA((uint)PInvoke.HKEY.HKEY_LOCAL_MACHINE, strKeyName, ref hkey);
                if (strRootKey.StartsWith(@"\registry\user"))
                    nRet = PInvoke.RegOpenKeyA((uint)PInvoke.HKEY.HKEY_USERS, strKeyName, ref hkey);

                if (nRet != 0)
                    return;

                this.hKey = hkey;

                // Begin Critical Region
                Thread.BeginCriticalRegion();

                // Flush hive key
                PInvoke.RegFlushKey(hkey);

                // Function will fail if file already exists
                if (File.Exists(this.strNewHivePath))
                    File.Delete(this.strNewHivePath);

                // Use API to rewrite the registry hive
                nRet = PInvoke.RegSaveKeyA(hkey, this.strNewHivePath, 0);
                if (nRet != 0)
                    throw new Win32Exception(nRet);

                this.lNewHiveSize = (uint)GetFileSize(this.strNewHivePath);

                if (File.Exists(this.strNewHivePath))
                    this.bAnaylzed = true;

                // End Critical Region
                Thread.EndCriticalRegion();
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("error analyzing registry hive");
            }
        }

        /// <summary>
        /// Compacts the registry hive
        /// </summary>
        public void CompactHive()
        {
            if (this.bAnaylzed == false || this.hKey <= 0 || !File.Exists(this.strNewHivePath))
                throw new Exception("You must analyze the hive before you can compact it");

            if (this.bCompacted)
                throw new Exception("The hive has already been compacted");

            // Begin Critical Region
            Thread.BeginCriticalRegion();

            // Old hive cant exist or function will fail
            if (File.Exists(this.strOldHivePath))
                File.Delete(this.strOldHivePath);

            // Replace hive with compressed hive
            int ret = PInvoke.RegReplaceKeyA(this.hKey, null, this.strNewHivePath, this.strOldHivePath);
            if (ret != 0)
                throw new Win32Exception(ret);

            // Hive should now be replaced with temporary hive
            PInvoke.RegCloseKey(this.hKey);

            // End Critical Region
            Thread.EndCriticalRegion();

            this.bCompacted = true;
        }

        /// <summary>
        /// Gets the file size
        /// </summary>
        /// <param name="filePath">Path to the filename</param>
        /// <returns>File Size</returns>
        private long GetFileSize(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);

                return fi.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns the filename of the hive
        /// </summary>
        /// <returns>Hive filename</returns>
        public override string ToString()
        {
            return (string)_fi.Name.Clone();
        }
    }
}

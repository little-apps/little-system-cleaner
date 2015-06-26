using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

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

        private string strRootKey, strKeyName;

        public bool SkipCompact
        {
            get;
            private set;
        }

        private string strOldHivePath;

        /// <summary>
        /// Where a backup copy of the hive is saved when RegReplaceKey is called
        /// </summary>
        public string OldHivePath
        {
            get { return strOldHivePath; }
        }

        private string strNewHivePath;

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

        private uint lNewHiveSize = 0;
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

        public bool bAnaylzed = false, bCompacted = false;

        private int hKey = 0;
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
                try
                {
                    this.GetTempHivePaths();
                }
                catch (Exception ex)
                {
                    this._fi = null;
                    this.lOldHiveSize = 0;

                    System.Diagnostics.Debug.WriteLine("The following error occurred trying to get temporary hive path: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Gets and sets temporary hive paths
        /// </summary>
        /// <see cref="HiveManager.GetTempHivePath"/>
        private void GetTempHivePaths()
        {
            // Temporary directory must be on same partition
            char drive = this.strHivePath[0];

            this.strOldHivePath = HiveManager.GetTempHivePath(drive);
            this.strNewHivePath = HiveManager.GetTempHivePath(drive);
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

        private void OpenHKey()
        {
            int nRet = 0;

            if (string.IsNullOrEmpty(this.strRootKey))
                this.strRootKey = this.strHiveName.ToLower();

            if (string.IsNullOrEmpty(this.strKeyName))
                this.strKeyName = strRootKey.Substring(strRootKey.LastIndexOf('\\') + 1);

            this.hKey = 0;

            // Open Handle to registry key
            if (strRootKey.StartsWith(@"\registry\machine"))
                nRet = PInvoke.RegOpenKeyA((uint)PInvoke.HKEY.HKEY_LOCAL_MACHINE, strKeyName, ref this.hKey);
            if (strRootKey.StartsWith(@"\registry\user"))
                nRet = PInvoke.RegOpenKeyA((uint)PInvoke.HKEY.HKEY_USERS, strKeyName, ref this.hKey);

            if (nRet != 0)
                throw new Win32Exception(nRet);

            if (this.hKey == 0)
                throw new Win32Exception(6); // ERROR_INVALID_HANDLE
        }

        /// <summary>
        /// Uses Windows RegSaveKeyA API to rewrite registry hive
        /// </summary>
        public void AnalyzeHive(Window window)
        {
            if (this.bAnaylzed)
                // Reset previous analyze info
                this.Reset();

            try
            {
                this.CanAnalyze();
            }
            catch (Exception ex)
            {
                // Don't compact hive
                this.SkipCompact = true;

                string message = string.Format("Unable to analyze registry hive: {0}\nThe following error occurred: {1}.\n\nPress 'Enter' to continue...", this.RegistryHive, ex.Message);

                Utils.MessageBoxThreadSafe(window, message, Little_System_Cleaner.Misc.Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            try
            {
                this.PerformAnalyze();
            }
            catch (Win32Exception ex)
            {
                // Don't compact hive
                this.SkipCompact = true;

                string message = string.Format("Unable to perform registry hive analyze on {0}\nError code {1} was returned.\n\nPress 'Enter' to continue...", this.RegistryHive, ex.NativeErrorCode);

                Utils.MessageBoxThreadSafe(window, message, Little_System_Cleaner.Misc.Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }
        }

        /// <summary>
        /// Checks if registry hive can be analyzed
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if the registry cannot be analyzed. Further information is included in the description of the exception.</exception>
        private void CanAnalyze()
        {
            try
            {
                this.OpenHKey();
            }
            catch (Win32Exception ex)
            {
                this.hKey = 0;

                throw new Exception(string.Format("A handle for the registry hive could not be opened (error code {0} was returned).", ex.NativeErrorCode));
            }
        }

        /// <summary>
        /// Analyzes the registry hive
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">This exception will be thrown if RegSaveKey fails</exception>
        private void PerformAnalyze()
        {
            int nRet = 0;

            // Begin Critical Region
            Thread.BeginCriticalRegion();

            // Flush hive key
            PInvoke.RegFlushKey(this.hKey);

            // Function will fail if file already exists
            if (File.Exists(this.strNewHivePath))
                File.Delete(this.strNewHivePath);

            // Use API to rewrite the registry hive
            nRet = PInvoke.RegSaveKeyA(this.hKey, this.strNewHivePath, 0);
            if (nRet != 0)
                throw new Win32Exception(nRet);

            this.lNewHiveSize = (uint)GetFileSize(this.strNewHivePath);

            if (File.Exists(this.strNewHivePath))
                this.bAnaylzed = true;

            // End Critical Region
            Thread.EndCriticalRegion();
        }

        /// <summary>
        /// Compacts the registry hive
        /// </summary>
        /// <param name="window">The window must be specified in order to show messageboxes over it</param>
        public void CompactHive(Window window)
        {
            try 
            {
                this.CanCompact();
            }
            catch (Exception ex)
            {
                string message = string.Format("Unable to compact registry hive: {0}\nThe following error occurred: {1}\n\nPress 'Enter' to continue...", this.RegistryHive, ex.Message);

                Utils.MessageBoxThreadSafe(window, message, Little_System_Cleaner.Misc.Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            try
            {
                this.PerformCompact();
            }
            catch (Win32Exception ex)
            {
                string message = string.Format("Unable to perform registry hive compact on {0}\nError code {1} was returned.\n\nPress 'Enter' to continue...", this.RegistryHive, ex.NativeErrorCode);

                Utils.MessageBoxThreadSafe(window, message, Little_System_Cleaner.Misc.Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }
        }

        /// <summary>
        /// Determines if registry hive can be compacted
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if the registry cannot be compacted. Further information is included in the description of the exception.</exception>
        private void CanCompact()
        {
            if (!this.bAnaylzed)
            {
                throw new Exception("The registry hive must be analyzed before it can be compacted.");
            }
            else if (this.hKey == 0)
            {
                // Try to open handle again
                try
                {
                    this.OpenHKey();
                }
                catch (Win32Exception ex)
                {
                    this.hKey = 0;

                    throw new Exception(string.Format("A handle for the registry hive could not be opened (error code {0} was returned).", ex.NativeErrorCode));
                }
            }
            else if (!File.Exists(this.strNewHivePath))
            {
                throw new Exception(string.Format("The compacted version of the registry hive ({0}) was not created or was deleted from {1}.", this.strHiveName, this.strNewHivePath));
            }
            else if (this.bCompacted)
            {
                throw new Exception(string.Format("The registry hive ({0}) has already been compacted.", this.strHiveName));
            }
        }

        /// <summary>
        /// Compacts the registry hive
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">This exception will be thrown if RegReplaceKey fails</exception>
        private void PerformCompact()
        {
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

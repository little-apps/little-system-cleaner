/*
    Little System Cleaner
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Little_System_Cleaner.Registry_Optimizer.Helpers
{
    internal static class PInvoke
    {
        [DllImport("advapi32.dll", EntryPoint = "RegOpenKey", SetLastError = true)]
        public static extern int RegOpenKeyA(uint hKey, string lpSubKey, ref int phkResult);
        [DllImport("advapi32.dll", EntryPoint = "RegReplaceKey", SetLastError = true)]
        public static extern int RegReplaceKeyA(int hKey, string lpSubKey, string lpNewFile, string lpOldFile);
        [DllImport("advapi32.dll", EntryPoint = "RegSaveKey", SetLastError = true)]
        public static extern int RegSaveKeyA(int hKey, string lpFile, int lpSecurityAttributes);
        [DllImport("advapi32.dll")]
        public static extern int RegCloseKey(int hKey);
        [DllImport("advapi32.dll")]
        public static extern int RegFlushKey(int hKey);
        [DllImport("advapi32.dll")]
        public static extern int RegSaveKeyEx(IntPtr hKey, string lpFile, IntPtr lpSecurityAttributes, int Flags);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
        [DllImport("shell32.dll")]
        public static extern bool IsUserAnAdmin();

        // Shutdown reason codes
        public const uint MajorOperatingSystem = 0x00020000;
        public const uint MinorReconfig = 0x00000004;
        public const uint FlagPlanned = 0x80000000;

        public enum HKEY : uint
        {
            HKEY_CLASSES_ROOT = 0x80000000,
            HKEY_CURRENT_USER = 0x80000001,
            HKEY_LOCAL_MACHINE = 0x80000002,
            HKEY_USERS = 0x80000003,
            HKEY_PERFORMANCE_DATA = 0x80000004,
            HKEY_PERFORMANCE_TEXT = 0x80000050,
            HKEY_PERFORMANCE_NLSTEXT = 0x80000060,
            HKEY_CURRENT_CONFIG = 0x80000005,
        }

        private enum REGFORMAT : int
        {
            REG_STANDARD_FORMAT = 1,
            REG_LATEST_FORMAT = 2,
            REG_NO_COMPRESSION = 4,
        }
    }

    public class Hive : IDisposable
    {
        private readonly string strHiveName, strHivePath;

        private volatile string strRootKey, strKeyName;

        private volatile string strOldHivePath = Utils.GetTempHivePath();
        public string OldHivePath
        {
            get { return strOldHivePath; }
        }

        private volatile string strNewHivePath = Utils.GetTempHivePath();
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
                this.strHivePath = Utils.ConvertDeviceToMSDOSName(HivePath);

            try
            {
                this._fi = new FileInfo(this.strHivePath);
                this.lOldHiveSize = GetFileSize(this.strHivePath);
            }
            catch { System.Diagnostics.Debug.WriteLine("error opening registry hive"); }
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

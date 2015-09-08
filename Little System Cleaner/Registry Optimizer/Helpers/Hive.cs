using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Registry_Optimizer.Helpers
{
    public class Hive : IDisposable
    {
        private readonly string _strHiveName;

        /// <summary>
        /// The file location of the hive that is loaded with Windows
        /// </summary>
        /// <remarks>This file is locked and cannot be removed or changed</remarks>
        private readonly string _strHivePath;

        private string _strRootKey, _strKeyName;

        public bool SkipCompact
        {
            get;
            private set;
        }

        private string _strOldHivePath;

        /// <summary>
        /// Where a backup copy of the hive is saved when RegReplaceKey is called
        /// </summary>
        public string OldHivePath => _strOldHivePath;

        private string _strNewHivePath;

        /// <summary>
        /// The location of the compacted hive (created with RegSaveKey)
        /// </summary>
        public string NewHivePath => _strNewHivePath;

        private readonly FileInfo _fi;
        public FileInfo HiveFileInfo => _fi;

        private readonly long _lOldHiveSize;
        public long OldHiveSize => _lOldHiveSize;

        private uint _lNewHiveSize;
        public long NewHiveSize => _lNewHiveSize;

        #region ListView Properties
        public string RegistryHive => HiveFileInfo.Name;

        public string RegistryHivePath => _strHivePath;

        public string CurrentSize => Utils.ConvertSizeToString(_lOldHiveSize);

        public string CompactSize => Utils.ConvertSizeToString(_lNewHiveSize);

        public Image Image { get; private set; }
        #endregion

        public bool bAnaylzed, bCompacted;

        private int _hKey;
        private bool _disposed;

        public bool IsValid
        {
            get
            {
                if (HiveFileInfo == null)
                    return false;

                if (_lOldHiveSize == 0)
                    return false;

                return File.Exists(_strHivePath);
            }
        }

        /// <summary>
        /// Constructor for Hive class
        /// </summary>
        /// <param name="hiveName">Name of Hive (\REGISTRY\USER\...)</param>
        /// <param name="hivePath">Path to Hive (\Device\HarddiskVolumeX\Windows\System32\config\... or C:\Windows\System32\config\...)</param>
        /// <param name="image">Registry icon</param>
        public Hive(string hiveName, string hivePath, Image image)
        {
            _strHiveName = hiveName;
            Image = image;

            _strHivePath = File.Exists(hivePath) ? hivePath : HiveManager.ConvertDeviceToMsdosName(hivePath);

            try
            {
                _fi = new FileInfo(_strHivePath);
                _lOldHiveSize = GetFileSize(_strHivePath);
            }
            catch (Exception ex) 
            {
                _fi = null;
                _lOldHiveSize = 0;

                Debug.WriteLine("The following error occurred trying to get registry hive information: " + ex.Message); 
            }

            if (!IsValid)
                return;

            try
            {
                GetTempHivePaths();
            }
            catch (Exception ex)
            {
                _fi = null;
                _lOldHiveSize = 0;

                Debug.WriteLine("The following error occurred trying to get temporary hive path: " + ex.Message);
            }
        }

        /// <summary>
        /// Gets and sets temporary hive paths
        /// </summary>
        /// <see cref="HiveManager.GetTempHivePath"/>
        private void GetTempHivePaths()
        {
            // Temporary directory must be on same partition
            char drive = _strHivePath[0];

            _strOldHivePath = HiveManager.GetTempHivePath(drive);
            _strNewHivePath = HiveManager.GetTempHivePath(drive);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_hKey != 0)
                    PInvoke.RegCloseKey(_hKey);

                _hKey = 0;

                if (File.Exists(_strOldHivePath))
                    File.Delete(_strOldHivePath);

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            // Clear HKey
            if (_hKey != 0)
                PInvoke.RegCloseKey(_hKey);

            _hKey = 0;

            // Remove hives
            if (File.Exists(_strOldHivePath))
                File.Delete(_strOldHivePath);

            if (File.Exists(_strNewHivePath))
                File.Delete(_strNewHivePath);

            // Reset registry size
            _lNewHiveSize = 0;

            // Reset analyzed
            bAnaylzed = false;
        }

        private void OpenHKey()
        {
            int nRet = 0;

            if (string.IsNullOrEmpty(_strRootKey))
                _strRootKey = _strHiveName.ToLower();

            if (string.IsNullOrEmpty(_strKeyName))
                _strKeyName = _strRootKey.Substring(_strRootKey.LastIndexOf('\\') + 1);

            _hKey = 0;

            // Open Handle to registry key
            if (_strRootKey.StartsWith(@"\registry\machine"))
                nRet = PInvoke.RegOpenKeyA((uint)PInvoke.HKEY.HKEY_LOCAL_MACHINE, _strKeyName, ref _hKey);
            if (_strRootKey.StartsWith(@"\registry\user"))
                nRet = PInvoke.RegOpenKeyA((uint)PInvoke.HKEY.HKEY_USERS, _strKeyName, ref _hKey);

            if (nRet != 0)
                throw new Win32Exception(nRet);

            if (_hKey == 0)
                throw new Win32Exception(6); // ERROR_INVALID_HANDLE
        }

        /// <summary>
        /// Uses Windows RegSaveKeyA API to rewrite registry hive
        /// </summary>
        public void AnalyzeHive(Window window)
        {
            if (bAnaylzed)
                // Reset previous analyze info
                Reset();

            try
            {
                CanAnalyze();
            }
            catch (Exception ex)
            {
                // Don't compact hive
                SkipCompact = true;

                string message = $"Unable to analyze registry hive: {RegistryHive}\nThe following error occurred: {ex.Message}.\n\nPress 'Enter' to continue...";

                Utils.MessageBoxThreadSafe(window, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            try
            {
                PerformAnalyze();
            }
            catch (Win32Exception ex)
            {
                // Don't compact hive
                SkipCompact = true;

                string message = $"Unable to perform registry hive analyze on {RegistryHive}\nError code {ex.NativeErrorCode} was returned.\n\nPress 'Enter' to continue...";

                Utils.MessageBoxThreadSafe(window, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
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
                OpenHKey();
            }
            catch (Win32Exception ex)
            {
                _hKey = 0;

                throw new Exception($"A handle for the registry hive could not be opened (error code {ex.NativeErrorCode} was returned).");
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
            PInvoke.RegFlushKey(_hKey);

            // Function will fail if file already exists
            if (File.Exists(_strNewHivePath))
                File.Delete(_strNewHivePath);

            // Use API to rewrite the registry hive
            nRet = PInvoke.RegSaveKeyA(_hKey, _strNewHivePath, 0);
            if (nRet != 0)
                throw new Win32Exception(nRet);

            _lNewHiveSize = (uint)GetFileSize(_strNewHivePath);

            if (File.Exists(_strNewHivePath))
                bAnaylzed = true;

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
                CanCompact();
            }
            catch (Exception ex)
            {
                string message = $"Unable to compact registry hive: {RegistryHive}\nThe following error occurred: {ex.Message}\n\nPress 'Enter' to continue...";

                Utils.MessageBoxThreadSafe(window, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            try
            {
                PerformCompact();
            }
            catch (Win32Exception ex)
            {
                string message = $"Unable to perform registry hive compact on {RegistryHive}\nError code {ex.NativeErrorCode} was returned.\n\nPress 'Enter' to continue...";

                Utils.MessageBoxThreadSafe(window, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Determines if registry hive can be compacted
        /// </summary>
        /// <exception cref="System.Exception">This exception will be thrown if the registry cannot be compacted. Further information is included in the description of the exception.</exception>
        private void CanCompact()
        {
            if (!bAnaylzed)
            {
                throw new Exception("The registry hive must be analyzed before it can be compacted.");
            }
            if (_hKey == 0)
            {
                // Try to open handle again
                try
                {
                    OpenHKey();
                }
                catch (Win32Exception ex)
                {
                    _hKey = 0;

                    throw new Exception($"A handle for the registry hive could not be opened (error code {ex.NativeErrorCode} was returned).");
                }
            }
            else if (!File.Exists(_strNewHivePath))
            {
                throw new Exception($"The compacted version of the registry hive ({_strHiveName}) was not created or was deleted from {_strNewHivePath}.");
            }
            else if (bCompacted)
            {
                throw new Exception($"The registry hive ({_strHiveName}) has already been compacted.");
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
            if (File.Exists(_strOldHivePath))
                File.Delete(_strOldHivePath);

            // Replace hive with compressed hive
            int ret = PInvoke.RegReplaceKeyA(_hKey, null, _strNewHivePath, _strOldHivePath);
            if (ret != 0)
                throw new Win32Exception(ret);

            // Hive should now be replaced with temporary hive
            PInvoke.RegCloseKey(_hKey);

            // End Critical Region
            Thread.EndCriticalRegion();

            bCompacted = true;
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

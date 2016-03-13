using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Registry_Optimizer.Helpers
{
    public class Hive : IDisposable
    {
        private readonly string _hiveName;
        private bool _disposed;

        private int _hKey;

        private uint _newHiveSize;

        private string _rootKey, _keyName;

        public bool Anaylzed, Compacted;

        /// <summary>
        ///     Constructor for Hive class
        /// </summary>
        /// <param name="hiveName">Name of Hive (\REGISTRY\USER\...)</param>
        /// <param name="hivePath">
        ///     Path to Hive (\Device\HarddiskVolumeX\Windows\System32\config\... or
        ///     C:\Windows\System32\config\...)
        /// </param>
        public Hive(string hiveName, string hivePath)
        {
            _hiveName = hiveName;
            RegistryHivePath = File.Exists(hivePath) ? hivePath : HiveManager.ConvertDeviceToMsdosName(hivePath);

            try
            {
                HiveFileInfo = new FileInfo(RegistryHivePath);
                OldHiveSize = GetFileSize(RegistryHivePath);
            }
            catch (Exception ex)
            {
                HiveFileInfo = null;
                OldHiveSize = 0;

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
                HiveFileInfo = null;
                OldHiveSize = 0;

                Debug.WriteLine("The following error occurred trying to get temporary hive path: " + ex.Message);
            }
        }

        public bool SkipCompact { get; private set; }

        /// <summary>
        ///     Where a backup copy of the hive is saved when RegReplaceKey is called
        /// </summary>
        public string OldHivePath { get; private set; }

        /// <summary>
        ///     The location of the compacted hive (created with RegSaveKey)
        /// </summary>
        public string NewHivePath { get; private set; }

        public FileInfo HiveFileInfo { get; }

        public long OldHiveSize { get; }
        public long NewHiveSize => _newHiveSize;

        public bool IsValid
        {
            get
            {
                if (HiveFileInfo == null)
                    return false;

                return OldHiveSize != 0 && File.Exists(RegistryHivePath);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_hKey != 0)
                    PInvoke.RegCloseKey(_hKey);

                _hKey = 0;

                if (File.Exists(OldHivePath))
                    File.Delete(OldHivePath);

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets and sets temporary hive paths
        /// </summary>
        /// <see cref="HiveManager.GetTempHivePath" />
        private void GetTempHivePaths()
        {
            // Temporary directory must be on same partition
            var drive = RegistryHivePath[0];

            OldHivePath = HiveManager.GetTempHivePath(drive);
            NewHivePath = HiveManager.GetTempHivePath(drive);
        }

        public void Reset()
        {
            // Clear HKey
            if (_hKey != 0)
                PInvoke.RegCloseKey(_hKey);

            _hKey = 0;

            // Remove hives
            if (File.Exists(OldHivePath))
                File.Delete(OldHivePath);

            if (File.Exists(NewHivePath))
                File.Delete(NewHivePath);

            // Reset registry size
            _newHiveSize = 0;

            // Reset analyzed
            Anaylzed = false;
        }

        private void OpenHKey()
        {
            var ret = 0;

            if (string.IsNullOrEmpty(_rootKey))
                _rootKey = _hiveName.ToLower();

            if (string.IsNullOrEmpty(_keyName))
                _keyName = _rootKey.Substring(_rootKey.LastIndexOf('\\') + 1);

            _hKey = 0;

            // Open Handle to registry key
            if (_rootKey.StartsWith(@"\registry\machine"))
                ret = PInvoke.RegOpenKeyA((uint) PInvoke.HKEY.HKEY_LOCAL_MACHINE, _keyName, ref _hKey);
            if (_rootKey.StartsWith(@"\registry\user"))
                ret = PInvoke.RegOpenKeyA((uint) PInvoke.HKEY.HKEY_USERS, _keyName, ref _hKey);

            if (ret != 0)
                throw new Win32Exception(ret);

            if (_hKey == 0)
                throw new Win32Exception(6); // ERROR_INVALID_HANDLE
        }

        /// <summary>
        ///     Uses Windows RegSaveKeyA API to rewrite registry hive
        /// </summary>
        public void AnalyzeHive(Window window)
        {
            if (Anaylzed)
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

                string message =
                    $"Unable to analyze registry hive: {RegistryHive}\nThe following error occurred: {ex.Message}.\n\nPress 'Enter' to continue...";

                Utils.MessageBoxThreadSafe(window, message, Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);

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

                string message =
                    $"Unable to perform registry hive analyze on {RegistryHive}\nError code {ex.NativeErrorCode} was returned.\n\nPress 'Enter' to continue...";

                Utils.MessageBoxThreadSafe(window, message, Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Checks if registry hive can be analyzed
        /// </summary>
        /// <exception cref="System.Exception">
        ///     This exception will be thrown if the registry cannot be analyzed. Further
        ///     information is included in the description of the exception.
        /// </exception>
        private void CanAnalyze()
        {
            try
            {
                OpenHKey();
            }
            catch (Win32Exception ex)
            {
                _hKey = 0;

                throw new Exception(
                    $"A handle for the registry hive could not be opened (error code {ex.NativeErrorCode} was returned).");
            }
        }

        /// <summary>
        ///     Analyzes the registry hive
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">This exception will be thrown if RegSaveKey fails</exception>
        private void PerformAnalyze()
        {
            // Begin Critical Region
            Thread.BeginCriticalRegion();

            // Flush hive key
            PInvoke.RegFlushKey(_hKey);

            // Function will fail if file already exists
            if (File.Exists(NewHivePath))
                File.Delete(NewHivePath);

            // Use API to rewrite the registry hive
            var retCode = PInvoke.RegSaveKeyA(_hKey, NewHivePath, 0);
            if (retCode != 0)
                throw new Win32Exception(retCode);

            _newHiveSize = (uint) GetFileSize(NewHivePath);

            if (File.Exists(NewHivePath))
                Anaylzed = true;

            // End Critical Region
            Thread.EndCriticalRegion();
        }

        /// <summary>
        ///     Compacts the registry hive
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
                string message =
                    $"Unable to compact registry hive: {RegistryHive}\nThe following error occurred: {ex.Message}\n\nPress 'Enter' to continue...";

                Utils.MessageBoxThreadSafe(window, message, Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            try
            {
                PerformCompact();
            }
            catch (Win32Exception ex)
            {
                string message =
                    $"Unable to perform registry hive compact on {RegistryHive}\nError code {ex.NativeErrorCode} was returned.\n\nPress 'Enter' to continue...";

                Utils.MessageBoxThreadSafe(window, message, Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Determines if registry hive can be compacted
        /// </summary>
        /// <exception cref="System.Exception">
        ///     This exception will be thrown if the registry cannot be compacted. Further
        ///     information is included in the description of the exception.
        /// </exception>
        private void CanCompact()
        {
            if (!Anaylzed)
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

                    throw new Exception(
                        $"A handle for the registry hive could not be opened (error code {ex.NativeErrorCode} was returned).");
                }
            }
            else if (!File.Exists(NewHivePath))
            {
                throw new Exception(
                    $"The compacted version of the registry hive ({_hiveName}) was not created or was deleted from {NewHivePath}.");
            }
            else if (Compacted)
            {
                throw new Exception($"The registry hive ({_hiveName}) has already been compacted.");
            }
        }

        /// <summary>
        ///     Compacts the registry hive
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">This exception will be thrown if RegReplaceKey fails</exception>
        private void PerformCompact()
        {
            // Begin Critical Region
            Thread.BeginCriticalRegion();

            // Old hive cant exist or function will fail
            if (File.Exists(OldHivePath))
                File.Delete(OldHivePath);

            // Replace hive with compressed hive
            var ret = PInvoke.RegReplaceKeyA(_hKey, null, NewHivePath, OldHivePath);
            if (ret != 0)
                throw new Win32Exception(ret);

            // Hive should now be replaced with temporary hive
            PInvoke.RegCloseKey(_hKey);

            // End Critical Region
            Thread.EndCriticalRegion();

            Compacted = true;
        }

        /// <summary>
        ///     Gets the file size
        /// </summary>
        /// <param name="filePath">Path to the filename</param>
        /// <returns>File Size</returns>
        private long GetFileSize(string filePath)
        {
            try
            {
                var fi = new FileInfo(filePath);

                return fi.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        ///     Returns the filename of the hive
        /// </summary>
        /// <returns>Hive filename</returns>
        public override string ToString()
        {
            return (string) HiveFileInfo.Name.Clone();
        }

        #region ListView Properties

        public string RegistryHive => HiveFileInfo.Name;

        /// <summary>
        ///     The file location of the hive that is loaded with Windows
        /// </summary>
        /// <remarks>This file is locked and cannot be removed or changed</remarks>
        public string RegistryHivePath { get; }

        public string CurrentSize => Utils.ConvertSizeToString(OldHiveSize);

        public string CompactSize => Utils.ConvertSizeToString(_newHiveSize);

        #endregion
    }
}
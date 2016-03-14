using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Microsoft.Win32;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Little_System_Cleaner.Uninstall_Manager.Helpers
{
    public class ProgramInfo : IComparable<ProgramInfo>
    {
        public ProgramInfo(RegistryKey regKey)
        {
            Key = regKey.Name.Substring(regKey.Name.LastIndexOf('\\') + 1);

            DisplayName = Convert.ToString(TryGetValue(regKey, "DisplayName", ""));
            QuietDisplayName = Convert.ToString(TryGetValue(regKey, "QuietDisplayName", ""));
            UninstallString = Convert.ToString(TryGetValue(regKey, "UninstallString", ""));
            QuietUninstallString = Convert.ToString(TryGetValue(regKey, "QuietUninstallString", ""));
            Publisher = Convert.ToString(TryGetValue(regKey, "Publisher", ""));
            DisplayVersion = Convert.ToString(TryGetValue(regKey, "DisplayVersion", ""));
            HelpLink = Convert.ToString(TryGetValue(regKey, "HelpLink", ""));
            UrlInfoAbout = Convert.ToString(TryGetValue(regKey, "URLInfoAbout", ""));
            HelpTelephone = Convert.ToString(TryGetValue(regKey, "HelpTelephone", ""));
            Contact = Convert.ToString(TryGetValue(regKey, "Contact", ""));
            Readme = Convert.ToString(TryGetValue(regKey, "Readme", ""));
            Comments = Convert.ToString(TryGetValue(regKey, "Comments", ""));
            DisplayIcon = Convert.ToString(TryGetValue(regKey, "DisplayIcon", ""));
            ParentKeyName = Convert.ToString(TryGetValue(regKey, "ParentKeyName", ""));
            InstallLocation = Convert.ToString(TryGetValue(regKey, "InstallLocation", ""));
            InstallSource = Convert.ToString(TryGetValue(regKey, "InstallSource", ""));

            NoModify = ConvertToNullableInt32(TryGetValue(regKey, "NoModify"));
            NoRepair = ConvertToNullableInt32(TryGetValue(regKey, "NoRepair"));

            SystemComponent = ConvertToNullableInt32(TryGetValue(regKey, "SystemComponent", 0)).GetValueOrDefault() == 1;
            _windowsInstaller = ConvertToNullableInt32(TryGetValue(regKey, "WindowsInstaller", 0));
            EstimatedSize = ConvertToNullableInt32(TryGetValue(regKey, "EstimatedSize", 0));

            GetArpCache();
        }

        private static int? ConvertToNullableInt32(object o)
        {
            if (o == null)
                return null;

            int? ret;

            try
            {
                ret = Convert.ToInt32(o);
            }
            catch (Exception ex)
            {
                ret = null;

                if (ex is OverflowException)
                    Debug.WriteLine("The {0} value {1} is outside the range of the Int32 type.", o.GetType().Name, o);
                else if (ex is FormatException)
                    Debug.WriteLine("The {0} value {1} is not in a recognizable format.", o.GetType().Name, o);
                else if (ex is InvalidCastException)
                    Debug.WriteLine("No conversion to an Int32 exists for the {0} value {1}.", o.GetType().Name, o);
                else
                    Debug.WriteLine("The following exception occurred trying to convert {0} with value {1}:\n{2}",
                        o.GetType().Name, o, ex.Message);
            }

            return ret;
        }

        /// <summary>
        ///     Gets cached information
        ///     Please note the ARP (Add/Remove Programs) cache is from Windows XP (which is no longer supported)
        /// </summary>
        private void GetArpCache()
        {
            try
            {
                RegistryKey regKey;

                var regKeyName = !string.IsNullOrEmpty(ParentKeyName) ? ParentKeyName : Key;

                if (
                    (regKey =
                        Registry.CurrentUser.OpenSubKey(
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management\ARPCache\" + regKeyName)) == null)
                    if (
                        (regKey =
                            Registry.LocalMachine.OpenSubKey(
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management\ARPCache\" + regKeyName)) ==
                        null)
                        return;

                var b = (byte[]) regKey.GetValue("SlowInfoCache");

                var gcHandle = GCHandle.Alloc(b, GCHandleType.Pinned);
                var ptr = gcHandle.AddrOfPinnedObject();
                var slowInfoCache = (SlowInfoCache) Marshal.PtrToStructure(ptr, typeof (SlowInfoCache));

                SlowCache = true;
                SlowInfoCacheRegKey = regKey.ToString();

                InstallSize = slowInfoCache.InstallSize;
                Frequency = slowInfoCache.Frequency;
                LastUsed = FileTime2DateTime(slowInfoCache.LastUsed);
                if (slowInfoCache.HasName == 1)
                    FileName = slowInfoCache.Name;

                if (gcHandle.IsAllocated)
                    gcHandle.Free();

                regKey.Close();
            }
            catch
            {
                SlowCache = false;
                InstallSize = 0;
                Frequency = 0;
                LastUsed = DateTime.MinValue;
                FileName = "";
            }
        }

        /// <summary>
        ///     Removes ARP Cache registry key (if it exists)
        ///     Please note the ARP (Add/Remove Programs) cache is from Windows XP (which is no longer supported)
        /// </summary>
        /// <returns>True if the ARP cache registry key was removed, otherwise, false</returns>
        private bool RemoveArpCache()
        {
            var ret = false;
            string baseKey, subKey;

            Utils.ParseRegKeyPath(SlowInfoCacheRegKey, out baseKey, out subKey);

            var regKey = Utils.RegOpenKey(baseKey, false);

            if (regKey != null)
            {
                regKey.DeleteSubKeyTree(subKey);
                regKey.Flush();

                ret = true;
            }

            regKey?.Close();

            return ret;
        }

        public bool Uninstall()
        {
            var cmdLine = "";

            if (!string.IsNullOrEmpty(UninstallString))
                cmdLine = UninstallString;
            else if (!string.IsNullOrEmpty(QuietUninstallString))
                cmdLine = QuietUninstallString;

            if (string.IsNullOrEmpty(cmdLine))
            {
                if (
                    MessageBox.Show(
                        "Unable to find uninstall string. Would you like to manually remove it from the registry?",
                        Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    RemoveFromRegistry();

                return false;
            }

            if (WindowsInstaller)
            {
                // Remove 'msiexec' from uninstall string
                var cmdArgs = cmdLine.Substring(cmdLine.IndexOf(' ') + 1);

                try
                {
                    var proc = Process.Start("msiexec.exe", cmdArgs);
                    proc?.WaitForExit();

                    if (proc != null && proc.ExitCode != 0)
                    {
                        MessageBox.Show(Application.Current.MainWindow,
                            "It appears the program couldn't be uninstalled or the uninstall was aborted by the user.",
                            Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is FileNotFoundException)
                    {
                        var message =
                            $"The Windows Installer tool (msiexec.exe) could not be found. Please ensure that it's located in either {Environment.GetFolderPath(Environment.SpecialFolder.Windows)} or {Environment.SystemDirectory} and also ensure that the PATH variable is properly set to include these directories.";
                        MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    else if (ex is Win32Exception)
                    {
                        var hr = Marshal.GetHRForException(ex);
                        if (hr == unchecked((int) 0x80004002))
                        {
                            MessageBox.Show(Application.Current.MainWindow,
                                "The following error occurred: " + ex.Message +
                                "\nThis can be caused by problems with permissions and the Windows Registry.",
                                Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show(Application.Current.MainWindow,
                                "The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(Application.Current.MainWindow, "The following error occurred: " + ex.Message,
                            Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return false;
                }
            }
            else
            {
                // Execute uninstall string
                try
                {
                    var proc = Process.Start(cmdLine);
                    if (proc != null)
                    {
                        proc.WaitForExit();

                        if (proc.ExitCode != 0)
                        {
                            MessageBox.Show(Application.Current.MainWindow,
                                "It appears the program couldn't be uninstalled or the uninstall was aborted by the user.",
                                Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                    else
                    {
                        throw new NullReferenceException();
                    }
                }
                catch (Exception ex)
                {
                    if (ex is FileNotFoundException)
                    {
                        string message = $"The file could not be found from the command: {cmdLine}";
                        MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    else if (ex is Win32Exception)
                    {
                        var hr = Marshal.GetHRForException(ex);
                        if (hr == unchecked((int) 0x80004002))
                        {
                            MessageBox.Show(Application.Current.MainWindow,
                                "The following error occurred: " + ex.Message +
                                "\nThis can be caused by problems with permissions and the Windows Registry.",
                                Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show(Application.Current.MainWindow,
                                "The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(Application.Current.MainWindow, "The following error occurred: " + ex.Message,
                            Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return false;
                }
            }

            if (SlowCache)
            {
                if (!RemoveArpCache())
                    MessageBox.Show(Application.Current.MainWindow,
                        "The Add/Remove Programs (ARP) cache registry key could not be removed", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }


            MessageBox.Show("Successfully uninstalled the program", Utils.ProductName, MessageBoxButton.OK,
                MessageBoxImage.Information);

            return true;
        }

        public bool RemoveFromRegistry()
        {
            var keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + Key;

            try
            {
                if (Registry.LocalMachine.OpenSubKey(keyName, true) != null)
                    Registry.LocalMachine.DeleteSubKeyTree(keyName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing registry key: {ex.Message}", Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            if (SlowCache)
            {
                if (!RemoveArpCache())
                    MessageBox.Show(Application.Current.MainWindow,
                        "The Add/Remove Programs (ARP) cache registry key could not be removed", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }

            MessageBox.Show("Successfully removed registry key", Utils.ProductName, MessageBoxButton.OK,
                MessageBoxImage.Information);

            return true;
        }

        /// <summary>
        ///     Converts FILETIME structure to DateTime structure
        /// </summary>
        /// <param name="ft">FILETIME structure</param>
        /// <returns>DateTime structure</returns>
        private static DateTime FileTime2DateTime(FILETIME ft)
        {
            DateTime dt;
            var hFT2 = ((long) ft.dwHighDateTime << 32) + ft.dwLowDateTime;

            try
            {
                dt = DateTime.FromFileTimeUtc(hFT2);
            }
            catch (ArgumentOutOfRangeException)
            {
                dt = DateTime.MaxValue;
            }

            return dt;
        }

        private static object TryGetValue(RegistryKey regKey, string valueName, object defaultValue = null)
        {
            var value = defaultValue;

            try
            {
                value = regKey.GetValue(valueName);

                if (value == null && defaultValue != null)
                    value = defaultValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to get registry value for " +
                                valueName + " in " + regKey);
            }

            return value;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        #region Slow Info Cache properties

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 552)]
        internal struct SlowInfoCache
        {
            public uint cbSize; // size of the SlowInfoCache (552 bytes)
            public uint HasName; // unknown
            public long InstallSize; // program size in bytes
            public FILETIME LastUsed; // last time program was used
            public uint Frequency; // 0-2 = rarely; 3-9 = occassionaly; 10+ = frequently

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 262)] public string Name;
                //remaining 524 bytes (max path of 260 + null) in unicode
        }

        public bool SlowCache;
        public long InstallSize;
        public uint Frequency;
        public DateTime LastUsed;
        public string FileName;
        public string SlowInfoCacheRegKey;

        #endregion

        #region Program Info

        public readonly string Key;
        public readonly string DisplayName;
        public readonly string UninstallString;
        public readonly string QuietDisplayName;
        public readonly string QuietUninstallString;
        public readonly string DisplayVersion;
        public readonly string Publisher;
        public readonly string UrlInfoAbout;
        public readonly string UrlUpdateInfo;
        public readonly string HelpLink;
        public readonly string HelpTelephone;
        public readonly string Contact;
        public readonly string Comments;
        public readonly string Readme;
        public readonly string DisplayIcon;
        public readonly string ParentKeyName;
        public readonly string InstallLocation;
        public readonly string InstallSource;

        public readonly int? NoModify;
        public readonly int? NoRepair;

        public readonly int? EstimatedSize;
        public readonly bool SystemComponent;
        private readonly int? _windowsInstaller;

        public bool WindowsInstaller
        {
            get
            {
                if (_windowsInstaller == 1)
                    return true;

                if (!string.IsNullOrEmpty(UninstallString))
                    if (UninstallString.Contains("msiexec"))
                        return true;

                if (!string.IsNullOrEmpty(QuietUninstallString))
                    if (QuietUninstallString.Contains("msiexec"))
                        return true;

                return false;
            }
        }

        public bool Uninstallable
            => !string.IsNullOrEmpty(UninstallString) || !string.IsNullOrEmpty(QuietUninstallString);

        #endregion

        #region ListView Properties

        public Image BitmapImg
            =>
                Uninstallable
                    ? Resources.uninstall.CreateBitmapSourceFromBitmap()
                    : Resources.cancel.CreateBitmapSourceFromBitmap();

        public string Program
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayName))
                    return DisplayName;

                return !string.IsNullOrEmpty(QuietDisplayName) ? QuietDisplayName : Key;
            }
        }

        public string Size => SizeBytes > 0 ? Utils.ConvertSizeToString(SizeBytes) : string.Empty;

        public long SizeBytes
        {
            get
            {
                if (InstallSize > 0)
                    return (uint) InstallSize;

                if (EstimatedSize.GetValueOrDefault(0) <= 0)
                    return 0;

                if (EstimatedSize != null)
                    return EstimatedSize.Value*1024;

                return 0;
            }
        }

        #endregion

        #region IComparable members

        public int CompareTo(ProgramInfo other)
        {
            return string.Compare(DisplayName, other?.DisplayName, StringComparison.Ordinal);
        }

        public bool Equals(ProgramInfo other)
        {
            return other.Key == Key;
        }

        #endregion
    }
}
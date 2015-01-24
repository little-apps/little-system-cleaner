using Little_System_Cleaner.Misc;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace Little_System_Cleaner.Uninstall_Manager.Helpers
{
    public class ProgramInfo : IComparable<ProgramInfo>
    {
        #region Slow Info Cache properties

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 552)]
        internal struct SlowInfoCache
        {

            public uint cbSize; // size of the SlowInfoCache (552 bytes)
            public uint HasName; // unknown
            public Int64 InstallSize; // program size in bytes
            public System.Runtime.InteropServices.ComTypes.FILETIME LastUsed; // last time program was used
            public uint Frequency; // 0-2 = rarely; 3-9 = occassionaly; 10+ = frequently
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 262)]
            public string Name; //remaining 524 bytes (max path of 260 + null) in unicode
        }

        public bool SlowCache;
        public Int64 InstallSize;
        public uint Frequency;
        public DateTime LastUsed;
        public string FileName;
        public string SlowInfoCacheRegKey;
        #endregion

        #region Program Info
        public readonly string Key;
        public readonly string _displayName;
        public readonly string _uninstallString;
        public readonly string _quietDisplayName;
        public readonly string _quietUninstallString;
        public readonly string _displayVersion;
        public readonly string _publisher;
        public readonly string _urlInfoAbout;
        public readonly string _urlUpdateInfo;
        public readonly string _helpLink;
        public readonly string _helpTelephone;
        public readonly string _contact;
        public readonly string _comments;
        public readonly string _readme;
        public readonly string _displayIcon;
        public readonly string _parentKeyName;
        public readonly string _installLocation;
        public readonly string _installSource;

        public readonly int? _noModify;
        public readonly int? _noRepair;

        public readonly int? _estimatedSize;
        public readonly bool _systemComponent;
        private readonly int? _windowsInstaller;

        public bool WindowsInstaller
        {
            get
            {
                if (_windowsInstaller == 1)
                    return true;

                if (!string.IsNullOrEmpty(_uninstallString))
                    if (_uninstallString.Contains("msiexec"))
                        return true;

                if (!string.IsNullOrEmpty(_quietUninstallString))
                    if (_quietUninstallString.Contains("msiexec"))
                        return true;

                return false;
            }
        }

        public bool Uninstallable
        {
            get { return ((!string.IsNullOrEmpty(_uninstallString)) || (!string.IsNullOrEmpty(_quietUninstallString))); }
        }
        #endregion

        #region ListView Properties
        public System.Windows.Controls.Image bMapImg
        {
            get
            {
                if (this.Uninstallable)
                    return Utils.CreateBitmapSourceFromBitmap(Properties.Resources.uninstall);
                else
                    return Utils.CreateBitmapSourceFromBitmap(Properties.Resources.cancel);
            }
        }

        public string Program
        {
            get
            {
                if (!string.IsNullOrEmpty(this._displayName))
                    return this._displayName;
                else if (!string.IsNullOrEmpty(this._quietDisplayName))
                    return this._quietDisplayName;
                else
                    return this.Key;
            }
        }

        public string Publisher
        {
            get
            {
                return _publisher;
            }
        }

        public string Size
        {
            get
            {
                if (this.InstallSize > 0)
                    return Utils.ConvertSizeToString((uint)this.InstallSize);
                else if (this._estimatedSize.GetValueOrDefault(0) > 0)
                    return Utils.ConvertSizeToString(this._estimatedSize.Value * 1024);
                else
                    return string.Empty;
            }
        }
        #endregion

        public ProgramInfo(RegistryKey regKey)
        {
            Key = regKey.Name.Substring(regKey.Name.LastIndexOf('\\') + 1);

            _displayName = Convert.ToString(this.TryGetValue(regKey, "DisplayName", ""));
            _quietDisplayName = Convert.ToString(this.TryGetValue(regKey, "QuietDisplayName", ""));
            _uninstallString = Convert.ToString(this.TryGetValue(regKey, "UninstallString", ""));
            _quietUninstallString = Convert.ToString(this.TryGetValue(regKey, "QuietUninstallString", ""));
            _publisher = Convert.ToString(this.TryGetValue(regKey, "Publisher", ""));
            _displayVersion = Convert.ToString(this.TryGetValue(regKey, "DisplayVersion", ""));
            _helpLink = Convert.ToString(this.TryGetValue(regKey, "HelpLink", ""));
            _urlInfoAbout = Convert.ToString(this.TryGetValue(regKey, "URLInfoAbout", ""));
            _helpTelephone = Convert.ToString(this.TryGetValue(regKey, "HelpTelephone", ""));
            _contact = Convert.ToString(this.TryGetValue(regKey, "Contact", ""));
            _readme = Convert.ToString(this.TryGetValue(regKey, "Readme", ""));
            _comments = Convert.ToString(this.TryGetValue(regKey, "Comments", ""));
            _displayIcon = Convert.ToString(this.TryGetValue(regKey, "DisplayIcon", ""));
            _parentKeyName = Convert.ToString(this.TryGetValue(regKey, "ParentKeyName", ""));
            _installLocation = Convert.ToString(this.TryGetValue(regKey, "InstallLocation", ""));
            _installSource = Convert.ToString(this.TryGetValue(regKey, "InstallSource", ""));

            _noModify = this.ConvertToNullableInt32(this.TryGetValue(regKey, "NoModify"));
            _noRepair = this.ConvertToNullableInt32(this.TryGetValue(regKey, "NoRepair"));

            _systemComponent = ((this.ConvertToNullableInt32(this.TryGetValue(regKey, "SystemComponent", 0)).GetValueOrDefault() == 1) ? (true) : (false));
            _windowsInstaller = this.ConvertToNullableInt32(this.TryGetValue(regKey, "WindowsInstaller", 0));
            _estimatedSize = this.ConvertToNullableInt32(this.TryGetValue(regKey, "EstimatedSize", 0));

            return;
        }

        private Nullable<int> ConvertToNullableInt32(object o)
        {
            if (o == null)
                return null;

            int? ret = null;

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
                    Debug.WriteLine("The following exception occurred trying to convert {0} with value {1}:\n{2}", o.GetType().Name, o, ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// Gets cached information
        /// </summary>
        private void GetARPCache()
        {
            RegistryKey regKey = null;

            try
            {
                if ((regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management\ARPCache\" + _parentKeyName)) == null)
                    if ((regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Management\ARPCache\" + _parentKeyName)) == null)
                        return;

                byte[] b = (byte[])regKey.GetValue("SlowInfoCache");

                GCHandle gcHandle = GCHandle.Alloc(b, GCHandleType.Pinned);
                IntPtr ptr = gcHandle.AddrOfPinnedObject();
                SlowInfoCache slowInfoCache = (SlowInfoCache)Marshal.PtrToStructure(ptr, typeof(SlowInfoCache));

                this.SlowCache = true;
                this.SlowInfoCacheRegKey = regKey.ToString();

                this.InstallSize = slowInfoCache.InstallSize;
                this.Frequency = slowInfoCache.Frequency;
                this.LastUsed = this.FileTime2DateTime(slowInfoCache.LastUsed);
                if (slowInfoCache.HasName == 1)
                    this.FileName = slowInfoCache.Name;

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

            return;
        }

        public bool Uninstall()
        {
            string cmdLine = "";

            if (!string.IsNullOrEmpty(_uninstallString))
                cmdLine = this._uninstallString;
            else if (!string.IsNullOrEmpty(_quietUninstallString))
                cmdLine = this._quietUninstallString;

            if (string.IsNullOrEmpty(cmdLine))
            {
                if (MessageBox.Show("Unable to find uninstall string. Would you like to manually remove it from the registry?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    this.RemoveFromRegistry();

                return false;
            }

            if (WindowsInstaller)
            {
                // Remove 'msiexec' from uninstall string
                string cmdArgs = cmdLine.Substring(cmdLine.IndexOf(' ') + 1);

                try
                {
                    Process proc = Process.Start("msiexec.exe", cmdArgs);
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        MessageBox.Show(App.Current.MainWindow, "It appears the program couldn't be uninstalled or the uninstall was aborted by the user.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is FileNotFoundException)
                    {
                        string message = string.Format("The Windows Installer tool (msiexec.exe) could not be found. Please ensure that it's located in either {0} or {1} and also ensure that the PATH variable is properly set to include these directories.", Environment.GetFolderPath(Environment.SpecialFolder.Windows), Environment.SystemDirectory);
                        MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (ex is Win32Exception)
                    {
                        int hr = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                        if (hr == unchecked((int)0x80004002))
                        {
                            MessageBox.Show(App.Current.MainWindow, "The following error occurred: " + ex.Message + "\nThis can be caused by problems with permissions and the Windows Registry.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show(App.Current.MainWindow, "The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(App.Current.MainWindow, "The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return false;
                }
            }
            else
            {
                // Execute uninstall string
                try
                {
                    Process proc = Process.Start(cmdLine);
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        MessageBox.Show(App.Current.MainWindow, "It appears the program couldn't be uninstalled or the uninstall was aborted by the user.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is FileNotFoundException)
                    {
                        string message = string.Format("The file could not be found from the command: {0}", cmdLine);
                        MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (ex is Win32Exception)
                    {
                        int hr = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                        if (hr == unchecked((int)0x80004002))
                        {
                            MessageBox.Show(App.Current.MainWindow, "The following error occurred: " + ex.Message + "\nThis can be caused by problems with permissions and the Windows Registry.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show(App.Current.MainWindow, "The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(App.Current.MainWindow, "The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    return false;
                }
                    
            }

            MessageBox.Show("Successfully uninstalled the program", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        public bool RemoveFromRegistry()
        {
            string strKeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + Key;

            try
            {
                if (Registry.LocalMachine.OpenSubKey(strKeyName, true) != null)
                    Registry.LocalMachine.DeleteSubKeyTree(strKeyName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error removing registry key: {0}", ex.Message), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }

            MessageBox.Show("Successfully removed registry key", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        /// <summary>
        /// Converts FILETIME structure to DateTime structure
        /// </summary>
        /// <param name="ft">FILETIME structure</param>
        /// <returns>DateTime structure</returns>
        private DateTime FileTime2DateTime(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            DateTime dt = DateTime.MaxValue;
            long hFT2 = (((long)ft.dwHighDateTime) << 32) + ft.dwLowDateTime;

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

        private object TryGetValue(RegistryKey regKey, string valueName, object defaultValue = null)
        {
            object value = defaultValue;

            try
            {
                value = regKey.GetValue(valueName);

                if (value == null && defaultValue != null)
                    value = defaultValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to get registry value for " + valueName + " in " + regKey.ToString());
            }

            return value;
        }

        public override string ToString()
        {
            return _displayName;
        }

        #region IComparable members
        public int CompareTo(ProgramInfo other)
        {
            return (_displayName == null) ? 0 : _displayName.CompareTo(other._displayName);
        }

        public bool Equals(ProgramInfo other)
        {
            return (other.Key == Key);
        }
        #endregion
    }
}

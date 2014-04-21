using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public readonly int _noModify;
        public readonly int _noRepair;

        public readonly int _estimatedSize;
        public readonly bool _systemComponent;
        private readonly int _windowsInstaller;

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
                else if (this._estimatedSize > 0)
                    return Utils.ConvertSizeToString(this._estimatedSize * 1024);
                else
                    return string.Empty;
            }
        }
        #endregion

        public ProgramInfo(RegistryKey regKey)
        {
            Key = regKey.Name.Substring(regKey.Name.LastIndexOf('\\') + 1);

            try
            {
                _displayName = regKey.GetValue("DisplayName") as string;
                _quietDisplayName = regKey.GetValue("QuietDisplayName") as string;
                _uninstallString = regKey.GetValue("UninstallString") as string;
                _quietUninstallString = regKey.GetValue("QuietUninstallString") as string;
                _publisher = regKey.GetValue("Publisher") as string;
                _displayVersion = regKey.GetValue("DisplayVersion") as string;
                _helpLink = regKey.GetValue("HelpLink") as string;
                _urlInfoAbout = regKey.GetValue("URLInfoAbout") as string;
                _helpTelephone = regKey.GetValue("HelpTelephone") as string;
                _contact = regKey.GetValue("Contact") as string;
                _readme = regKey.GetValue("Readme") as string;
                _comments = regKey.GetValue("Comments") as string;
                _displayIcon = regKey.GetValue("DisplayIcon") as string;
                _parentKeyName = regKey.GetValue("ParentKeyName") as string;
                _installLocation = regKey.GetValue("InstallLocation") as string;
                _installSource = regKey.GetValue("InstallSource") as string;

                _noModify = (Int32)regKey.GetValue("NoModify", 0);
                _noRepair = (Int32)regKey.GetValue("NoRepair", 0);

                _systemComponent = (((Int32)regKey.GetValue("SystemComponent", 0) == 1) ? (true) : (false));
                _windowsInstaller = (Int32)regKey.GetValue("WindowsInstaller", 0);
                _estimatedSize = (Int32)regKey.GetValue("EstimatedSize", 0);
            }
            catch (Exception)
            {
                _systemComponent = false;
                _estimatedSize = 0;
            }

            return;
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
                this.LastUsed = Utils.FileTime2DateTime(slowInfoCache.LastUsed);
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

            try
            {
                if (WindowsInstaller)
                {
                    // Remove 'msiexec' from uninstall string
                    string cmdArgs = cmdLine.Substring(cmdLine.IndexOf(' ') + 1);

                    Process proc = Process.Start("msiexec.exe", cmdArgs);
                    proc.WaitForExit();
                }
                else
                {
                    // Execute uninstall string
                    Process proc = Process.Start(cmdLine);
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error uninstalling program: {0}", ex.Message), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }

            MessageBox.Show("Sucessfully uninstalled program", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

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

            MessageBox.Show("Sucessfully removed registry key", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
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

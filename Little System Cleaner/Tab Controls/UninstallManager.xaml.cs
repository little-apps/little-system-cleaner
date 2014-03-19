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
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.ComponentModel;

namespace Little_System_Cleaner.Controls
{
	public partial class UninstallManager
	{
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

		public UninstallManager()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
            PopulateListView();

            // Manually sort listview
            Sort((this.listViewProgs.View as GridView).Columns[0].Header as string, _lastDirection);
            _lastDirection = ((_lastDirection == ListSortDirection.Ascending) ? (ListSortDirection.Descending) : (ListSortDirection.Ascending));
		}

        private void PopulateListView()
        {
            List<ProgramInfo> listProgInfo = new List<ProgramInfo>();

            // Clear listview
            this.listViewProgs.Items.Clear();

            // Turn textbox into regex pattern
            Regex regex = new Regex("", RegexOptions.IgnoreCase);

            if (this.textBoxSearch.HasText)
            {
                StringBuilder result = new StringBuilder();
                foreach (string str in this.textBoxSearch.Text.Split(' '))
                {
                    result.Append(Regex.Escape(str));
                    result.Append(".*");
                }

                regex = new Regex(result.ToString(), RegexOptions.IgnoreCase);
            }

            // Get the program info list
            using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (regKey != null)
                {
                    foreach (string strSubKeyName in regKey.GetSubKeyNames())
                    {
                        using (RegistryKey subKey = regKey.OpenSubKey(strSubKeyName))
                        {
                            if (subKey != null)
                                listProgInfo.Add(new ProgramInfo(subKey));
                        }
                    }
                }
            }

            // (x64 registry keys)
            if (Utils.Is64BitOS)
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (regKey != null)
                    {
                        foreach (string strSubKeyName in regKey.GetSubKeyNames())
                        {
                            using (RegistryKey subKey = regKey.OpenSubKey(strSubKeyName))
                            {
                                if (subKey != null)
                                    listProgInfo.Add(new ProgramInfo(subKey));
                            }
                        }
                    }
                }
            }


            // Populate list view
            foreach (ProgramInfo progInfo in listProgInfo)
            {
                if ((!string.IsNullOrEmpty(progInfo._displayName))
                    && (string.IsNullOrEmpty(progInfo._parentKeyName))
                    && (!progInfo._systemComponent))
                {

                    if (regex.IsMatch(progInfo.Program))
                        this.listViewProgs.Items.Add(progInfo);
                }
            }

            // Resize columns
            Utils.AutoResizeColumns(this.listViewProgs);
        }

        private void SearchTextBox_Search(object sender, RoutedEventArgs e)
        {
            PopulateListView();
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;
                    Sort(header, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }


                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(this.listViewProgs.Items);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (this.listViewProgs.SelectedItems.Count > 0)
            {
                ProgramInfo progInfo = this.listViewProgs.SelectedItems[0] as ProgramInfo;

                if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to remove this program from the registry?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    progInfo.RemoveFromRegistry();

                PopulateListView();
            }
        }

        private void buttonUninstall_Click(object sender, RoutedEventArgs e)
        {
            if (this.listViewProgs.SelectedItems.Count > 0)
            {
                ProgramInfo progInfo = this.listViewProgs.SelectedItems[0] as ProgramInfo;

                if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to remove this program?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    progInfo.Uninstall();

                PopulateListView();
            }
        }

	}

    public class ProgramInfo
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
                    return Utils.CreateBitmapSourceFromBitmap(Properties.Resources.Repair);
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
    }
}
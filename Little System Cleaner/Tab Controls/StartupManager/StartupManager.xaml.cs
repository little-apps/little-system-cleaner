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
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Drawing;
using CommonTools.TreeListView.Tree;
using System.Diagnostics;

namespace Little_System_Cleaner.Controls.StartupManager
{
    public partial class StartupManager
    {
		public StartupManager()
		{
			this.InitializeComponent();

            LoadStartupFiles();
		}

        /// <summary>
        /// Loads files that load on startup
        /// </summary>
        private void LoadStartupFiles()
        {
            this._tree.Model = StartupMgrModel.CreateStarupMgrModel();

            // Expands treeview
            this._tree.UpdateLayout();
            this._tree.ExpandAll();
            this._tree.AutoResizeColumns();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadStartupFiles();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            AddEditEntry addEditEntryWnd = new AddEditEntry();
            if (addEditEntryWnd.ShowDialog().GetValueOrDefault() == true)
                // Refresh treelistview
                LoadStartupFiles();
        }

        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            if (this._tree.SelectedItem == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "No entry selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StartupEntry selectedItem = this._tree.SelectedNode.Tag as StartupEntry;

            // If root node -> display msg box and exit
            if (selectedItem.Children.Count > 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "Entry cannot be registry key or folder", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AddEditEntry addEditEntryWnd = new AddEditEntry(selectedItem.Parent.SectionName, selectedItem.SectionName, selectedItem.Path, selectedItem.Args, selectedItem.RegKey);

            if (addEditEntryWnd.ShowDialog().GetValueOrDefault() == true)
                // Refresh treelistview
                LoadStartupFiles();
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (this._tree.SelectedNodes.Count > 0)
            {
                StartupEntry node = this._tree.SelectedNode.Tag as StartupEntry;

                if (node.IsLeaf)
                {
                    if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to remove the selected entry from startup?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        bool bFailed = false;

                        string sectionName = (node.Parent as StartupEntry).SectionName;

                        if (Directory.Exists(sectionName))
                        {
                            // Startup folder
                            string strPath = Path.Combine(sectionName, node.SectionName);

                            try
                            {
                                if (File.Exists(strPath))
                                    File.Delete(strPath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(Application.Current.MainWindow, ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                                bFailed = true;
                            }
                        }
                        else
                        {
                            // Registry key
                            string strMainKey = sectionName.Substring(0, sectionName.IndexOf('\\'));
                            string strSubKey = sectionName.Substring(sectionName.IndexOf('\\') + 1);
                            RegistryKey rk = Utils.RegOpenKey(strMainKey, strSubKey);

                            try
                            {
                                if (rk != null)
                                    rk.DeleteValue(node.SectionName);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(Application.Current.MainWindow, ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                                bFailed = true;
                            }

                            rk.Close();
                        }

                        if (!bFailed)
                            MessageBox.Show(Application.Current.MainWindow, "Successfully removed startup entry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    }
                }
                else
                {
                    if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to remove these entries from startup?\nNOTE: This will remove all the entries in the selected startup area", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        bool bFailed = false;

                        // Registry key or folder
                        string sectionName = node.SectionName;

                        if (Directory.Exists(sectionName))
                        {

                            try
                            {
                                if (Directory.Exists(sectionName))
                                    Directory.Delete(sectionName);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(Application.Current.MainWindow, ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                                bFailed = true;
                            }
                        }
                        else
                        {
                            // Registry key
                            string strMainKey = sectionName.Substring(0, sectionName.IndexOf('\\'));
                            string strSubKey = sectionName.Substring(sectionName.IndexOf('\\') + 1);
                            RegistryKey rk = Utils.RegOpenKey(strMainKey, null);

                            try
                            {
                                if (rk != null)
                                    rk.DeleteSubKey(strSubKey);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(Application.Current.MainWindow, ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                                bFailed = true;
                            }

                            rk.Close();
                        }

                        if (!bFailed)
                            MessageBox.Show(Application.Current.MainWindow, "Successfully removed startup entries", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    }
                }

                LoadStartupFiles();
            }
            
        }

        private void buttonView_Click(object sender, RoutedEventArgs e)
        {
            if (this._tree.SelectedNode != null)
            {
                StartupEntry node = this._tree.SelectedNode.Tag as StartupEntry;

                if (!node.IsLeaf)
                    return;

                if (node.RegKey == null)
                {
                    Process.Start(node.Parent.SectionName);
                }
                else
                {
                    RegEditGo.GoTo(node.RegKey.Name, node.SectionName);
                }
            }

        }

        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            if (this._tree.SelectedNode != null)
            {
                StartupEntry node = this._tree.SelectedNode.Tag as StartupEntry;

                if (!node.IsLeaf)
                    return;

                if (MessageBox.Show("Are you sure you want to run this program?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    Process.Start(node.Path, node.Args);
            }

            MessageBox.Show("Successfully started program", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        
	}

    public class StartupEntry : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly ObservableCollection<StartupEntry> _children = new ObservableCollection<StartupEntry>();
        public ObservableCollection<StartupEntry> Children
        {
            get { return _children; }
        }

        public RegistryKey RegKey { get; set; }

        public StartupEntry Parent { get; set; }

        public bool IsLeaf 
        {
            get { return (Children.Count == 0); } 
        }

        public string SectionName { get; set; }
        public string Path { get; set; }
        public string Args { get; set; }

        public System.Windows.Controls.Image bMapImg { get; set; }

        public StartupEntry()
        {
        }
    }

    public class StartupMgrModel : ITreeModel
    {
        public StartupEntry Root { get; private set; }

        public StartupMgrModel()
        {
            Root = new StartupEntry();
        }

        public static StartupMgrModel CreateStarupMgrModel()
        {
            StartupMgrModel treeModel = new StartupMgrModel();

            // Adds registry keys
            try 
            {
                // all user keys
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true));

                if (Utils.Is64BitOS)
                {
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true));
                }

                // current user keys
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true));

                if (Utils.Is64BitOS)
                {
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true));
                }
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            // Adds startup folders
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP));
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(Utils.CSIDL_COMMON_STARTUP));

            return treeModel;
        }

        /// <summary>
        /// Loads registry sub key into tree view
        /// </summary>
        private static void LoadRegistryAutoRun(StartupMgrModel treeModel, RegistryKey regKey)
        {
            if (regKey == null)
                return;

            if (regKey.ValueCount <= 0)
                return;

            StartupEntry nodeRoot = new StartupEntry() { SectionName = regKey.Name };

            if (regKey.Name.Contains(Registry.CurrentUser.ToString()))
                nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.User);
            else
                nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.Users);

            foreach (string strItem in regKey.GetValueNames())
            {
                string strFilePath = regKey.GetValue(strItem) as string;

                if (!string.IsNullOrEmpty(strFilePath))
                {
                    // Get file arguments
                    string strFile = "", strArgs = "";

                    if (Utils.FileExists(strFilePath))
                        strFile = strFilePath;
                    else
                    {
                        if (!Utils.ExtractArguments(strFilePath, out strFile, out strArgs))
                            if (!Utils.ExtractArguments2(strFilePath, out strFile, out strArgs))
                                // If command line cannot be extracted, set file path to command line
                                strFile = strFilePath;
                    }

                    StartupEntry node = new StartupEntry() { Parent = nodeRoot, SectionName = strItem, Path = strFile, Args = strArgs, RegKey = regKey };

                    Icon ico = Utils.ExtractIcon(strFile);
                    if (ico != null)
                        node.bMapImg = Utils.CreateBitmapSourceFromBitmap(ico.ToBitmap().Clone() as Bitmap);
                    else
                        node.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.app);

                    nodeRoot.Children.Add(node);
                }
            }

            if (nodeRoot.Children.Count > 0)
                treeModel.Root.Children.Add(nodeRoot);
        }

        /// <summary>
        /// Loads startup folder into tree view
        /// </summary>
        private static void AddStartupFolder(StartupMgrModel treeModel, string strFolder)
        {
            try
            {
                if (string.IsNullOrEmpty(strFolder) || !Directory.Exists(strFolder))
                    return;

                StartupEntry nodeRoot = new StartupEntry() { SectionName = strFolder };

                if (Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP) == strFolder)
                    nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.User);
                else
                    nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.Users);

                foreach (string strShortcut in Directory.GetFiles(strFolder))
                {
                    string strShortcutName = Path.GetFileName(strShortcut);
                    string strFilePath, strFileArgs;

                    if (Path.GetExtension(strShortcut) == ".lnk")
                    {
                        if (!Utils.ResolveShortcut(strShortcut, out strFilePath, out strFileArgs))
                            continue;

                        StartupEntry node = new StartupEntry() { Parent = nodeRoot, SectionName = strShortcutName, Path = strFilePath, Args = strFileArgs };

                        Icon ico = Utils.ExtractIcon(strFilePath);
                        if (ico != null)
                            node.bMapImg = Utils.CreateBitmapSourceFromBitmap(ico.ToBitmap().Clone() as Bitmap);
                        else
                            node.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.app);

                        nodeRoot.Children.Add(node);
                    }
                }

                if (nodeRoot.Children.Count > 0)
                    treeModel.Root.Children.Add(nodeRoot);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

        }

        public System.Collections.IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;
            return (parent as StartupEntry).Children;
        }

        public bool HasChildren(object parent)
        {
            return (parent as StartupEntry).Children.Count > 0;
        }
    }
}
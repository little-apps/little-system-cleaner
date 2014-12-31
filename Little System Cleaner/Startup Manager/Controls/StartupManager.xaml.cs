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
using System.Diagnostics;
using Little_System_Cleaner.Startup_Manager.Helpers;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Startup_Manager.Controls
{
    public partial class StartupManager
    {
		public StartupManager()
		{
			this.InitializeComponent();
		}

        public void OnLoaded()
        {
            LoadStartupFiles();
        }

        public bool OnUnloaded(bool forceExit)
        {
            this._tree.Model = null;

            return true;
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
            Main.Watcher.Event("Startup Manager", "Add");

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

            Main.Watcher.Event("Startup Manager", "Edit");

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

                        Main.Watcher.Event("Startup Manager", "Delete Single Entry");

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
                            RegistryKey rk = Utils.RegOpenKey(strMainKey, strSubKey, false);

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

                        Main.Watcher.Event("Startup Manager", "Delete Multiple Entries");

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
                            using (RegistryKey rk = Utils.RegOpenKey(strMainKey, strSubKey, false))
                            {
                                if (rk == null)
                                {
                                    string message = string.Format("Unable to open registry key ({0}) to delete all entries.", sectionName);
                                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                                    bFailed = true;
                                }
                                else
                                {
                                    try
                                    {
                                        string[] valueNames = rk.GetValueNames();

                                        // Clear all values
                                        foreach (string valueName in valueNames)
                                        {
                                            rk.DeleteValue(valueName, false);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show(Application.Current.MainWindow, ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                                        bFailed = true;
                                    }
                                }
                            }
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

                Main.Watcher.Event("Startup Manager", "View");

                if (node.RegKey == null)
                {
                    // Folder

                    try
                    {
                        Process.Start(node.Parent.SectionName);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("Unable to open startup folder.\nThe following error occurred: {0}", ex.Message);

                        MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Registry key

                    try
                    {
                        RegEditGo.GoTo(node.RegKey.Name, node.SectionName);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("Unable to open registry key in RegEdit.\nThe following error occurred: {0}", ex.Message);

                        MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
                {
                    Main.Watcher.Event("Startup Manager", "Run");

                    try
                    {
                        Process.Start(node.Path, node.Args);

                        MessageBox.Show(App.Current.MainWindow, "Successfully started program", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        string message;

                        if (ex is FileNotFoundException)
                            message = "The file (" + (ex as FileNotFoundException).FileName + ") could not be found. This could mean the startup entry is erroneous.";
                        else
                            message = "The startup entry command (" + node.Command + ") could not be executed.\nThe following error occurred: " + ex.Message;

                        MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                    
            }
        }   
	}
}
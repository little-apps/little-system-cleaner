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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Drawing;
using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Registry_Cleaner.Scanners;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using System.IO;
using Little_System_Cleaner.Xml;
using System.Diagnostics;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Sections.xaml
    /// </summary>
    public partial class Start : System.Windows.Controls.UserControl
    {
        public ScanWizard scanBase;
        ObservableCollection<RestoreFile> _restoreFiles = new ObservableCollection<RestoreFile>();

        readonly ExcludeArray _excludeArray;
        public ExcludeArray ExcludeArray
        {
            get { return _excludeArray; }
        }

        public ObservableCollection<RestoreFile> RestoreFiles
        {
            get { return _restoreFiles; }
        }

        public Start(ScanWizard sb)
        {
            InitializeComponent();

            this._tree.Model = SectionModel.CreateSectionModel();

            scanBase = sb;

            this.textBoxBackups.Text = Properties.Settings.Default.optionsBackupDir;

            this.checkBoxLog.IsChecked = Properties.Settings.Default.registryCleanerOptionsLog;
            this.checkBoxShowLog.IsChecked = Properties.Settings.Default.registryCleanerOptionsShowLog;
            this.checkBoxAutoRescan.IsChecked = Properties.Settings.Default.registryCleanerOptionsAutoRescan;
            this.checkBoxDelBackup.IsChecked = Properties.Settings.Default.registryCleanerOptionsDelBackup;
            this.checkBoxIgnoreRemMedia.IsChecked = Properties.Settings.Default.registryCleanerOptionsRemMedia;
            this.checkBoxAutoRepair.IsChecked = Properties.Settings.Default.registryCleanerOptionsAutoRepair;
            this.checkBoxAutoExit.IsChecked = Properties.Settings.Default.registryCleanerOptionsAutoExit;

            this._excludeArray = Properties.Settings.Default.arrayExcludeList;

            PopulateListView();
        }

        private void PopulateListView()
        {
            DirectoryInfo di = new DirectoryInfo(Properties.Settings.Default.optionsBackupDir);

            // If directory doesnt exist -> create it
            if (!di.Exists)
                di.Create();

            // If list is already populated -> clear it
            if (RestoreFiles.Count > 0)
                RestoreFiles.Clear();

            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.Extension.CompareTo(".bakx") == 0)
                {
                    this._restoreFiles.Add(new RestoreFile(fi));
                }
            }

            // Refresh listview
            this.listViewFiles.Items.Refresh();

            // Auto resize columns
            if (this.listViewFiles.Items.Count > 0)
                Utils.AutoResizeColumns(this.listViewFiles);
        }

        public void UpdateSettings()
        {
            Properties.Settings.Default.registryCleanerOptionsLog = this.checkBoxLog.IsChecked.Value;
            Properties.Settings.Default.registryCleanerOptionsAutoRescan = this.checkBoxAutoRescan.IsChecked.Value;
            Properties.Settings.Default.registryCleanerOptionsDelBackup = this.checkBoxDelBackup.IsChecked.Value;
            Properties.Settings.Default.registryCleanerOptionsRemMedia = this.checkBoxIgnoreRemMedia.IsChecked.Value;
            Properties.Settings.Default.registryCleanerOptionsAutoRepair = this.checkBoxAutoRepair.IsChecked.Value;
            Properties.Settings.Default.registryCleanerOptionsAutoExit = this.checkBoxAutoExit.IsChecked.Value;

            if (this.textBoxBackups.Text != Properties.Settings.Default.optionsBackupDir)
                Properties.Settings.Default.optionsBackupDir = this.textBoxBackups.Text;

            Properties.Settings.Default.arrayExcludeList = ExcludeArray;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Properties.Settings.Default.optionsBackupDir);
        }

        private void buttonRestore_Click(object sender, RoutedEventArgs e)
        {
            long lSeqNum = 0;
            xmlReader xmlReader = new xmlReader();
            xmlRegistry xmlReg = new xmlRegistry();

            if (this.listViewFiles.SelectedItem == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "No restore file selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SysRestore.StartRestore("Before Little Registry Cleaner Restore", out lSeqNum);

            if (xmlReg.loadAsXml(xmlReader, (this.listViewFiles.SelectedItem as RestoreFile).FileInfo.FullName))
            {
                MessageBox.Show(Application.Current.MainWindow, "Successfully restored registry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                if (Properties.Settings.Default.registryCleanerOptionsDelBackup)
                {
                    // Delete file
                    (this.listViewFiles.SelectedItem as RestoreFile).FileInfo.Delete();

                    // Remove from listview and refresh
                    RestoreFiles.Remove(this.listViewFiles.SelectedItem as RestoreFile);
                    PopulateListView();
                }
            }
            else
                MessageBox.Show(Application.Current.MainWindow, "Error restoring the registry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

            SysRestore.EndRestore(lSeqNum);
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateListView();
        }

        private void buttonBrowseBackupDir_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowserDlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowserDlg.Description = "Select the folder where the backup files will be placed";
                folderBrowserDlg.SelectedPath = this.textBoxBackups.Text;
                folderBrowserDlg.ShowNewFolderButton = true;

                if (folderBrowserDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    this.textBoxBackups.Text = folderBrowserDlg.SelectedPath;

                UpdateSettings();
            }
        }

        private void menuItemAddFile_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog openFileDlg = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDlg.Multiselect = true;
                if (openFileDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (string filePath in openFileDlg.FileNames)
                    {
                        ExcludeItem excludeItem = new ExcludeItem() { FilePath = filePath };
                        if (!ExcludeArray.Contains(excludeItem))
                        {
                            ExcludeArray.Add(excludeItem);
                            this.listView1.Items.Refresh();
                        }
                        else
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("File ({0}) already exists", filePath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
            }

            UpdateSettings();
        }

        private void menuItemAddFolder_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderDlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDlg.Description = "Select the folder to exclude";
                folderDlg.ShowNewFolderButton = true;

                if (folderDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ExcludeItem excludeItem = new ExcludeItem() { FolderPath = folderDlg.SelectedPath };

                    if (!ExcludeArray.Contains(excludeItem))
                    {
                        ExcludeArray.Add(excludeItem);
                        this.listView1.Items.Refresh();
                    }
                    else
                        MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("Folder ({0}) already exists", folderDlg.SelectedPath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                }
            }

            UpdateSettings();
        }

        private void menuItemAddRegKey_Click(object sender, RoutedEventArgs e)
        {
            AddExcludeItem addExcludeItem = new AddExcludeItem();
            if (addExcludeItem.ShowDialog() == true)
            {
                ExcludeItem excludeItem = new ExcludeItem() { RegistryPath = addExcludeItem.RegistryPath };
                if (!ExcludeArray.Contains(excludeItem))
                {
                    ExcludeArray.Add(excludeItem);
                    this.listView1.Items.Refresh();
                }
                else
                    MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("Registry key ({0}) already exists", addExcludeItem.RegistryPath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            UpdateSettings();
        }


        private void menuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (this.listView1.SelectedItems.Count > 0)
            {
                if (MessageBox.Show(System.Windows.Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    ExcludeArray.Remove(this.listView1.SelectedItem as ExcludeItem);

                this.listView1.Items.Refresh();
            }

            UpdateSettings();
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            // Update settings before scanning
            this.UpdateSettings();

            this.scanBase.Model = this._tree.Model as SectionModel;

            this.scanBase.MoveNext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this._tree.ExpandAll();
            this._tree.AutoResizeColumns();
        }

    }
}

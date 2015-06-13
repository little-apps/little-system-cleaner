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
using System.Diagnostics;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Registry_Cleaner.Helpers.Backup;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Sections.xaml
    /// </summary>
    public partial class Start : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public Wizard scanBase;
        private ObservableCollection<RestoreFile> _restoreFiles;
        private ObservableCollection<ExcludeItem> _excludeArray;

        public ObservableCollection<ExcludeItem> ExcludeArray
        {
            get { return _excludeArray; }
            set
            {
                this._excludeArray = value;

                this.OnPropertyChanged("ExcludeArray");
            }
        }

        public ObservableCollection<RestoreFile> RestoreFiles
        {
            get { return _restoreFiles; }
            set
            {
                this._restoreFiles = value;

                this.OnPropertyChanged("RestoreFiles");
            }
        }

        public Start(Wizard sb)
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
            this.checkBoxShowErrors.IsChecked = Properties.Settings.Default.registryCleanerOptionsShowErrors;
            this.checkBoxDeleteOnBackupError.IsEnabled = (!this.checkBoxShowErrors.IsChecked.Value);
            this.checkBoxDeleteOnBackupError.IsChecked = Properties.Settings.Default.registryCleanerOptionsDeleteOnBackupError;
            this.checkBoxAutoRepair.IsChecked = Properties.Settings.Default.registryCleanerOptionsAutoRepair;
            this.checkBoxAutoExit.IsChecked = Properties.Settings.Default.registryCleanerOptionsAutoExit;

            this.ExcludeArray = Properties.Settings.Default.arrayExcludeList;
            this.RestoreFiles = new ObservableCollection<RestoreFile>();

            PopulateListView();
        }

        private void PopulateListView()
        {
            string error;
            DirectoryInfo di;

            try
            {
                di = new DirectoryInfo(Properties.Settings.Default.optionsBackupDir);

                // If directory doesnt exist -> create it
                if (!di.Exists)
                    di.Create();

                // If list is already populated -> clear it
                if (RestoreFiles.Count > 0)
                    RestoreFiles.Clear();
            }
            catch (Exception ex)
            {
                string message = string.Format("Unable to get files from backup directory.\nThe following error occurred: {0}", ex.Message);
                MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }
            
            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.Extension.CompareTo(".bakx") == 0)
                {
                    // Deserialize to creation date
                    using (BackupRegistry backupReg = new BackupRegistry(fi.FullName))
                    {
                        if (!backupReg.Open(true))
                            continue;

                        if (!backupReg.Deserialize(out error))
                            continue;

                        this._restoreFiles.Add(new RestoreFile(fi, backupReg.Created));
                    }

                    
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
            Properties.Settings.Default.registryCleanerOptionsShowErrors = this.checkBoxShowErrors.IsChecked.Value;
            Properties.Settings.Default.registryCleanerOptionsDeleteOnBackupError = this.checkBoxDeleteOnBackupError.IsChecked.Value;
            Properties.Settings.Default.registryCleanerOptionsAutoRepair = this.checkBoxAutoRepair.IsChecked.Value;
            Properties.Settings.Default.registryCleanerOptionsAutoExit = this.checkBoxAutoExit.IsChecked.Value;

            if (this.checkBoxDeleteOnBackupError.IsEnabled != (!this.checkBoxShowErrors.IsChecked.Value))
                this.checkBoxDeleteOnBackupError.IsEnabled = (!this.checkBoxShowErrors.IsChecked.Value);

            if (this.textBoxBackups.Text != Properties.Settings.Default.optionsBackupDir)
                Properties.Settings.Default.optionsBackupDir = this.textBoxBackups.Text;

            Properties.Settings.Default.arrayExcludeList = ExcludeArray;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            string windir = Environment.GetEnvironmentVariable("WINDIR");

            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = windir + @"\explorer.exe";
                proc.StartInfo.Arguments = Properties.Settings.Default.optionsBackupDir;
                proc.Start();
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException) 
                {
                    MessageBox.Show(App.Current.MainWindow, "Could not find Windows Explorer to browse to a folder (" + Properties.Settings.Default.optionsBackupDir + ")", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
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
                
            }
            
        }

        private void buttonRestore_Click(object sender, RoutedEventArgs e)
        {
            long lSeqNum = 0;
            string message;

            if (this.listViewFiles.SelectedItem == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "No restore file selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            string filePath = (this.listViewFiles.SelectedItem as RestoreFile).FileInfo.FullName;

            using (BackupRegistry backupReg = new BackupRegistry(filePath))
            {
                if (!backupReg.Open(true))
                {
                    message = string.Format("Failed to open backup file ({0}).\nUnable to restore registry.", filePath);
                    MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    SysRestore.StartRestore("Before Little System Cleaner (Registry Cleaner) Restore", out lSeqNum);
                }
                catch (Win32Exception ex)
                {
                    message = string.Format("Unable to create system restore point.\nThe following error occurred: {0}", ex.Message);
                    MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (!backupReg.Deserialize(out message))
                {
                    MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (backupReg.RegistryEntries.Count == 0)
                {
                    MessageBox.Show(App.Current.MainWindow, "No registry entries found in backup file.\nUnable to restore registry.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (backupReg.Restore())
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
                {
                    MessageBox.Show(Application.Current.MainWindow, "Error restoring the registry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (lSeqNum != 0)
                {
                    try
                    {
                        SysRestore.EndRestore(lSeqNum);
                    }
                    catch (Win32Exception ex)
                    {
                        message = string.Format("Unable to create system restore point.\nThe following error occurred: {0}", ex.Message);
                        MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
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
                PopulateListView();
            }
        }

        private void menuItemAddFile_Click(object sender, RoutedEventArgs e)
        {
            AddEditExcludeItem addExcludeItem = new AddEditExcludeItem(AddEditExcludeItem.ExcludeTypes.File);

            if (addExcludeItem.ShowDialog().GetValueOrDefault())
            {
                ExcludeItem excludeItem = addExcludeItem.ExcludeItem;
                if (!ExcludeArray.Contains(excludeItem))
                {
                    this.ExcludeArray.Add(excludeItem);

                    MessageBox.Show(App.Current.MainWindow, "Successfully added file to exclude list.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    UpdateSettings();
                    Utils.AutoResizeColumns(this.listView1);
                }
                else
                    MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("File ({0}) already exists", addExcludeItem.FilePath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItemAddFolder_Click(object sender, RoutedEventArgs e)
        {
            AddEditExcludeItem addExcludeItem = new AddEditExcludeItem(AddEditExcludeItem.ExcludeTypes.Folder);

            if (addExcludeItem.ShowDialog().GetValueOrDefault())
            {
                ExcludeItem excludeItem = addExcludeItem.ExcludeItem;
                if (!ExcludeArray.Contains(excludeItem)) {
                    this.ExcludeArray.Add(excludeItem);

                    MessageBox.Show(App.Current.MainWindow, "Successfully added folder to exclude list.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    UpdateSettings();
                    Utils.AutoResizeColumns(this.listView1);
                }
                else
                    MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("Folder ({0}) already exists", addExcludeItem.FolderPath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItemAddRegKey_Click(object sender, RoutedEventArgs e)
        {
            AddEditExcludeItem addExcludeItem = new AddEditExcludeItem(AddEditExcludeItem.ExcludeTypes.Registry);
            if (addExcludeItem.ShowDialog().GetValueOrDefault())
            {
                ExcludeItem excludeItem = addExcludeItem.ExcludeItem;
                if (!ExcludeArray.Contains(excludeItem)) 
                {
                    this.ExcludeArray.Add(excludeItem);

                    MessageBox.Show(App.Current.MainWindow, "Successfully added registry key to exclude list.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    UpdateSettings();
                    Utils.AutoResizeColumns(this.listView1);
                }
                else
                    MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("Registry key ({0}) already exists", addExcludeItem.RegistryPath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItemEdit_Click(object sender, RoutedEventArgs e)
        {
            if (this.listView1.SelectedItems.Count > 0)
            {
                ExcludeItem excItem = this.listView1.SelectedItem as ExcludeItem;
                int pos = this.ExcludeArray.IndexOf(excItem);

                if (pos == -1)
                {
                    MessageBox.Show(App.Current.MainWindow, "The selected entry could not be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                AddEditExcludeItem editExcludeItem = new AddEditExcludeItem(excItem);
                if (editExcludeItem.ShowDialog().GetValueOrDefault())
                {
                    this.ExcludeArray[pos] = editExcludeItem.ExcludeItem;

                    MessageBox.Show(App.Current.MainWindow, "Successfully updated exclude entry.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    UpdateSettings();
                    Utils.AutoResizeColumns(this.listView1);
                }
            }
        }

        private void menuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (this.listView1.SelectedItems.Count > 0)
            {
                if (MessageBox.Show(System.Windows.Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    this.ExcludeArray.Remove(this.listView1.SelectedItem as ExcludeItem);

                    UpdateSettings();
                    Utils.AutoResizeColumns(this.listView1);
                }
            }
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            // Update settings before scanning
            this.UpdateSettings();

            this.scanBase.Model = this._tree.Model as SectionModel;

            if (Scan.EnabledScanners.Count == 0)
            {
                MessageBox.Show(App.Current.MainWindow, "At least one section must be selected in order for the Windows Registry to be scanned.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            this.scanBase.MoveNext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this._tree.ExpandAll();
            this._tree.AutoResizeColumns();
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateSettings();
        }

        

    }
}

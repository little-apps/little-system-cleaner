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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Little_System_Cleaner.Registry_Cleaner.Helpers.Backup;
using Little_System_Cleaner.Registry_Cleaner.Helpers.Sections;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Sections.xaml
    /// </summary>
    public partial class Start : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public Wizard ScanBase;
        private ObservableCollection<RestoreFile> _restoreFiles;
        private ObservableCollection<ExcludeItem> _excludeArray;

        public ObservableCollection<ExcludeItem> ExcludeArray
        {
            get { return _excludeArray; }
            set
            {
                _excludeArray = value;

                OnPropertyChanged("ExcludeArray");
            }
        }

        public ObservableCollection<RestoreFile> RestoreFiles
        {
            get { return _restoreFiles; }
            set
            {
                _restoreFiles = value;

                OnPropertyChanged("RestoreFiles");
            }
        }

        public Start(Wizard sb)
        {
            InitializeComponent();

            _tree.Model = SectionModel.CreateSectionModel();

            ScanBase = sb;

            textBoxBackups.Text = Settings.Default.OptionsBackupDir;

            checkBoxLog.IsChecked = Settings.Default.registryCleanerOptionsLog;
            checkBoxShowLog.IsChecked = Settings.Default.registryCleanerOptionsShowLog;
            checkBoxAutoRescan.IsChecked = Settings.Default.registryCleanerOptionsAutoRescan;
            checkBoxDelBackup.IsChecked = Settings.Default.registryCleanerOptionsDelBackup;
            checkBoxIgnoreRemMedia.IsChecked = Settings.Default.registryCleanerOptionsRemMedia;
            checkBoxShowErrors.IsChecked = Settings.Default.registryCleanerOptionsShowErrors;
            checkBoxDeleteOnBackupError.IsEnabled = (!checkBoxShowErrors.IsChecked.Value);
            checkBoxDeleteOnBackupError.IsChecked = Settings.Default.registryCleanerOptionsDeleteOnBackupError;
            checkBoxAutoRepair.IsChecked = Settings.Default.registryCleanerOptionsAutoRepair;
            checkBoxAutoExit.IsChecked = Settings.Default.registryCleanerOptionsAutoExit;

            ExcludeArray = Settings.Default.ArrayExcludeList;
            RestoreFiles = new ObservableCollection<RestoreFile>();

            PopulateListView();
        }

        private void PopulateListView()
        {
            DirectoryInfo di;

            try
            {
                di = new DirectoryInfo(Settings.Default.OptionsBackupDir);

                // If directory doesnt exist -> create it
                if (!di.Exists)
                    di.Create();

                // If list is already populated -> clear it
                if (RestoreFiles.Count > 0)
                    RestoreFiles.Clear();
            }
            catch (Exception ex)
            {
                string message = $"Unable to get files from backup directory.\nThe following error occurred: {ex.Message}";
                MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }
            
            foreach (FileInfo fi in di.GetFiles().Where(fi => fi.Extension.CompareTo(".bakx") == 0))
            {
                // Deserialize to creation date
                using (BackupRegistry backupReg = new BackupRegistry(fi.FullName))
                {
                    if (!backupReg.Open(true))
                        continue;

                    string error;
                    if (!backupReg.Deserialize(out error))
                        continue;

                    _restoreFiles.Add(new RestoreFile(fi, backupReg.Created));
                }
            }

            // Refresh listview
            listViewFiles.Items.Refresh();

            // Auto resize columns
            if (listViewFiles.Items.Count > 0)
                Utils.AutoResizeColumns(listViewFiles);
        }

        public void UpdateSettings()
        {
            Settings.Default.registryCleanerOptionsLog = checkBoxLog.IsChecked.Value;
            Settings.Default.registryCleanerOptionsAutoRescan = checkBoxAutoRescan.IsChecked.Value;
            Settings.Default.registryCleanerOptionsDelBackup = checkBoxDelBackup.IsChecked.Value;
            Settings.Default.registryCleanerOptionsRemMedia = checkBoxIgnoreRemMedia.IsChecked.Value;
            Settings.Default.registryCleanerOptionsShowErrors = checkBoxShowErrors.IsChecked.Value;
            Settings.Default.registryCleanerOptionsDeleteOnBackupError = checkBoxDeleteOnBackupError.IsChecked.Value;
            Settings.Default.registryCleanerOptionsAutoRepair = checkBoxAutoRepair.IsChecked.Value;
            Settings.Default.registryCleanerOptionsAutoExit = checkBoxAutoExit.IsChecked.Value;

            if (checkBoxDeleteOnBackupError.IsEnabled != (!checkBoxShowErrors.IsChecked.Value))
                checkBoxDeleteOnBackupError.IsEnabled = (!checkBoxShowErrors.IsChecked.Value);

            if (textBoxBackups.Text != Settings.Default.OptionsBackupDir)
                Settings.Default.OptionsBackupDir = textBoxBackups.Text;

            Settings.Default.ArrayExcludeList = ExcludeArray;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            string windir = Environment.GetEnvironmentVariable("WINDIR");

            try
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = windir + @"\explorer.exe",
                        Arguments = Settings.Default.OptionsBackupDir
                    }
                };

                proc.Start();
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException) 
                {
                    MessageBox.Show(Application.Current.MainWindow, "Could not find Windows Explorer to browse to a folder (" + Settings.Default.OptionsBackupDir + ")", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (ex is Win32Exception)
                {
                    int hr = Marshal.GetHRForException(ex);
                    if (hr == unchecked((int)0x80004002))
                    {
                        MessageBox.Show(Application.Current.MainWindow, "The following error occurred: " + ex.Message + "\nThis can be caused by problems with permissions and the Windows Registry.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(Application.Current.MainWindow, "The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show(Application.Current.MainWindow, "The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
            
        }

        private void buttonRestore_Click(object sender, RoutedEventArgs e)
        {
            long lSeqNum = 0;

            if (listViewFiles.SelectedItem == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "No restore file selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            string filePath = (listViewFiles.SelectedItem as RestoreFile).FileInfo.FullName;

            using (BackupRegistry backupReg = new BackupRegistry(filePath))
            {
                string message;
                if (!backupReg.Open(true))
                {
                    message = $"Failed to open backup file ({filePath}).\nUnable to restore registry.";
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    SysRestore.StartRestore("Before Little System Cleaner (Registry Cleaner) Restore", out lSeqNum);
                }
                catch (Win32Exception ex)
                {
                    message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (!backupReg.Deserialize(out message))
                {
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (backupReg.RegistryEntries.Count == 0)
                {
                    MessageBox.Show(Application.Current.MainWindow, "No registry entries found in backup file.\nUnable to restore registry.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (backupReg.Restore())
                {
                    MessageBox.Show(Application.Current.MainWindow, "Successfully restored registry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                    if (Settings.Default.registryCleanerOptionsDelBackup)
                    {
                        // Delete file
                        (listViewFiles.SelectedItem as RestoreFile).FileInfo.Delete();

                        // Remove from listview and refresh
                        RestoreFiles.Remove(listViewFiles.SelectedItem as RestoreFile);
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
                        message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                        MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
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
            using (FolderBrowserDialog folderBrowserDlg = new FolderBrowserDialog())
            {
                folderBrowserDlg.Description = "Select the folder where the backup files will be placed";
                folderBrowserDlg.SelectedPath = textBoxBackups.Text;
                folderBrowserDlg.ShowNewFolderButton = true;

                if (folderBrowserDlg.ShowDialog() == DialogResult.OK)
                    textBoxBackups.Text = folderBrowserDlg.SelectedPath;

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
                    ExcludeArray.Add(excludeItem);

                    MessageBox.Show(Application.Current.MainWindow, "Successfully added file to exclude list.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    UpdateSettings();
                    Utils.AutoResizeColumns(listView1);
                }
                else
                    MessageBox.Show(Application.Current.MainWindow, $"File ({addExcludeItem.FilePath}) already exists", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItemAddFolder_Click(object sender, RoutedEventArgs e)
        {
            AddEditExcludeItem addExcludeItem = new AddEditExcludeItem(AddEditExcludeItem.ExcludeTypes.Folder);

            if (!addExcludeItem.ShowDialog().GetValueOrDefault())
                return;

            ExcludeItem excludeItem = addExcludeItem.ExcludeItem;
            if (!ExcludeArray.Contains(excludeItem)) {
                ExcludeArray.Add(excludeItem);

                MessageBox.Show(Application.Current.MainWindow, "Successfully added folder to exclude list.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateSettings();
                Utils.AutoResizeColumns(listView1);
            }
            else
                MessageBox.Show(Application.Current.MainWindow, $"Folder ({addExcludeItem.FolderPath}) already exists", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void menuItemAddRegKey_Click(object sender, RoutedEventArgs e)
        {
            AddEditExcludeItem addExcludeItem = new AddEditExcludeItem(AddEditExcludeItem.ExcludeTypes.Registry);
            if (!addExcludeItem.ShowDialog().GetValueOrDefault())
                return;

            ExcludeItem excludeItem = addExcludeItem.ExcludeItem;
            if (!ExcludeArray.Contains(excludeItem)) 
            {
                ExcludeArray.Add(excludeItem);

                MessageBox.Show(Application.Current.MainWindow, "Successfully added registry key to exclude list.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateSettings();
                Utils.AutoResizeColumns(listView1);
            }
            else
                MessageBox.Show(Application.Current.MainWindow, $"Registry key ({addExcludeItem.RegistryPath}) already exists", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void menuItemEdit_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            ExcludeItem excItem = listView1.SelectedItem as ExcludeItem;
            int pos = ExcludeArray.IndexOf(excItem);

            if (pos == -1)
            {
                MessageBox.Show(Application.Current.MainWindow, "The selected entry could not be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AddEditExcludeItem editExcludeItem = new AddEditExcludeItem(excItem);
            if (!editExcludeItem.ShowDialog().GetValueOrDefault())
                return;

            ExcludeArray[pos] = editExcludeItem.ExcludeItem;

            MessageBox.Show(Application.Current.MainWindow, "Successfully updated exclude entry.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateSettings();
            Utils.AutoResizeColumns(listView1);
        }

        private void menuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            ExcludeArray.Remove(listView1.SelectedItem as ExcludeItem);

            UpdateSettings();
            Utils.AutoResizeColumns(listView1);
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            ScanBase.Model = _tree.Model as SectionModel;

            if (Scan.EnabledScanners.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "At least one section must be selected in order for the Windows Registry to be scanned.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            ScanBase.MoveNext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _tree.ExpandAll();
            _tree.AutoResizeColumns();
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            UpdateSettings();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Update settings before unloading
            UpdateSettings();
        }

        

    }
}

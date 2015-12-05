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
    ///     Interaction logic for Sections.xaml
    /// </summary>
    public partial class Start : INotifyPropertyChanged
    {
        private ObservableCollection<ExcludeItem> _excludeArray;
        private ObservableCollection<RestoreFile> _restoreFiles;

        public Wizard ScanBase;

        public Start(Wizard sb)
        {
            InitializeComponent();

            Tree.Model = SectionModel.CreateSectionModel();

            ScanBase = sb;

            TextBoxBackups.Text = Settings.Default.OptionsBackupDir;

            CheckBoxLog.IsChecked = Settings.Default.registryCleanerOptionsLog;
            CheckBoxShowLog.IsChecked = Settings.Default.registryCleanerOptionsShowLog;
            CheckBoxAutoRescan.IsChecked = Settings.Default.registryCleanerOptionsAutoRescan;
            CheckBoxDelBackup.IsChecked = Settings.Default.registryCleanerOptionsDelBackup;
            CheckBoxIgnoreRemMedia.IsChecked = Settings.Default.registryCleanerOptionsRemMedia;
            CheckBoxShowErrors.IsChecked = Settings.Default.registryCleanerOptionsShowErrors;
            CheckBoxDeleteOnBackupError.IsEnabled = !CheckBoxShowErrors.IsChecked.Value;
            CheckBoxDeleteOnBackupError.IsChecked = Settings.Default.registryCleanerOptionsDeleteOnBackupError;
            CheckBoxAutoRepair.IsChecked = Settings.Default.registryCleanerOptionsAutoRepair;
            CheckBoxAutoExit.IsChecked = Settings.Default.registryCleanerOptionsAutoExit;

            ExcludeArray = Settings.Default.ArrayExcludeList;
            RestoreFiles = new ObservableCollection<RestoreFile>();

            PopulateListView();
        }

        public ObservableCollection<ExcludeItem> ExcludeArray
        {
            get { return _excludeArray; }
            set
            {
                _excludeArray = value;

                OnPropertyChanged(nameof(ExcludeArray));
            }
        }

        public ObservableCollection<RestoreFile> RestoreFiles
        {
            get { return _restoreFiles; }
            set
            {
                _restoreFiles = value;

                OnPropertyChanged(nameof(RestoreFiles));
            }
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
                string message =
                    $"Unable to get files from backup directory.\nThe following error occurred: {ex.Message}";
                MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            foreach (
                var fi in
                    di.GetFiles().Where(fi => string.Compare(fi.Extension, ".bakx", StringComparison.Ordinal) == 0))
            {
                // Deserialize to creation date
                using (var backupReg = new BackupRegistry(fi.FullName))
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
            ListViewFiles.Items.Refresh();

            // Auto resize columns
            if (ListViewFiles.Items.Count > 0)
                ListViewFiles.AutoResizeColumns();
        }

        public void UpdateSettings()
        {
            Settings.Default.registryCleanerOptionsLog = CheckBoxLog.IsChecked.GetValueOrDefault();
            Settings.Default.registryCleanerOptionsAutoRescan = CheckBoxAutoRescan.IsChecked.GetValueOrDefault();
            Settings.Default.registryCleanerOptionsDelBackup = CheckBoxDelBackup.IsChecked.GetValueOrDefault();
            Settings.Default.registryCleanerOptionsRemMedia = CheckBoxIgnoreRemMedia.IsChecked.GetValueOrDefault();
            Settings.Default.registryCleanerOptionsShowErrors = CheckBoxShowErrors.IsChecked.GetValueOrDefault();
            Settings.Default.registryCleanerOptionsDeleteOnBackupError =
                CheckBoxDeleteOnBackupError.IsChecked.GetValueOrDefault();
            Settings.Default.registryCleanerOptionsAutoRepair = CheckBoxAutoRepair.IsChecked.GetValueOrDefault();
            Settings.Default.registryCleanerOptionsAutoExit = CheckBoxAutoExit.IsChecked.GetValueOrDefault();

            if (CheckBoxDeleteOnBackupError.IsEnabled != !CheckBoxShowErrors.IsChecked.GetValueOrDefault())
                CheckBoxDeleteOnBackupError.IsEnabled = !CheckBoxShowErrors.IsChecked.GetValueOrDefault();

            if (TextBoxBackups.Text != Settings.Default.OptionsBackupDir)
                Settings.Default.OptionsBackupDir = TextBoxBackups.Text;

            Settings.Default.ArrayExcludeList = ExcludeArray;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            var windir = Environment.GetEnvironmentVariable("WINDIR");

            try
            {
                var proc = new Process
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
                    MessageBox.Show(Application.Current.MainWindow,
                        "Could not find Windows Explorer to browse to a folder (" + Settings.Default.OptionsBackupDir +
                        ")", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
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
                        MessageBox.Show(Application.Current.MainWindow, "The following error occurred: " + ex.Message,
                            Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show(Application.Current.MainWindow, "The following error occurred: " + ex.Message,
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void buttonRestore_Click(object sender, RoutedEventArgs e)
        {
            long lSeqNum = 0;

            if (ListViewFiles.SelectedItem == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "No restore file selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var filePath = (ListViewFiles.SelectedItem as RestoreFile)?.FileInfo.FullName;

            using (var backupReg = new BackupRegistry(filePath))
            {
                string message;
                if (!backupReg.Open(true))
                {
                    message = $"Failed to open backup file ({filePath}).\nUnable to restore registry.";
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                try
                {
                    SysRestore.StartRestore("Before Little System Cleaner (Registry Cleaner) Restore", out lSeqNum);
                }
                catch (Win32Exception ex)
                {
                    message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                if (!backupReg.Deserialize(out message))
                {
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                if (backupReg.RegistryEntries.Count == 0)
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "No registry entries found in backup file.\nUnable to restore registry.", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (backupReg.Restore())
                {
                    MessageBox.Show(Application.Current.MainWindow, "Successfully restored registry", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    if (Settings.Default.registryCleanerOptionsDelBackup)
                    {
                        // Delete file
                        (ListViewFiles.SelectedItem as RestoreFile)?.FileInfo.Delete();

                        // Remove from listview and refresh
                        RestoreFiles.Remove(ListViewFiles.SelectedItem as RestoreFile);
                        PopulateListView();
                    }
                }
                else
                {
                    MessageBox.Show(Application.Current.MainWindow, "Error restoring the registry", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (lSeqNum == 0)
                    return;

                try
                {
                    SysRestore.EndRestore(lSeqNum);
                }
                catch (Win32Exception ex)
                {
                    message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateListView();
        }

        private void buttonBrowseBackupDir_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDlg = new FolderBrowserDialog())
            {
                folderBrowserDlg.Description = "Select the folder where the backup files will be placed";
                folderBrowserDlg.SelectedPath = TextBoxBackups.Text;
                folderBrowserDlg.ShowNewFolderButton = true;

                if (folderBrowserDlg.ShowDialog() == DialogResult.OK)
                    TextBoxBackups.Text = folderBrowserDlg.SelectedPath;

                UpdateSettings();
                PopulateListView();
            }
        }

        private void menuItemAddFile_Click(object sender, RoutedEventArgs e)
        {
            var addExcludeItem = new AddEditExcludeItem(AddEditExcludeItem.ExcludeTypes.File);

            if (!addExcludeItem.ShowDialog().GetValueOrDefault())
                return;

            var excludeItem = addExcludeItem.ExcludeItem;
            if (!ExcludeArray.Contains(excludeItem))
            {
                ExcludeArray.Add(excludeItem);

                MessageBox.Show(Application.Current.MainWindow, "Successfully added file to exclude list.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateSettings();
                ListViewExcludes.AutoResizeColumns();
            }
            else
                MessageBox.Show(Application.Current.MainWindow, $"File ({addExcludeItem.FilePath}) already exists",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void menuItemAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var addExcludeItem = new AddEditExcludeItem(AddEditExcludeItem.ExcludeTypes.Folder);

            if (!addExcludeItem.ShowDialog().GetValueOrDefault())
                return;

            var excludeItem = addExcludeItem.ExcludeItem;
            if (!ExcludeArray.Contains(excludeItem))
            {
                ExcludeArray.Add(excludeItem);

                MessageBox.Show(Application.Current.MainWindow, "Successfully added folder to exclude list.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateSettings();
                ListViewExcludes.AutoResizeColumns();
            }
            else
                MessageBox.Show(Application.Current.MainWindow, $"Folder ({addExcludeItem.FolderPath}) already exists",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void menuItemAddRegKey_Click(object sender, RoutedEventArgs e)
        {
            var addExcludeItem = new AddEditExcludeItem(AddEditExcludeItem.ExcludeTypes.Registry);
            if (!addExcludeItem.ShowDialog().GetValueOrDefault())
                return;

            var excludeItem = addExcludeItem.ExcludeItem;
            if (!ExcludeArray.Contains(excludeItem))
            {
                ExcludeArray.Add(excludeItem);

                MessageBox.Show(Application.Current.MainWindow, "Successfully added registry key to exclude list.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateSettings();
                ListViewExcludes.AutoResizeColumns();
            }
            else
                MessageBox.Show(Application.Current.MainWindow,
                    $"Registry key ({addExcludeItem.RegistryPath}) already exists", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void menuItemEdit_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewExcludes.SelectedItems.Count == 0)
                return;

            var excItem = ListViewExcludes.SelectedItem as ExcludeItem;
            var pos = ExcludeArray.IndexOf(excItem);

            if (pos == -1)
            {
                MessageBox.Show(Application.Current.MainWindow, "The selected entry could not be found.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var editExcludeItem = new AddEditExcludeItem(excItem);
            if (!editExcludeItem.ShowDialog().GetValueOrDefault())
                return;

            ExcludeArray[pos] = editExcludeItem.ExcludeItem;

            MessageBox.Show(Application.Current.MainWindow, "Successfully updated exclude entry.", Utils.ProductName,
                MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateSettings();
            ListViewExcludes.AutoResizeColumns();
        }

        private void menuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewExcludes.SelectedItems.Count == 0)
                return;

            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            ExcludeArray.Remove(ListViewExcludes.SelectedItem as ExcludeItem);

            UpdateSettings();
            ListViewExcludes.AutoResizeColumns();
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            ScanBase.Model = Tree.Model as SectionModel;

            if (Scan.EnabledScanners.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow,
                    "At least one section must be selected in order for the Windows Registry to be scanned.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            ScanBase.MoveNext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Tree.ExpandAll();
            Tree.AutoResizeColumns();
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

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
    }
}
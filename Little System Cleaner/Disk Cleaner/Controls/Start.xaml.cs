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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Little_System_Cleaner.Disk_Cleaner.Helpers;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Little_System_Cleaner.Disk_Cleaner.Controls
{
    /// <summary>
    ///     Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : INotifyPropertyChanged
    {
        public Wizard ScanBase;

        public Start(Wizard sb)
        {
            InitializeComponent();

            ScanBase = sb;
        }

        public ObservableCollection<LviDrive> DrivesCollection => Wizard.DiskDrives;

        public ObservableCollection<LviFolder> IncFoldersCollection { get; } = new ObservableCollection<LviFolder>();

        public ObservableCollection<LviFolder> ExcFoldersCollection { get; } = new ObservableCollection<LviFolder>();

        public ObservableCollection<LviFile> ExcFilesCollection { get; } = new ObservableCollection<LviFile>();

        public bool? JunkFilesDelete
        {
            get { return Settings.Default.diskCleanerRemoveMode == 0; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerRemoveMode = 0;

                OnPropertyChanged("JunkFilesDelete");
                OnPropertyChanged("JunkFilesRecycle");
                OnPropertyChanged("JunkFilesMove");
            }
        }

        public bool? JunkFilesRecycle
        {
            get { return Settings.Default.diskCleanerRemoveMode == 1; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerRemoveMode = 1;

                OnPropertyChanged("JunkFilesDelete");
                OnPropertyChanged("JunkFilesRecycle");
                OnPropertyChanged("JunkFilesMove");
            }
        }

        public bool? JunkFilesMove
        {
            get { return Settings.Default.diskCleanerRemoveMode == 2; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerRemoveMode = 2;

                OnPropertyChanged("JunkFilesDelete");
                OnPropertyChanged("JunkFilesRecycle");
                OnPropertyChanged("JunkFilesMove");
            }
        }

        public string MoveFolder
        {
            get { return Settings.Default.diskCleanerMoveFolder; }
            set
            {
                Settings.Default.diskCleanerMoveFolder = value;
                OnPropertyChanged("MoveFolder");
            }
        }

        public bool? IgnoreWriteProtectedFiles
        {
            get { return Settings.Default.diskCleanerIgnoreWriteProtected; }
            set
            {
                Settings.Default.diskCleanerIgnoreWriteProtected = value.GetValueOrDefault();
                OnPropertyChanged("IgnoreWriteProtectedFiles");
            }
        }

        public bool? ZeroLengthFilesAreJunk
        {
            get { return Settings.Default.diskCleanerSearchZeroByte; }
            set
            {
                Settings.Default.diskCleanerSearchZeroByte = value.GetValueOrDefault();
                OnPropertyChanged("ZeroLengthFilesAreJunk");
            }
        }

        public string SearchFilter
        {
            get { return Settings.Default.diskCleanerSearchFilters; }
            set
            {
                Settings.Default.diskCleanerSearchFilters = value;

                OnPropertyChanged("SearchFilter");
            }
        }

        public bool? SearchFilterSafe
        {
            get { return Settings.Default.diskCleanerFilterMode == 0; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerFilterMode = 0;

                SearchFilterChange();

                OnPropertyChanged("SearchFilterSafe");
                OnPropertyChanged("SearchFilterMedium");
                OnPropertyChanged("SearchFilterAggressive");
            }
        }

        public bool? SearchFilterMedium
        {
            get { return Settings.Default.diskCleanerFilterMode == 1; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerFilterMode = 1;

                SearchFilterChange();

                OnPropertyChanged("SearchFilterSafe");
                OnPropertyChanged("SearchFilterMedium");
                OnPropertyChanged("SearchFilterAggressive");
            }
        }

        public bool? SearchFilterAggressive
        {
            get { return Settings.Default.diskCleanerFilterMode == 2; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerFilterMode = 2;

                SearchFilterChange();

                OnPropertyChanged("SearchFilterSafe");
                OnPropertyChanged("SearchFilterMedium");
                OnPropertyChanged("SearchFilterAggressive");
            }
        }

        public bool? AutoRemoveFiles
        {
            get { return Settings.Default.diskCleanerAutoClean; }
            set
            {
                Settings.Default.diskCleanerAutoClean = value.GetValueOrDefault();

                OnPropertyChanged("AutoRemoveFiles");
            }
        }

        public bool? AttributesHidden
        {
            get { return Settings.Default.diskCleanerSearchHidden; }
            set
            {
                Settings.Default.diskCleanerSearchHidden = value.GetValueOrDefault();

                OnPropertyChanged("AttributesHidden");
            }
        }

        public bool? AttributesReadonly
        {
            get { return Settings.Default.diskCleanerSearchReadOnly; }
            set
            {
                Settings.Default.diskCleanerSearchReadOnly = value.GetValueOrDefault();

                OnPropertyChanged("AttributesReadonly");
            }
        }

        public bool? AttributesArchive
        {
            get { return Settings.Default.diskCleanerSearchArchives; }
            set
            {
                Settings.Default.diskCleanerSearchArchives = value.GetValueOrDefault();

                OnPropertyChanged("AttributesArchive");
            }
        }

        public bool? AttributesSystem
        {
            get { return Settings.Default.diskCleanerSearchSystem; }
            set
            {
                Settings.Default.diskCleanerSearchSystem = value.GetValueOrDefault();

                OnPropertyChanged("AttributesSystem");
            }
        }


        public bool? FindFilesCreated
        {
            get { return Settings.Default.diskCleanerFindFilesMode == 0; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerFindFilesMode = 0;

                OnPropertyChanged("FindFilesCreated");
                OnPropertyChanged("FindFilesModified");
                OnPropertyChanged("FindFilesAccessed");
            }
        }

        public bool? FindFilesModified
        {
            get { return Settings.Default.diskCleanerFindFilesMode == 1; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerFindFilesMode = 1;

                OnPropertyChanged("FindFilesCreated");
                OnPropertyChanged("FindFilesModified");
                OnPropertyChanged("FindFilesAccessed");
            }
        }

        public bool? FindFilesAccessed
        {
            get { return Settings.Default.diskCleanerFindFilesMode == 2; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.diskCleanerFindFilesMode = 2;

                OnPropertyChanged("FindFilesCreated");
                OnPropertyChanged("FindFilesModified");
                OnPropertyChanged("FindFilesAccessed");
            }
        }

        public bool? FindFilesAfter
        {
            get { return Settings.Default.diskCleanerFindFilesAfter; }
            set
            {
                Settings.Default.diskCleanerFindFilesAfter = value.GetValueOrDefault();

                OnPropertyChanged("FindFilesAfter");
            }
        }

        public bool? FindFilesBefore
        {
            get { return Settings.Default.diskCleanerFindFilesBefore; }
            set
            {
                Settings.Default.diskCleanerFindFilesBefore = value.GetValueOrDefault();

                OnPropertyChanged("FindFilesBefore");
            }
        }

        public DateTime? FindFilesAfterDateTime
        {
            get { return Settings.Default.diskCleanerDateTimeAfter; }
            set
            {
                Settings.Default.diskCleanerDateTimeAfter = value.GetValueOrDefault();

                OnPropertyChanged("FindFilesAfterDateTime");
            }
        }

        public DateTime? FindFilesBeforeDateTime
        {
            get { return Settings.Default.diskCleanerDateTimeBefore; }
            set
            {
                Settings.Default.diskCleanerDateTimeBefore = value.GetValueOrDefault();

                OnPropertyChanged("FindFilesBeforeDateTime");
            }
        }

        public bool? FindFilesBySize
        {
            get { return Settings.Default.diskCleanerCheckFileSize; }
            set
            {
                Settings.Default.diskCleanerCheckFileSize = value.GetValueOrDefault();

                OnPropertyChanged("FindFilesBySize");
            }
        }

        public int? FindFilesBySizeAtLeast
        {
            get { return Settings.Default.diskCleanerCheckFileSizeLeast; }
            set
            {
                Settings.Default.diskCleanerCheckFileSizeLeast = value.GetValueOrDefault();

                OnPropertyChanged("FindFilesBySizeAtLeast");
            }
        }

        public int? FindFilesBySizeAtMost
        {
            get { return Settings.Default.diskCleanerCheckFileSizeMost; }
            set
            {
                Settings.Default.diskCleanerCheckFileSizeMost = value.GetValueOrDefault();

                OnPropertyChanged("FindFilesBySizeAtMost");
            }
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            UpdateOptions();

            ScanBase.SelectedDrives.Clear();

            foreach (var lvi in Wizard.DiskDrives.Where(lvi => lvi.Checked.GetValueOrDefault()))
            {
                ScanBase.SelectedDrives.Add(lvi.Tag as DriveInfo);
            }

            if (ScanBase.SelectedDrives.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "No drives selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchFilter))
            {
                MessageBox.Show(Application.Current.MainWindow, "At least one search filter must be specified",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ScanBase.MoveNext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Wizard.DrivesLoaded)
            {
                Wizard.DiskDrives = new ObservableCollection<LviDrive>();

                var winDir = Environment.GetFolderPath(Environment.SpecialFolder.System);

                DrivesCollection.AddRange(DriveInfo.GetDrives()
                    .Where(driveInfo => driveInfo.IsReady && driveInfo.DriveType == DriveType.Fixed)
                    .Select(
                        driveInfo =>
                            new LviDrive(winDir.Contains(driveInfo.Name), driveInfo.Name, driveInfo.DriveFormat,
                                Utils.ConvertSizeToString(driveInfo.TotalSize),
                                Utils.ConvertSizeToString(driveInfo.TotalFreeSpace), driveInfo)));

                if (Settings.Default.diskCleanerIncludedFolders == null)
                {
                    Settings.Default.diskCleanerIncludedFolders = new StringCollection
                    {
                        Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User),
                        Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine),
                        Environment.GetFolderPath(Environment.SpecialFolder.Recent),
                        Environment.GetFolderPath(Environment.SpecialFolder.InternetCache)
                    };
                }
            }

            OnPropertyChanged("DrivesCollection");

            // Excluded Dirs
            ExcFoldersCollection.AddRange(
                Settings.Default.diskCleanerExcludedDirs.Cast<string>()
                    .Select(excludeDir => new LviFolder {Folder = excludeDir}));
            //this.listViewExcludeFolders.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Excluded Files
            ExcFilesCollection.AddRange(
                Settings.Default.diskCleanerExcludedFileTypes.Cast<string>()
                    .Select(excludeFile => new LviFile {File = excludeFile}));
            //this.listViewFiles.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Included Folders
            IncFoldersCollection.AddRange(
                Settings.Default.diskCleanerIncludedFolders.Cast<string>()
                    .Select(includedFolder => new LviFolder {Folder = includedFolder}));
            //this.listViewIncFolders.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void UpdateOptions()
        {
            // Included Folders
            Settings.Default.diskCleanerIncludedFolders.Clear();
            Settings.Default.diskCleanerIncludedFolders.AddRange(
                IncFoldersCollection.Select(lvi => lvi.Folder).ToArray());

            // Excluded Folders
            Settings.Default.diskCleanerExcludedDirs.Clear();
            Settings.Default.diskCleanerExcludedDirs.AddRange(
                ExcFoldersCollection.Select(lvi => lvi.Folder).ToArray());

            // Excluded Files
            Settings.Default.diskCleanerExcludedFileTypes.Clear();
            Settings.Default.diskCleanerExcludedFileTypes.AddRange(
                ExcFilesCollection.Select(lvi => lvi.File).ToArray());
        }

        private void buttonAddIncFolder_Click(object sender, RoutedEventArgs e)
        {
            var addIncFolder = new AddIncludeFolder();
            addIncFolder.AddIncFolder += addIncFolder_AddIncFolder;
            addIncFolder.ShowDialog();
        }

        private void addIncFolder_AddIncFolder(object sender, AddIncFolderEventArgs e)
        {
            IncFoldersCollection.Add(new LviFolder {Folder = e.FolderPath});
        }

        private void buttonAddExcludeFolder_Click(object sender, RoutedEventArgs e)
        {
            var addExcFolder = new AddExcludeFolder();
            addExcFolder.AddExcludeFolderDelegate += addFolder_AddExcludeFolder;
            addExcFolder.ShowDialog();
        }

        private void addFolder_AddExcludeFolder(object sender, AddExcludeFolderEventArgs e)
        {
            ExcFoldersCollection.Add(new LviFolder {Folder = e.FolderPath});
        }

        private void buttonFilesAdd_Click(object sender, RoutedEventArgs e)
        {
            var addFileType = new AddExcludeFileType();
            addFileType.AddFileType += addFileType_AddFileType;
            addFileType.ShowDialog();
        }

        private void addFileType_AddFileType(object sender, AddFileTypeEventArgs e)
        {
            ExcFilesCollection.Add(new LviFile {File = e.FileType});
        }

        private void buttonRemExcludeFile_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewFiles.SelectedItems.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "No file was selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ExcFilesCollection.Remove(ListViewFiles.SelectedItems[0] as LviFile);
            }
        }

        private void buttonRemExcludeFolder_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewExcludeFolders.SelectedItems.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "No folder was selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ExcFoldersCollection.Remove(ListViewExcludeFolders.SelectedItems[0] as LviFolder);
            }
        }

        private void buttonRemIncFolder_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewIncFolders.SelectedItems.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "No folder was selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                IncFoldersCollection.Remove(ListViewIncFolders.SelectedItems[0] as LviFolder);
            }
        }

        private void SearchFilterChange()
        {
            var masks = new List<string>();
            string[] allFilters =
            {
                "*.tmp", "*.temp", "*.gid", "*.chk", "*.~*", "*.old", "*.fts", "*.ftg", "*.$$$",
                "*.---", "~*.*", "*.??$", "*.___", "*._mp", "*.dmp", "*.prv", "CHKLIST.MS", "*.$db", "*.??~", "*.db$",
                "chklist.*", "mscreate.dir", "*.wbk", "*log.txt", "*.err", "*.log", "*.sik", "*.bak", "*.ilk", "*.aps",
                "*.ncb", "*.pch", "*.?$?", "*.?~?", "*.^", "*._dd", "*._detmp", "0*.nch", "*.*_previous", "*_previous"
            };
            string[] filters = {};

            if (SearchFilterSafe.GetValueOrDefault())
                filters = new[] {"*.tmp", "*.temp", "*.gid", "*.chk", "*.~*"};
            else if (SearchFilterMedium.GetValueOrDefault())
                filters = new[]
                {
                    "*.tmp", "*.temp", "*.gid", "*.chk", "*.~*", "*.old", "*.fts", "*.ftg", "*.$$$", "*.---", "~*.*",
                    "*.??$", "*.___", "*._mp", "*.dmp", "*.prv", "CHKLIST.MS", "*.$db", "*.??~", "*.db$", "chklist.*",
                    "mscreate.dir"
                };
            else if (SearchFilterAggressive.GetValueOrDefault())
                filters = new[]
                {
                    "*.tmp", "*.temp", "*.gid", "*.chk", "*.~*", "*.old", "*.fts", "*.ftg", "*.$$$", "*.---", "~*.*",
                    "*.??$", "*.___", "*._mp", "*.dmp", "*.prv", "CHKLIST.MS", "*.$db", "*.??~", "*.db$", "chklist.*",
                    "mscreate.dir", "*.wbk", "*log.txt", "*.err", "*.log", "*.sik", "*.bak", "*.ilk", "*.aps", "*.ncb",
                    "*.pch", "*.?$?", "*.?~?", "*.^", "*._dd", "*._detmp", "0*.nch", "*.*_previous", "*_previous"
                };

            masks.AddRange(
                SearchFilter.Split(';')
                    .Select(mask => mask.Trim())
                    .Where(maskTrimmed => !string.IsNullOrEmpty(maskTrimmed) && !allFilters.Contains(maskTrimmed))
                );

            masks.AddRange(filters.Where(mask => !masks.Contains(mask)));

            SearchFilter = string.Join("; ", masks);
        }

        private void buttonSelectMoveFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDlg = new FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(MoveFolder))
                    folderBrowserDlg.SelectedPath = MoveFolder;

                if (folderBrowserDlg.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) != DialogResult.OK)
                    return;

                MoveFolder = folderBrowserDlg.SelectedPath;

                MessageBox.Show(Application.Current.MainWindow, "Folder to move junk files to has been updated",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
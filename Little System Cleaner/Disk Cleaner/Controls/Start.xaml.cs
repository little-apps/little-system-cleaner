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

using Little_System_Cleaner.Disk_Cleaner.Helpers;
using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace Little_System_Cleaner.Disk_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : UserControl, INotifyPropertyChanged
    {
        ObservableCollection<lviFolder> _incFoldersCollection = new ObservableCollection<lviFolder>();
        ObservableCollection<lviFolder> _excFoldersCollection = new ObservableCollection<lviFolder>();
        ObservableCollection<lviFile> _excFilesCollection = new ObservableCollection<lviFile>();

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public Wizard scanBase;

        public ObservableCollection<lviDrive> DrivesCollection
        {
            get { return Wizard.DiskDrives; }
        }

        public ObservableCollection<lviFolder> IncFoldersCollection
        {
            get { return this._incFoldersCollection; }
        }
        public ObservableCollection<lviFolder> ExcFoldersCollection
        {
            get { return this._excFoldersCollection; }
        }
        public ObservableCollection<lviFile> ExcFilesCollection
        {
            get { return this._excFilesCollection; }
        }

        public bool? JunkFilesDelete
        {
            get { return (Properties.Settings.Default.diskCleanerRemoveMode == 0); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerRemoveMode = 0;

                this.OnPropertyChanged("JunkFilesDelete");
                this.OnPropertyChanged("JunkFilesRecycle");
                this.OnPropertyChanged("JunkFilesMove");
            }
        }

        public bool? JunkFilesRecycle
        {
            get { return (Properties.Settings.Default.diskCleanerRemoveMode == 1); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerRemoveMode = 1;

                this.OnPropertyChanged("JunkFilesDelete");
                this.OnPropertyChanged("JunkFilesRecycle");
                this.OnPropertyChanged("JunkFilesMove");
            }
        }

        public bool? JunkFilesMove
        {
            get { return (Properties.Settings.Default.diskCleanerRemoveMode == 2); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerRemoveMode = 2;

                this.OnPropertyChanged("JunkFilesDelete");
                this.OnPropertyChanged("JunkFilesRecycle");
                this.OnPropertyChanged("JunkFilesMove");
            }
        }

        public string MoveFolder
        {
            get { return Properties.Settings.Default.diskCleanerMoveFolder; }
            set
            {
                Properties.Settings.Default.diskCleanerMoveFolder = value;
                this.OnPropertyChanged("MoveFolder");
            }
        }

        public bool? AutoCreateSysRestorePoint
        {
            get { return Properties.Settings.Default.diskCleanerAutoRestorePoints; }
            set
            {
                Properties.Settings.Default.diskCleanerAutoRestorePoints = value.GetValueOrDefault();
                this.OnPropertyChanged("AutoCreateSysRestorePoint");
            }
        }

        public bool? IgnoreWriteProtectedFiles
        {
            get { return Properties.Settings.Default.diskCleanerIgnoreWriteProtected; }
            set
            {
                Properties.Settings.Default.diskCleanerIgnoreWriteProtected = value.GetValueOrDefault();
                this.OnPropertyChanged("IgnoreWriteProtectedFiles");
            }
        }

        public bool? ZeroLengthFilesAreJunk
        {
            get { return Properties.Settings.Default.diskCleanerSearchZeroByte; }
            set
            {
                Properties.Settings.Default.diskCleanerSearchZeroByte = value.GetValueOrDefault();
                this.OnPropertyChanged("ZeroLengthFilesAreJunk");
            }
        }

        public string SearchFilter
        {
            get { return Properties.Settings.Default.diskCleanerSearchFilters; }
            set
            {
                Properties.Settings.Default.diskCleanerSearchFilters = value;

                this.OnPropertyChanged("SearchFilter");
            }
        }

        public bool? SearchFilterSafe
        {
            get { return (Properties.Settings.Default.diskCleanerFilterMode == 0); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerFilterMode = 0;

                this.SearchFilterChange();

                this.OnPropertyChanged("SearchFilterSafe");
                this.OnPropertyChanged("SearchFilterMedium");
                this.OnPropertyChanged("SearchFilterAggressive");
            }
        }

        public bool? SearchFilterMedium
        {
            get { return (Properties.Settings.Default.diskCleanerFilterMode == 1); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerFilterMode = 1;

                this.SearchFilterChange();

                this.OnPropertyChanged("SearchFilterSafe");
                this.OnPropertyChanged("SearchFilterMedium");
                this.OnPropertyChanged("SearchFilterAggressive");
            }
        }

        public bool? SearchFilterAggressive
        {
            get { return (Properties.Settings.Default.diskCleanerFilterMode == 2); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerFilterMode = 2;

                this.SearchFilterChange();

                this.OnPropertyChanged("SearchFilterSafe");
                this.OnPropertyChanged("SearchFilterMedium");
                this.OnPropertyChanged("SearchFilterAggressive");
            }
        }

        public bool? AutoRemoveFiles
        {
            get { return Properties.Settings.Default.diskCleanerAutoClean; }
            set
            {
                Properties.Settings.Default.diskCleanerAutoClean = value.GetValueOrDefault();

                this.OnPropertyChanged("AutoRemoveFiles");
            }
        }

        public bool? AttributesHidden
        {
            get { return Properties.Settings.Default.diskCleanerSearchHidden; }
            set
            {
                Properties.Settings.Default.diskCleanerSearchHidden = value.GetValueOrDefault();

                this.OnPropertyChanged("AttributesHidden");
            }
        }

        public bool? AttributesReadonly
        {
            get { return Properties.Settings.Default.diskCleanerSearchReadOnly; }
            set
            {
                Properties.Settings.Default.diskCleanerSearchReadOnly = value.GetValueOrDefault();

                this.OnPropertyChanged("AttributesReadonly");
            }
        }

        public bool? AttributesArchive
        {
            get { return Properties.Settings.Default.diskCleanerSearchArchives; }
            set
            {
                Properties.Settings.Default.diskCleanerSearchArchives = value.GetValueOrDefault();

                this.OnPropertyChanged("AttributesArchive");
            }
        }

        public bool? AttributesSystem
        {
            get { return Properties.Settings.Default.diskCleanerSearchSystem; }
            set
            {
                Properties.Settings.Default.diskCleanerSearchSystem = value.GetValueOrDefault();

                this.OnPropertyChanged("AttributesSystem");
            }
        }


        public bool? FindFilesCreated
        {
            get { return (Properties.Settings.Default.diskCleanerFindFilesMode == 0); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerFindFilesMode = 0;

                this.OnPropertyChanged("FindFilesCreated");
                this.OnPropertyChanged("FindFilesModified");
                this.OnPropertyChanged("FindFilesAccessed");
            }
        }

        public bool? FindFilesModified
        {
            get { return (Properties.Settings.Default.diskCleanerFindFilesMode == 1); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerFindFilesMode = 1;

                this.OnPropertyChanged("FindFilesCreated");
                this.OnPropertyChanged("FindFilesModified");
                this.OnPropertyChanged("FindFilesAccessed");
            }
        }

        public bool? FindFilesAccessed
        {
            get { return (Properties.Settings.Default.diskCleanerFindFilesMode == 2); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.diskCleanerFindFilesMode = 2;

                this.OnPropertyChanged("FindFilesCreated");
                this.OnPropertyChanged("FindFilesModified");
                this.OnPropertyChanged("FindFilesAccessed");
            }
        }

        public bool? FindFilesAfter
        {
            get { return Properties.Settings.Default.diskCleanerFindFilesAfter; }
            set
            {
                Properties.Settings.Default.diskCleanerFindFilesAfter = value.GetValueOrDefault();

                this.OnPropertyChanged("FindFilesAfter");
            }
        }

        public bool? FindFilesBefore
        {
            get { return Properties.Settings.Default.diskCleanerFindFilesBefore; }
            set
            {
                Properties.Settings.Default.diskCleanerFindFilesBefore = value.GetValueOrDefault();

                this.OnPropertyChanged("FindFilesBefore");
            }
        }

        public DateTime? FindFilesAfterDateTime
        {
            get { return Properties.Settings.Default.diskCleanerDateTimeAfter; }
            set
            {
                Properties.Settings.Default.diskCleanerDateTimeAfter = value.GetValueOrDefault();

                this.OnPropertyChanged("FindFilesAfterDateTime");
            }
        }

        public DateTime? FindFilesBeforeDateTime
        {
            get { return Properties.Settings.Default.diskCleanerDateTimeBefore; }
            set
            {
                Properties.Settings.Default.diskCleanerDateTimeBefore = value.GetValueOrDefault();

                this.OnPropertyChanged("FindFilesBeforeDateTime");
            }
        }

        public bool? FindFilesBySize
        {
            get { return Properties.Settings.Default.diskCleanerCheckFileSize; }
            set
            {
                Properties.Settings.Default.diskCleanerCheckFileSize = value.GetValueOrDefault();

                this.OnPropertyChanged("FindFilesBySize");
            }
        }

        public int? FindFilesBySizeAtLeast
        {
            get { return Properties.Settings.Default.diskCleanerCheckFileSizeLeast; }
            set
            {
                Properties.Settings.Default.diskCleanerCheckFileSizeLeast = value.GetValueOrDefault();

                this.OnPropertyChanged("FindFilesBySizeAtLeast");
            }
        }

        public int? FindFilesBySizeAtMost
        {
            get { return Properties.Settings.Default.diskCleanerCheckFileSizeMost; }
            set
            {
                Properties.Settings.Default.diskCleanerCheckFileSizeMost = value.GetValueOrDefault();

                this.OnPropertyChanged("FindFilesBySizeAtMost");
            }
        }

        public Start(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateOptions();

            this.scanBase.selectedDrives.Clear();

            foreach (lviDrive lvi in Wizard.DiskDrives)
            {
                if (lvi.Checked.GetValueOrDefault())
                    this.scanBase.selectedDrives.Add(lvi.Tag as DriveInfo);
            }

            if (this.scanBase.selectedDrives.Count == 0)
            {
                System.Windows.MessageBox.Show(App.Current.MainWindow, "No drives selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.SearchFilter))
            {
                System.Windows.MessageBox.Show(App.Current.MainWindow, "At least one search filter must be specified", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.scanBase.MoveNext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Wizard.DrivesLoaded)
            {
                Wizard.DiskDrives = new ObservableCollection<lviDrive>();

                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
                {
                    if (!driveInfo.IsReady || driveInfo.DriveType != DriveType.Fixed)
                        continue;

                    string freeSpace = Utils.ConvertSizeToString(driveInfo.TotalFreeSpace);
                    string totalSpace = Utils.ConvertSizeToString(driveInfo.TotalSize);

                    bool isChecked = false;
                    if (winDir.Contains(driveInfo.Name))
                        isChecked = true;

                    lviDrive listViewItem = new lviDrive(isChecked, driveInfo.Name, driveInfo.DriveFormat, totalSpace, freeSpace, driveInfo);

                    this.DrivesCollection.Add(listViewItem);
                }

                if (Properties.Settings.Default.diskCleanerIncludedFolders == null)
                {
                    Properties.Settings.Default.diskCleanerIncludedFolders = new System.Collections.Specialized.StringCollection();

                    Properties.Settings.Default.diskCleanerIncludedFolders.Add(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User));
                    Properties.Settings.Default.diskCleanerIncludedFolders.Add(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine));
                    Properties.Settings.Default.diskCleanerIncludedFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
                    Properties.Settings.Default.diskCleanerIncludedFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache));
                }
            }

            this.OnPropertyChanged("DrivesCollection");

            // Excluded Dirs
            foreach (string excludeDir in Properties.Settings.Default.diskCleanerExcludedDirs)
                this._excFoldersCollection.Add(new lviFolder() { Folder = excludeDir });
            //this.listViewExcludeFolders.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Excluded Files
            foreach (string excludeFile in Properties.Settings.Default.diskCleanerExcludedFileTypes)
                this._excFilesCollection.Add(new lviFile() { File = excludeFile });
            //this.listViewFiles.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Included Folders
            foreach (string includedFolder in Properties.Settings.Default.diskCleanerIncludedFolders)
                this._incFoldersCollection.Add(new lviFolder() { Folder = includedFolder });
            //this.listViewIncFolders.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void UpdateOptions()
        {
            // Included Folders
            Properties.Settings.Default.diskCleanerIncludedFolders.Clear();
            foreach (lviFolder lvi in this._incFoldersCollection)
                Properties.Settings.Default.diskCleanerIncludedFolders.Add(lvi.Folder);

            // Excluded Folders
            Properties.Settings.Default.diskCleanerExcludedDirs.Clear();
            foreach (lviFolder lvi in this._excFoldersCollection)
                Properties.Settings.Default.diskCleanerExcludedDirs.Add(lvi.Folder);

            // Excluded Files
            Properties.Settings.Default.diskCleanerExcludedFileTypes.Clear();
            foreach (lviFile lvi in this._excFilesCollection)
                Properties.Settings.Default.diskCleanerExcludedFileTypes.Add(lvi.File);
        }

        private void buttonAddIncFolder_Click(object sender, RoutedEventArgs e)
        {
            AddIncludeFolder addIncFolder = new AddIncludeFolder();
            addIncFolder.AddIncFolder += new AddIncFolderEventHandler(addIncFolder_AddIncFolder);
            addIncFolder.ShowDialog();
        }
        void addIncFolder_AddIncFolder(object sender, AddIncFolderEventArgs e)
        {
            this._incFoldersCollection.Add(new lviFolder() { Folder = e.folderPath });
        }

        private void buttonAddExcludeFolder_Click(object sender, RoutedEventArgs e)
        {
            AddExcludeFolder addExcFolder = new AddExcludeFolder();
            addExcFolder.AddExcludeFolderDelegate += new AddExcludeFolderEventHandler(addFolder_AddExcludeFolder);
            addExcFolder.ShowDialog();
        }

        void addFolder_AddExcludeFolder(object sender, AddExcludeFolderEventArgs e)
        {
            this._excFoldersCollection.Add(new lviFolder() { Folder = e.folderPath });
        }

        private void buttonFilesAdd_Click(object sender, RoutedEventArgs e)
        {
            AddExcludeFileType addFileType = new AddExcludeFileType();
            addFileType.AddFileType += new AddFileTypeEventHandler(addFileType_AddFileType);
            addFileType.ShowDialog();
        }

        void addFileType_AddFileType(object sender, AddFileTypeEventArgs e)
        {
            this._excFilesCollection.Add(new lviFile() { File = e.fileType });
        }

        private void buttonRemExcludeFile_Click(object sender, RoutedEventArgs e)
        {
            if (this.listViewFiles.SelectedItems[0] != null)
            {
                if (System.Windows.MessageBox.Show(Window.GetWindow(this), "Are you sure?", System.Windows.Forms.Application.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
                {
                    this._excFilesCollection.Remove(this.listViewFiles.SelectedItems[0] as lviFile);
                }
            }
        }

        private void buttonRemExcludeFolder_Click(object sender, RoutedEventArgs e)
        {
            if (this.listViewExcludeFolders.SelectedItems[0] != null)
            {
                if (System.Windows.MessageBox.Show(Window.GetWindow(this), "Are you sure?", System.Windows.Forms.Application.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
                {
                    this._excFoldersCollection.Remove(this.listViewExcludeFolders.SelectedItems[0] as lviFolder);
                }
            }
        }

        private void buttonRemIncFolder_Click(object sender, RoutedEventArgs e)
        {
            if (this.listViewIncFolders.SelectedItems[0] != null)
            {
                if (System.Windows.MessageBox.Show(Window.GetWindow(this), "Are you sure?", System.Windows.Forms.Application.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
                {
                    this._incFoldersCollection.Remove(this.listViewIncFolders.SelectedItems[0] as lviFolder);
                }
            }
        }

        private void SearchFilterChange()
        {
            List<string> masks = new List<string>();
            string[] allFilters = new string[] { "*.tmp", "*.temp", "*.gid", "*.chk", "*.~*", "*.old", "*.fts", "*.ftg", "*.$$$", "*.---", "~*.*", "*.??$", "*.___", "*._mp", "*.dmp", "*.prv", "CHKLIST.MS", "*.$db", "*.??~", "*.db$", "chklist.*", "mscreate.dir", "*.wbk", "*log.txt", "*.err", "*.log", "*.sik", "*.bak", "*.ilk", "*.aps", "*.ncb", "*.pch", "*.?$?", "*.?~?", "*.^", "*._dd", "*._detmp", "0*.nch", "*.*_previous", "*_previous" };
            string[] filters = new string[] { };

            if (this.SearchFilterSafe.GetValueOrDefault())
                filters = new string[] { "*.tmp", "*.temp", "*.gid", "*.chk", "*.~*" };
            else if (this.SearchFilterMedium.GetValueOrDefault())
                filters = new string[] { "*.tmp", "*.temp", "*.gid", "*.chk", "*.~*", "*.old", "*.fts", "*.ftg", "*.$$$", "*.---", "~*.*", "*.??$", "*.___", "*._mp", "*.dmp", "*.prv", "CHKLIST.MS", "*.$db", "*.??~", "*.db$", "chklist.*", "mscreate.dir" };
            else if (this.SearchFilterAggressive.GetValueOrDefault())
                filters = new string[] { "*.tmp", "*.temp", "*.gid", "*.chk", "*.~*", "*.old", "*.fts", "*.ftg", "*.$$$", "*.---", "~*.*", "*.??$", "*.___", "*._mp", "*.dmp", "*.prv", "CHKLIST.MS", "*.$db", "*.??~", "*.db$", "chklist.*", "mscreate.dir", "*.wbk", "*log.txt", "*.err", "*.log", "*.sik", "*.bak", "*.ilk", "*.aps", "*.ncb", "*.pch", "*.?$?", "*.?~?", "*.^", "*._dd", "*._detmp", "0*.nch", "*.*_previous", "*_previous" };

            foreach (string mask in this.SearchFilter.Split(';'))
            {
                string maskTrimmed = mask.Trim();

                if (!string.IsNullOrWhiteSpace(mask) && !allFilters.Contains(maskTrimmed))
                    masks.Add(maskTrimmed);
            }

            foreach (string mask in filters)
            {
                if (!masks.Contains(mask))
                    masks.Add(mask);
            }

            this.SearchFilter = string.Join("; ", masks);
        }

        private void buttonSelectMoveFolder_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowserDlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(this.MoveFolder))
                    folderBrowserDlg.SelectedPath = this.MoveFolder;

                if (folderBrowserDlg.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) == System.Windows.Forms.DialogResult.OK)
                {
                    this.MoveFolder = folderBrowserDlg.SelectedPath;

                    System.Windows.MessageBox.Show(App.Current.MainWindow, "Folder to move junk files to has been updated", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}

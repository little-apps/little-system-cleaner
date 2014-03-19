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

using Little_System_Cleaner.Disk_Cleaner.Controls.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using Xceed.Wpf.Toolkit;

namespace Little_System_Cleaner.Disk_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : UserControl
    {
        ObservableCollection<lviDrive> _drivesCollection = new ObservableCollection<lviDrive>();
        ObservableCollection<lviFolder> _incFoldersCollection = new ObservableCollection<lviFolder>();
        ObservableCollection<lviFolder> _excFoldersCollection = new ObservableCollection<lviFolder>();
        ObservableCollection<lviFile> _excFilesCollection = new ObservableCollection<lviFile>();

        public Wizard scanBase;
        public ObservableCollection<lviDrive> DrivesCollection
        {
            get { return this._drivesCollection; }
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

        public Start(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateOptions();

            this.scanBase.selectedDrives.Clear();

            foreach (lviDrive lvi in Properties.Settings.Default.diskCleanerDiskDrives)
            {
                if (lvi.Checked.GetValueOrDefault())
                    this.scanBase.selectedDrives.Add(lvi.Tag as DriveInfo);
            }

            if (this.scanBase.selectedDrives.Count == 0)
            {
                System.Windows.MessageBox.Show(Window.GetWindow(this), "No drives selected", System.Windows.Forms.Application.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.scanBase.MoveNext();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this._drivesCollection.Clear();

            // Drives
            foreach (lviDrive lvi in Properties.Settings.Default.diskCleanerDiskDrives)
                this._drivesCollection.Add(lvi);

            // Advanced
            this.checkBoxArchive.IsChecked = Properties.Settings.Default.diskCleanerSearchArchives;
            this.checkBoxHidden.IsChecked = Properties.Settings.Default.diskCleanerSearchHidden;
            this.checkBoxReadOnly.IsChecked = Properties.Settings.Default.diskCleanerSearchReadOnly;
            this.checkBoxSystem.IsChecked = Properties.Settings.Default.diskCleanerSearchSystem;

            if (Properties.Settings.Default.diskCleanerFindFilesMode == 0)
                this.radioButtonFindCreated.IsChecked = true;
            else if (Properties.Settings.Default.diskCleanerFindFilesMode == 1)
                this.radioButtonFindModified.IsChecked = true;
            else if (Properties.Settings.Default.diskCleanerFindFilesMode == 2)
                this.radioButtonFindAccessed.IsChecked = true;

            this.checkBoxFindAfter.IsChecked = Properties.Settings.Default.diskCleanerFindFilesAfter;
            this.checkBoxFindBefore.IsChecked = Properties.Settings.Default.diskCleanerFindFilesBefore;

            this.dateTimePickerAfter.Value = Properties.Settings.Default.diskCleanerDateTimeAfter;
            this.dateTimePickerBefore.Value = Properties.Settings.Default.diskCleanerDateTimeBefore;

            this.checkBoxSize.IsChecked = Properties.Settings.Default.diskCleanerCheckFileSize;
            this.numericUpDownSizeAtLeast.Value = (int?)Convert.ToDecimal(Properties.Settings.Default.diskCleanerCheckFileSizeLeast);
            this.numericUpDownSizeAtMost.Value = (int?)Convert.ToDecimal(Properties.Settings.Default.diskCleanerCheckFileSizeMost);

            // Search Options
            this.checkBoxWriteProtected.IsChecked = Properties.Settings.Default.diskCleanerIgnoreWriteProtected;
            this.checkBoxZeroLength.IsChecked = Properties.Settings.Default.diskCleanerSearchZeroByte;

            if (Properties.Settings.Default.diskCleanerFilterMode == 0)
                this.radioButtonFilterSafe.IsChecked = true;
            else if (Properties.Settings.Default.diskCleanerFilterMode == 1)
                this.radioButtonFilterMed.IsChecked = true;
            else if (Properties.Settings.Default.diskCleanerFilterMode == 2)
                this.radioButtonFilterAgg.IsChecked = true;

            this.textBoxSearchFilters.Text = Properties.Settings.Default.diskCleanerSearchFilters;

            this.checkBoxAutoClean.IsChecked = Properties.Settings.Default.diskCleanerAutoClean;

            // Removal
            if (Properties.Settings.Default.diskCleanerRemoveMode == 0)
                this.radioButtonRemove.IsChecked = true;
            else if (Properties.Settings.Default.diskCleanerRemoveMode == 1)
                this.radioButtonRecycle.IsChecked = true;
            else if (Properties.Settings.Default.diskCleanerRemoveMode == 2)
                this.radioButtonMove.IsChecked = true;
            this.textBoxMoveFolder.Text = Properties.Settings.Default.diskCleanerMoveFolder;
            this.checkBoxAutoSysRestore.IsChecked = Properties.Settings.Default.diskCleanerAutoRestorePoints;

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
            // Drives
            Properties.Settings.Default.diskCleanerDiskDrives = new System.Collections.ArrayList(this._drivesCollection);

            // Searching
            if (this.radioButtonFilterSafe.IsChecked == true)
                Properties.Settings.Default.diskCleanerFilterMode = 0;
            else if (this.radioButtonFilterMed.IsChecked == true)
                Properties.Settings.Default.diskCleanerFilterMode = 1;
            else if (this.radioButtonFilterAgg.IsChecked == true)
                Properties.Settings.Default.diskCleanerFilterMode = 2;

            Properties.Settings.Default.diskCleanerSearchFilters = this.textBoxSearchFilters.Text;

            Properties.Settings.Default.diskCleanerIgnoreWriteProtected = this.checkBoxWriteProtected.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.diskCleanerSearchZeroByte = this.checkBoxZeroLength.IsChecked.GetValueOrDefault();

            Properties.Settings.Default.diskCleanerAutoClean = this.checkBoxAutoClean.IsChecked.GetValueOrDefault();

            // Advanced
            Properties.Settings.Default.diskCleanerSearchHidden = this.checkBoxHidden.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.diskCleanerSearchArchives = this.checkBoxArchive.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.diskCleanerSearchReadOnly = this.checkBoxReadOnly.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.diskCleanerSearchSystem = this.checkBoxSystem.IsChecked.GetValueOrDefault();

            if (this.radioButtonFindCreated.IsChecked == true)
                Properties.Settings.Default.diskCleanerFindFilesMode = 0;
            else if (this.radioButtonFindModified.IsChecked == true)
                Properties.Settings.Default.diskCleanerFindFilesMode = 1;
            else if (this.radioButtonFindAccessed.IsChecked == true)
                Properties.Settings.Default.diskCleanerFindFilesMode = 2;

            Properties.Settings.Default.diskCleanerFindFilesAfter = this.checkBoxFindAfter.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.diskCleanerFindFilesBefore = this.checkBoxFindBefore.IsChecked.GetValueOrDefault();

            Properties.Settings.Default.diskCleanerDateTimeAfter = this.dateTimePickerAfter.Value.GetValueOrDefault();
            Properties.Settings.Default.diskCleanerDateTimeBefore = this.dateTimePickerBefore.Value.GetValueOrDefault();

            Properties.Settings.Default.diskCleanerCheckFileSize = this.checkBoxSize.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.diskCleanerCheckFileSizeLeast = Convert.ToInt32(this.numericUpDownSizeAtLeast.Value);
            Properties.Settings.Default.diskCleanerCheckFileSizeMost = Convert.ToInt32(this.numericUpDownSizeAtMost.Value);

            // Removal
            if (this.radioButtonRemove.IsChecked == true)
                Properties.Settings.Default.diskCleanerRemoveMode = 0;
            else if (this.radioButtonRecycle.IsChecked == true)
                Properties.Settings.Default.diskCleanerRemoveMode = 1;
            else if (this.radioButtonMove.IsChecked == true)
                Properties.Settings.Default.diskCleanerRemoveMode = 2;

            Properties.Settings.Default.diskCleanerMoveFolder = this.textBoxMoveFolder.Text;

            Properties.Settings.Default.diskCleanerAutoRestorePoints = this.checkBoxAutoSysRestore.IsChecked.GetValueOrDefault();

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
    }

    public class lviDrive
    {
        public bool? Checked
        {
            get;
            set;
        }

        public string Drive
        {
            get;
            set;
        }

        public string DriveFormat
        {
            get;
            set;
        }

        public string DriveCapacity
        {
            get;
            set;
        }

        public string DriveFreeSpace
        {
            get;
            set;
        }

        public object Tag
        {
            get;
            set;
        }

        public lviDrive(bool isChecked, string driveName, string driveFormat, string driveCapacity, string driveFreeSpace, DriveInfo di)
        {
            this.Checked = isChecked;
            this.Drive = driveName;
            this.DriveFormat = driveFormat;
            this.DriveCapacity = driveCapacity;
            this.DriveFreeSpace = driveFreeSpace;
            this.Tag = di;
        }
    }

    public class lviFolder
    {
        public string Folder
        {
            get;
            set;
        }

        public lviFolder()
        {

        }
    }

    public class lviFile
    {
        public string File
        {
            get;
            set;
        }

        public lviFile()
        {

        }
    }
}

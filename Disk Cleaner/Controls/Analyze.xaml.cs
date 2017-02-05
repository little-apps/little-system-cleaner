﻿/*
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using Disk_Cleaner.Annotations;
using Disk_Cleaner.Helpers;
using Shared;

namespace Disk_Cleaner.Controls
{
    /// <summary>
    ///     Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Analyze : INotifyPropertyChanged
    {
        private readonly Task _taskMain;
        private CancellationTokenSource _cancellationTokenSource;
        private string _currentFile;

        public Wizard ScanBase;

        public string CurrentFile
        {
            get
            {
                return _currentFile;
            }
            set
            {
                _currentFile = value;
                OnPropertyChanged(nameof(CurrentFile));
            }
        }

        public string FilesFound => $"Files Found: {Wizard.FileList?.Count}";

        public Analyze(Wizard sb)
        {
            InitializeComponent();

            ScanBase = sb;

            if (Wizard.FileList == null)
                Wizard.FileList = new ObservableCollection<ProblemFile>();

            Wizard.FileList.CollectionChanged += FileListOnCollectionChanged;

            Wizard.FileList.Clear();

            // Set scan start time
            Wizard.ScanStartTime = DateTime.Now;

            _cancellationTokenSource = new CancellationTokenSource();
            _taskMain = new Task(AnalyzeDisk, _cancellationTokenSource.Token);
            _taskMain.Start();
        }

        private void FileListOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => FileListOnCollectionChanged(sender, e));
                return;
            }

            OnPropertyChanged(nameof(FilesFound));
        }

        private void AnalyzeDisk()
        {
            var completedSuccessfully = false;

            try
            {
                // Show taskbar progress bar
                Dispatcher.BeginInvoke(
                    new Action(() => Utils.TaskbarProgressState = TaskbarItemProgressState.Indeterminate));

                foreach (var driveInfo in ScanBase.SelectedDrives)
                {
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    ScanFiles(driveInfo.RootDirectory);
                }

                Utils.Watcher.EventPeriod("Disk Cleaner", "Analyze",
                    (int)DateTime.Now.Subtract(Wizard.ScanStartTime).TotalSeconds, true);

                if (Wizard.FileList.Count > 0)
                    completedSuccessfully = true;
                else
                    Utils.MessageBoxThreadSafe("No problem files were detected", Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                // Will end up here if user accepts to change tab
                // No need to change tab

                completedSuccessfully = false;
            }
            finally
            {
                ResetInfo(completedSuccessfully);

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void ResetInfo(bool success)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Utils.TaskbarProgressState = TaskbarItemProgressState.None;
                ProgressBar.IsIndeterminate = false;
                TextBlockPleaseWait.Visibility = Visibility.Hidden;
            }));

            CancelAnalyze();
            CurrentFile = "";

            if (success)
            {
                CurrentFile = "View the results by clicking \"Continue\" below.";
                Dispatcher.Invoke(new Action(() => ButtonContinue.IsEnabled = true));
            }
            else
            {
                CurrentFile = "Click \"Cancel\" to go back to the previous screen.";
            }
        }

        private void ScanFiles(DirectoryInfo parentInfo)
        {
            try
            {
                foreach (
                    var fileInfo in
                        parentInfo.GetFiles().TakeWhile(fileInfo => !_cancellationTokenSource.IsCancellationRequested))
                {
                    try
                    {
                        CurrentFile = fileInfo.FullName;

                        // Check if file is exclude
                        if (FileTypeIsExcluded(fileInfo.Name))
                            continue;

                        // Check for zero-byte files
                        if (Settings.Default.diskCleanerSearchZeroByte)
                        {
                            if (fileInfo.Length == 0)
                            {
                                Wizard.FileList.Add(new ProblemFile(fileInfo));
                                continue;
                            }
                        }

                        // Check if file matches types
                        if (!CompareWildcards(fileInfo.Name, Settings.Default.diskCleanerSearchFilters))
                            continue;

                        // Check if file is in use or write protected
                        if (Settings.Default.diskCleanerIgnoreWriteProtected && fileInfo.IsReadOnly)
                            continue;

                        if (Settings.Default.diskCleanerIgnoreWriteProtected && IsFileLocked(fileInfo))
                            continue;

                        // Check file attributes
                        if (!FileCheckAttributes(fileInfo))
                            continue;

                        // Check file dates
                        if (Settings.Default.diskCleanerFindFilesAfter || Settings.Default.diskCleanerFindFilesBefore)
                            if (!FileCheckDate(fileInfo))
                                continue;

                        // Check file size
                        if (Settings.Default.diskCleanerCheckFileSize)
                            if (!FileCheckSize(fileInfo))
                                continue;

                        AddToFileList(new ProblemFile(fileInfo));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check of file...");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to scan files.");
            }

            try
            {
                foreach (
                    var childInfo in
                        parentInfo.GetDirectories()
                            .TakeWhile(childInfo => !_cancellationTokenSource.IsCancellationRequested))
                {
                    try
                    {
                        var dirPath = childInfo.FullName;

                        if (FolderIsIncluded(dirPath) && !FolderIsExcluded(dirPath))
                        {
                            foreach (var fileInfo in childInfo.GetFiles())
                            {
                                AddToFileList(new ProblemFile(fileInfo));
                            }

                            continue;
                        }

                        if (!FolderIsExcluded(dirPath))
                            ScanFiles(childInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message +
                                        "\nSkipping check of directory...");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to scan directories.");
            }
        }

        private void AddToFileList(ProblemFile problemFile)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AddToFileList(problemFile));
                return;
            }

            Wizard.FileList.Add(problemFile);
        }

        private static bool FileCheckSize(FileInfo fileInfo)
        {
            try
            {
                var fileSize = fileInfo.Length / 1024;

                if (Settings.Default.diskCleanerCheckFileSizeLeast > 0)
                    if (fileSize <= Settings.Default.diskCleanerCheckFileSizeLeast)
                        return false;

                if (Settings.Default.diskCleanerCheckFileSizeMost > 0)
                    if (fileSize >= Settings.Default.diskCleanerCheckFileSizeMost)
                        return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check file size.");
            }

            return true;
        }

        /// <summary>
        ///     Checks if file is in specified date/time range
        /// </summary>
        /// <param name="fileInfo">File information</param>
        /// <returns>True if file is in date/time range</returns>
        private static bool FileCheckDate(FileSystemInfo fileInfo)
        {
            var dateTimeFile = DateTime.MinValue;
            var ret = false;

            try
            {
                switch (Settings.Default.diskCleanerFindFilesMode)
                {
                    case 0:
                        dateTimeFile = fileInfo.CreationTime;
                        break;

                    case 1:
                        dateTimeFile = fileInfo.LastWriteTime;
                        break;

                    case 2:
                        dateTimeFile = fileInfo.LastAccessTime;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check file time.");
                return false;
            }

            if (Settings.Default.diskCleanerFindFilesAfter)
            {
                if (DateTime.Compare(dateTimeFile, Settings.Default.diskCleanerDateTimeAfter) >= 0)
                    ret = true;
            }

            if (Settings.Default.diskCleanerFindFilesBefore)
            {
                if (DateTime.Compare(dateTimeFile, Settings.Default.diskCleanerDateTimeBefore) <= 0)
                    ret = true;
            }

            return ret;
        }

        /// <summary>
        ///     Checks file attributes to match what user specified to search for
        /// </summary>
        /// <param name="fileInfo">File Information</param>
        /// <returns>True if file matches attributes</returns>
        private bool FileCheckAttributes(FileInfo fileInfo)
        {
            FileAttributes fileAttribs;

            try
            {
                fileAttribs = fileInfo.Attributes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check file attributes.");
                return false;
            }

            if (!Settings.Default.diskCleanerSearchHidden &&
                ((fileAttribs & FileAttributes.Hidden) == FileAttributes.Hidden))
                return false;

            if (!Settings.Default.diskCleanerSearchArchives &&
                ((fileAttribs & FileAttributes.Archive) == FileAttributes.Archive))
                return false;

            if (!Settings.Default.diskCleanerSearchReadOnly &&
                ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))
                return false;

            if (!Settings.Default.diskCleanerSearchSystem &&
                ((fileAttribs & FileAttributes.System) == FileAttributes.System))
                return false;

            return true;
        }

        /// <summary>
        ///     Checks if file is in use
        /// </summary>
        /// <param name="fileInfo">FileInfo class</param>
        /// <returns>True if file is in use</returns>
        private static bool IsFileLocked(FileInfo fileInfo)
        {
            Stream stream = null;
            var ret = false;

            try
            {
                stream = fileInfo.Open(FileMode.Open);

                if (!stream.CanWrite)
                    ret = true;
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                    ret = true;
            }
            finally
            {
                stream?.Close();
            }

            return ret;
        }

        private static bool FolderIsIncluded(string dirPath)
        {
            var includeDirsList = Settings.Default.diskCleanerIncludedFolders.Cast<string>();

            return
                includeDirsList.Any(
                    includeDir => Utils.CompareWildcard(dirPath, includeDir) || includeDir == dirPath);
        }

        private static bool FolderIsExcluded(string dirPath)
        {
            var excludeDirsList = Settings.Default.diskCleanerExcludedDirs.Cast<string>();

            return excludeDirsList.Any(excludeDir => Utils.CompareWildcard(dirPath, excludeDir));
        }

        private static bool FileTypeIsExcluded(string fileName)
        {
            var excludeFileTypesList = Settings.Default.diskCleanerExcludedFileTypes.Cast<string>();

            return excludeFileTypesList.Any(excludeFileType => Utils.CompareWildcard(fileName, excludeFileType));
        }

        /// <summary>
        ///     Compare multiple wildcards to string
        /// </summary>
        /// <param name="wildString">String to compare</param>
        /// <param name="masks">Wildcard masks seperated by a semicolon (;)</param>
        /// <param name="ignoreCase">Ignore case for comparison (default is true)</param>
        /// <returns>True if match found</returns>
        private static bool CompareWildcards(string wildString, string masks, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(masks))
                return false;

            if (masks == "*")
                return true;

            var masksListTrimmed =
                masks.Split(';').Select(s => s.Trim()).Where(maskTrimmed => !string.IsNullOrEmpty(maskTrimmed));

            return masksListTrimmed.Any(maskTrimmed => Utils.CompareWildcard(wildString, maskTrimmed, ignoreCase));
        }

        /// <summary>
        ///     Cancels timer and thread
        /// </summary>
        public void CancelAnalyze()
        {
            Wizard.FileList.CollectionChanged -= FileListOnCollectionChanged;
            _cancellationTokenSource?.Cancel();
        }

        private async void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_taskMain.IsCompleted)
            {
                if (
                    MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to cancel?",
                        Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            CancelAnalyze();

            await _taskMain;

            ScanBase.MovePrev();
        }

        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            ScanBase.MoveNext();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
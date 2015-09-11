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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using Little_System_Cleaner.Disk_Cleaner.Helpers;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Timer = System.Timers.Timer;

namespace Little_System_Cleaner.Disk_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Analyze
    {
        internal Timer TimerUpdate = new Timer(100);
        public Wizard ScanBase;

        public Thread ThreadMain
        {
            get;
            set;
        }

        internal static string CurrentFile
        {
            get;
            set;
        }

        public Analyze(Wizard sb)
        {
            InitializeComponent();

            ScanBase = sb;

            if (Wizard.FileList == null)
                Wizard.FileList = new ObservableCollection<ProblemFile>();
            else
                Wizard.FileList.Clear();

            // Set scan start time
            Wizard.ScanStartTime = DateTime.Now;

            // Increase total number of scans
            Settings.Default.totalScans++;

            // Zero last scan errors found + fixed and elapsed
            Settings.Default.lastScanErrors = 0;
            Settings.Default.lastScanErrorsFixed = 0;
            Settings.Default.lastScanElapsed = 0;

            // Set last scan date
            Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            // Start timer
            TimerUpdate.Elapsed += timerUpdate_Elapsed;
            TimerUpdate.Start();

            ThreadMain = new Thread(AnalyzeDisk);
            ThreadMain.Start();
        }

        void timerUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new ElapsedEventHandler(timerUpdate_Elapsed), sender, e);
                return;
            }

            currentFile.Text = CurrentFile;
            FilesFound.Text = $"Files Found: {Wizard.FileList.Count}";
        }

        private void AnalyzeDisk()
        {
            try
            {
                // Show taskbar progress bar
                Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.Indeterminate));

                foreach (DriveInfo driveInfo in ScanBase.SelectedDrives)
                {
                    ScanFiles(driveInfo.RootDirectory);
                }

                Main.Watcher.EventPeriod("Disk Cleaner", "Analyze", (int)DateTime.Now.Subtract(Wizard.ScanStartTime).TotalSeconds, true);

                if (Wizard.FileList.Count > 0) {
                    EnableContinueButton();
                }
                else
                {
                    CurrentFile = "";
                    Utils.MessageBoxThreadSafe("No problem files were detected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (ThreadAbortException)
            {
                // Will end up here if user accepts to change tab
                // No need to change tab

                Thread.ResetAbort();

                CurrentFile = "";
            }
            finally
            {
                Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.None));
            }
        }

        private void EnableContinueButton()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new Action(EnableContinueButton));
                return;
            }

            CurrentFile = "View the results by clicking \"Continue\" below.";
            ButtonContinue.IsEnabled = true;
        }

        private void ScanFiles(DirectoryInfo parentInfo)
        {
            try
            {
                foreach (FileInfo fileInfo in parentInfo.GetFiles())
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

                        Wizard.FileList.Add(new ProblemFile(fileInfo));
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

            try {
                foreach (DirectoryInfo childInfo in parentInfo.GetDirectories())
                {
                    try
                    {
                        string dirPath = childInfo.FullName;

                        if (FolderIsIncluded(dirPath) && !FolderIsExcluded(dirPath))
                        {
                            foreach (FileInfo fileInfo in childInfo.GetFiles())
                            {
                                Wizard.FileList.Add(new ProblemFile(fileInfo));
                            }

                            continue;
                        }

                        if (!FolderIsExcluded(dirPath))
                            ScanFiles(childInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check of directory...");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to scan directories.");
            }
        }

        private static bool FileCheckSize(FileInfo fileInfo)
        {
            try
            {
                long fileSize = fileInfo.Length / 1024;

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
        /// Checks if file is in specified date/time range
        /// </summary>
        /// <param name="fileInfo">File information</param>
        /// <returns>True if file is in date/time range</returns>
        private static bool FileCheckDate(FileInfo fileInfo)
        {
            DateTime dateTimeFile = DateTime.MinValue;
            bool bRet = false;

            try
            {
                if (Settings.Default.diskCleanerFindFilesMode == 0)
                    dateTimeFile = fileInfo.CreationTime;
                else if (Settings.Default.diskCleanerFindFilesMode == 1)
                    dateTimeFile = fileInfo.LastWriteTime;
                else if (Settings.Default.diskCleanerFindFilesMode == 2)
                    dateTimeFile = fileInfo.LastAccessTime;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check file time.");
                return false;
            }

            if (Settings.Default.diskCleanerFindFilesAfter)
            {
                if (DateTime.Compare(dateTimeFile, Settings.Default.diskCleanerDateTimeAfter) >= 0)
                    bRet = true;
            }

            if (Settings.Default.diskCleanerFindFilesBefore)
            {
                if (DateTime.Compare(dateTimeFile, Settings.Default.diskCleanerDateTimeBefore) <= 0)
                    bRet = true;
            }

            return bRet;
        }

        /// <summary>
        /// Checks file attributes to match what user specified to search for
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

            if ((!Settings.Default.diskCleanerSearchHidden) && ((fileAttribs & FileAttributes.Hidden) == FileAttributes.Hidden))
                return false;

            if ((!Settings.Default.diskCleanerSearchArchives) && ((fileAttribs & FileAttributes.Archive) == FileAttributes.Archive))
                return false;

            if ((!Settings.Default.diskCleanerSearchReadOnly) && ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))
                return false;

            if ((!Settings.Default.diskCleanerSearchSystem) && ((fileAttribs & FileAttributes.System) == FileAttributes.System))
                return false;

            return true;
        }


        /// <summary>
        /// Checks if file is in use
        /// </summary>
        /// <param name="fileInfo">FileInfo class</param>
        /// <returns>True if file is in use</returns>
        private bool IsFileLocked(FileInfo fileInfo)
        {
            Stream stream = null;
            bool ret = false;

            try {
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

        private bool FolderIsIncluded(string dirPath)
        {
            var includeDirsList = Settings.Default.diskCleanerIncludedFolders.Cast<string>();

            return (includeDirsList.Any(includeDir => Utils.CompareWildcard(dirPath, includeDir) || string.Compare(includeDir, dirPath) == 0));
        }

        private bool FolderIsExcluded(string dirPath)
        {
            var excludeDirsList = Settings.Default.diskCleanerExcludedDirs.Cast<string>();

            return (excludeDirsList.Any(excludeDir => Utils.CompareWildcard(dirPath, excludeDir)));
        }

        private bool FileTypeIsExcluded(string fileName)
        {
            var excludeFileTypesList = Settings.Default.diskCleanerExcludedFileTypes.Cast<string>();

            return (excludeFileTypesList.Any(excludeFileType => Utils.CompareWildcard(fileName, excludeFileType)));
        }

        /// <summary>
        /// Compare multiple wildcards to string
        /// </summary>
        /// <param name="wildString">String to compare</param>
        /// <param name="masks">Wildcard masks seperated by a semicolon (;)</param>
        /// <param name="ignoreCase">Ignore case for comparison (default is true)</param>
        /// <returns>True if match found</returns>
        private bool CompareWildcards(string wildString, string masks, bool ignoreCase = true)
        {
            if (String.IsNullOrEmpty(masks))
                return false;

            if (masks == "*")
                return true;

            var masksListTrimmed = masks.Split(';').Select(s => s.Trim()).Where(maskTrimmed => !string.IsNullOrEmpty(maskTrimmed));

            return masksListTrimmed.Any(maskTrimmed => Utils.CompareWildcard(wildString, maskTrimmed, ignoreCase));
        }

        /// <summary>
        /// Cancels timer and thread
        /// </summary>
        public void CancelAnalyze()
        {
            TimerUpdate.Stop();

            ThreadMain?.Abort();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            
            CancelAnalyze();
            ScanBase.MovePrev();
        }

        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            // Set last scan errors found
            Settings.Default.lastScanErrors = Wizard.FileList.Count;

            ScanBase.MoveNext();
        }
    }
}

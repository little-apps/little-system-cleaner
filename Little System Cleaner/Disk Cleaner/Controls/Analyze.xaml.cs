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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace Little_System_Cleaner.Disk_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Analyze : UserControl
    {
        internal System.Timers.Timer timerUpdate = new System.Timers.Timer(100);
        public Wizard scanBase;

        public Thread threadMain
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

            this.scanBase = sb;

            if (Wizard.fileList == null)
                Wizard.fileList = new ObservableCollection<ProblemFile>();
            else
                Wizard.fileList.Clear();

            // Set scan start time
            Wizard.ScanStartTime = DateTime.Now;

            // Start timer
            this.timerUpdate.Elapsed += new System.Timers.ElapsedEventHandler(timerUpdate_Elapsed);
            this.timerUpdate.Start();

            this.threadMain = new Thread(new ThreadStart(AnalyzeDisk));
            this.threadMain.Start();
        }

        void timerUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(timerUpdate_Elapsed), new object[] { sender, e });
                return;
            }

            this.currentFile.Text = Analyze.CurrentFile;
            this.filesFound.Text = string.Format("Files Found: {0}", Wizard.fileList.Count);
        }

        private void AnalyzeDisk()
        {
            try
            {
                // Show taskbar progress bar
                this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.Indeterminate));

                // Set last scan date
                Properties.Settings.Default.lastScanDate = DateTime.Now.ToBinary();

                // Increase total number of scans
                Properties.Settings.Default.totalScans++;

                // Zero last scan errors found + fixed
                Properties.Settings.Default.lastScanErrors = 0;
                Properties.Settings.Default.lastScanErrorsFixed = 0;

                foreach (DriveInfo driveInfo in this.scanBase.selectedDrives)
                {
                    ScanFiles(driveInfo.RootDirectory);
                }

                if (Wizard.fileList.Count > 0)
                {
                    // Set last scan errors found
                    Properties.Settings.Default.lastScanErrors = Wizard.fileList.Count;

                    this.scanBase.MoveNext();
                }
                else
                {
                    this.msgBox("No problem files were detected");
                    this.scanBase.MovePrev();
                }
            }
            catch (ThreadAbortException)
            {
                // Will end up here if user accepts to change tab
                // No need to change tab
            }
            finally
            {
                this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.None));
            }
        }

        private void msgBox(string text)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action(() => msgBox(text)));
                return;
            }

            MessageBox.Show(Window.GetWindow(this), text, System.Windows.Forms.Application.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ScanFiles(DirectoryInfo parentInfo)
        {
            try
            {
                foreach (FileInfo fileInfo in parentInfo.GetFiles())
                {
                    try
                    {
                        Analyze.CurrentFile = fileInfo.FullName;

                        // Check if file is exclude
                        if (FileTypeIsExcluded(fileInfo.Name))
                            continue;

                        // Check for zero-byte files
                        if (Properties.Settings.Default.diskCleanerSearchZeroByte)
                        {
                            if (fileInfo.Length == 0)
                            {
                                Wizard.fileList.Add(new ProblemFile(fileInfo));
                                continue;
                            }
                        }

                        // Check if file matches types
                        if (!this.CompareWildcards(fileInfo.Name, Properties.Settings.Default.diskCleanerSearchFilters))
                            continue;

                        // Check if file is in use or write protected
                        if (Properties.Settings.Default.diskCleanerIgnoreWriteProtected && fileInfo.IsReadOnly)
                            continue;

                        // Check file attributes
                        if (!FileCheckAttributes(fileInfo))
                            continue;

                        // Check file dates
                        if (Properties.Settings.Default.diskCleanerFindFilesAfter || Properties.Settings.Default.diskCleanerFindFilesBefore)
                            if (!FileCheckDate(fileInfo))
                                continue;

                        // Check file size
                        if (Properties.Settings.Default.diskCleanerCheckFileSize)
                            if (!FileCheckSize(fileInfo))
                                continue;

                        Wizard.fileList.Add(new ProblemFile(fileInfo));
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
                                Wizard.fileList.Add(new ProblemFile(fileInfo));
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

        private bool FileCheckSize(FileInfo fileInfo)
        {
            try
            {
                long fileSize = fileInfo.Length / 1024;

                if (Properties.Settings.Default.diskCleanerCheckFileSizeLeast > 0)
                    if (fileSize <= Properties.Settings.Default.diskCleanerCheckFileSizeLeast)
                        return false;

                if (Properties.Settings.Default.diskCleanerCheckFileSizeMost > 0)
                    if (fileSize >= Properties.Settings.Default.diskCleanerCheckFileSizeMost)
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
        private bool FileCheckDate(FileInfo fileInfo)
        {
            DateTime dateTimeFile = DateTime.MinValue;
            bool bRet = false;

            try
            {
                if (Properties.Settings.Default.diskCleanerFindFilesMode == 0)
                    dateTimeFile = fileInfo.CreationTime;
                else if (Properties.Settings.Default.diskCleanerFindFilesMode == 1)
                    dateTimeFile = fileInfo.LastWriteTime;
                else if (Properties.Settings.Default.diskCleanerFindFilesMode == 2)
                    dateTimeFile = fileInfo.LastAccessTime;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check file time.");
                return false;
            }

            if (Properties.Settings.Default.diskCleanerFindFilesAfter)
            {
                if (DateTime.Compare(dateTimeFile, Properties.Settings.Default.diskCleanerDateTimeAfter) >= 0)
                    bRet = true;
            }

            if (Properties.Settings.Default.diskCleanerFindFilesBefore)
            {
                if (DateTime.Compare(dateTimeFile, Properties.Settings.Default.diskCleanerDateTimeBefore) <= 0)
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

            if ((!Properties.Settings.Default.diskCleanerSearchHidden) && ((fileAttribs & FileAttributes.Hidden) == FileAttributes.Hidden))
                return false;

            if ((!Properties.Settings.Default.diskCleanerSearchArchives) && ((fileAttribs & FileAttributes.Archive) == FileAttributes.Archive))
                return false;

            if ((!Properties.Settings.Default.diskCleanerSearchReadOnly) && ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly))
                return false;

            if ((!Properties.Settings.Default.diskCleanerSearchSystem) && ((fileAttribs & FileAttributes.System) == FileAttributes.System))
                return false;

            return true;
        }

        private bool FolderIsIncluded(string dirPath)
        {
            foreach (string includeDir in Properties.Settings.Default.diskCleanerIncludedFolders)
            {
                if (string.Compare(includeDir, dirPath) == 0 || Utils.CompareWildcard(dirPath, includeDir))
                    return true;
            }

            return false;
        }

        private bool FolderIsExcluded(string dirPath)
        {
            foreach (string excludeDir in Properties.Settings.Default.diskCleanerExcludedDirs)
            {
                if (Utils.CompareWildcard(dirPath, excludeDir))
                    return true;
            }

            return false;
        }

        private bool FileTypeIsExcluded(string fileName)
        {
            foreach (string excludeFileType in Properties.Settings.Default.diskCleanerExcludedFileTypes)
            {
                if (Utils.CompareWildcard(fileName, excludeFileType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Compare multiple wildcards to string
        /// </summary>
        /// <param name="WildString">String to compare</param>
        /// <param name="Mask">Wildcard masks seperated by a semicolon (;)</param>
        /// <returns>True if match found</returns>
        private bool CompareWildcards(string WildString, string Mask, bool IgnoreCase = true)
        {
            int i = 0;

            if (String.IsNullOrEmpty(Mask))
                return false;
            if (Mask == "*")
                return true;

            while (i != Mask.Length)
            {
                if (Utils.CompareWildcard(WildString, Mask.Substring(i), IgnoreCase))
                    return true;

                while (i != Mask.Length && Mask[i] != ';')
                    i += 1;

                if (i != Mask.Length && Mask[i] == ';')
                {
                    i += 1;

                    while (i != Mask.Length && Mask[i] == ' ')
                        i += 1;
                }
            }

            return false;
        }
    }
}

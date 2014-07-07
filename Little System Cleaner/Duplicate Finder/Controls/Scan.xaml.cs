using System;
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
using System.Diagnostics;
using System.Threading;
using Little_System_Cleaner.Duplicate_Finder.Helpers;
using System.IO;
using System.ComponentModel;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    /// Interaction logic for Scan.xaml
    /// </summary>
    public partial class Scan : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public Wizard scanBase;

        private Thread threadScan;

        private readonly List<FileEntry> FileList;

        private string _statusText;
        private string _currentFile;

        internal static List<string> validAudioFiles = new List<string>() { "aac", "aif", "ape", "wma", "aa", "aax", "flac", "mka", "mpc", "mp+", "mpp", "mp4", "m4a", "ogg", "oga", "wav", "wv", "mp3", "m2a", "mp2", "mp1" };

        public string StatusText
        {
            get { return this._statusText; }
            set
            {
                if (this.Dispatcher.Thread != Thread.CurrentThread)
                {
                    this.Dispatcher.BeginInvoke(new Action(() => this.StatusText = value));
                    return;
                }

                this._statusText = value;
                this.OnPropertyChanged("StatusText");
            }
        }

        public string CurrentFile
        {
            get { return this._currentFile; }
            set
            {
                if (this.Dispatcher.Thread != Thread.CurrentThread)
                {
                    this.Dispatcher.BeginInvoke(new Action(() => this.CurrentFile = value));
                    return;
                }

                this._currentFile = value;
                this.OnPropertyChanged("CurrentFile");
            }
        }

        public Scan(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            this.FileList = new List<FileEntry>();

            // Increase total number of scans
            Properties.Settings.Default.totalScans++;

            // Zero last scan errors found + fixed and elapsed
            Properties.Settings.Default.lastScanErrors = 0;
            Properties.Settings.Default.lastScanErrorsFixed = 0;
            Properties.Settings.Default.lastScanElapsed = 0;

            // Set last scan date
            Properties.Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            this.threadScan = new Thread(new ThreadStart(this.ScanDisk));
            this.threadScan.Start();
        }

        private void ScanDisk()
        {
            try
            {
                DateTime dtStart = DateTime.Now;

                this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate));
                this.StatusText = "Building list of all files";

                if (this.scanBase.Options.AllDrives.GetValueOrDefault())
                {
                    foreach (DriveInfo di in DriveInfo.GetDrives())
                    {
                        if (di.IsReady && di.TotalFreeSpace != 0 && (di.DriveType == DriveType.Fixed || di.DriveType == DriveType.Network || di.DriveType == DriveType.Removable))
                            RecurseDirectory(di.RootDirectory);
                    }
                }
                else if (this.scanBase.Options.AllExceptDrives.GetValueOrDefault())
                {
                    int drives = 0;

                    foreach (DriveInfo di in DriveInfo.GetDrives())
                    {
                        if (!di.IsReady || di.TotalFreeSpace == 0 || di.DriveType == DriveType.NoRootDirectory)
                            continue;

                        if (this.scanBase.Options.AllExceptSystem.GetValueOrDefault() && Environment.SystemDirectory[0] == di.RootDirectory.Name[0])
                            continue;

                        if (this.scanBase.Options.AllExceptRemovable.GetValueOrDefault() && di.DriveType == DriveType.Removable)
                            continue;

                        if (this.scanBase.Options.AllExceptNetwork.GetValueOrDefault() && di.DriveType == DriveType.Network)
                            continue;

                        RecurseDirectory(di.RootDirectory);

                        drives++;
                    }

                    if (drives == 0)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show(App.Current.MainWindow, "No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                            Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                        }));

                        Properties.Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;

                        this.scanBase.MoveFirst();
                        return;
                    }
                }
                else if (this.scanBase.Options.OnlySelectedDrives.GetValueOrDefault())
                {
                    foreach (DriveInfo di in DriveInfo.GetDrives())
                    {
                        if (!di.IsReady || di.TotalFreeSpace == 0 || di.DriveType.HasFlag(DriveType.NoRootDirectory))
                            continue;

                        if (this.scanBase.Options.Drives.Contains(new IncludeDrive(di)))
                            RecurseDirectory(di.RootDirectory);
                    }
                }
                else // Only selected folders
                {
                    foreach (IncludeFolder dir in this.scanBase.Options.IncFolders)
                    {
                        RecurseDirectory(dir.DirInfo);
                    }
                }

                if (this.scanBase.Options.CompareFilename.GetValueOrDefault())
                {
                    // Group by filename
                    this.GroupByFilename();

                    if (this.scanBase.FilesGroupedByFilename.Count == 0)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show(App.Current.MainWindow, "No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                            Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                        }));

                        Properties.Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;

                        this.scanBase.MoveFirst();
                        return;
                    }

                    Properties.Settings.Default.lastScanErrors = this.scanBase.FilesGroupedByFilename.Count;
                }

                if (this.scanBase.Options.CompareChecksum.GetValueOrDefault() || this.scanBase.Options.CompareChecksumFilename.GetValueOrDefault())
                {
                    // Group by filename and/or checksum
                    this.GroupByChecksum();

                    if (this.scanBase.FilesGroupedByHash.Count == 0)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        { 
                            MessageBox.Show(App.Current.MainWindow, "No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information); 
                            Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                        }));

                        Properties.Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;

                        this.scanBase.MoveFirst();
                        return;
                    }

                    Properties.Settings.Default.lastScanErrors = this.scanBase.FilesGroupedByHash.Count;
                }

                if (this.scanBase.Options.CompareMusicTags.GetValueOrDefault())
                {
                    // Group by audio tags
                    this.GroupByTags();

                    if (this.scanBase.FilesGroupedByHash.Count == 0)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show(App.Current.MainWindow, "No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                            Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                        }));

                        Properties.Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;

                        this.scanBase.MoveFirst();
                        return;
                    }

                    Properties.Settings.Default.lastScanErrors = this.scanBase.FilesGroupedByHash.Count;
                }

                Properties.Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;

                this.scanBase.MoveNext();
            }
            catch (ThreadAbortException)
            {

            }
            finally
            {
                this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None));
            }
        }

        public void AbortScanThread()
        {
            if ((this.threadScan != null) && threadScan.IsAlive)
                this.threadScan.Abort();
        }

        private void RecurseDirectory(DirectoryInfo di)
        {
            if (!di.Exists || di.FullName.Length > 248)
                return;

            if (this.scanBase.Options.ExcludeFolders.Contains(new ExcludeFolder(di.FullName)))
                return;

            string[] files = Directory.GetFiles(di.FullName);

            if (files.Length > 0)
            {
                foreach (string file in files)
                {
                    this.CurrentFile = file;

                    if (file.Length > 260)
                        continue;

                    try
                    {
                        FileInfo fi = new FileInfo(file);

                        if (this.scanBase.Options.SkipZeroByteFiles.GetValueOrDefault() && fi.Length == 0)
                            continue;

                        if (!this.scanBase.Options.IncHiddenFiles.GetValueOrDefault() && fi.Attributes.HasFlag(FileAttributes.Hidden))
                            continue;

                        if (this.IsSizeGreaterThan(fi.Length))
                            continue;

                        FileEntry fileEntry = new FileEntry(fi, this.scanBase.Options.CompareMusicTags.GetValueOrDefault());
                        this.FileList.Add(fileEntry);
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }
                    catch (PathTooLongException)
                    {
                        // Just in case
                        Debug.WriteLine("Path ({0}) is too long", file);
                    }
                }
            }


            if (this.scanBase.Options.ScanSubDirs.GetValueOrDefault()) // Will stop if false and only include root directory
            {
                string[] dirs = Directory.GetDirectories(di.FullName);

                if (dirs.Length > 0)
                {
                    foreach (string dir in dirs)
                    {
                        this.CurrentFile = dir;

                        if (dir.Length > 248)
                            continue;

                        try
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(dir);

                            if (!this.scanBase.Options.IncHiddenFiles.GetValueOrDefault() && dirInfo.Attributes.HasFlag(FileAttributes.Hidden))
                                continue;

                            RecurseDirectory(dirInfo);
                        }
                        catch (UnauthorizedAccessException)
                        {

                        }
                        catch (PathTooLongException)
                        {
                            // Just in case
                        }
                        
                    }
                }
            }
        }

        private bool IsSizeGreaterThan(long size)
        {
            if (!this.scanBase.Options.SkipFilesGreaterThan.GetValueOrDefault())
                return false;

            long maxSize = this.scanBase.Options.SkipFilesGreaterSize;
            double multiplier = 1;

            // Get size in bytes
            switch (this.scanBase.Options.SkipFilesGreaterUnit)
            {
                case "KB":
                    multiplier = Math.Pow(1024, 1);
                    break;

                case "MB":
                    multiplier = Math.Pow(1024, 2);
                    break;

                case "GB":
                    multiplier = Math.Pow(1024, 3);
                    break;
            }

            long maxSizeBytes = maxSize * (long)multiplier;

            if (size > maxSizeBytes)
                return true;
            else
                return false;
        }

        private void GroupByFilename()
        {
            this.StatusText = "Grouping files by filename";

            this.scanBase.FilesGroupedByFilename.Clear();

            var query = from p in this.FileList
                        where p.IsDeleteable == true
                        group p by p.FileName into g
                        where g.Count() > 1
                        select new { FileName = g.Key, Files = g };

            foreach (var group in query)
            {
                this.scanBase.FilesGroupedByFilename.Add(group.FileName, group.Files.ToList<FileEntry>());
            }
        }

        private void GroupByChecksum()
        {
            Dictionary<long, List<FileEntry>> filesGroupedBySize = new Dictionary<long, List<FileEntry>>();
            bool compareFilename = this.scanBase.Options.CompareChecksumFilename.GetValueOrDefault();
            long totalFiles = 0;

            this.StatusText = "Grouping files by size";
            this.CurrentFile = "Please wait...";

            this.scanBase.FilesGroupedByHash.Clear();

            var query2 = from p in this.FileList
                            where p.IsDeleteable == true
                            group p by p.FileSize into g
                            where g.Count() > 1
                            select new { FileSize = g.Key, Files = g };

            foreach (var group in query2)
            {
                List<FileEntry> filesGroup = group.Files.ToList<FileEntry>();

                filesGroupedBySize.Add(group.FileSize, filesGroup);

                totalFiles += filesGroup.Count;
            }

            if (filesGroupedBySize.Count == 0)
                // Nothing found
                return;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.progressBar.IsIndeterminate = false;
                this.progressBar.Value = 0;
                this.progressBar.Minimum = 0;
                this.progressBar.Maximum = totalFiles;
            }));
            
            this.StatusText = "Getting checksums from files";

            // Sort file sizes from least to greatest
            var fileSizesSorted = from p in filesGroupedBySize
                        orderby p.Key ascending
                        select p.Value;

            foreach (List<FileEntry> files in fileSizesSorted)
            {
                foreach (FileEntry file in files)
                {
                    this.CurrentFile = file.FilePath;

                    file.GetChecksum(this.scanBase.Options.HashAlgorithm.Algorithm, compareFilename);

                    this.Dispatcher.BeginInvoke(new Action(() => this.progressBar.Value++ ));
                }

                var query3 = from p in files
                                group p by p.Checksum into g
                                where g.Count() > 1
                                select new { Checksum = g.Key, Files = g.ToList<FileEntry>() };

                foreach (var group in query3)
                {
                    if (group.Files.Count<FileEntry>() > 0)
                    {
                        if (!this.scanBase.FilesGroupedByHash.ContainsKey(group.Checksum))
                            this.scanBase.FilesGroupedByHash.Add(group.Checksum, group.Files);
                        else
                        {
                            List<FileEntry> existingKey = this.scanBase.FilesGroupedByHash[group.Checksum];

                            var mergedList = existingKey.Union(group.Files).Distinct();

                            this.scanBase.FilesGroupedByHash[group.Checksum] = mergedList.ToList();
                        }
                    }
                        
                }
            }
        }

        private void GroupByTags()
        {
            this.StatusText = "Getting checksums from audio files";
            this.CurrentFile = "Please wait...";

            List<FileEntry> fileEntriesTags = new List<FileEntry>();

            foreach (FileEntry fileEntry in this.FileList)
            {
                if (fileEntry.IsDeleteable && fileEntry.HasAudioTags)
                {
                    fileEntry.GetTagsChecksum(this.scanBase.Options);
                    fileEntriesTags.Add(fileEntry);
                }
            }

            this.StatusText = "Grouping files by audio tags";
            this.CurrentFile = "Please wait...";

            var query = from p in fileEntriesTags
                        where p.HasAudioTags == true
                        group p by p.TagsChecksum into g
                        where g.Count() > 1
                        select new { Checksum = g.Key, Files = g.ToList<FileEntry>() };

            foreach (var group in query)
            {
                string checksum = group.Checksum;
                List<FileEntry> files = group.Files;

                this.scanBase.FilesGroupedByHash.Add(checksum, files);
            }
        }
    }
}

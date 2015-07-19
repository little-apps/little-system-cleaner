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
        internal static Dictionary<string, KeyValuePair<int, byte[]>> compressedFiles = new Dictionary<string, KeyValuePair<int, byte[]>>()
            {
                { "cab", new KeyValuePair<int, byte[]>(0, new byte[] { 0x4d, 0x53, 0x43, 0x46 } ) },
                { "zip", new KeyValuePair<int, byte[]>(0, new byte[] { 0x50, 0x4B, 0x03, 0x04 } ) },
                { "jar", new KeyValuePair<int, byte[]>(0, new byte[] { 0x50, 0x4B, 0x03, 0x04 } ) },
                { "rar", new KeyValuePair<int, byte[]>(0, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 } ) },
                { "tar", new KeyValuePair<int, byte[]>(257, new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72 } ) }, // offset 257
                { "gz", new KeyValuePair<int, byte[]>(0, new byte[] { 0x1F, 0x8B, 0x08 } ) },
                { "tgz", new KeyValuePair<int, byte[]>(0, new byte[] { 0x1F, 0x8B, 0x08 } ) },
                { "bz", new KeyValuePair<int, byte[]>(0, new byte[] { 0x42, 0x5A, 0x68 } ) },
                { "tbz", new KeyValuePair<int, byte[]>(0, new byte[] { 0x42, 0x5A, 0x68 } ) },
                { "bz2", new KeyValuePair<int, byte[]>(0, new byte[] { 0x42, 0x5A, 0x68 } ) },
                { "tbz2", new KeyValuePair<int, byte[]>(0, new byte[] { 0x42, 0x5A, 0x68 } ) },
                { "xz", new KeyValuePair<int, byte[]>(0, new byte[] { 0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00 } ) },
                { "xar", new KeyValuePair<int, byte[]>(0, new byte[] { 0x78, 0x61, 0x72, 0x21 } ) },
                { "rpm", new KeyValuePair<int, byte[]>(0, new byte[] { 0xED, 0xAB, 0xEE, 0xDB } ) },
            };

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
                    this.Dispatcher.Invoke(new Action(() => this.CurrentFile = value));
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
            bool completedSucessfully = false;

            try
            {
                DateTime dtStart = DateTime.Now;
                this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate));
                this.StatusText = "Building list of all files";

                if (!this.BuildFileList(dtStart))
                {
                    this.ResetInfo(completedSucessfully);
                    this.SetLastScanElapsed(dtStart);
                }

                if (this.scanBase.Options.CompareFilename.GetValueOrDefault())
                {
                    // Group by filename
                    this.GroupByFilename();

                    if (this.scanBase.FilesGroupedByFilename.Count == 0)
                    {
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                        this.ResetInfo(completedSucessfully);
                        this.SetLastScanElapsed(dtStart);

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
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                        this.ResetInfo(completedSucessfully);
                        this.SetLastScanElapsed(dtStart);

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
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                        this.ResetInfo(completedSucessfully);
                        this.SetLastScanElapsed(dtStart);

                        return;
                    }

                    Properties.Settings.Default.lastScanErrors = this.scanBase.FilesGroupedByHash.Count;
                }

                this.SetLastScanElapsed(dtStart);
                completedSucessfully = true;
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            finally
            {
                this.ResetInfo(completedSucessfully);
            }
        }

        public void AbortScanThread()
        {
            if ((this.threadScan != null) && threadScan.IsAlive)
                this.threadScan.Abort();
        }

        private void ResetInfo(bool success)
        {
            this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None));
            this.CurrentFile = "";

            if (success) 
            {
                this.StatusText = "View the results by clicking \"Continue\" below.";
                this.Dispatcher.Invoke(new Action(() => this.buttonContinue.IsEnabled = true));
            }
            else
            {
                this.StatusText = "Click \"Cancel\" to go back to the previous screen.";
            }
                
        }

        private void SetLastScanElapsed(DateTime dtStart)
        {
            Properties.Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;
        }

        /// <summary>
        /// Builds list of files
        /// </summary>
        /// <param name="dtStart">Date/time scan started</param>
        /// <returns>True if file list created. Otherwise, false if no directories or files were located.</returns>
        private bool BuildFileList(DateTime dtStart)
        {
            if (this.scanBase.Options.AllDrives.GetValueOrDefault())
            {
                bool driveSelected = false;

                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (di.IsReady && di.TotalFreeSpace != 0 && (di.DriveType == DriveType.Fixed || di.DriveType == DriveType.Network || di.DriveType == DriveType.Removable))
                    {
                        if (!driveSelected)
                            driveSelected = true;

                        RecurseDirectory(di.RootDirectory);
                    }

                }

                if (!driveSelected)
                {
                    Utils.MessageBoxThreadSafe("No disk drives could be found to scan.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan All Drives", (int)DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }
            else if (this.scanBase.Options.AllExceptDrives.GetValueOrDefault())
            {
                bool driveSelected = false;

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

                    if (!driveSelected)
                        driveSelected = true;

                    RecurseDirectory(di.RootDirectory);
                }

                if (!driveSelected)
                {
                    Utils.MessageBoxThreadSafe("No disk drives could be found to scan.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan All Drives Except", (int)DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }
            else if (this.scanBase.Options.OnlySelectedDrives.GetValueOrDefault())
            {
                bool driveSelected = false;

                foreach (IncludeDrive incDrive in this.scanBase.Options.Drives)
                {
                    if (incDrive.IsChecked.GetValueOrDefault())
                    {
                        if (!driveSelected)
                            driveSelected = true;

                        try
                        {
                            DriveInfo di = new DriveInfo(incDrive.Name);

                            if (!di.IsReady || di.TotalFreeSpace == 0 || di.DriveType == DriveType.NoRootDirectory)
                                continue;

                            if (this.scanBase.Options.Drives.Contains(new IncludeDrive(di)))
                                RecurseDirectory(di.RootDirectory);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Unable to scan drive ({0}). The following error occurred: {1}", incDrive.Name, ex.Message);
                            continue;
                        }
                    }

                }

                if (!driveSelected)
                {
                    Utils.MessageBoxThreadSafe("No disk drives could be found to scan.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan Selected Drives", (int)DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }
            else // Only selected folders
            {
                bool dirSelected = false;

                foreach (IncludeFolder dir in this.scanBase.Options.IncFolders)
                {
                    if (dir.IsChecked.GetValueOrDefault())
                    {
                        if (!dirSelected)
                            dirSelected = true;

                        RecurseDirectory(dir.DirInfo);
                    }
                }

                if (!dirSelected)
                {
                    Utils.MessageBoxThreadSafe("No folders are selected.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan Selected Folders", (int)DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }

            if (this.FileList.Count == 0)
            {
                Utils.MessageBoxThreadSafe("No files were found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                
                return false;
            }

            return true;
        }

        private void RecurseDirectory(DirectoryInfo di)
        {
            if (!di.Exists || di.FullName.Length > 248)
                return;

            if (this.scanBase.Options.ExcludeFolders.Contains(new ExcludeFolder(di.FullName)))
                return;

            try
            {
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

                            if (!this.scanBase.Options.IncHiddenFiles.GetValueOrDefault() && (fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                continue;

                            if (this.IsSizeGreaterThan(fi.Length))
                                continue;

                            if ((this.scanBase.Options.SkipCompressedFiles.GetValueOrDefault()) && this.IsCompressedFile(fi))
                                continue;

                            FileEntry fileEntry = new FileEntry(fi, this.scanBase.Options.CompareMusicTags.GetValueOrDefault());
                            this.FileList.Add(fileEntry);
                        }
                        catch (UnauthorizedAccessException)
                        {

                        }
                        catch (System.Security.SecurityException)
                        {

                        }
                        catch (IOException ex)
                        {
                            if (ex is PathTooLongException)
                            {
                                // Just in case
                                Debug.WriteLine("Path ({0}) is too long", (object)file);
                            }
                            
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine("The following I/O error occurred reading files: {0}", (object)ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following unknown exception occurred reading files: {0}", (object)ex.Message);

#if (DEBUG)
                throw ex;
#endif
            }
            

            if (this.scanBase.Options.ScanSubDirs.GetValueOrDefault()) // Will stop if false and only include root directory
            {
                try
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

                                if (!this.scanBase.Options.IncHiddenFiles.GetValueOrDefault() && (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                    continue;

                                RecurseDirectory(dirInfo);
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                Debug.WriteLine("Could not access directories: {0}", (object)ex.Message);
                            }
                            catch (System.Security.SecurityException ex)
                            {
                                Debug.WriteLine("A security exception error occurred reading directories: {0}", (object)ex.Message);
                            }
                            catch (IOException ex)
                            {
                                if (ex is PathTooLongException)
                                {
                                    // Just in case
                                    Debug.WriteLine("Path ({0}) is too long", (object)dir);
                                }

                            }

                        }
                    }
                }
                catch (IOException ex)
                {
                    Debug.WriteLine("The following I/O error occurred reading directories: {0}", (object)ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following unknown exception occurred reading directories: {0}", (object)ex.Message);

#if (DEBUG)
                    throw ex;
#endif
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

        /// <summary>
        /// Checks if file is a compressed file
        /// </summary>
        /// <param name="fileInfo">FileInfo class</param>
        /// <returns>True if file matches compressed file extension and signature</returns>
        private bool IsCompressedFile(FileInfo fileInfo)
        {
            // Make sure fileInfo isn't null
            if (fileInfo == null)
                return false;

            // Get file extension
            string fileExt = fileInfo.Extension;

            if (string.IsNullOrWhiteSpace(fileExt))
                return false;

            if (!compressedFiles.ContainsKey(fileExt))
                return false;

            int offset = compressedFiles[fileExt].Key;

            byte[] expectedSignature = compressedFiles[fileExt].Value;
            int expectedSignatureLen = expectedSignature.Length;

            byte[] actualSignature = new byte[expectedSignatureLen];

            FileStream fileStream = null;

            try
            {
                // Open file
                fileStream = fileInfo.OpenRead();

                if (!fileStream.CanRead)
                    throw new IOException("Unable to read file stream");

                if (!fileStream.CanSeek)
                    throw new IOException("Unable to seek file stream");

                if ((offset + expectedSignatureLen) >= fileStream.Length)
                    throw new IOException("File is smaller than offset and size of expected signature");

                if (fileStream.Seek(offset, SeekOrigin.Begin) != offset)
                    throw new IOException("There was an error seeking to position " + offset);

                int bytesToRead = expectedSignatureLen;
                int bytesRead = 0;

                while (bytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead. 
                    int n = fileStream.Read(actualSignature, bytesRead, bytesToRead);

                    // Break when the end of the file is reached. 
                    if (n == 0)
                        break;

                    bytesRead += n;
                    bytesToRead -= n;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred trying to read file ({0}): {1}", fileInfo.FullName, ex.Message);
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
            }

            if (expectedSignature.SequenceEqual(actualSignature))
                return true;

            return false;
        }

        private void GroupByFilename()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Main.TaskbarProgressState != System.Windows.Shell.TaskbarItemProgressState.Indeterminate)
                    Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;

                if (!this.progressBar.IsIndeterminate)
                    this.progressBar.IsIndeterminate = true;
            }));

            this.StatusText = "Grouping files by filename";

            Main.Watcher.Event("Duplicate Finder", "Group by filename");

            this.scanBase.FilesGroupedByFilename.Clear();

            var query = from p in this.FileList
                        where p.IsDeleteable == true
                        group p by p.FileName into g
                        where g.Count() > 1
                        select new { FileName = g.Key, Files = g };

            foreach (var group in query)
            {
                if (!string.IsNullOrEmpty(group.FileName) && group.Files.Count() > 0)
                    this.scanBase.FilesGroupedByFilename.Add(group.FileName, group.Files.ToList<FileEntry>());
            }
        }

        private void GroupByChecksum()
        {
            Dictionary<long, List<FileEntry>> filesGroupedBySize = new Dictionary<long, List<FileEntry>>();
            bool compareFilename = this.scanBase.Options.CompareChecksumFilename.GetValueOrDefault();
            long totalFiles = 0;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Main.TaskbarProgressState != System.Windows.Shell.TaskbarItemProgressState.Indeterminate)
                    Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;

                if (!this.progressBar.IsIndeterminate)
                    this.progressBar.IsIndeterminate = true;
            }));

            this.StatusText = "Grouping files by size";
            this.CurrentFile = "Please wait...";

            Main.Watcher.Event("Duplicate Finder", "Group by checksum");

            this.scanBase.FilesGroupedByHash.Clear();
            
            var query2 = from p in this.FileList
                            where p.IsDeleteable == true
                            group p by p.FileSize into g
                            where g.Count() > 1
                            select new { FileSize = g.Key, Files = g };

            foreach (var group in query2)
            {
                if (group.Files.Count() > 0)
                {
                    List<FileEntry> filesGroup = group.Files.ToList<FileEntry>();

                    filesGroupedBySize.Add(group.FileSize, filesGroup);

                    totalFiles += filesGroup.Count;
                }
            }

            if (filesGroupedBySize.Count == 0)
                // Nothing found
                return;

            this.StatusText = "Getting checksums from files";

            // Sort file sizes from least to greatest
            var fileSizesSorted = from p in filesGroupedBySize
                        orderby p.Key ascending
                        select p.Value;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                Main.TaskbarProgressValue = 0D;

                this.progressBar.IsIndeterminate = false;
                this.progressBar.Value = 0;
                this.progressBar.Minimum = 0;
                this.progressBar.Maximum = totalFiles;
            }));

            foreach (List<FileEntry> files in fileSizesSorted)
            {
                if (files.Count > 0)
                {
                    foreach (FileEntry file in files)
                    {
                        this.CurrentFile = file.FilePath;

                        file.GetChecksum(this.scanBase.Options.HashAlgorithm.Algorithm, compareFilename);

                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.progressBar.Value += 1;
                            Main.TaskbarProgressValue = (this.progressBar.Value / this.progressBar.Maximum);
                        }));
                    }

                    var query3 = from p in files
                                 group p by p.Checksum into g
                                 where g.Count() > 1
                                 select new { Checksum = g.Key, Files = g.ToList<FileEntry>() };

                    foreach (var group in query3)
                    {
                        if (!string.IsNullOrEmpty(group.Checksum) && group.Files.Count<FileEntry>() > 0)
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
        }

        private void GroupByTags()
        {
            List<FileEntry> fileEntriesTags = new List<FileEntry>();

            this.StatusText = "Getting checksums from audio files";
            this.CurrentFile = "Please wait...";

            Main.Watcher.Event("Duplicate Finder", "Group by audio tags");

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Main.TaskbarProgressState != System.Windows.Shell.TaskbarItemProgressState.Indeterminate)
                    Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;

                if (!this.progressBar.IsIndeterminate)
                    this.progressBar.IsIndeterminate = true;
            }));
            
            foreach (FileEntry fileEntry in this.FileList)
            {
                if (fileEntry.IsDeleteable && fileEntry.HasAudioTags)
                {
                    fileEntry.GetTagsChecksum(this.scanBase.Options);
                    fileEntriesTags.Add(fileEntry);
                }

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.progressBar.Value += 1;
                    Main.TaskbarProgressValue = (this.progressBar.Value / this.progressBar.Maximum);
                }));
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

                if (!string.IsNullOrEmpty(checksum) && files.Count > 0)
                    this.scanBase.FilesGroupedByHash.Add(checksum, files);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.AbortScanThread();
                this.scanBase.MovePrev();
            }
        }

        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            this.scanBase.MoveNext();
        }
    }
}

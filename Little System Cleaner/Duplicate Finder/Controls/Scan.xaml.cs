using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Shell;
using Little_System_Cleaner.Duplicate_Finder.Helpers;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    /// Interaction logic for Scan.xaml
    /// </summary>
    public partial class Scan : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public Wizard ScanBase;

        private readonly Thread _threadScan;

        private readonly List<FileEntry> _fileList;

        private string _statusText;
        private string _currentFile;

        internal static List<string> ValidAudioFiles = new List<string> { "aac", "aif", "ape", "wma", "aa", "aax", "flac", "mka", "mpc", "mp+", "mpp", "mp4", "m4a", "ogg", "oga", "wav", "wv", "mp3", "m2a", "mp2", "mp1" };
        internal static Dictionary<string, KeyValuePair<int, byte[]>> CompressedFiles = new Dictionary<string, KeyValuePair<int, byte[]>>
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
                { "rpm", new KeyValuePair<int, byte[]>(0, new byte[] { 0xED, 0xAB, 0xEE, 0xDB } ) }
            };

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (Dispatcher.Thread != Thread.CurrentThread)
                {
                    Dispatcher.BeginInvoke(new Action(() => StatusText = value));
                    return;
                }

                _statusText = value;
                OnPropertyChanged("StatusText");
            }
        }

        public string CurrentFile
        {
            get { return _currentFile; }
            set
            {
                if (Dispatcher.Thread != Thread.CurrentThread)
                {
                    Dispatcher.Invoke(new Action(() => CurrentFile = value));
                    return;
                }

                _currentFile = value;
                OnPropertyChanged("CurrentFile");
            }
        }

        public Scan(Wizard sb)
        {
            InitializeComponent();

            ScanBase = sb;

            _fileList = new List<FileEntry>();

            // Increase total number of scans
            Settings.Default.totalScans++;

            // Zero last scan errors found + fixed and elapsed
            Settings.Default.lastScanErrors = 0;
            Settings.Default.lastScanErrorsFixed = 0;
            Settings.Default.lastScanElapsed = 0;

            // Set last scan date
            Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            _threadScan = new Thread(ScanDisk);
            _threadScan.Start();
        }

        private void ScanDisk()
        {
            bool completedSucessfully = false;

            try
            {
                DateTime dtStart = DateTime.Now;
                Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.Indeterminate));
                StatusText = "Building list of all files";

                if (!BuildFileList(dtStart))
                {
                    ResetInfo(completedSucessfully);
                    SetLastScanElapsed(dtStart);
                }

                if (ScanBase.Options.CompareFilename.GetValueOrDefault())
                {
                    // Group by filename
                    GroupByFilename();

                    if (ScanBase.FilesGroupedByFilename.Count == 0)
                    {
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetInfo(completedSucessfully);
                        SetLastScanElapsed(dtStart);

                        return;
                    }

                    Settings.Default.lastScanErrors = ScanBase.FilesGroupedByFilename.Count;
                }

                if (ScanBase.Options.CompareChecksum.GetValueOrDefault() || ScanBase.Options.CompareChecksumFilename.GetValueOrDefault())
                {
                    // Group by filename and/or checksum
                    GroupByChecksum();

                    if (ScanBase.FilesGroupedByHash.Count == 0)
                    {
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetInfo(completedSucessfully);
                        SetLastScanElapsed(dtStart);

                        return;
                    }

                    Settings.Default.lastScanErrors = ScanBase.FilesGroupedByHash.Count;
                }

                if (ScanBase.Options.CompareMusicTags.GetValueOrDefault())
                {
                    // Group by audio tags
                    GroupByTags();

                    if (ScanBase.FilesGroupedByHash.Count == 0)
                    {
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetInfo(completedSucessfully);
                        SetLastScanElapsed(dtStart);

                        return;
                    }

                    Settings.Default.lastScanErrors = ScanBase.FilesGroupedByHash.Count;
                }

                SetLastScanElapsed(dtStart);
                completedSucessfully = true;
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            finally
            {
                ResetInfo(completedSucessfully);
            }
        }

        public void AbortScanThread()
        {
            if ((_threadScan != null) && _threadScan.IsAlive)
                _threadScan.Abort();
        }

        private void ResetInfo(bool success)
        {
            Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.None));
            CurrentFile = "";

            if (success) 
            {
                StatusText = "View the results by clicking \"Continue\" below.";
                Dispatcher.Invoke(new Action(() => ButtonContinue.IsEnabled = true));
            }
            else
            {
                StatusText = "Click \"Cancel\" to go back to the previous screen.";
            }
                
        }

        private void SetLastScanElapsed(DateTime dtStart)
        {
            Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;
        }

        /// <summary>
        /// Builds list of files
        /// </summary>
        /// <param name="dtStart">Date/time scan started</param>
        /// <returns>True if file list created. Otherwise, false if no directories or files were located.</returns>
        private bool BuildFileList(DateTime dtStart)
        {
            if (ScanBase.Options.AllDrives.GetValueOrDefault())
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
            else if (ScanBase.Options.AllExceptDrives.GetValueOrDefault())
            {
                bool driveSelected = false;

                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (!di.IsReady || di.TotalFreeSpace == 0 || di.DriveType == DriveType.NoRootDirectory)
                        continue;

                    if (ScanBase.Options.AllExceptSystem.GetValueOrDefault() && Environment.SystemDirectory[0] == di.RootDirectory.Name[0])
                        continue;

                    if (ScanBase.Options.AllExceptRemovable.GetValueOrDefault() && di.DriveType == DriveType.Removable)
                        continue;

                    if (ScanBase.Options.AllExceptNetwork.GetValueOrDefault() && di.DriveType == DriveType.Network)
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
            else if (ScanBase.Options.OnlySelectedDrives.GetValueOrDefault())
            {
                bool driveSelected = false;

                foreach (IncludeDrive incDrive in ScanBase.Options.Drives.Where(incDrive => incDrive.IsChecked.GetValueOrDefault()))
                {
                    if (!driveSelected)
                        driveSelected = true;

                    try
                    {
                        DriveInfo di = new DriveInfo(incDrive.Name);

                        if (!di.IsReady || di.TotalFreeSpace == 0 || di.DriveType == DriveType.NoRootDirectory)
                            continue;

                        if (ScanBase.Options.Drives.Contains(new IncludeDrive(di)))
                            RecurseDirectory(di.RootDirectory);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to scan drive ({0}). The following error occurred: {1}", incDrive.Name, ex.Message);
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

                foreach (IncludeFolder dir in ScanBase.Options.IncFolders.Where(dir => dir.IsChecked.GetValueOrDefault()))
                {
                    if (!dirSelected)
                        dirSelected = true;

                    RecurseDirectory(dir.DirInfo);
                }

                if (!dirSelected)
                {
                    Utils.MessageBoxThreadSafe("No folders are selected.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan Selected Folders", (int)DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }

            if (_fileList.Count != 0)
                return true;

            Utils.MessageBoxThreadSafe("No files were found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                
            return false;
        }

        private void RecurseDirectory(DirectoryInfo di)
        {
            if (!di.Exists || di.FullName.Length > 248)
                return;

            if (ScanBase.Options.ExcludeFolders.Contains(new ExcludeFolder(di.FullName)))
                return;

            try
            {
                string[] files = Directory.GetFiles(di.FullName);

                if (files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        CurrentFile = file;

                        if (file.Length > 260)
                            continue;

                        try
                        {
                            FileInfo fi = new FileInfo(file);

                            if (ScanBase.Options.SkipZeroByteFiles.GetValueOrDefault() && fi.Length == 0)
                                continue;

                            if (!ScanBase.Options.IncHiddenFiles.GetValueOrDefault() && (fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                continue;

                            if (IsSizeGreaterThan(fi.Length))
                                continue;

                            if ((ScanBase.Options.SkipCompressedFiles.GetValueOrDefault()) && IsCompressedFile(fi))
                                continue;

                            FileEntry fileEntry = new FileEntry(fi, ScanBase.Options.CompareMusicTags.GetValueOrDefault());
                            _fileList.Add(fileEntry);
                        }
                        catch (UnauthorizedAccessException)
                        {

                        }
                        catch (SecurityException)
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
                throw;
#endif
            }
            

            if (ScanBase.Options.ScanSubDirs.GetValueOrDefault()) // Will stop if false and only include root directory
            {
                try
                {
                    string[] dirs = Directory.GetDirectories(di.FullName);

                    if (dirs.Length > 0)
                    {
                        foreach (string dir in dirs)
                        {
                            CurrentFile = dir;

                            if (dir.Length > 248)
                                continue;

                            try
                            {
                                DirectoryInfo dirInfo = new DirectoryInfo(dir);

                                if (!ScanBase.Options.IncHiddenFiles.GetValueOrDefault() && (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                    continue;

                                RecurseDirectory(dirInfo);
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                Debug.WriteLine("Could not access directories: {0}", (object)ex.Message);
                            }
                            catch (SecurityException ex)
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
                    throw;
#endif
                }
            }
        }

        private bool IsSizeGreaterThan(long size)
        {
            if (!ScanBase.Options.SkipFilesGreaterThan.GetValueOrDefault())
                return false;

            long maxSize = ScanBase.Options.SkipFilesGreaterSize;
            double multiplier = 1;

            // Get size in bytes
            switch (ScanBase.Options.SkipFilesGreaterUnit)
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
            return false;
        }

        /// <summary>
        /// Checks if file is a compressed file
        /// </summary>
        /// <param name="fileInfo">FileInfo class</param>
        /// <returns>True if file matches compressed file extension and signature</returns>
        private bool IsCompressedFile(FileInfo fileInfo)
        {
            // Get file extension
            string fileExt = fileInfo?.Extension;

            if (string.IsNullOrWhiteSpace(fileExt))
                return false;

            if (!CompressedFiles.ContainsKey(fileExt))
                return false;

            int offset = CompressedFiles[fileExt].Key;

            byte[] expectedSignature = CompressedFiles[fileExt].Value;
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
                fileStream?.Close();
            }

            if (expectedSignature.SequenceEqual(actualSignature))
                return true;

            return false;
        }

        private void GroupByFilename()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Main.TaskbarProgressState != TaskbarItemProgressState.Indeterminate)
                    Main.TaskbarProgressState = TaskbarItemProgressState.Indeterminate;

                if (!ProgressBar.IsIndeterminate)
                    ProgressBar.IsIndeterminate = true;
            }));

            StatusText = "Grouping files by filename";

            Main.Watcher.Event("Duplicate Finder", "Group by filename");

            ScanBase.FilesGroupedByFilename.Clear();

            var query = from p in _fileList
                        where p.IsDeleteable
                        group p by p.FileName into g
                        where g.Count() > 1
                        select new { FileName = g.Key, Files = g };

            foreach (var group in query)
            {
                if (!string.IsNullOrEmpty(group.FileName) && group.Files.Any())
                    ScanBase.FilesGroupedByFilename.Add(group.FileName, group.Files.ToList());
            }
        }

        private void GroupByChecksum()
        {
            Dictionary<long, List<FileEntry>> filesGroupedBySize = new Dictionary<long, List<FileEntry>>();
            bool compareFilename = ScanBase.Options.CompareChecksumFilename.GetValueOrDefault();
            long totalFiles = 0;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Main.TaskbarProgressState != TaskbarItemProgressState.Indeterminate)
                    Main.TaskbarProgressState = TaskbarItemProgressState.Indeterminate;

                if (!ProgressBar.IsIndeterminate)
                    ProgressBar.IsIndeterminate = true;
            }));

            StatusText = "Grouping files by size";
            CurrentFile = "Please wait...";

            Main.Watcher.Event("Duplicate Finder", "Group by checksum");

            ScanBase.FilesGroupedByHash.Clear();
            
            var query2 = from p in _fileList
                            where p.IsDeleteable
                            group p by p.FileSize into g
                            where g.Count() > 1
                            select new { FileSize = g.Key, Files = g };

            foreach (var group in query2)
            {
                if (!@group.Files.Any())
                    continue;
                
                List<FileEntry> filesGroup = @group.Files.ToList();

                filesGroupedBySize.Add(@group.FileSize, filesGroup);

                totalFiles += filesGroup.Count;
            }

            if (filesGroupedBySize.Count == 0)
                // Nothing found
                return;

            StatusText = "Getting checksums from files";

            // Sort file sizes from least to greatest
            var fileSizesSorted = from p in filesGroupedBySize
                        orderby p.Key ascending
                        select p.Value;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
                Main.TaskbarProgressValue = 0D;

                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 0;
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = totalFiles;
            }));

            foreach (List<FileEntry> files in fileSizesSorted)
            {
                if (files.Count > 0)
                {
                    foreach (FileEntry file in files)
                    {
                        CurrentFile = file.FilePath;

                        file.GetChecksum(ScanBase.Options.HashAlgorithm.Algorithm, compareFilename);

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ProgressBar.Value += 1;
                            Main.TaskbarProgressValue = (ProgressBar.Value / ProgressBar.Maximum);
                        }));
                    }

                    var query3 = from p in files
                                 group p by p.Checksum into g
                                 where g.Count() > 1
                                 select new { Checksum = g.Key, Files = g.ToList() };

                    foreach (var group in query3)
                    {
                        if (!string.IsNullOrEmpty(group.Checksum) && group.Files.Any())
                        {
                            if (!ScanBase.FilesGroupedByHash.ContainsKey(group.Checksum))
                                ScanBase.FilesGroupedByHash.Add(group.Checksum, group.Files);
                            else
                            {
                                List<FileEntry> existingKey = ScanBase.FilesGroupedByHash[group.Checksum];

                                var mergedList = existingKey.Union(group.Files).Distinct();

                                ScanBase.FilesGroupedByHash[group.Checksum] = mergedList.ToList();
                            }
                        }

                    }
                }
            }
        }

        private void GroupByTags()
        {
            List<FileEntry> fileEntriesTags = new List<FileEntry>();

            StatusText = "Getting checksums from audio files";
            CurrentFile = "Please wait...";

            Main.Watcher.Event("Duplicate Finder", "Group by audio tags");

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Main.TaskbarProgressState != TaskbarItemProgressState.Indeterminate)
                    Main.TaskbarProgressState = TaskbarItemProgressState.Indeterminate;

                if (!ProgressBar.IsIndeterminate)
                    ProgressBar.IsIndeterminate = true;
            }));
            
            foreach (FileEntry fileEntry in _fileList)
            {
                if (fileEntry.IsDeleteable && fileEntry.HasAudioTags)
                {
                    fileEntry.GetTagsChecksum(ScanBase.Options);
                    fileEntriesTags.Add(fileEntry);
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBar.Value += 1;
                    Main.TaskbarProgressValue = (ProgressBar.Value / ProgressBar.Maximum);
                }));
            }

            StatusText = "Grouping files by audio tags";
            CurrentFile = "Please wait...";

            var query = from p in fileEntriesTags
                        where p.HasAudioTags
                        group p by p.TagsChecksum into g
                        where g.Count() > 1
                        select new { Checksum = g.Key, Files = g.ToList() };

            foreach (var group in query)
            {
                string checksum = group.Checksum;
                List<FileEntry> files = group.Files;

                if (!string.IsNullOrEmpty(checksum) && files.Count > 0)
                    ScanBase.FilesGroupedByHash.Add(checksum, files);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AbortScanThread();
                ScanBase.MovePrev();
            }
        }

        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            ScanBase.MoveNext();
        }
    }
}

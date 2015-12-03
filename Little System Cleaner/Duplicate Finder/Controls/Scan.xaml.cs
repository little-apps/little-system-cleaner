using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using Little_System_Cleaner.Duplicate_Finder.Helpers;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    ///     Interaction logic for Scan.xaml
    /// </summary>
    public partial class Scan : INotifyPropertyChanged
    {
        internal static List<string> ValidAudioFiles = new List<string>
        {
            "aac",
            "aif",
            "ape",
            "wma",
            "aa",
            "aax",
            "flac",
            "mka",
            "mpc",
            "mp+",
            "mpp",
            "mp4",
            "m4a",
            "ogg",
            "oga",
            "wav",
            "wv",
            "mp3",
            "m2a",
            "mp2",
            "mp1"
        };

        internal static List<string> ValidImageFiles = new List<string>
        {
            "jpg",
            "jpeg",
            "png",
            "gif",
            "bmp",
            "ico"
        };

        internal static Dictionary<string, KeyValuePair<int, byte[]>> CompressedFiles = new Dictionary
            <string, KeyValuePair<int, byte[]>>
        {
            {"cab", new KeyValuePair<int, byte[]>(0, new byte[] {0x4d, 0x53, 0x43, 0x46})},
            {"zip", new KeyValuePair<int, byte[]>(0, new byte[] {0x50, 0x4B, 0x03, 0x04})},
            {"jar", new KeyValuePair<int, byte[]>(0, new byte[] {0x50, 0x4B, 0x03, 0x04})},
            {"rar", new KeyValuePair<int, byte[]>(0, new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07})},
            {"tar", new KeyValuePair<int, byte[]>(257, new byte[] {0x75, 0x73, 0x74, 0x61, 0x72})}, // offset 257
            {"gz", new KeyValuePair<int, byte[]>(0, new byte[] {0x1F, 0x8B, 0x08})},
            {"tgz", new KeyValuePair<int, byte[]>(0, new byte[] {0x1F, 0x8B, 0x08})},
            {"bz", new KeyValuePair<int, byte[]>(0, new byte[] {0x42, 0x5A, 0x68})},
            {"tbz", new KeyValuePair<int, byte[]>(0, new byte[] {0x42, 0x5A, 0x68})},
            {"bz2", new KeyValuePair<int, byte[]>(0, new byte[] {0x42, 0x5A, 0x68})},
            {"tbz2", new KeyValuePair<int, byte[]>(0, new byte[] {0x42, 0x5A, 0x68})},
            {"xz", new KeyValuePair<int, byte[]>(0, new byte[] {0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00})},
            {"xar", new KeyValuePair<int, byte[]>(0, new byte[] {0x78, 0x61, 0x72, 0x21})},
            {"rpm", new KeyValuePair<int, byte[]>(0, new byte[] {0xED, 0xAB, 0xEE, 0xDB})}
        };

        private readonly List<FileEntry> _fileList;

        private readonly Task _taskScan;
        private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private string _currentFile;

        private string _statusText;

        private bool _statusTextCanChange = true;

        public Wizard ScanBase;

        public Scan(Wizard sb)
        {
            InitializeComponent();

            ScanBase = sb;

            _fileList = new List<FileEntry>();

            // Clear previous results
            ScanBase.FilesGroupedByFilename.Clear();
            ScanBase.FilesGroupedByHash.Clear();

            // Increase total number of scans
            Settings.Default.totalScans++;

            // Zero last scan errors found + fixed and elapsed
            Settings.Default.lastScanErrors = 0;
            Settings.Default.lastScanErrorsFixed = 0;
            Settings.Default.lastScanElapsed = 0;

            // Set last scan date
            Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            _taskScan = new Task(ScanDisk, _cancelTokenSource.Token);
            _taskScan.Start();

            //_threadScan = new Thread(ScanDisk);
            //_threadScan.Start();
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (!_statusTextCanChange)
                    return;

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

        private void ScanDisk()
        {
            var completedSucessfully = false;

            try
            {
                var dtStart = DateTime.Now;
                Dispatcher.BeginInvoke(
                    new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.Indeterminate));
                StatusText = "Building list of all files";

                if (!BuildFileList(dtStart))
                {
                    ResetInfo(false);
                    SetLastScanElapsed(dtStart);
                }

                if (ScanBase.Options.CompareFilename.GetValueOrDefault())
                {
                    // Group by filename
                    GroupByFilename();

                    if (ScanBase.FilesGroupedByFilename.Count == 0)
                    {
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetInfo(false);
                        SetLastScanElapsed(dtStart);

                        return;
                    }

                    Settings.Default.lastScanErrors = ScanBase.FilesGroupedByFilename.Count;
                }

                if (ScanBase.Options.CompareChecksum.GetValueOrDefault() ||
                    ScanBase.Options.CompareChecksumFilename.GetValueOrDefault())
                {
                    // Group by filename and/or checksum
                    GroupByChecksum();

                    if (ScanBase.FilesGroupedByHash.Count == 0)
                    {
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetInfo(false);
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
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetInfo(false);
                        SetLastScanElapsed(dtStart);

                        return;
                    }

                    Settings.Default.lastScanErrors = ScanBase.FilesGroupedByHash.Count;
                }

                if (ScanBase.Options.CompareImages.GetValueOrDefault())
                {
                    // Group by pixels
                    GroupByImage();

                    if (ScanBase.FilesGroupedByHash.Count == 0)
                    {
                        Utils.MessageBoxThreadSafe("No duplicate files could be found.", Utils.ProductName,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetInfo(false);
                        SetLastScanElapsed(dtStart);

                        return;
                    }

                    Settings.Default.lastScanErrors = ScanBase.FilesGroupedByHash.Count;
                }

                _cancelTokenSource.Token.ThrowIfCancellationRequested();

                SetLastScanElapsed(dtStart);
                completedSucessfully = true;
            }
            catch (OperationCanceledException)
            {
                //Thread.ResetAbort();
            }
            finally
            {
                _cancelTokenSource.Dispose();
                _cancelTokenSource = null;

                ResetInfo(completedSucessfully);
            }
        }

        public void AbortScanTask()
        {
            _cancelTokenSource?.Cancel();

            StatusText = "Please wait while the scan operation is being cancelled...";
            CurrentFile = "";

            _statusTextCanChange = false;
        }

        private void ResetInfo(bool success)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Main.TaskbarProgressState = TaskbarItemProgressState.None;
                ProgressBar.IsIndeterminate = false;
                TextBlockPleaseWait.Visibility = Visibility.Hidden;
            }));
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

        private static void SetLastScanElapsed(DateTime dtStart)
        {
            Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;
        }

        /// <summary>
        ///     Builds list of files
        /// </summary>
        /// <param name="dtStart">Date/time scan started</param>
        /// <returns>True if file list created. Otherwise, false if no directories or files were located.</returns>
        private bool BuildFileList(DateTime dtStart)
        {
            if (ScanBase.Options.AllDrives.GetValueOrDefault())
            {
                var driveSelected = false;

                foreach (
                    var di in
                        DriveInfo.GetDrives()
                            .Where(
                                di =>
                                    di.IsReady && di.TotalFreeSpace != 0 &&
                                    (di.DriveType == DriveType.Fixed || di.DriveType == DriveType.Network ||
                                     di.DriveType == DriveType.Removable)))
                {
                    if (!driveSelected)
                        driveSelected = true;

                    RecurseDirectory(di.RootDirectory);
                }

                _cancelTokenSource.Token.ThrowIfCancellationRequested();

                if (!driveSelected)
                {
                    Utils.MessageBoxThreadSafe("No disk drives could be found to scan.", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan All Drives",
                    (int) DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }
            else if (ScanBase.Options.AllExceptDrives.GetValueOrDefault())
            {
                var driveSelected = false;

                foreach (
                    var di in
                        DriveInfo.GetDrives()
                            .Where(
                                di =>
                                    di.IsReady && di.TotalFreeSpace != 0 &&
                                    di.DriveType != DriveType.NoRootDirectory)
                            .Where(di =>
                                !ScanBase.Options.AllExceptSystem.GetValueOrDefault() ||
                                Environment.SystemDirectory[0] != di.RootDirectory.Name[0])
                            .Where(di =>
                                !ScanBase.Options.AllExceptRemovable.GetValueOrDefault() ||
                                di.DriveType != DriveType.Removable)
                            .Where(di =>
                                !ScanBase.Options.AllExceptNetwork.GetValueOrDefault() ||
                                di.DriveType != DriveType.Network))
                {
                    if (!driveSelected)
                        driveSelected = true;

                    RecurseDirectory(di.RootDirectory);
                }

                _cancelTokenSource.Token.ThrowIfCancellationRequested();

                if (!driveSelected)
                {
                    Utils.MessageBoxThreadSafe("No disk drives could be found to scan.", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan All Drives Except",
                    (int) DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }
            else if (ScanBase.Options.OnlySelectedDrives.GetValueOrDefault())
            {
                var driveSelected = false;

                foreach (
                    var incDrive in ScanBase.Options.Drives.Where(incDrive => incDrive.IsChecked.GetValueOrDefault()))
                {
                    if (!driveSelected)
                        driveSelected = true;

                    try
                    {
                        var di = new DriveInfo(incDrive.Name);

                        if (!di.IsReady || di.TotalFreeSpace == 0 || di.DriveType == DriveType.NoRootDirectory)
                            continue;

                        if (ScanBase.Options.Drives.Contains(new IncludeDrive(di)))
                            RecurseDirectory(di.RootDirectory);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to scan drive ({0}). The following error occurred: {1}", incDrive.Name,
                            ex.Message);
                    }
                }

                _cancelTokenSource.Token.ThrowIfCancellationRequested();

                if (!driveSelected)
                {
                    Utils.MessageBoxThreadSafe("No disk drives could be found to scan.", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan Selected Drives",
                    (int) DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }
            else // Only selected folders
            {
                var dirSelected = false;

                foreach (var dir in ScanBase.Options.IncFolders.Where(dir => dir.IsChecked.GetValueOrDefault()))
                {
                    if (!dirSelected)
                        dirSelected = true;

                    RecurseDirectory(dir.DirInfo);
                }

                _cancelTokenSource.Token.ThrowIfCancellationRequested();

                if (!dirSelected)
                {
                    Utils.MessageBoxThreadSafe("No folders are selected.", Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return false;
                }

                Main.Watcher.EventPeriod("Duplicate Finder", "Scan Selected Folders",
                    (int) DateTime.Now.Subtract(dtStart).TotalSeconds, true);
            }

            if (_fileList.Count != 0)
                return true;

            Utils.MessageBoxThreadSafe("No files were found.", Utils.ProductName, MessageBoxButton.OK,
                MessageBoxImage.Information);

            return false;
        }

        private void RecurseDirectory(DirectoryInfo di)
        {
            if (_cancelTokenSource.IsCancellationRequested)
                return;

            if (!di.Exists || di.FullName.Length > 248)
                return;

            if (ScanBase.Options.ExcludeFolders.Contains(new ExcludeFolder(di.FullName)))
                return;

            try
            {
                foreach (
                    var file in
                        Directory.GetFiles(di.FullName).TakeWhile(file => !_cancelTokenSource.IsCancellationRequested))
                {
                    CurrentFile = file;

                    if (file.Length > 260)
                        continue;

                    try
                    {
                        var fi = new FileInfo(file);

                        if (ScanBase.Options.SkipZeroByteFiles.GetValueOrDefault() && fi.Length == 0)
                            continue;

                        if (!ScanBase.Options.IncHiddenFiles.GetValueOrDefault() &&
                            (fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        if (IsSizeGreaterThan(fi.Length))
                            continue;

                        if (ScanBase.Options.SkipCompressedFiles.GetValueOrDefault() && IsCompressedFile(fi))
                            continue;

                        _fileList.Add(new FileEntry(fi, ScanBase.Options.ScanMethod));
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
                            Debug.WriteLine("Path ({0}) is too long", (object) file);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine("The following I/O error occurred reading files: {0}", (object) ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following unknown exception occurred reading files: {0}", (object) ex.Message);

#if (DEBUG)
                throw;
#endif
            }


            if (!ScanBase.Options.ScanSubDirs.GetValueOrDefault())
                return;

            try
            {
                foreach (
                    var dir in
                        Directory.GetDirectories(di.FullName)
                            .TakeWhile(dir => !_cancelTokenSource.IsCancellationRequested))
                {
                    CurrentFile = dir;

                    if (dir.Length > 248)
                        continue;

                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);

                        if (!ScanBase.Options.IncHiddenFiles.GetValueOrDefault() &&
                            (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        RecurseDirectory(dirInfo);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Debug.WriteLine("Could not access directories: {0}", (object) ex.Message);
                    }
                    catch (SecurityException ex)
                    {
                        Debug.WriteLine("A security exception error occurred reading directories: {0}",
                            (object) ex.Message);
                    }
                    catch (IOException ex)
                    {
                        if (ex is PathTooLongException)
                        {
                            // Just in case
                            Debug.WriteLine("Path ({0}) is too long", (object) dir);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine("The following I/O error occurred reading directories: {0}", (object) ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following unknown exception occurred reading directories: {0}", (object) ex.Message);

#if (DEBUG)
                throw;
#endif
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

            var maxSizeBytes = maxSize*(long) multiplier;

            return size > maxSizeBytes;
        }

        /// <summary>
        ///     Checks if file is a compressed file
        /// </summary>
        /// <param name="fileInfo">FileInfo class</param>
        /// <returns>True if file matches compressed file extension and signature</returns>
        private bool IsCompressedFile(FileInfo fileInfo)
        {
            // Get file extension
            var fileExt = fileInfo?.Extension;

            if (string.IsNullOrWhiteSpace(fileExt))
                return false;

            if (!CompressedFiles.ContainsKey(fileExt))
                return false;

            var offset = CompressedFiles[fileExt].Key;

            var expectedSignature = CompressedFiles[fileExt].Value;
            var expectedSignatureLen = expectedSignature.Length;

            var actualSignature = new byte[expectedSignatureLen];

            FileStream fileStream = null;

            try
            {
                // Open file
                fileStream = fileInfo.OpenRead();

                if (!fileStream.CanRead)
                    throw new IOException("Unable to read file stream");

                if (!fileStream.CanSeek)
                    throw new IOException("Unable to seek file stream");

                if (offset + expectedSignatureLen >= fileStream.Length)
                    throw new IOException("File is smaller than offset and size of expected signature");

                if (fileStream.Seek(offset, SeekOrigin.Begin) != offset)
                    throw new IOException("There was an error seeking to position " + offset);

                var bytesToRead = expectedSignatureLen;
                var bytesRead = 0;

                while (bytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead. 
                    var n = fileStream.Read(actualSignature, bytesRead, bytesToRead);

                    // Break when the end of the file is reached. 
                    if (n == 0)
                        break;

                    bytesRead += n;
                    bytesToRead -= n;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred trying to read file ({0}): {1}", fileInfo.FullName,
                    ex.Message);
            }
            finally
            {
                fileStream?.Close();
            }

            return expectedSignature.SequenceEqual(actualSignature);
        }

        private void GroupByImage()
        {
            StatusText = "Determing if files are images";
            
            var countFileEntries = _fileList.Count(fileEntry => fileEntry.IsImage());

            if (countFileEntries == 0)
                // No images
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Main.TaskbarProgressState == TaskbarItemProgressState.Indeterminate)
                    Main.TaskbarProgressState = TaskbarItemProgressState.Normal;

                if (ProgressBar.IsIndeterminate)
                    ProgressBar.IsIndeterminate = false;
                
                ProgressBar.Maximum = countFileEntries;
            }));

            StatusText = "Analyzing images";

            var i = 0;

            foreach (
                    var fileEntry in
                        _fileList.Where(
                            fileEntry => fileEntry.IsImage())
                            .OrderBy(fileEntry => fileEntry.FileSize)
                            .TakeWhile(fileEntry => !_cancelTokenSource.IsCancellationRequested))
            {
                Dispatcher.BeginInvoke(new Action<int>(currentIndex =>
                {
                    CurrentFile = fileEntry.FilePath;

                    ProgressBar.Value = currentIndex;
                    Main.TaskbarProgressValue = currentIndex / (double)countFileEntries;
                }), i++);

                fileEntry.AnalyzeImage();
            }

            if (_cancelTokenSource.IsCancellationRequested)
                return;

            i = 0;
            
            StatusText = "Grouping files by pixels";

            Main.Watcher.Event("Duplicate Finder", "Group by pixels");

            // TODO: Improve memory usage
            foreach (
                var fileEntry1 in
                    _fileList.Where(fileEntry => fileEntry.IsImage())
                        .TakeWhile(fileEntry => !_cancelTokenSource.IsCancellationRequested))
            {
                Dispatcher.BeginInvoke(new Action<int>(currentIndex =>
                {
                    CurrentFile = fileEntry1.FilePath;

                    ProgressBar.Value = currentIndex;
                    Main.TaskbarProgressValue = currentIndex / (double)countFileEntries;
                }), i++);

                var likeImages =
                    _fileList.Where(
                        fileEntry2 =>
                            fileEntry2.IsImage() &&
                            fileEntry1.FilePath != fileEntry2.FilePath &&
                            fileEntry1.CompareImages(fileEntry2) > ScanBase.Options.CompareImagesMinPercent).ToList();

                if (likeImages.Count <= 0)
                    continue;

                likeImages.Add(fileEntry1);

                ScanBase.FilesGroupedByHash.Add(fileEntry1.FilePath, likeImages);
            }
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

            /*var query = from p in _fileList
                        where p.IsDeleteable
                        group p by p.FileName into g
                        where g.Count() > 1
                        select new { FileName = g.Key, Files = g };*/

            if (_cancelTokenSource.IsCancellationRequested)
                return;

            var groupedByFilename = _fileList
                .Where(fileEntry => fileEntry.IsDeleteable)
                .GroupBy(fileEntry => fileEntry.FileName)
                .Where(g => g.Count() > 1)
                .Select(g => new {FileName = g.Key, Files = g})
                .Where(@group => !string.IsNullOrEmpty(@group.FileName) && @group.Files.Any());

            foreach (var @group in groupedByFilename.TakeWhile(@group => !_cancelTokenSource.IsCancellationRequested))
            {
                ScanBase.FilesGroupedByFilename.Add(@group.FileName, @group.Files.ToList());
            }
        }

        private void GroupByChecksum()
        {
            var filesGroupedBySize = new Dictionary<long, List<FileEntry>>();
            var compareFilename = ScanBase.Options.CompareChecksumFilename.GetValueOrDefault();
            long totalFiles = 0;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Main.TaskbarProgressState != TaskbarItemProgressState.Indeterminate)
                    Main.TaskbarProgressState = TaskbarItemProgressState.Indeterminate;

                if (!ProgressBar.IsIndeterminate)
                    ProgressBar.IsIndeterminate = true;
            }));

            if (_cancelTokenSource.IsCancellationRequested)
                return;

            StatusText = "Grouping files by size";
            CurrentFile = "Please wait...";

            Main.Watcher.Event("Duplicate Finder", "Group by checksum");

            /*var query2 = from p in _fileList
                            where p.IsDeleteable
                            group p by p.FileSize into g
                            where g.Count() > 1
                            select new { FileSize = g.Key, Files = g };*/

            var groupedByFileSize = _fileList
                .Where(fileEntry => fileEntry.IsDeleteable)
                .GroupBy(fileEntry => fileEntry.FileSize)
                .Where(g => g.Count() > 1)
                .Select(g => new {FileSize = g.Key, Files = g})
                .Where(g => g.Files.Any());

            foreach (var group in groupedByFileSize)
            {
                /*if (!@group.Files.Any())
                    continue;*/

                if (_cancelTokenSource.IsCancellationRequested)
                    return;

                var filesGroup = @group.Files.ToList();

                filesGroupedBySize.Add(@group.FileSize, filesGroup);

                totalFiles += filesGroup.Count;
            }

            if (filesGroupedBySize.Count == 0)
                // Nothing found
                return;

            StatusText = "Getting checksums from files";

            // Sort file sizes from least to greatest
            /*var fileSizesSorted = from p in filesGroupedBySize
                        orderby p.Key ascending
                        select p.Value;*/

            var sortedByFileSize = filesGroupedBySize
                .OrderBy(g => g.Key)
                .Select(g => g.Value)
                .Where(files => files.Count > 0);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
                Main.TaskbarProgressValue = 0D;

                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 0;
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = totalFiles;
            }));

            foreach (var files in sortedByFileSize)
            {
                foreach (var file in files)
                {
                    if (_cancelTokenSource.IsCancellationRequested)
                        return;

                    CurrentFile = file.FilePath;

                    file.GetChecksum(ScanBase.Options.HashAlgorithm.Algorithm, compareFilename);

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ProgressBar.Value += 1;
                        Main.TaskbarProgressValue = ProgressBar.Value/ProgressBar.Maximum;
                    }));
                }

                /*var query3 = from p in files
                                 group p by p.Checksum into g
                                 where g.Count() > 1
                                 select new { Checksum = g.Key, Files = g.ToList() };*/

                var groupedByChecksum = files
                    .GroupBy(fileEntry => fileEntry.Checksum)
                    .Where(g => g.Count() > 1)
                    .Select(g => new {Checksum = g.Key, Files = g.ToList()})
                    .Where(group => !string.IsNullOrEmpty(@group.Checksum) && @group.Files.Any());

                foreach (var @group in groupedByChecksum)
                {
                    if (_cancelTokenSource.IsCancellationRequested)
                        return;

                    if (!ScanBase.FilesGroupedByHash.ContainsKey(@group.Checksum))
                        ScanBase.FilesGroupedByHash.Add(@group.Checksum, @group.Files);
                    else
                    {
                        var existingKey = ScanBase.FilesGroupedByHash[@group.Checksum];

                        var mergedList = existingKey.Union(@group.Files).Distinct();

                        ScanBase.FilesGroupedByHash[@group.Checksum] = mergedList.ToList();
                    }
                }
            }
        }

        private void GroupByTags()
        {
            var fileEntriesTags = new List<FileEntry>();

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

            foreach (var fileEntry in _fileList)
            {
                if (_cancelTokenSource.IsCancellationRequested)
                    return;

                if (fileEntry.IsDeleteable && fileEntry.HasAudioTags)
                {
                    fileEntry.GetTagsChecksum(ScanBase.Options);
                    fileEntriesTags.Add(fileEntry);
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBar.Value += 1;
                    Main.TaskbarProgressValue = ProgressBar.Value/ProgressBar.Maximum;
                }));
            }

            StatusText = "Grouping files by audio tags";
            CurrentFile = "Please wait...";

            /*var query = from p in fileEntriesTags
                        where p.HasAudioTags
                        group p by p.TagsChecksum into g
                        where g.Count() > 1
                        select new { Checksum = g.Key, Files = g.ToList() };*/

            var groupedByTagsChecksum = fileEntriesTags
                .Where(fileEntry => fileEntry.HasAudioTags)
                .GroupBy(fileEntry => fileEntry.TagsChecksum)
                .Where(group => group.Count() > 1)
                .Select(group => new {Checksum = group.Key, Files = group.ToList()});

            foreach (var group in groupedByTagsChecksum)
            {
                if (_cancelTokenSource.IsCancellationRequested)
                    return;

                var checksum = group.Checksum;
                var files = group.Files;

                if (!string.IsNullOrEmpty(checksum) && files.Count > 0)
                    ScanBase.FilesGroupedByHash.Add(checksum, files);
            }
        }

        private async void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to cancel?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            AbortScanTask();

            await _taskScan;
            ScanBase.MoveFirst();
        }

        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            ScanBase.MoveNext();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
    }
}
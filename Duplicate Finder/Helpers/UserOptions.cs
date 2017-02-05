﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Shared;

namespace Duplicate_Finder.Helpers
{
    [XmlRoot("Options")]
    public class UserOptions : INotifyPropertyChanged
    {
        private bool? _allExceptDrives = true;
        private ExcludeFolder _excFolderSelected;

        private HashAlgorithm _hashAlgorithm;

        private ObservableCollection<HashAlgorithm> _hashAlgorithms;

        private IncludeFolder _incFolderSelected;
        private bool? _onlySelectedDrives = false;
        private bool? _onlySelectedFolders = false;

        public enum ScanMethods
        {
            ChecksumFilename = 0,
            Checksum = 1,
            Filename = 2,
            MusicTags = 3,
            Images = 4
        }

        public ScanMethods ScanMethod;

        private int _skipFilesGreaterSize = 512;
        private bool? _skipFilesGreaterThan = true;
        private string _skipFilesGreaterUnit = "MB";
        private bool? _skipSysAppDirs = false;

        private bool? _skipTempFiles = false;
        private bool? _skipWindowsDir = false;

        /// <summary>
        /// Hidden constructor for <see cref="UserOptions"/>
        /// </summary>
        /// <remarks>UserOptions can only be initialized by calling <see cref="GetUserOptions"/></remarks>
        private UserOptions()
        {
        }

        public static void StoreUserOptions(UserOptions userOptions)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(UserOptions));
                serializer.Serialize(ms, userOptions);

                var xml = Convert.ToBase64String(ms.ToArray());
                Settings.Default.duplicateFinderOptions = xml;
                Settings.Default.Save();
            }
        }

        public static UserOptions GetUserOptions()
        {
            UserOptions userOptions;

            try
            {
                using (var ms = new MemoryStream(Convert.FromBase64String(Settings.Default.duplicateFinderOptions)))
                {
                    var serializer = new XmlSerializer(typeof(UserOptions));
                    userOptions = (UserOptions)serializer.Deserialize(ms);
                }
            }
            catch
            {
                userOptions = new UserOptions();
            }

            return userOptions;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion INotifyPropertyChanged Members

        #region Drives/Folders Properties

        public ObservableCollection<IncludeDrive> Drives { get; } = new ObservableCollection<IncludeDrive>();

        public ObservableCollection<IncludeFolder> IncFolders { get; set; } = new ObservableCollection<IncludeFolder>();

        public IncludeFolder IncludeFolderSelected
        {
            get { return _incFolderSelected; }
            set
            {
                _incFolderSelected = value;
                OnPropertyChanged(nameof(IncludeFolderSelected));
            }
        }

        public bool? AllDrives { get; set; } = false;

        public bool? AllExceptDrives
        {
            get { return _allExceptDrives; }
            set
            {
                _allExceptDrives = value;
                OnPropertyChanged(nameof(AllExceptEnabled));
            }
        }

        public bool? AllExceptSystem { get; set; } = true;

        public bool? AllExceptRemovable { get; set; } = true;

        public bool? AllExceptNetwork { get; set; } = true;

        public bool? OnlySelectedDrives
        {
            get { return _onlySelectedDrives; }
            set
            {
                _onlySelectedDrives = value;
                OnPropertyChanged(nameof(SelectedDrivesEnabled));
            }
        }

        public bool? OnlySelectedFolders
        {
            get { return _onlySelectedFolders; }
            set
            {
                _onlySelectedFolders = value;
                OnPropertyChanged(nameof(SelectedFoldersEnabled));
            }
        }

        public bool AllExceptEnabled => AllExceptDrives.GetValueOrDefault();

        public bool SelectedDrivesEnabled => OnlySelectedDrives.GetValueOrDefault();

        public bool SelectedFoldersEnabled => OnlySelectedFolders.GetValueOrDefault();

        #endregion Drives/Folders Properties

        #region Files Properties

        public bool? CompareChecksumFilename
        {
            get { return ScanMethod == ScanMethods.ChecksumFilename; }
            set
            {
                if (value.GetValueOrDefault())
                    ScanMethod = ScanMethods.ChecksumFilename;
            }
        }

        public bool? CompareChecksum
        {
            get { return ScanMethod == ScanMethods.Checksum; }
            set
            {
                if (value.GetValueOrDefault())
                    ScanMethod = ScanMethods.Checksum;
            }
        }

        public bool? CompareFilename
        {
            get { return ScanMethod == ScanMethods.Filename; }
            set
            {
                if (value.GetValueOrDefault())
                    ScanMethod = ScanMethods.Filename;
            }
        }

        public bool? CompareMusicTags
        {
            get { return ScanMethod == ScanMethods.MusicTags; }
            set
            {
                if (value.GetValueOrDefault())
                    ScanMethod = ScanMethods.MusicTags;

                OnPropertyChanged(nameof(MusicTagsEnabled));
            }
        }

        public bool? CompareImages
        {
            get { return ScanMethod == ScanMethods.Images; }
            set
            {
                if (value.GetValueOrDefault())
                    ScanMethod = ScanMethods.Images;

                OnPropertyChanged(nameof(CompareImagesOptionsEnabled));
            }
        }

        public bool? SkipTempFiles
        {
            get { return _skipTempFiles; }
            set
            {
                _skipTempFiles = value;

                string[] excFolders =
                {
                    Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine),
                    Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User)
                };

                foreach (var excFolderPath in excFolders)
                {
                    var excFolder = new ExcludeFolder(excFolderPath, true);
                    var index = ExcludeFolders.IndexOf(excFolder);

                    if (value.GetValueOrDefault())
                    {
                        if (index == -1)
                            ExcludeFolders.Add(excFolder);
                        else if (ExcludeFolders[index].ReadOnly == false)
                            ExcludeFolders[index].ReadOnly = true;
                    }
                    else
                    {
                        if (index != -1)
                            ExcludeFolders.RemoveAt(index);
                    }
                }

                OnPropertyChanged(nameof(SkipTempFiles));
                OnPropertyChanged(nameof(ExcludeFolders));
            }
        }

        public bool? ScanSubDirs { get; set; } = true;

        public bool? SkipSysAppDirs
        {
            get { return _skipSysAppDirs; }
            set
            {
                _skipSysAppDirs = value;

                string[] excFolders =
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), // Program files directory
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    // Program files (x86) directory
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), // Programdata directory
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), // AppData directory
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                };

                foreach (var excFolderPath in excFolders)
                {
                    var excFolder = new ExcludeFolder(excFolderPath, true);
                    var index = ExcludeFolders.IndexOf(excFolder);

                    if (value.GetValueOrDefault())
                    {
                        if (index == -1)
                            ExcludeFolders.Add(excFolder);
                        else if (ExcludeFolders[index].ReadOnly == false)
                            ExcludeFolders[index].ReadOnly = true;
                    }
                    else
                    {
                        if (index != -1)
                            ExcludeFolders.RemoveAt(index);
                    }
                }

                OnPropertyChanged(nameof(SkipSysAppDirs));
                OnPropertyChanged(nameof(ExcludeFolders));
            }
        }

        public bool? SkipZeroByteFiles { get; set; } = true;

        public bool? IncHiddenFiles { get; set; } = false;

        public bool? SkipCompressedFiles { get; set; } = false;

        public bool? SkipWindowsDir
        {
            get { return _skipWindowsDir; }
            set
            {
                _skipWindowsDir = value;

                string[] excFolders =
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows)
                };

                foreach (var excFolderPath in excFolders)
                {
                    var excFolder = new ExcludeFolder(excFolderPath, true);
                    var index = ExcludeFolders.IndexOf(excFolder);

                    if (value.GetValueOrDefault())
                    {
                        if (index == -1)
                            ExcludeFolders.Add(excFolder);
                        else if (ExcludeFolders[index].ReadOnly == false)
                            ExcludeFolders[index].ReadOnly = true;
                    }
                    else
                    {
                        if (index != -1)
                            ExcludeFolders.RemoveAt(index);
                    }
                }

                OnPropertyChanged(nameof(SkipWindowsDir));
                OnPropertyChanged(nameof(ExcludeFolders));
            }
        }

        public bool? SkipFilesGreaterThan
        {
            get { return _skipFilesGreaterThan; }
            set
            {
                _skipFilesGreaterThan = value;
                OnPropertyChanged(nameof(SkipFilesGreaterThan));
                OnPropertyChanged(nameof(SkipFilesGreaterEnabled));
            }
        }

        public int SkipFilesGreaterSize
        {
            get { return _skipFilesGreaterSize; }
            set
            {
                _skipFilesGreaterSize = value;
                OnPropertyChanged(nameof(SkipFilesGreaterSize));
            }
        }

        public string[] SkipFilesGreaterUnits => new[] { "B", "KB", "MB", "GB" };

        public string SkipFilesGreaterUnit
        {
            get { return _skipFilesGreaterUnit; }
            set
            {
                _skipFilesGreaterUnit = value;
                OnPropertyChanged(nameof(SkipFilesGreaterUnit));
            }
        }

        public bool SkipFilesGreaterEnabled => SkipFilesGreaterThan.GetValueOrDefault();

        [XmlIgnore]
        public HashAlgorithm HashAlgorithm
        {
            get { return _hashAlgorithm; }
            set
            {
                _hashAlgorithm = value;
                OnPropertyChanged(nameof(HashAlgorithm));
            }
        }

        [XmlIgnore]
        public ObservableCollection<HashAlgorithm> HashAlgorithms
        {
            get { return _hashAlgorithms; }
            set
            {
                _hashAlgorithms = value;
                OnPropertyChanged(nameof(HashAlgorithm));
            }
        }

        #endregion Files Properties

        #region Music Tags Properties

        public bool MusicTagsEnabled => CompareMusicTags.GetValueOrDefault();

        public bool? MusicTagTitle { get; set; } = true;

        public bool? MusicTagYear { get; set; } = false;

        public bool? MusicTagArtist { get; set; } = true;

        public bool? MusicTagGenre { get; set; } = false;

        public bool? MusicTagAlbum { get; set; } = false;

        public bool? MusicTagDuration { get; set; } = false;

        public bool? MusicTagTrackNo { get; set; } = false;

        public bool? MusicTagBitRate { get; set; } = false;

        #endregion Music Tags Properties

        #region Compare Images Pixels by Pixels Options

        public bool CompareImagesOptionsEnabled => CompareImages.GetValueOrDefault();
        public decimal CompareImagesMinPercent { get; set; } = 0.95M;

        #endregion Compare Images Pixels by Pixels Options

        #region Exclude Folders Properties

        public ObservableCollection<ExcludeFolder> ExcludeFolders { get; } = new ObservableCollection<ExcludeFolder>();

        public ExcludeFolder ExcludeFolderSelected
        {
            get { return _excFolderSelected; }
            set
            {
                _excFolderSelected = value;
                OnPropertyChanged(nameof(ExcludeFolderSelected));
            }
        }

        #endregion Exclude Folders Properties
    }
}
using Little_System_Cleaner.Duplicate_Finder.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class UserOptions : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private ObservableCollection<IncludeDrive> _drives = new ObservableCollection<IncludeDrive>();
        private ObservableCollection<IncludeFolder> _incFolders = new ObservableCollection<IncludeFolder>();
        private ObservableCollection<HashAlgorithm> _hashAlgorithms;

        private IncludeFolder _incFolderSelected;
        private ExcludeFolder _excFolderSelected;

        private bool? _allDrives = false;
        private bool? _allExceptDrives = true;
        private bool? _allExceptSystem = true;
        private bool? _allExceptRemovable = true;
        private bool? _allExceptNetwork = true;
        private bool? _onlySelectedDrives = false;
        private bool? _onlySelectedFolders = false;

        private bool? _compareChecksumFilename = false;
        private bool? _compareChecksum = true;
        private bool? _compareFilename = false;
        private bool? _compareMusicTags = false;

        private bool? _skipTempFiles = false;
        private bool? _scanSubDirs = true;
        private bool? _skipSysAppDirs = false;
        private bool? _skipZeroByteFiles = true;
        private bool? _incHiddenFiles = false;
        private bool? _skipCompressedFiles = false;
        private bool? _skipWindowsDir = false;
        private bool? _skipFilesGreaterThan = true;
        private int _skipFilesGreaterSize = 512;
        private string _skipFilesGreaterUnit = "MB";

        private HashAlgorithm _hashAlgorithm;

        private bool? _musicTagTitle = true;
        private bool? _musicTagYear = false;
        private bool? _musicTagArtist = true;
        private bool? _musicTagGenre = false;
        private bool? _musicTagAlbum = false;
        private bool? _musicTagDuration = false;
        private bool? _musicTagTrackNo = false;
        private bool? _musicTagBitRate = false;

        private ObservableCollection<ExcludeFolder> _excFolders = new ObservableCollection<ExcludeFolder>();

        #region Drives/Folders Properties
        public ObservableCollection<IncludeDrive> Drives
        {
            get { return this._drives; }
        }

        public ObservableCollection<IncludeFolder> IncFolders
        {
            get { return this._incFolders; }
        }

        public IncludeFolder IncludeFolderSelected
        {
            get { return this._incFolderSelected; }
            set
            {
                this._incFolderSelected = value;
                this.OnPropertyChanged("IncludeFolderSelected");
            }
        }

        public bool? AllDrives
        {
            get { return this._allDrives; }
            set { this._allDrives = value; }
        }

        public bool? AllExceptDrives
        {
            get { return this._allExceptDrives; }
            set
            {
                this._allExceptDrives = value;
                this.OnPropertyChanged("AllExceptEnabled");
            }
        }

        public bool? AllExceptSystem
        {
            get { return this._allExceptSystem; }
            set { this._allExceptSystem = value; }
        }
        public bool? AllExceptRemovable
        {
            get { return this._allExceptRemovable; }
            set { this._allExceptRemovable = value; }
        }
        public bool? AllExceptNetwork
        {
            get { return this._allExceptNetwork; }
            set { this._allExceptNetwork = value; }
        }
        public bool? OnlySelectedDrives
        {
            get { return this._onlySelectedDrives; }
            set
            {
                this._onlySelectedDrives = value;
                this.OnPropertyChanged("SelectedDrivesEnabled");
            }
        }
        public bool? OnlySelectedFolders
        {
            get { return this._onlySelectedFolders; }
            set
            {
                this._onlySelectedFolders = value;
                this.OnPropertyChanged("SelectedFoldersEnabled");
            }
        }

        public bool AllExceptEnabled
        {
            get
            {
                return (this.AllExceptDrives.GetValueOrDefault());
            }
        }

        public bool SelectedDrivesEnabled
        {
            get
            {
                return (this.OnlySelectedDrives.GetValueOrDefault());
            }
        }

        public bool SelectedFoldersEnabled
        {
            get
            {
                return (this.OnlySelectedFolders.GetValueOrDefault());
            }
        }
        #endregion

        #region Files Properties
        public bool? CompareChecksumFilename
        {
            get { return this._compareChecksumFilename; }
            set { this._compareChecksumFilename = value; }
        }

        public bool? CompareChecksum
        {
            get { return this._compareChecksum; }
            set { this._compareChecksum = value; }
        }
        public bool? CompareFilename
        {
            get { return this._compareFilename; }
            set { this._compareFilename = value; }
        }
        public bool? CompareMusicTags
        {
            get { return this._compareMusicTags; }
            set
            {
                this._compareMusicTags = value;
                this.OnPropertyChanged("MusicTagsEnabled");
            }
        }

        public bool? SkipTempFiles
        {
            get { return this._skipTempFiles; }
            set
            {
                this._skipTempFiles = value;

                string[] excFolders = new string[] {
                        Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine),
                        Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User)
                    };

                foreach (string excFolderPath in excFolders)
                {
                    ExcludeFolder excFolder = new ExcludeFolder(excFolderPath, true);
                    int index = this.ExcludeFolders.IndexOf(excFolder);

                    if (value.GetValueOrDefault())
                    {
                        if (index == -1)
                            this.ExcludeFolders.Add(excFolder);
                        else if (this.ExcludeFolders[index].ReadOnly == false)
                            this.ExcludeFolders[index].ReadOnly = true;
                    }
                    else
                    {
                        if (index != -1)
                            this.ExcludeFolders.RemoveAt(index);
                    }
                }

                this.OnPropertyChanged("SkipTempFiles");
                this.OnPropertyChanged("ExcludeFolders");
            }
        }

        public bool? ScanSubDirs
        {
            get { return this._scanSubDirs; }
            set { this._scanSubDirs = value; }
        }
        public bool? SkipSysAppDirs
        {
            get { return this._skipSysAppDirs; }
            set
            {
                this._skipSysAppDirs = value;

                string[] excFolders = new string[] {
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), // Program files directory
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), 
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), // Program files (x86) directory
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), // Programdata directory
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), // AppData directory
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                    };

                foreach (string excFolderPath in excFolders)
                {
                    ExcludeFolder excFolder = new ExcludeFolder(excFolderPath, true);
                    int index = this.ExcludeFolders.IndexOf(excFolder);

                    if (value.GetValueOrDefault())
                    {
                        if (index == -1)
                            this.ExcludeFolders.Add(excFolder);
                        else if (this.ExcludeFolders[index].ReadOnly == false)
                            this.ExcludeFolders[index].ReadOnly = true;
                    }
                    else
                    {
                        if (index != -1)
                            this.ExcludeFolders.RemoveAt(index);
                    }
                }

                this.OnPropertyChanged("SkipSysAppDirs");
                this.OnPropertyChanged("ExcludeFolders");
            }
        }
        public bool? SkipZeroByteFiles
        {
            get { return this._skipZeroByteFiles; }
            set { this._skipZeroByteFiles = value; }
        }
        public bool? IncHiddenFiles
        {
            get { return this._incHiddenFiles; }
            set { this._incHiddenFiles = value; }
        }
        public bool? SkipCompressedFiles
        {
            get { return this._skipCompressedFiles; }
            set { this._skipCompressedFiles = value; }
        }
        public bool? SkipWindowsDir
        {
            get { return this._skipWindowsDir; }
            set
            {
                this._skipWindowsDir = value;

                string[] excFolders = new string[] {
                        Environment.GetFolderPath(Environment.SpecialFolder.Windows)
                    };

                foreach (string excFolderPath in excFolders)
                {
                    ExcludeFolder excFolder = new ExcludeFolder(excFolderPath, true);
                    int index = this.ExcludeFolders.IndexOf(excFolder);

                    if (value.GetValueOrDefault())
                    {
                        if (index == -1)
                            this.ExcludeFolders.Add(excFolder);
                        else if (this.ExcludeFolders[index].ReadOnly == false)
                            this.ExcludeFolders[index].ReadOnly = true;
                    }
                    else
                    {
                        if (index != -1)
                            this.ExcludeFolders.RemoveAt(index);
                    }
                }

                this.OnPropertyChanged("SkipWindowsDir");
                this.OnPropertyChanged("ExcludeFolders");
            }
        }

        public bool? SkipFilesGreaterThan
        {
            get { return this._skipFilesGreaterThan; }
            set
            {
                this._skipFilesGreaterThan = value;
                this.OnPropertyChanged("SkipFilesGreaterThan");
                this.OnPropertyChanged("SkipFilesGreaterEnabled");
            }
        }

        public int SkipFilesGreaterSize
        {
            get { return this._skipFilesGreaterSize; }
            set
            {
                this._skipFilesGreaterSize = value;
                this.OnPropertyChanged("SkipFilesGreaterSize");
            }
        }

        public string[] SkipFilesGreaterUnits
        {
            get
            {
                return new string[] { "B", "KB", "MB", "GB" };
            }
        }

        public string SkipFilesGreaterUnit
        {
            get { return this._skipFilesGreaterUnit; }
            set
            {
                this._skipFilesGreaterUnit = value;
                this.OnPropertyChanged("SkipFilesGreaterUnit");
            }
        }

        public bool SkipFilesGreaterEnabled
        {
            get { return (this.SkipFilesGreaterThan.GetValueOrDefault()); }
        }

        public HashAlgorithm HashAlgorithm
        {
            get { return this._hashAlgorithm; }
            set
            {
                this._hashAlgorithm = value;
                this.OnPropertyChanged("HashAlgorithm");
            }
        }

        public ObservableCollection<HashAlgorithm> HashAlgorithms
        {
            get { return this._hashAlgorithms; }
            set
            {
                this._hashAlgorithms = value;
                this.OnPropertyChanged("HashAlgorithms");
            }
        }
        #endregion

        #region Music Tags Properties
        public bool MusicTagsEnabled
        {
            get { return (this.CompareMusicTags.GetValueOrDefault()); }
        }

        public bool? MusicTagTitle
        {
            get { return this._musicTagTitle; }
            set { this._musicTagTitle = value; }
        }
        public bool? MusicTagYear
        {
            get { return this._musicTagYear; }
            set { this._musicTagYear = value; }
        }
        public bool? MusicTagArtist
        {
            get { return this._musicTagArtist; }
            set { this._musicTagArtist = value; }
        }
        public bool? MusicTagGenre
        {
            get { return this._musicTagGenre; }
            set { this._musicTagGenre = value; }
        }
        public bool? MusicTagAlbum
        {
            get { return this._musicTagAlbum; }
            set { this._musicTagAlbum = value; }
        }
        public bool? MusicTagDuration
        {
            get { return this._musicTagDuration; }
            set { this._musicTagDuration = value; }
        }
        public bool? MusicTagTrackNo
        {
            get { return this._musicTagTrackNo; }
            set { this._musicTagTrackNo = value; }
        }
        public bool? MusicTagBitRate
        {
            get { return this._musicTagBitRate; }
            set { this._musicTagBitRate = value; }
        }
        #endregion

        #region Exclude Folders Properties
        public ObservableCollection<ExcludeFolder> ExcludeFolders
        {
            get { return this._excFolders; }
        }

        public ExcludeFolder ExcludeFolderSelected
        {
            get { return this._excFolderSelected; }
            set
            {
                this._excFolderSelected = value;
                this.OnPropertyChanged("ExcludeFolderSelected");
            }
        }
        #endregion

        public UserOptions()
        {
        }
    }
}

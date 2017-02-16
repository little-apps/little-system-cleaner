using System;
using System.Windows;
using Duplicate_Finder.Helpers;
using Shared;

namespace Duplicate_Finder.Controls
{
    /// <summary>
    ///     Interaction logic for FileInfo.xaml
    /// </summary>
    public partial class Details
    {
        private readonly FileEntry _fileEntry;
        private readonly Wizard _scanBase;

        public Details(Wizard scanBase, FileEntry fileEntry)
        {
            _scanBase = scanBase;
            _fileEntry = fileEntry;

            InitializeComponent();
        }

        private void buttonGoBack_Click(object sender, RoutedEventArgs e)
        {
            _scanBase.HideFileInfo();
        }

        #region File information

        public string FileName => _fileEntry.FileName;

        public string Size => Utils.ConvertSizeToString(_fileEntry.FileSize);
        public string FilePath => _fileEntry.FilePath;

        #endregion File information

        #region Audio information

        public bool HasAudioTags => _fileEntry.HasAudioTags;

        public string Artist
        {
            get
            {
                if (_fileEntry.HasAudioTags && !string.IsNullOrEmpty(_fileEntry.Artist))
                    return _fileEntry.Artist;
                return "N/A";
            }
        }

        public string Title
        {
            get
            {
                if (_fileEntry.HasAudioTags && !string.IsNullOrEmpty(_fileEntry.Title))
                    return _fileEntry.Title;
                return "N/A";
            }
        }

        public string Year
        {
            get
            {
                if (_fileEntry.HasAudioTags && _fileEntry.Year > 0)
                    return Convert.ToString(_fileEntry.Year);
                return "N/A";
            }
        }

        public string Genre
        {
            get
            {
                if (_fileEntry.HasAudioTags && !string.IsNullOrEmpty(_fileEntry.Genre))
                    return _fileEntry.Genre;
                return "N/A";
            }
        }

        public string Album
        {
            get
            {
                if (_fileEntry.HasAudioTags && !string.IsNullOrEmpty(_fileEntry.Album))
                    return _fileEntry.Album;
                return "N/A";
            }
        }

        public string Duration
        {
            get
            {
                if (_fileEntry.HasAudioTags && _fileEntry.Duration.TotalSeconds > 0)
                    return _fileEntry.Duration.ToString();
                return "N/A";
            }
        }

        public string TrackNo
        {
            get
            {
                if (_fileEntry.HasAudioTags && _fileEntry.TrackNo > 0)
                    return Convert.ToString(_fileEntry.TrackNo);
                return "N/A";
            }
        }

        public string Bitrate
        {
            get
            {
                if (_fileEntry.HasAudioTags && _fileEntry.Bitrate > 0)
                    return $"{_fileEntry.Bitrate} kbps";
                return "N/A";
            }
        }

        #endregion Audio information
    }
}
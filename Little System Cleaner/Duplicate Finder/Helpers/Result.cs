using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class Result : INotifyPropertyChanged
    {
        #region File Types
        private static readonly Dictionary<string, string> MimeTypesDictionary = new Dictionary<string, string> 
        {
            {"ai", "application/postscript"},
            {"aif", "Audio"},
            {"aifc", "Audio"},
            {"aiff", "Audio"},
            {"atom", "Atom Syndication File"},
            {"au", "Audio"},
            {"avi", "Video"},
            {"bin", "Binary File"},
            {"bmp", "Picture"},
            {"cgm", "Picture"},
            {"csh", "Shell Script"},
            {"css", "Cascading Style Sheet"},
            {"dcr", "Video"},
            {"dif", "Video"},
            {"dir", "Video"},
            {"dll", "Dynamically Linked Library (Should not be removed)"},
            {"dmg", "Mac Disk Image"},
            {"doc", "Document"},
            {"docx","Document"},
            {"dotx", "Document"},
            {"docm","Document"},
            {"dotm","Document"},
            {"dtd", "Document Type Definition"},
            {"dv", "Video"},
            {"dvi", "Document"},
            {"dxr", "Video"},
            {"eps", "Picture"},
            {"exe", "Executable file"},
            {"gif", "Picture"},
            {"gtar", "Compressed File"},
            {"htm", "Webpage"},
            {"html", "Webpage"},
            {"ice", "x-conference/x-cooltalk"},
            {"ico", "Icon"},
            {"ics", "iCalendar File"},
            {"ief", "Picture"},
            {"ifb", "iCalendar File"},
            {"jnlp", "application/x-java-jnlp-file"},
            {"jp2", "Picture"},
            {"jpe", "Picture"},
            {"jpeg", "Picture"},
            {"jpg", "Picture"},
            {"js", "JavaScript File"},
            {"kar", "Audio"},
            {"latex", "Document"},
            {"lha", "Compressed File"},
            {"lzh", "Compressed File"},
            {"m3u", "Audio Playlist"},
            {"m4a", "Audio"},
            {"m4b", "Audio"},
            {"m4p", "Audio"},
            {"m4u", "Video"},
            {"m4v", "Video"},
            {"mac", "Picture"},
            {"man", "Document"},
            {"mid", "Audio"},
            {"midi", "Audio"},
            {"mov", "Video"},
            {"movie", "Video"},
            {"mp2", "Audio"},
            {"mp3", "Audio"},
            {"mp4", "Video"},
            {"mpe", "Video"},
            {"mpeg", "Video"},
            {"mpg", "Video"},
            {"mpga", "Audio"},
            {"mxu", "Video"},
            {"oda", "Document"},
            {"ogg", "Audio"},
            {"pbm", "Picture"},
            {"pct", "Picture"},
            {"pdf", "Document"},
            {"pgm", "Picture"},
            {"pic", "Picture"},
            {"pict", "Picture"},
            {"png", "Picture"}, 
            {"pnm", "Picture"},
            {"pnt", "Picture"},
            {"pntg", "Picture"},
            {"ppm", "Picture"},
            {"ppt", "Presentation"},
            {"pptx","Presentation"},
            {"potx","Presentation"},
            {"ppsx","Presentation"},
            {"ppam","Presentation"},
            {"pptm","Presentation"},
            {"potm","Presentation"},
            {"ppsm","Presentation"},
            {"ps", "Postscript"},
            {"qt", "Video"},
            {"qti", "Picture"},
            {"qtif", "Picture"},
            {"ra", "Audio"},
            {"ram", "Audio"},
            {"rar", "Compressed File"},
            {"ras", "Picture"},
            {"rgb", "Picture"},
            {"rm", "Video"},
            {"rtf", "Document"},
            {"rtx", "Document"},
            {"sh", "Shell Script"},
            {"snd", "Audio"},
            {"so", "Shared Object"},
            {"svg", "Picture"},
            {"swf", "Video"},
            {"tar", "Compressed File"},
            {"tex", "Document"},
            {"texi", "Document"},
            {"texinfo", "Document"},
            {"tif", "Picture"},
            {"tiff", "Picture"},
            {"tsv", "Spreadsheet"},
            {"txt", "Document"},
            {"vcd", "Virtual CD"},
            {"wav", "Audio"},
            {"wbmp", "Picture"},
            {"xbm", "Picture"},
            {"xht", "Webpage"},
            {"xhtml", "Webpage"},
            {"xls", "Spreadsheet"}, 
            {"xml", "Document"},
            {"xpm", "Picture"},
            {"xsl", "eXtensible Markup Language File"},
            {"xlsx","Spreadsheet"},
            {"xltx","Spreadsheet"},
            {"xlsm","Spreadsheet"},
            {"xltm","Spreadsheet"},
            {"xlam","Spreadsheet"},
            {"xlsb","Spreadsheet"},
            {"xwd", "Picture"},
            {"zip", "Compressed File"}  
        };

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public ObservableCollection<Result> Children { get; } = new ObservableCollection<Result>();

        public bool IsParent => (Children.Count > 0);

        private readonly FileEntry _fileEntry;
        private bool? _bIsChecked = false;

        #region IsChecked Methods
        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _bIsChecked)
                return;

            _bIsChecked = value;

            if (updateChildren && _bIsChecked.HasValue)
                Children.ToList().ForEach(c => c.SetIsChecked(_bIsChecked, true, false));

            if (updateParent)
                Parent?.VerifyCheckState();

            OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < Children.Count; ++i)
            {
                bool? current = Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            SetIsChecked(state, false, true);
        }
        #endregion

        public bool? IsChecked
        {
            get { return _bIsChecked; }
            set { SetIsChecked(value, true, true); }
        }

        public Result Parent { get; set; }

        public string FileName
        {
            get
            {
                if (_fileEntry != null) 
                {
                    return FileEntry.FileName;
                }

                if (!IsParent)
                    return string.Empty;

                string firstFileName = Children.First().FileName;

                if (Children.All(s => string.Equals(s.FileName, firstFileName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return firstFileName;
                }

                var groupedByFilenames = Children.GroupBy(s => s.FileName.ToLower()).Select(g => g.Key).ToArray();

                string fileNames = string.Join(", ", groupedByFilenames);

                return ShortenStringForDisplay(fileNames);
            }
        }

        public string FileSize
        {
            get
            {
                if (_fileEntry != null)
                {
                    return Utils.ConvertSizeToString(FileEntry.FileSize, false);
                }
                if (IsParent)
                {
                    string firstFileSize = Children.First().FileSize;

                    if (Children.All(s => s.FileSize == firstFileSize)) 
                    {
                        return firstFileSize;
                    }
                    var groupedByFileSizes = Children.GroupBy(s => s.FileSize).Select(g => g.Key).ToArray();

                    string fileSizes = string.Join(", ", groupedByFileSizes);

                    return ShortenStringForDisplay(fileSizes);
                }

                return string.Empty;
            }
        }

        public string FileFormat
        {
            get
            {
                if (_fileEntry != null)
                {
                    string fileExt = Path.GetExtension(FileEntry.FilePath);

                    if (string.IsNullOrEmpty(fileExt))
                        return "(No Extension)";

                    fileExt = fileExt.Substring(1).ToLower();

                    string ext;

                    if (MimeTypesDictionary.ContainsKey(fileExt))
                        ext = MimeTypesDictionary[fileExt] + " (." + fileExt.ToUpper() + ")";
                    else
                        ext = "." + fileExt.ToUpper() + " File";

                    return ext;
                }
                if (IsParent)
                {
                    string firstFileFormat = Children.First().FileFormat;

                    if (Children.All(s => s.FileFormat == firstFileFormat)) 
                    {
                        return firstFileFormat;
                    }
                    var groupedByFileFormats = Children.GroupBy(s => s.FileFormat).Select(g => g.Key).ToArray();

                    string fileFormats = string.Join(", ", groupedByFileFormats);

                    return ShortenStringForDisplay(fileFormats);
                }
                return string.Empty;
            }
        }

        public string FilePath => _fileEntry != null ? _fileEntry.FilePath : string.Empty;

        public FileEntry FileEntry => _fileEntry;

        public Result(Result parent = null)
        {
            Parent = parent;
        }

        public Result(FileEntry fileEntry, Result parent)
        {
            _fileEntry = fileEntry;
            Parent = parent;
        }

        private string ShortenStringForDisplay(string str)
        {
            int maxLength = 30;
            string appendText = "...";

            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length > maxLength)
                str = str.Substring(0, maxLength - appendText.Length) + appendText;

            return str;
        }
    }
}

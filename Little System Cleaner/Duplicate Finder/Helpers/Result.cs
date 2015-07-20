using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class Result : INotifyPropertyChanged
    {
        #region File Types
        private static readonly Dictionary<string, string> MIMETypesDictionary = new Dictionary<string, string> 
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
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly ObservableCollection<Result> _children = new ObservableCollection<Result>();
        public ObservableCollection<Result> Children
        {
            get { return _children; }
        }

        public bool IsParent
        {
            get
            {
                return (this.Children.Count > 0);
            }
        }

        private readonly FileEntry _fileEntry;
        private bool? _bIsChecked = false;

        #region IsChecked Methods
        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _bIsChecked)
                return;

            _bIsChecked = value;

            if (updateChildren && _bIsChecked.HasValue)
                this.Children.ToList().ForEach(c => c.SetIsChecked(_bIsChecked, true, false));

            if (updateParent && Parent != null)
                Parent.VerifyCheckState();

            this.OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool? current = this.Children[i].IsChecked;
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
            this.SetIsChecked(state, false, true);
        }
        #endregion

        public bool? IsChecked
        {
            get { return _bIsChecked; }
            set { this.SetIsChecked(value, true, true); }
        }

        public Result Parent { get; set; }

        public string FileName
        {
            get
            {
                if (this._fileEntry != null) 
                {
                    return this.FileEntry.FileName;
                }
                else if (this.IsParent)
                {
                    string firstFileName = this.Children.First().FileName;

                    if (this.Children.All(s => s.FileName.ToLower() == firstFileName.ToLower()))
                    {
                        return firstFileName;
                    }
                    else
                    {
                        var groupedByFilenames = this.Children.GroupBy(s => s.FileName.ToLower()).Select(g => g.Key).ToArray<string>();

                        string fileNames = string.Join(", ", groupedByFilenames);

                        return this.ShortenStringForDisplay(fileNames);
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string FileSize
        {
            get
            {
                if (this._fileEntry != null)
                {
                    return Utils.ConvertSizeToString(this.FileEntry.FileSize, false);
                }
                else if (this.IsParent)
                {
                    string firstFileSize = this.Children.First().FileSize;

                    if (this.Children.All(s => s.FileSize == firstFileSize)) 
                    {
                        return firstFileSize;
                    }
                    else
                    {
                        var groupedByFileSizes = this.Children.GroupBy(s => s.FileSize).Select(g => g.Key).ToArray<string>();

                        string fileSizes = string.Join(", ", groupedByFileSizes);

                        return this.ShortenStringForDisplay(fileSizes);
                    }
                }

                return string.Empty;
            }
        }

        public string FileFormat
        {
            get
            {
                if (this._fileEntry != null)
                {
                    string fileExt = Path.GetExtension(this.FileEntry.FilePath);

                    if (string.IsNullOrEmpty(fileExt))
                        return "(No Extension)";

                    fileExt = fileExt.Substring(1).ToLower();

                    string ext;

                    if (Result.MIMETypesDictionary.ContainsKey(fileExt))
                        ext = Result.MIMETypesDictionary[fileExt] + " (." + fileExt.ToUpper() + ")";
                    else
                        ext = "." + fileExt.ToUpper() + " File";

                    return ext;
                }
                else if (this.IsParent)
                {
                    string firstFileFormat = this.Children.First().FileFormat;

                    if (this.Children.All(s => s.FileFormat == firstFileFormat)) 
                    {
                        return firstFileFormat;
                    }
                    else
                    {
                        var groupedByFileFormats = this.Children.GroupBy(s => s.FileFormat).Select(g => g.Key).ToArray<string>();

                        string fileFormats = string.Join(", ", groupedByFileFormats);

                        return this.ShortenStringForDisplay(fileFormats);
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string FilePath
        {
            get 
            {
                if (this._fileEntry != null)
                    return this._fileEntry.FilePath;
                else
                    // File paths are always going to be different
                    return string.Empty;
            }
        }

        public FileEntry FileEntry
        {
            get { return this._fileEntry; }
        }

        public Result(Result parent = null)
        {
            this.Parent = parent;
        }

        public Result(FileEntry fileEntry, Result parent)
        {
            this._fileEntry = fileEntry;
            this.Parent = parent;
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

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
                    return this.FileEntry.FileName;
                else
                    return string.Empty;
            }
        }

        public string FileSize
        {
            get
            {
                if (this._fileEntry != null)
                    return Utils.ConvertSizeToString(this.FileEntry.FileSize, false);
                else
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
                else
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
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class ExcludeFolder : INotifyPropertyChanged, IEquatable<ExcludeFolder>
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private string _folderPath;

        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                _folderPath = value;
                OnPropertyChanged("FolderPath");
            }
        }

        public bool ReadOnly { get; set; }

        public ExcludeFolder()
        {

        }

        public ExcludeFolder(string folderPath, bool readOnly = false) 
        {
            FolderPath = folderPath;
            ReadOnly = readOnly;
        }

        public bool Equals(ExcludeFolder other)
        {
            return other != null && FolderPath == other.FolderPath;
        }

        public bool Equals(string other)
        {
            return !string.IsNullOrEmpty(other) && FolderPath == other;
        }

        public override bool Equals(object obj)
        {
            var a = obj as ExcludeFolder;
            if (a != null)
                return Equals(a);

            var s = obj as string;
            return s != null && Equals(s);
        }

        public override int GetHashCode()
        {
            return FolderPath.GetHashCode();
        }

        [Obsolete]
        internal static ObservableCollection<ExcludeFolder> GetDefaultExcFolders()
        {
            var excFolders = new ObservableCollection<ExcludeFolder>();

            string[] folderPaths =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), // Windows directory
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), // Program files directory
                Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), // Program files (x86) directory
                Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), // Programdata directory
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) // AppData directory
            };

            excFolders.AddRange(
                folderPaths.Select(folderPath => new ExcludeFolder(folderPath, true))
                    .Where(excFolder => !excFolders.Contains(excFolder)));

            return excFolders;
        }
    }
}

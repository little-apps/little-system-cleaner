using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class ExcludeFolder : INotifyPropertyChanged, IEquatable<ExcludeFolder>
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private string _folderPath;
        private bool _readOnly;

        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                _folderPath = value;
                OnPropertyChanged("FolderPath");
            }
        }

        public bool ReadOnly
        {
            get { return this._readOnly; }
            set { this._readOnly = value; }
        }

        public ExcludeFolder(string folderPath, bool readOnly = false) 
        {
            this.FolderPath = folderPath;
            this.ReadOnly = readOnly;
        }

        public bool Equals(ExcludeFolder other)
        {
            return (other != null && this.FolderPath == other.FolderPath);
        }

        public bool Equals(string other)
        {
            return (!string.IsNullOrEmpty(other) && this.FolderPath == other);
        }

        public override bool Equals(object obj)
        {
            if (obj is ExcludeFolder)
                return Equals(obj as ExcludeFolder);
            else if (obj is string)
                return Equals(obj as string);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return this.FolderPath.GetHashCode();
        }

        public static ObservableCollection<ExcludeFolder> GetDefaultExcFolders()
        {
            ObservableCollection<ExcludeFolder> excFolders = new ObservableCollection<ExcludeFolder>();

            string[] folderPaths = {
                                       Environment.GetFolderPath(Environment.SpecialFolder.Windows), // Windows directory
                                       Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), // Program files directory
                                       Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), 
                                       Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), // Program files (x86) directory
                                       Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
                                       Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), // Programdata directory
                                       Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) // AppData directory
                                   };

            foreach (string folderPath in folderPaths)
            {
                ExcludeFolder excFolder = new ExcludeFolder(folderPath, true);
                if (!excFolders.Contains(excFolder))
                    excFolders.Add(excFolder);
            }

            return excFolders;
        }
    }
}

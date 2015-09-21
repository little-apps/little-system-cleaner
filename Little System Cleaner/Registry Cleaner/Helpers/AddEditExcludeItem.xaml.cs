/*
    Little System Cleaner
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using Little_System_Cleaner.Misc;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    /// <summary>
    /// Interaction logic for AddExcludeItem.xaml
    /// </summary>
    public partial class AddEditExcludeItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private string _winTitle = "";
        private string _addEditText;
        private string _rootKey;
        private string _subKey;
        private string _filePath;
        private string _folderPath;

        public ExcludeItem ExcludeItem
        {
            get
            {
                ExcludeItem excItem = new ExcludeItem();

                if (SelectedType == ExcludeTypes.Registry)
                    excItem.RegistryPath = RegistryPath;
                else if (SelectedType == ExcludeTypes.File)
                    excItem.FilePath = FilePath;
                else
                    excItem.FolderPath = FolderPath;

                return excItem;
            }
        }

        public ExcludeTypes SelectedType { get; }

        public ObservableCollection<string> RootKeys { get; } = new ObservableCollection<string>
        {
            "HKEY_CLASSES_ROOT",
            "HKEY_CURRENT_USER",
            "HKEY_LOCAL_MACHINE",
            "HKEY_USERS"
        };

        public string WindowTitle
        {
            get { return _winTitle; }
            set
            {
                _winTitle = value;

                OnPropertyChanged("WindowTitle");
            }
        }

        public string AddEditText
        {
            get { return _addEditText; }
            set
            {
                _addEditText = value;

                OnPropertyChanged("AddEditText");
            }
        }

        public string Description
        {
            get
            {
                string text = "Please enter a ";

                switch (SelectedType)
                {
                    case ExcludeTypes.Registry:
                        text += "registry";
                        break;
                    case ExcludeTypes.Folder:
                        text += "folder";
                        break;
                    case ExcludeTypes.File:
                        text += "file";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                text += " path. Wildcards are supported. (a question mark (?) represents a single character and an asterisk (*) represents 0 or more characters)";

                return text;
            }
        }

        public string RootKey
        {
            get { return _rootKey; }
            set
            {
                _rootKey = value;

                OnPropertyChanged("RootKey");
            }
        }

        public string FilePath
        {
            get { return _filePath; }
            set
            {
                string val = (string.IsNullOrEmpty(value) ? string.Empty : value.Trim());
                _filePath = val;

                OnPropertyChanged("FilePath");
            }
        }

        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                string val = (string.IsNullOrEmpty(value) ? string.Empty : value.Trim());
                _folderPath = val;

                OnPropertyChanged("FolderPath");
            }
        }

        public Visibility RegistryVisible => (SelectedType == ExcludeTypes.Registry ? Visibility.Visible : Visibility.Collapsed);

        public Visibility FileVisible => (SelectedType == ExcludeTypes.File ? Visibility.Visible : Visibility.Collapsed);

        public Visibility FolderVisible => (SelectedType == ExcludeTypes.Folder ? Visibility.Visible : Visibility.Collapsed);

        /// <summary>
        /// The registry path selected by the user
        /// </summary>
        public string RegistryPath
        {
            get
            {
                if (string.IsNullOrEmpty(SubKeyPath))
                    return string.Empty;

                return $@"{RootKey}\{SubKeyPath}";
            }
        }

        public string SubKeyPath
        {
            get { return _subKey; }
            set
            {
                string val = (string.IsNullOrEmpty(value) ? string.Empty : value.Trim());
                _subKey = val;

                OnPropertyChanged("SubKeyPath");
            }
        }

        public enum ExcludeTypes
        {
            File,
            Folder,
            Registry
        };

        public AddEditExcludeItem(ExcludeTypes excType)
        {
            this.HideIcon();

            InitializeComponent();

            WindowTitle = "Add To Exclude List";
            AddEditText = "Add Entry";

            RootKey = RootKeys[0];
            SelectedType = excType;

            OnPropertyChanged("Description");
            OnPropertyChanged("RegistryVisible");
            OnPropertyChanged("FileVisible");
            OnPropertyChanged("FolderVisible");
        }

        public AddEditExcludeItem(ExcludeItem excItem)
        {
            InitializeComponent();

            WindowTitle = "Edit Exclude Entry";
            AddEditText = "Edit Entry";

            RootKey = RootKeys[0];

            ExcludeTypes excType;

            if (!excItem.IsPath)
            {
                excType = ExcludeTypes.Registry;

                string regPath = excItem.RegistryPath;

                // Get root key
                int slashPos = regPath.IndexOf('\\');
                string rootKey = regPath.Substring(0, slashPos);

                RootKey = RootKeys.First(key => key == rootKey);

                // Get sub key
                SubKeyPath = regPath.Substring(slashPos + 1);
            }
            else
            {
                if (!string.IsNullOrEmpty(excItem.FilePath))
                {
                    excType = ExcludeTypes.File;
                    FilePath = excItem.FilePath;
                }
                else
                {
                    excType = ExcludeTypes.Folder;
                    FolderPath = excItem.FolderPath;
                }
            }

            SelectedType = excType;

            OnPropertyChanged("Description");
            OnPropertyChanged("RegistryVisible");
            OnPropertyChanged("FileVisible");
            OnPropertyChanged("FolderVisible");
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (RegistryVisible == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(SubKeyPath))
                {
                    MessageBox.Show(Application.Current.MainWindow, "No registry key entered", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (FileVisible == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(FilePath))
                {
                    MessageBox.Show(Application.Current.MainWindow, "No file path entered", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!Regex.IsMatch(FilePath, @"^[a-z?]:\\", RegexOptions.IgnoreCase))
                {
                    MessageBox.Show(Application.Current.MainWindow, "File path must start with drive letter (Example: C:\\).\nDrive letter may also be substituted with ? to match any drive letter.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (FolderVisible == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(FolderPath))
                {
                    MessageBox.Show(Application.Current.MainWindow, "No folder path entered", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!Regex.IsMatch(FolderPath, @"^[a-z?]:\\", RegexOptions.IgnoreCase))
                {
                    MessageBox.Show(Application.Current.MainWindow, "Folder path must start with drive letter (Example: C:\\).\nDrive letter may also be substituted with ? to match any drive letter.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }


            DialogResult = true;
            Close();
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (FileVisible == Visibility.Visible)
            {
                using (OpenFileDialog fileDlg = new OpenFileDialog())
                {
                    fileDlg.Title = "Select file path to ignore";

                    if (File.Exists(FilePath))
                    {
                        fileDlg.InitialDirectory = Path.GetDirectoryName(FilePath);
                        fileDlg.FileName = Path.GetFileName(FilePath);
                    }

                    if (fileDlg.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) == System.Windows.Forms.DialogResult.OK)
                    {
                        FilePath = fileDlg.FileName;
                    }
                }
            }
            else if (FolderVisible == Visibility.Visible)
            {
                using (FolderBrowserDialog folderDlg = new FolderBrowserDialog())
                {
                    folderDlg.Description = "Select folder path to ignore";

                    if (Directory.Exists(FolderPath))
                        folderDlg.SelectedPath = FolderPath;

                    if (folderDlg.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) == System.Windows.Forms.DialogResult.OK)
                    {
                        FolderPath = folderDlg.SelectedPath;
                    }
                }
            }
        }
    }
}

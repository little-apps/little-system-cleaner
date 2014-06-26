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

using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text.RegularExpressions;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    /// <summary>
    /// Interaction logic for AddExcludeItem.xaml
    /// </summary>
    public partial class AddEditExcludeItem : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private ObservableCollection<string> _rootKeys = new ObservableCollection<string>() 
        {
            "HKEY_CLASSES_ROOT",
            "HKEY_CURRENT_USER",
            "HKEY_LOCAL_MACHINE",
            "HKEY_USERS"
        };

        private readonly ExcludeTypes _selectedType;
        private string _winTitle = "";
        private string _addEditText;
        private string _rootKey;
        private string _subKey;
        private string _regPath;
        private string _filePath;
        private string _folderPath;

        public ExcludeItem ExcludeItem
        {
            get
            {
                ExcludeItem excItem = new ExcludeItem();

                if (this.SelectedType == ExcludeTypes.Registry)
                    excItem.RegistryPath = this.RegistryPath;
                else if (this.SelectedType == ExcludeTypes.File)
                    excItem.FilePath = this.FilePath;
                else
                    excItem.FolderPath = this.FolderPath;

                return excItem;
            }
        }

        public ExcludeTypes SelectedType
        {
            get { return this._selectedType; }
        }

        public ObservableCollection<string> RootKeys
        {
            get { return this._rootKeys; }
        }

        public string WindowTitle
        {
            get { return this._winTitle; }
            set
            {
                this._winTitle = value;

                this.OnPropertyChanged("WindowTitle");
            }
        }

        public string AddEditText
        {
            get { return this._addEditText; }
            set
            {
                this._addEditText = value;

                this.OnPropertyChanged("AddEditText");
            }
        }

        public string Description
        {
            get
            {
                string text = "Please enter a ";

                if (this.SelectedType == ExcludeTypes.Registry)
                    text += "registry";
                else if (this.SelectedType == ExcludeTypes.Folder)
                    text += "folder";
                else if (this.SelectedType == ExcludeTypes.File)
                    text += "file";

                text += " path. Wildcards are supported. (a question mark (?) represents a single character and an asterisk (*) represents 0 or more characters)";

                return text;
            }
        }

        public string RootKey
        {
            get { return this._rootKey; }
            set 
            { 
                this._rootKey = value;

                this.OnPropertyChanged("RootKey");
            }
        }

        public string FilePath
        {
            get { return this._filePath; }
            set
            {
                string val = (string.IsNullOrEmpty(value) ? string.Empty : value.Trim());
                this._filePath = val;

                this.OnPropertyChanged("FilePath");
            }
        }

        public string FolderPath
        {
            get { return this._folderPath; }
            set
            {
                string val = (string.IsNullOrEmpty(value) ? string.Empty : value.Trim());
                this._folderPath = val;

                this.OnPropertyChanged("FolderPath");
            }
        }

        public Visibility RegistryVisible
        {
            get { return (this.SelectedType == ExcludeTypes.Registry ? Visibility.Visible : Visibility.Collapsed); }
        }

        public Visibility FileVisible
        {
            get { return (this.SelectedType == ExcludeTypes.File ? Visibility.Visible : Visibility.Collapsed); }
        }

        public Visibility FolderVisible
        {
            get { return (this.SelectedType == ExcludeTypes.Folder ? Visibility.Visible : Visibility.Collapsed); }
        }

        /// <summary>
        /// The registry path selected by the user
        /// </summary>
        public string RegistryPath
        {
            get
            {
                if (string.IsNullOrEmpty(this.SubKeyPath))
                    return string.Empty;

                return string.Format(@"{0}\{1}", this.RootKey, this.SubKeyPath);
            }
        }

        public string SubKeyPath
        {
            get { return this._subKey; }
            set 
            {
                string val = (string.IsNullOrEmpty(value) ? string.Empty : value.Trim());
                this._subKey = val;

                this.OnPropertyChanged("SubKeyPath");
            }
        }

        public enum ExcludeTypes { File, Folder, Registry };

        public AddEditExcludeItem(ExcludeTypes excType)
        {
            InitializeComponent();

            this.WindowTitle = "Add To Exclude List";
            this.AddEditText = "Add Entry";

            this.RootKey = this.RootKeys[0];
            this._selectedType = excType;

            this.OnPropertyChanged("Description");
            this.OnPropertyChanged("RegistryVisible");
            this.OnPropertyChanged("FileVisible");
            this.OnPropertyChanged("FolderVisible");
        }

        public AddEditExcludeItem(ExcludeItem excItem)
        {
            InitializeComponent();

            this.WindowTitle = "Edit Exclude Entry";
            this.AddEditText = "Edit Entry";

            this.RootKey = this.RootKeys[0];

            ExcludeTypes excType;

            if (!excItem.IsPath) {
                excType = ExcludeTypes.Registry;

                string regPath = excItem.RegistryPath;

                // Get root key
                int slash_pos = regPath.IndexOf('\\');
                string rootKey = regPath.Substring(0, slash_pos);
                
                foreach (string key in this.RootKeys)
                {
                    if (key == rootKey)
                    {
                        this.RootKey = key;
                        break;
                    }
                }

                // Get sub key
                this.SubKeyPath = regPath.Substring(slash_pos + 1);
            }
            else
            {
                if (!string.IsNullOrEmpty(excItem.FilePath)) {
                    excType = ExcludeTypes.File;
                    this.FilePath = excItem.FilePath;
                }
                else
                {
                    excType = ExcludeTypes.Folder;
                    this.FolderPath = excItem.FolderPath;
                }
            }

            this._selectedType = excType;

            this.OnPropertyChanged("Description");
            this.OnPropertyChanged("RegistryVisible");
            this.OnPropertyChanged("FileVisible");
            this.OnPropertyChanged("FolderVisible");
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (this.RegistryVisible == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(this.SubKeyPath))
                {
                    MessageBox.Show(Application.Current.MainWindow, "No registry key entered", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (this.FileVisible == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(this.FilePath))
                {
                    MessageBox.Show(Application.Current.MainWindow, "No file path entered", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!Regex.IsMatch(this.FilePath, @"^[a-z?]:\\", RegexOptions.IgnoreCase))
                {
                    MessageBox.Show(Application.Current.MainWindow, "File path must start with drive letter (Example: C:\\).\nDrive letter may also be substitued with ? to match any drive letter.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (this.FolderVisible == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(this.FolderPath))
                {
                    MessageBox.Show(Application.Current.MainWindow, "No folder path entered", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!Regex.IsMatch(this.FolderPath, @"^[a-z?]:\\", RegexOptions.IgnoreCase))
                {
                    MessageBox.Show(Application.Current.MainWindow, "Folder path must start with drive letter (Example: C:\\).\nDrive letter may also be substitued with ? to match any drive letter.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            

            this.DialogResult = true;
            this.Close();
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (this.FileVisible == Visibility.Visible)
            {
                using (System.Windows.Forms.OpenFileDialog fileDlg = new System.Windows.Forms.OpenFileDialog())
                {
                    fileDlg.Title = "Select file path to ignore";

                    if (File.Exists(this.FilePath))
                    {
                        fileDlg.InitialDirectory = Path.GetDirectoryName(this.FilePath);
                        fileDlg.FileName = Path.GetFileName(this.FilePath);
                    }

                    if (fileDlg.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) == System.Windows.Forms.DialogResult.OK)
                    {
                        this.FilePath = fileDlg.FileName;
                    }
                }
            }
            else if (this.FolderVisible == Visibility.Visible)
            {
                using (System.Windows.Forms.FolderBrowserDialog folderDlg = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderDlg.Description = "Select folder path to ignore";

                    if (Directory.Exists(this.FolderPath))
                        folderDlg.SelectedPath = this.FolderPath;

                    if (folderDlg.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) == System.Windows.Forms.DialogResult.OK)
                    {
                        this.FolderPath = folderDlg.SelectedPath;
                    }
                }
            }
        }
    }
}

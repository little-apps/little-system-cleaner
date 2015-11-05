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
using System.Windows;
using System.Windows.Forms;
using Little_System_Cleaner.Misc;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;

namespace Little_System_Cleaner.Disk_Cleaner.Helpers
{
    /// <summary>
    /// Interaction logic for AddExcludeFolder.xaml
    /// </summary>
    public partial class AddIncludeFolder
    {
        public event AddIncFolderEventHandler AddIncFolder;

        public AddIncludeFolder()
        {
            this.HideIcon();

            InitializeComponent();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBox.Text.Trim()))
            {
                MessageBox.Show(this, "Please enter a folder", Application.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AddIncFolder != null)
            {
                AddIncFolderEventArgs eventArgs = new AddIncFolderEventArgs { FolderPath = TextBox.Text.Trim() };
                AddIncFolder(this, eventArgs);
            }

            Close();
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            var browserDlg = new FolderBrowserDialog();
            browserDlg.ShowDialog(new WindowWrapper(this));
            TextBox.Text = browserDlg.SelectedPath;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            const string message = "Wildcards are supported. Please note a question mark (?) represents a single character and an asterisk (*) represents 0 or more characters\n\n" +
                                   "Example: ?:\\test matches a root folder named test";

            MessageBox.Show(this, message, "Add Include Folder Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class AddIncFolderEventArgs : EventArgs
    {
        public string FolderPath
        {
            get;
            set;
        }
    }
    public delegate void AddIncFolderEventHandler(object sender, AddIncFolderEventArgs e);
}

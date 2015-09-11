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
    public partial class AddExcludeFolder
    {
        public event AddExcludeFolderEventHandler AddExcludeFolderDelegate;

        public AddExcludeFolder()
        {
            InitializeComponent();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBox.Text.Trim()))
            {
                MessageBox.Show(this, "Please enter a folder", Application.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AddExcludeFolderDelegate != null)
            {
                AddExcludeFolderEventArgs eventArgs = new AddExcludeFolderEventArgs { FolderPath = TextBox.Text.Trim() };
                AddExcludeFolderDelegate(this, eventArgs);
            }

            Close();
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog browserDlg = new FolderBrowserDialog();
            browserDlg.ShowDialog(new WindowWrapper(this));
            TextBox.Text = browserDlg.SelectedPath;
        }
    }

    public class AddExcludeFolderEventArgs : EventArgs
    {
        public string FolderPath
        {
            get;
            set;
        }
    }
    public delegate void AddExcludeFolderEventHandler(object sender, AddExcludeFolderEventArgs e);
}

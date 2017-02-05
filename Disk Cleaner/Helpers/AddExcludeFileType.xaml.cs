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
using Shared;

namespace Disk_Cleaner.Helpers
{
    /// <summary>
    ///     Interaction logic for AddExcludeFileType.xaml
    /// </summary>
    public partial class AddExcludeFileType
    {
        public AddExcludeFileType()
        {
            this.HideIcon();

            InitializeComponent();
        }

        public event AddFileTypeEventHandler AddFileType;

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBox.Text.Trim()))
            {
                MessageBox.Show(this, "Please enter a file type", Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            if (AddFileType != null)
            {
                var eventArgs = new AddFileTypeEventArgs { FileType = TextBox.Text.Trim() };
                AddFileType(this, eventArgs);
            }

            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            const string message =
                "Wildcards are supported. Please note a question mark (?) represents a single character and an asterisk (*) represents 0 or more characters\n\n" +
                "Example: *.old matches files that end with .old";

            MessageBox.Show(this, message, "Add Exclude File Type Information", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    public class AddFileTypeEventArgs : EventArgs
    {
        public string FileType { get; set; }
    }

    public delegate void AddFileTypeEventHandler(object sender, AddFileTypeEventArgs e);
}
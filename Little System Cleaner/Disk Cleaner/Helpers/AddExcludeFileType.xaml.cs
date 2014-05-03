﻿/*
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Little_System_Cleaner.Disk_Cleaner.Helpers
{
    /// <summary>
    /// Interaction logic for AddExcludeFileType.xaml
    /// </summary>
    public partial class AddExcludeFileType : Window
    {
        public event AddFileTypeEventHandler AddFileType;

        public AddExcludeFileType()
        {
            InitializeComponent();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox.Text.Trim()))
            {
                MessageBox.Show(this, "Please enter a file type", System.Windows.Forms.Application.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (AddFileType != null)
            {
                AddFileTypeEventArgs eventArgs = new AddFileTypeEventArgs();
                eventArgs.fileType = this.textBox.Text.Trim();
                AddFileType(this, eventArgs);
            }

            this.Close();
        }
    }

    public class AddFileTypeEventArgs : EventArgs
    {
        public string fileType
        {
            get;
            set;
        }
    }

    public delegate void AddFileTypeEventHandler(object sender, AddFileTypeEventArgs e);
}
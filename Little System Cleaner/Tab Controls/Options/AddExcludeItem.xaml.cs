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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Little_System_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for AddExcludeItem.xaml
    /// </summary>
    public partial class AddExcludeItem : Window
    {
        private string _regPath;
        /// <summary>
        /// The registry path selected by the user
        /// </summary>
        public string RegistryPath
        {
            get { return _regPath; }
        }

        public AddExcludeItem()
        {
            InitializeComponent();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.RegKeyExists(this.comboBoxSection.Text, this.textBoxSubKey.Text))
            {
                MessageBox.Show(Application.Current.MainWindow, "Registry key doesn't exist", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(this.textBoxSubKey.Text))
            {
                MessageBox.Show(Application.Current.MainWindow, "No registry key entered", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _regPath = string.Format(@"{0}\{1}", this.comboBoxSection.Text, this.textBoxSubKey.Text);

            this.DialogResult = true;
            this.Close();
        }


    }
}

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
using System.IO;
using Microsoft.Win32;

namespace Little_System_Cleaner.Startup_Manager.Helpers
{
    /// <summary>
    /// Interaction logic for AddEditEntry.xaml
    /// </summary>
    public partial class AddEditEntry : Window
    {
        readonly bool IsEditing;
        readonly string OldStartupPath;
        readonly RegistryKey OldRegKey;
        readonly string OldValueName;

        public AddEditEntry(string sectionName, string entryName, string filePath, string fileArgs, RegistryKey oldRegKey)
        {
            InitializeComponent();

            PopulateComboBox();

            SetComboBox(sectionName);

            if (entryName.EndsWith(".lnk"))
                this.textBoxName.Text = entryName.Remove(entryName.IndexOf(".lnk"));
            else
                this.textBoxName.Text = entryName;
            this.textBoxPath.Text = filePath;
            this.textBoxArgs.Text = fileArgs;

            IsEditing = true;

            // Store old registry key so it can be removed
            if (oldRegKey != null)
            {
                OldRegKey = oldRegKey;
                OldValueName = entryName;
            }
            else
                OldStartupPath = System.IO.Path.Combine(sectionName, entryName);
        }

        public AddEditEntry()
        {
            InitializeComponent();

            PopulateComboBox();

            this.comboBox1.SelectedIndex = 0;

            IsEditing = false;
        }

        private void PopulateComboBox()
        {
            // Startup folders
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Startup\All Users"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Startup\Current User"));

            // All users startup registry keys (32bit)
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x86)\Policies\Explorer\Run"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x86)\Run Services"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x86)\Run Services Once"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x86)\Run Once"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x86)\Run Once\Setup"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x86)\Run"));

            // All users startup registry keys (32bit)
            if (Utils.Is64BitOS) 
            {
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x64)\Policies\Explorer\Run"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x64)\Run Services"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x64)\Run Services Once"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x64)\Run Once"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x64)\Run Once\Setup"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.Users, @"Registry\All Users (x64)\Run"));
            }

            // Current user startup registry keys (32bit)
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x86)\Policies\Explorer\Run"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x86)\Run Services"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x86)\Run Services Once"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x86)\Run Once"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x86)\Run Once\Setup"));
            this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x86)\Run"));

            // Current user startup registry keys (32bit)
            if (Utils.Is64BitOS)
            {
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x64)\Policies\Explorer\Run"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x64)\Run Services"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x64)\Run Services Once"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x64)\Run Once"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x64)\Run Once\Setup"));
                this.comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.User, @"Registry\Current User (x64)\Run"));
            }
        }

        private void SetComboBox(string sectionName)
        {            
            if (sectionName == Utils.GetSpecialFolderPath(Utils.CSIDL_COMMON_STARTUP))
                this.comboBox1.SelectedIndex = 0;
            else if (sectionName == Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP))
                this.comboBox1.SelectedIndex = 1;

            if (Utils.Is64BitOS)
            {
                // All users startup registry keys (32bit)
                if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    this.comboBox1.SelectedIndex = 2;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    this.comboBox1.SelectedIndex = 3;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    this.comboBox1.SelectedIndex = 4;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    this.comboBox1.SelectedIndex = 5;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    this.comboBox1.SelectedIndex = 6;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run")
                    this.comboBox1.SelectedIndex = 7;

                // All users startup registry keys (64bit)
                if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    this.comboBox1.SelectedIndex = 8;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    this.comboBox1.SelectedIndex = 9;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    this.comboBox1.SelectedIndex = 10;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    this.comboBox1.SelectedIndex = 11;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    this.comboBox1.SelectedIndex = 12;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run")
                    this.comboBox1.SelectedIndex = 13;

                // Current user startup registry keys (32bit)
                if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    this.comboBox1.SelectedIndex = 14;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    this.comboBox1.SelectedIndex = 15;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    this.comboBox1.SelectedIndex = 16;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    this.comboBox1.SelectedIndex = 17;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    this.comboBox1.SelectedIndex = 18;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run")
                    this.comboBox1.SelectedIndex = 19;

                // Current user startup registry keys (64bit)
                if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    this.comboBox1.SelectedIndex = 20;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    this.comboBox1.SelectedIndex = 21;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    this.comboBox1.SelectedIndex = 22;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    this.comboBox1.SelectedIndex = 23;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    this.comboBox1.SelectedIndex = 24;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run")
                    this.comboBox1.SelectedIndex = 25;
            }
            else
            {
                // All users startup registry keys (32bit)
                if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    this.comboBox1.SelectedIndex = 2;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    this.comboBox1.SelectedIndex = 3;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    this.comboBox1.SelectedIndex = 4;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    this.comboBox1.SelectedIndex = 5;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    this.comboBox1.SelectedIndex = 6;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run")
                    this.comboBox1.SelectedIndex = 7;

                // Current user startup registry keys (32bit)
                if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    this.comboBox1.SelectedIndex = 8;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    this.comboBox1.SelectedIndex = 9;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    this.comboBox1.SelectedIndex = 10;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    this.comboBox1.SelectedIndex = 11;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    this.comboBox1.SelectedIndex = 12;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run")
                    this.comboBox1.SelectedIndex = 13;
            }
        }

        private ComboBoxItem CreateComboBoxItem(System.Drawing.Bitmap bitMap, string startupPath) 
        {
            ComboBoxItem comboItem = new ComboBoxItem();

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;

            Image bMapImg = Utils.CreateBitmapSourceFromBitmap(bitMap);

            // Resize image to 16x16
            bMapImg.Width = bMapImg.Height = 16;
            
            stackPanel.Children.Add(bMapImg);

            TextBlock textBlock = new TextBlock();
            textBlock.Text = startupPath;

            stackPanel.Children.Add(textBlock);

            comboItem.Content = stackPanel;

            return comboItem;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBoxName.Text) || string.IsNullOrEmpty(this.textBoxPath.Text))
            {
                MessageBox.Show(this, "You must enter a name and a path", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Remove old key if old entry is in registry
            if (IsEditing) 
            {
                if (OldRegKey != null)
                    OldRegKey.DeleteValue(OldValueName);
                else // Otherwise remove old startup path
                    System.IO.File.Delete(OldStartupPath);
            }

            // Store changed entry
            RegistryKey regKey;
            string filePath;

            if (this.comboBox1.SelectedIndex <= 1)
            {
                if (this.comboBox1.SelectedIndex == 0)
                    filePath = System.IO.Path.Combine(Utils.GetSpecialFolderPath(Utils.CSIDL_COMMON_STARTUP), this.textBoxName.Text + ".lnk");
                else
                    filePath = System.IO.Path.Combine(Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP), this.textBoxName.Text + ".lnk");

                string fileDir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(fileDir))
                    Directory.CreateDirectory(fileDir);

                Utils.CreateShortcut(filePath, this.textBoxPath.Text, this.textBoxArgs.Text);
            }
            else
            {
                string strPath = "";

                if (!string.IsNullOrEmpty(this.textBoxPath.Text) && !string.IsNullOrEmpty(this.textBoxArgs.Text))
                    strPath = string.Format("\"{0}\" {1}", this.textBoxPath.Text, this.textBoxArgs.Text);
                else
                    strPath = string.Format("\"{0}\"", this.textBoxPath.Text);

                using (regKey = GetSelectedRegKey())
                {
                    regKey.SetValue(this.textBoxName.Text, strPath);
                }
            }

            MessageBox.Show(this, "Successfully created startup entry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            this.DialogResult = true;
            this.Close();
        }

        private RegistryKey GetSelectedRegKey()
        {
            RegistryKey regKey = null;

            if (Utils.Is64BitOS)
            {
                switch (this.comboBox1.SelectedIndex)
                {
                    case 2:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run");
                        break;
                    case 3:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices");
                        break;
                    case 4:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce");
                        break;
                    case 5:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce");
                        break;
                    case 6:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup");
                        break;
                    case 7:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                        break;

                    case 8:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run");
                        break;
                    case 9:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices");
                        break;
                    case 10:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce");
                        break;
                    case 11:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce");
                        break;
                    case 12:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup");
                        break;
                    case 13:
                        regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run");
                        break;

                    case 14:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run");
                        break;
                    case 15:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices");
                        break;
                    case 16:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce");
                        break;
                    case 17:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce");
                        break;
                    case 18:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup");
                        break;
                    case 19:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                        break;

                    case 20:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run");
                        break;
                    case 21:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices");
                        break;
                    case 22:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce");
                        break;
                    case 23:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce");
                        break;
                    case 24:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup");
                        break;
                    case 25:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run");
                        break;
                }
            }
            else
            {
                switch (this.comboBox1.SelectedIndex) 
                {
	                case 2:
	                    regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run");
	                    break;
	                case 3:
	                    regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices");
	                    break;
	                case 4:
	                    regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce");
	                    break;
	                case 5:
	                    regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce");
	                    break;
	                case 6:
	                    regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup");
	                    break;
	                case 7:
	                    regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
	                    break;
	
	                case 8:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run");
                        break;
                    case 9:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices");
                        break;
                    case 10:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce");
                        break;
                    case 11:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce");
                        break;
                    case 12:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup");
                        break;
                    case 13:
                        regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                        break;
                }
            }

            return regKey;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            BrowseFile();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            // Don't do anything, just exit
            this.DialogResult = false;
            this.Close();
        }

        private void textBoxPath_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BrowseFile();
        }

        private void BrowseFile()
        {
            OpenFileDialog openFileDlg = new OpenFileDialog();

            openFileDlg.Multiselect = false;

            if (!string.IsNullOrEmpty(this.textBoxPath.Text))
            {
                openFileDlg.InitialDirectory = Path.GetDirectoryName(this.textBoxPath.Text);
                openFileDlg.FileName = this.textBoxPath.Text;
            }

            if (openFileDlg.ShowDialog(this).GetValueOrDefault())
            {
                this.textBoxPath.Text = openFileDlg.FileName;
            }
        }

    }
}

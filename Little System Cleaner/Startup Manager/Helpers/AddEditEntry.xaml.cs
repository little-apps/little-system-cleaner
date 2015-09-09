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
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Little_System_Cleaner.Misc;
using Microsoft.Win32;
using Image = System.Windows.Controls.Image;

namespace Little_System_Cleaner.Startup_Manager.Helpers
{
    /// <summary>
    /// Interaction logic for AddEditEntry.xaml
    /// </summary>
    public partial class AddEditEntry
    {
        readonly bool _isEditing;
        readonly string _oldStartupPath;
        readonly RegistryKey _oldRegKey;
        readonly string _oldValueName;

        public AddEditEntry(string sectionName, string entryName, string filePath, string fileArgs, RegistryKey oldRegKey)
        {
            InitializeComponent();

            PopulateComboBox();

            SetComboBox(sectionName);

            textBoxName.Text = entryName.EndsWith(".lnk") ? entryName.Remove(entryName.IndexOf(".lnk")) : entryName;
            textBoxPath.Text = filePath;
            textBoxArgs.Text = fileArgs;

            _isEditing = true;

            // Store old registry key so it can be removed
            if (oldRegKey != null)
            {
                _oldRegKey = oldRegKey;
                _oldValueName = entryName;
            }
            else
                _oldStartupPath = Path.Combine(sectionName, entryName);
        }

        public AddEditEntry()
        {
            InitializeComponent();

            PopulateComboBox();

            comboBox1.SelectedIndex = 0;

            _isEditing = false;
        }

        private void PopulateComboBox()
        {
            // Startup folders
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Startup\All Users"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Startup\Current User"));

            // All users startup registry keys (32bit)
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x86)\Policies\Explorer\Run"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x86)\Run Services"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x86)\Run Services Once"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x86)\Run Once"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x86)\Run Once\Setup"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x86)\Run"));

            // All users startup registry keys (32bit)
            if (Utils.Is64BitOs) 
            {
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x64)\Policies\Explorer\Run"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x64)\Run Services"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x64)\Run Services Once"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x64)\Run Once"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x64)\Run Once\Setup"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.all_users, @"Registry\All Users (x64)\Run"));
            }

            // Current user startup registry keys (32bit)
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x86)\Policies\Explorer\Run"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x86)\Run Services"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x86)\Run Services Once"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x86)\Run Once"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x86)\Run Once\Setup"));
            comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x86)\Run"));

            // Current user startup registry keys (32bit)
            if (Utils.Is64BitOs)
            {
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x64)\Policies\Explorer\Run"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x64)\Run Services"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x64)\Run Services Once"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x64)\Run Once"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x64)\Run Once\Setup"));
                comboBox1.Items.Add(CreateComboBoxItem(Properties.Resources.current_user, @"Registry\Current User (x64)\Run"));
            }
        }

        private void SetComboBox(string sectionName)
        {
            if (sectionName == Utils.GetSpecialFolderPath(PInvoke.CSIDL_COMMON_STARTUP))
                comboBox1.SelectedIndex = 0;
            else if (sectionName == Utils.GetSpecialFolderPath(PInvoke.CSIDL_STARTUP))
                comboBox1.SelectedIndex = 1;

            if (Utils.Is64BitOs)
            {
                // All users startup registry keys (32bit)
                if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    comboBox1.SelectedIndex = 2;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    comboBox1.SelectedIndex = 3;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    comboBox1.SelectedIndex = 4;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    comboBox1.SelectedIndex = 5;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    comboBox1.SelectedIndex = 6;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run")
                    comboBox1.SelectedIndex = 7;

                // All users startup registry keys (64bit)
                if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    comboBox1.SelectedIndex = 8;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    comboBox1.SelectedIndex = 9;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    comboBox1.SelectedIndex = 10;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    comboBox1.SelectedIndex = 11;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    comboBox1.SelectedIndex = 12;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run")
                    comboBox1.SelectedIndex = 13;

                // Current user startup registry keys (32bit)
                if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    comboBox1.SelectedIndex = 14;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    comboBox1.SelectedIndex = 15;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    comboBox1.SelectedIndex = 16;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    comboBox1.SelectedIndex = 17;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    comboBox1.SelectedIndex = 18;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run")
                    comboBox1.SelectedIndex = 19;

                // Current user startup registry keys (64bit)
                if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    comboBox1.SelectedIndex = 20;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    comboBox1.SelectedIndex = 21;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    comboBox1.SelectedIndex = 22;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    comboBox1.SelectedIndex = 23;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    comboBox1.SelectedIndex = 24;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run")
                    comboBox1.SelectedIndex = 25;
            }
            else
            {
                // All users startup registry keys (32bit)
                if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    comboBox1.SelectedIndex = 2;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    comboBox1.SelectedIndex = 3;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    comboBox1.SelectedIndex = 4;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    comboBox1.SelectedIndex = 5;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    comboBox1.SelectedIndex = 6;
                else if (sectionName == "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run")
                    comboBox1.SelectedIndex = 7;

                // Current user startup registry keys (32bit)
                if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run")
                    comboBox1.SelectedIndex = 8;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices")
                    comboBox1.SelectedIndex = 9;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce")
                    comboBox1.SelectedIndex = 10;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
                    comboBox1.SelectedIndex = 11;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup")
                    comboBox1.SelectedIndex = 12;
                else if (sectionName == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run")
                    comboBox1.SelectedIndex = 13;
            }
        }

        private ComboBoxItem CreateComboBoxItem(Bitmap bitMap, string startupPath) 
        {
            ComboBoxItem comboItem = new ComboBoxItem();

            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            Image bMapImg = Utils.CreateBitmapSourceFromBitmap(bitMap);

            // Resize image to 16x16
            bMapImg.Width = bMapImg.Height = 16;
            
            stackPanel.Children.Add(bMapImg);

            TextBlock textBlock = new TextBlock { Text = startupPath };

            stackPanel.Children.Add(textBlock);

            comboItem.Content = stackPanel;

            return comboItem;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxName.Text) || string.IsNullOrWhiteSpace(textBoxPath.Text))
            {
                MessageBox.Show(this, "You must enter a name and a path", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Remove old key if old entry is in registry
            if (_isEditing) 
            {
                try
                {
                    if (_oldRegKey != null)
                        _oldRegKey.DeleteValue(_oldValueName);
                    else // Otherwise remove old startup path
                        File.Delete(_oldStartupPath);
                }
                catch (Exception ex)
                {
                    string message = $"There was an error removing the previous startup entry from the registry or folder.\nThe following error occurred: {ex.Message}";

                    MessageBox.Show(this, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Store changed entry
            RegistryKey regKey = null;
            bool created = false;

            if (comboBox1.SelectedIndex <= 1)
            {
                try
                {
                    var filePath = Path.Combine(comboBox1.SelectedIndex == 0 ? Utils.GetSpecialFolderPath(PInvoke.CSIDL_COMMON_STARTUP) : Utils.GetSpecialFolderPath(PInvoke.CSIDL_STARTUP), textBoxName.Text + ".lnk");

                    string fileDir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(fileDir))
                        Directory.CreateDirectory(fileDir);

                    // Make sure file doesn't already exist
                    if (File.Exists(filePath))
                    {
                        MessageBox.Show(this, "A startup entry already exists with that specified name. Please change it before continuing.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!(created = CreateShortcut(filePath, textBoxPath.Text, textBoxArgs.Text)))
                        MessageBox.Show(this, "There was an error creating the shortcut for the startup entry.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    string message = $"There was an error creating the shortcut for the startup entry.\nThe following error occurred: {ex.Message}";

                    MessageBox.Show(this, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                string strPath = (!string.IsNullOrEmpty(textBoxPath.Text) && !string.IsNullOrEmpty(textBoxArgs.Text) ? $"\"{textBoxPath.Text}\" {textBoxArgs.Text}" : $"\"{textBoxPath.Text}\"");

                try
                {
                    regKey = GetSelectedRegKey();

                    // Make sure registry value name doesn't already exist
                    if (regKey.GetValue(textBoxName.Text) != null)
                    {
                        MessageBox.Show(this, "A startup entry already exists with that specified name. Please change it before continuing.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    regKey.SetValue(textBoxName.Text, strPath);

                    created = true;
                }
                catch (Exception ex)
                {
                    string message = $"There was an error adding the startup entry to the registry.\nThe following error occurred: {ex.Message}";

                    MessageBox.Show(this, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    regKey?.Close();
                }
            }

            if (created)
            {
                MessageBox.Show(this, "Successfully created startup entry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            
        }

        private RegistryKey GetSelectedRegKey()
        {
            RegistryKey regKey = null;

            if (Utils.Is64BitOs)
            {
                switch (comboBox1.SelectedIndex)
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
                switch (comboBox1.SelectedIndex) 
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
            DialogResult = false;
            Close();
        }

        private void textBoxPath_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BrowseFile();
        }

        private void BrowseFile()
        {
            OpenFileDialog openFileDlg = new OpenFileDialog { Multiselect = false };
            
            if (!string.IsNullOrEmpty(textBoxPath.Text))
            {
                openFileDlg.InitialDirectory = Path.GetDirectoryName(textBoxPath.Text);
                openFileDlg.FileName = textBoxPath.Text;
            }

            if (openFileDlg.ShowDialog(this).GetValueOrDefault())
            {
                textBoxPath.Text = openFileDlg.FileName;
            }
        }

        /// <summary>
        /// Creates .lnk shortcut to filename
        /// </summary>
        /// <param name="filename">.lnk shortcut</param>
        /// <param name="path">path for filename</param>
        /// <param name="arguments">arguments for shortcut (can be null)</param>
        /// <returns>True if shortcut was created</returns>
        private bool CreateShortcut(string filename, string path, string arguments)
        {
            PInvoke.ShellLink link = new PInvoke.ShellLink();
            ((PInvoke.IShellLinkW)link).SetPath(path);
            if (!string.IsNullOrEmpty(arguments))
                ((PInvoke.IShellLinkW)link).SetArguments(arguments);
            ((PInvoke.IPersistFile)link).Save(filename, false);

            return (File.Exists(filename));
        }
    }
}

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
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Documents;
using System.Runtime.InteropServices;

namespace Little_System_Cleaner.Controls
{
	public partial class Options
    {
        readonly ExcludeArray _excludeArray;
        public ExcludeArray excludeArray
        {
            get { return _excludeArray; }
        }
		public Options()
		{
            this.InitializeComponent();

            this.textBoxLog.Text = Properties.Settings.Default.optionsLogDir;

            if (Properties.Settings.Default.optionsUpdateDelay == 3)
                this.comboBoxUpdateDelay.SelectedIndex = 0;
            else if (Properties.Settings.Default.optionsUpdateDelay == 5)
                this.comboBoxUpdateDelay.SelectedIndex = 1;
            else if (Properties.Settings.Default.optionsUpdateDelay == 7)
                this.comboBoxUpdateDelay.SelectedIndex = 2;
            else if (Properties.Settings.Default.optionsUpdateDelay == 14)
                this.comboBoxUpdateDelay.SelectedIndex = 3;

            this.checkBoxSysRestore.IsChecked = Properties.Settings.Default.optionsSysRestore;
            this.checkBoxUsageStats.IsChecked = Properties.Settings.Default.optionsUsageStats;
		}

        public void ShowAboutTab()
        {
            this.tabControl1.SelectedIndex = this.tabControl1.Items.IndexOf(this.tabItemAbout);
        }

        private void UpdateSettings(object sender, RoutedEventArgs e)
        {
            this.UpdateSettings();
        }

        public void UpdateSettings()
        {
            if (this.textBoxLog != null)
                Properties.Settings.Default.optionsLogDir = this.textBoxLog.Text;

            if (this.checkBoxAutoUpdate != null)
                Properties.Settings.Default.optionsUpdate = this.checkBoxAutoUpdate.IsChecked.Value;

            if (this.comboBoxUpdateDelay != null)
            {
                if (this.comboBoxUpdateDelay.SelectedIndex == 0)
                    Properties.Settings.Default.optionsUpdateDelay = 3;
                else if (this.comboBoxUpdateDelay.SelectedIndex == 1)
                    Properties.Settings.Default.optionsUpdateDelay = 5;
                else if (this.comboBoxUpdateDelay.SelectedIndex == 2)
                    Properties.Settings.Default.optionsUpdateDelay = 7;
                else if (this.comboBoxUpdateDelay.SelectedIndex == 3)
                    Properties.Settings.Default.optionsUpdateDelay = 14;
            }

            if (this.checkBoxSysRestore != null)
                Properties.Settings.Default.optionsSysRestore = this.checkBoxSysRestore.IsChecked.GetValueOrDefault();
            if (this.checkBoxUsageStats != null)
                Properties.Settings.Default.optionsUsageStats = this.checkBoxUsageStats.IsChecked.GetValueOrDefault();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Utils.LaunchURI(e.Uri.ToString());
        }

        private void buttonSupport_Click(object sender, RoutedEventArgs e)
        {
            Utils.LaunchURI(@"http://www.little-apps.com/faqs/");
        }

        private void buttonWebsite_Click(object sender, RoutedEventArgs e)
        {
            Utils.LaunchURI(@"http://www.little-apps.com/");
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowserDlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowserDlg.Description = "Select the folder where the log files will be placed";
                folderBrowserDlg.SelectedPath = this.textBoxLog.Text;
                folderBrowserDlg.ShowNewFolderButton = true;

                if (folderBrowserDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    this.textBoxLog.Text = folderBrowserDlg.SelectedPath;

                UpdateSettings();
            }
        }
        
	}
}
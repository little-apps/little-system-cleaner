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
using System.ComponentModel;
using System.Security;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Controls
{
    public partial class Options : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public Visibility ShowProxySettings
        {
            get 
            {
                if (this.radioButtonProxy.IsChecked.GetValueOrDefault())
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Hidden;
            }
        }

        public bool ProxyAuthenticate
        {
            get
            {
                return (Properties.Settings.Default.optionsProxyAuthenticate);
            }
        }

        public int SelectedUpdateDelay
        {
            get
            {
                if (Properties.Settings.Default.optionsUpdateDelay == 3)
                    return 0;
                else if (Properties.Settings.Default.optionsUpdateDelay == 5)
                    return 1;
                else if (Properties.Settings.Default.optionsUpdateDelay == 7)
                    return 2;
                else // if (Properties.Settings.Default.optionsUpdateDelay == 14)
                    return 3;
            }
            set
            {
                int index = value;

                if (index == 0)
                    Properties.Settings.Default.optionsUpdateDelay = 3;
                else if (index == 1)
                    Properties.Settings.Default.optionsUpdateDelay = 5;
                else if (index == 2)
                    Properties.Settings.Default.optionsUpdateDelay = 7;
                else // if (index == 3)
                    Properties.Settings.Default.optionsUpdateDelay = 14;

                RaisePropertyChanged("SelectedUpdateDelay");
            }
        }

		public Options()
		{
            this.InitializeComponent();

            this.textBoxLog.Text = Properties.Settings.Default.optionsLogDir;

            this.checkBoxAutoUpdate.IsChecked = Properties.Settings.Default.updateAuto;

            this.checkBoxSysRestore.IsChecked = Properties.Settings.Default.optionsSysRestore;
            this.checkBoxUsageStats.IsChecked = Properties.Settings.Default.optionsUsageStats;

            if (Properties.Settings.Default.optionsUseProxy == 0)
                this.radioButtonNoProxy.IsChecked = true;
            else if (Properties.Settings.Default.optionsUseProxy == 1)
                this.radioButtonIEProxy.IsChecked = true;
            else if (Properties.Settings.Default.optionsUseProxy == 2)
                this.radioButtonProxy.IsChecked = true;

            RaisePropertyChanged("ShowProxySettings");

            this.proxyAddress.Text = Properties.Settings.Default.optionsProxyHost;
            this.numericUpDownProxyPort.Value = Properties.Settings.Default.optionsProxyPort;

            this.checkBoxProxyAuthenticate.IsChecked = (Properties.Settings.Default.optionsProxyAuthenticate);

            RaisePropertyChanged("ProxyAuthenticate");

            this.proxyUser.Text = Properties.Settings.Default.optionsProxyUser;

            using (SecureString secureStr = Utils.DecryptString(Properties.Settings.Default.optionsProxyPassword)) {
                this.proxyPassword.Password = Utils.ToInsecureString(secureStr);
            }
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
                Properties.Settings.Default.optionsUpdate = this.checkBoxAutoUpdate.IsChecked.GetValueOrDefault();

            if (this.checkBoxSysRestore != null)
                Properties.Settings.Default.optionsSysRestore = this.checkBoxSysRestore.IsChecked.GetValueOrDefault();
            if (this.checkBoxUsageStats != null)
                Properties.Settings.Default.optionsUsageStats = this.checkBoxUsageStats.IsChecked.GetValueOrDefault();

            if (this.radioButtonNoProxy != null 
                && this.radioButtonIEProxy != null
                && this.radioButtonProxy != null
                && this.proxyAddress != null
                && this.numericUpDownProxyPort != null
                && this.checkBoxProxyAuthenticate != null
                && this.proxyUser != null
                && this.proxyPassword != null)
            {
                if (Properties.Settings.Default.optionsProxyHost != this.proxyAddress.Text.Trim())
                    Properties.Settings.Default.optionsProxyHost = this.proxyAddress.Text.Trim();

                if (Properties.Settings.Default.optionsProxyPort != this.numericUpDownProxyPort.Value.GetValueOrDefault()) {
                    if (this.numericUpDownProxyPort.Value.GetValueOrDefault() > 0 && this.numericUpDownProxyPort.Value.GetValueOrDefault() < 65535) 
                    {
                        Properties.Settings.Default.optionsProxyPort = this.numericUpDownProxyPort.Value.GetValueOrDefault();
                    } 
                    else
                    {
                        this.numericUpDownProxyPort.Value = Properties.Settings.Default.optionsProxyPort;
                    }
                }

                if (Properties.Settings.Default.optionsProxyUser != this.proxyUser.Text.Trim())
                    Properties.Settings.Default.optionsProxyUser = this.proxyUser.Text.Trim();

                string encryptedPassword = Utils.EncryptString(this.proxyPassword.SecurePassword);
                if (Properties.Settings.Default.optionsProxyPassword != encryptedPassword)
                    Properties.Settings.Default.optionsProxyPassword = encryptedPassword;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (!Utils.LaunchURI(e.Uri.ToString()))
                System.Windows.MessageBox.Show(App.Current.MainWindow, "Unable to detect web browser to open link", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void buttonSupportThisProject_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.LaunchURI(@"http://www.little-apps.com/?donate"))
                System.Windows.MessageBox.Show(App.Current.MainWindow, "Unable to detect web browser to open link", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void buttonWebsite_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.LaunchURI(@"http://www.little-apps.com/"))
                System.Windows.MessageBox.Show(App.Current.MainWindow, "Unable to detect web browser to open link", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void ChangeProxyUse(object sender, RoutedEventArgs e)
        {
            if (this.radioButtonNoProxy.IsChecked.GetValueOrDefault() && Properties.Settings.Default.optionsUseProxy != 0)
            {
                Properties.Settings.Default.optionsUseProxy = 0;
                RaisePropertyChanged("ShowProxySettings");
            }
            else if (this.radioButtonIEProxy.IsChecked.GetValueOrDefault() && Properties.Settings.Default.optionsUseProxy != 1) 
            {
                Properties.Settings.Default.optionsUseProxy = 1;
                RaisePropertyChanged("ShowProxySettings");
            }
            else if (this.radioButtonProxy.IsChecked.GetValueOrDefault() && Properties.Settings.Default.optionsUseProxy != 2)
            {
                Properties.Settings.Default.optionsUseProxy = 2;
                RaisePropertyChanged("ShowProxySettings");
            }
        }

        private void ShowOrHideProxyAuthenticate(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.optionsProxyAuthenticate != this.checkBoxProxyAuthenticate.IsChecked.GetValueOrDefault())
            {
                Properties.Settings.Default.optionsProxyAuthenticate = this.checkBoxProxyAuthenticate.IsChecked.GetValueOrDefault();
                RaisePropertyChanged("ProxyAuthenticate");
            }
        }
	}
}
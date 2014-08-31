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

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

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

                this.OnPropertyChanged("SelectedUpdateDelay");
            }
        }

        public bool? AutoUpdate
        {
            get { return Properties.Settings.Default.updateAuto; }
            set
            {
                Properties.Settings.Default.updateAuto = value.GetValueOrDefault();

                this.OnPropertyChanged("AutoUpdate");
            }
        }

        public bool? SysRestore
        {
            get { return Properties.Settings.Default.optionsSysRestore; }
            set
            {
                Properties.Settings.Default.optionsSysRestore = value.GetValueOrDefault();

                this.OnPropertyChanged("SysRestore");
            }
        }

        public bool? UsageStats
        {
            get { return Properties.Settings.Default.optionsUsageStats; }
            set
            {
                Properties.Settings.Default.optionsUsageStats = value.GetValueOrDefault();

                this.OnPropertyChanged("UsageStats");
            }
        }

        public string LogDirectory
        {
            get { return Properties.Settings.Default.optionsLogDir; }
            set
            {
                Properties.Settings.Default.optionsLogDir = value;

                this.OnPropertyChanged("LogDirectory");
            }
        }

        public bool? NoProxy
        {
            get { return (Properties.Settings.Default.optionsUseProxy == 0); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.optionsUseProxy = 0;

                this.OnPropertyChanged("NoProxy");
                this.OnPropertyChanged("IEProxy");
                this.OnPropertyChanged("Proxy");
                this.OnPropertyChanged("ShowProxySettings");
            }
        }

        public bool? IEProxy
        {
            get { return (Properties.Settings.Default.optionsUseProxy == 1); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.optionsUseProxy = 1;

                this.OnPropertyChanged("NoProxy");
                this.OnPropertyChanged("IEProxy");
                this.OnPropertyChanged("Proxy");
                this.OnPropertyChanged("ShowProxySettings");
            }
        }

        public bool? Proxy
        {
            get { return (Properties.Settings.Default.optionsUseProxy == 2); }
            set
            {
                if (value.GetValueOrDefault())
                    Properties.Settings.Default.optionsUseProxy = 2;

                this.OnPropertyChanged("NoProxy");
                this.OnPropertyChanged("IEProxy");
                this.OnPropertyChanged("Proxy");
                this.OnPropertyChanged("ShowProxySettings");
            }
        }

        public Visibility ShowProxySettings
        {
            get
            {
                if (this.Proxy.GetValueOrDefault())
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Hidden;
            }
        }

        public string ProxyAddress
        {
            get { return Properties.Settings.Default.optionsProxyHost; }
            set
            {
                Properties.Settings.Default.optionsProxyHost = value;

                this.OnPropertyChanged("ProxyAddress");
            }
        }

        public int? ProxyPort
        {
            get { return Properties.Settings.Default.optionsProxyPort; }
            set
            {
                Properties.Settings.Default.optionsProxyPort = value.GetValueOrDefault();

                this.OnPropertyChanged("ProxyPort");
            }
        }

        public bool? ProxyAuthenticate
        {
            get { return Properties.Settings.Default.optionsProxyAuthenticate; }
            set
            {
                Properties.Settings.Default.optionsProxyAuthenticate = value.GetValueOrDefault();

                this.OnPropertyChanged("ProxyAuthenticate");
            }
        }

        public string ProxyUser
        {
            get { return Properties.Settings.Default.optionsProxyUser; }
            set
            {
                Properties.Settings.Default.optionsProxyUser = value;

                this.OnPropertyChanged("ProxyUser");
            }
        }

        public string ProxyPassword
        {
            get
            {
                using (SecureString secureStr = Utils.DecryptString(Properties.Settings.Default.optionsProxyPassword))
                {
                    return Utils.ToInsecureString(secureStr);
                }
            }

            set
            {
                string encryptedPassword = Utils.EncryptString(this.proxyPassword.SecurePassword);
                if (Properties.Settings.Default.optionsProxyPassword != encryptedPassword)
                    Properties.Settings.Default.optionsProxyPassword = encryptedPassword;
            }
        }


		public Options()
		{
            this.InitializeComponent();

            using (SecureString secureStr = Utils.DecryptString(Properties.Settings.Default.optionsProxyPassword)) {
                this.proxyPassword.Password = Utils.ToInsecureString(secureStr);
            }
		}

        public void ShowAboutTab()
        {
            this.tabControl1.SelectedIndex = this.tabControl1.Items.IndexOf(this.tabItemAbout);
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
                    this.LogDirectory = folderBrowserDlg.SelectedPath;
            }
        }

        private void proxyPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            string encryptedPassword = Utils.EncryptString(this.proxyPassword.SecurePassword);
            if (Properties.Settings.Default.optionsProxyPassword != encryptedPassword)
                Properties.Settings.Default.optionsProxyPassword = encryptedPassword;
        }
	}
}
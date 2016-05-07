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

using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Navigation;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Little_System_Cleaner.Tab_Controls.Options
{
    public partial class Options : INotifyPropertyChanged
    {
        public Options()
        {
            InitializeComponent();

            using (var secureStr = Utils.DecryptString(Settings.Default.optionsProxyPassword))
            {
                proxyPassword.Password = Utils.ToInsecureString(secureStr);
            }
        }

        public int SelectedUpdateDelay
        {
            get
            {
                switch (Settings.Default.optionsUpdateDelay)
                {
                    case 3:
                        return 0;

                    case 5:
                        return 1;

                    case 7:
                        return 2;

                    default:
                        return 3;
                }
            }
            set
            {
                var index = value;

                switch (index)
                {
                    case 0:
                        Settings.Default.optionsUpdateDelay = 3;
                        break;

                    case 1:
                        Settings.Default.optionsUpdateDelay = 5;
                        break;

                    case 2:
                        Settings.Default.optionsUpdateDelay = 7;
                        break;

                    default:
                        Settings.Default.optionsUpdateDelay = 14;
                        break;
                }

                OnPropertyChanged(nameof(SelectedUpdateDelay));
            }
        }

        public bool? AutoUpdate
        {
            get { return Settings.Default.updateAuto; }
            set
            {
                Settings.Default.updateAuto = value.GetValueOrDefault();

                OnPropertyChanged(nameof(AutoUpdate));
            }
        }

        public bool? SysRestore
        {
            get { return Settings.Default.optionsSysRestore; }
            set
            {
                Settings.Default.optionsSysRestore = value.GetValueOrDefault();

                OnPropertyChanged(nameof(SysRestore));
            }
        }

        public bool? UsageStats
        {
            get { return Settings.Default.optionsUsageStats; }
            set
            {
                Settings.Default.optionsUsageStats = value.GetValueOrDefault();

                OnPropertyChanged(nameof(UsageStats));
            }
        }

        public string LogDirectory
        {
            get { return Settings.Default.OptionsLogDir; }
            set
            {
                Settings.Default.OptionsLogDir = value;

                OnPropertyChanged(nameof(LogDirectory));
            }
        }

        public bool? NoProxy
        {
            get { return Settings.Default.optionsUseProxy == 0; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.optionsUseProxy = 0;

                OnPropertyChanged(nameof(NoProxy));
                OnPropertyChanged(nameof(IeProxy));
                OnPropertyChanged(nameof(Proxy));
                OnPropertyChanged(nameof(ShowProxySettings));
            }
        }

        public bool? IeProxy
        {
            get { return Settings.Default.optionsUseProxy == 1; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.optionsUseProxy = 1;

                OnPropertyChanged(nameof(NoProxy));
                OnPropertyChanged(nameof(IeProxy));
                OnPropertyChanged(nameof(Proxy));
                OnPropertyChanged(nameof(ShowProxySettings));
            }
        }

        public bool? Proxy
        {
            get { return Settings.Default.optionsUseProxy == 2; }
            set
            {
                if (value.GetValueOrDefault())
                    Settings.Default.optionsUseProxy = 2;

                OnPropertyChanged(nameof(NoProxy));
                OnPropertyChanged(nameof(IeProxy));
                OnPropertyChanged(nameof(Proxy));
                OnPropertyChanged(nameof(ShowProxySettings));
            }
        }

        public Visibility ShowProxySettings => Proxy.GetValueOrDefault() ? Visibility.Visible : Visibility.Hidden;

        public string ProxyAddress
        {
            get { return Settings.Default.optionsProxyHost; }
            set
            {
                Settings.Default.optionsProxyHost = value;

                OnPropertyChanged(nameof(ProxyAddress));
            }
        }

        public int? ProxyPort
        {
            get { return Settings.Default.optionsProxyPort; }
            set
            {
                Settings.Default.optionsProxyPort = value.GetValueOrDefault();

                OnPropertyChanged(nameof(ProxyPort));
            }
        }

        public bool? ProxyAuthenticate
        {
            get { return Settings.Default.optionsProxyAuthenticate; }
            set
            {
                Settings.Default.optionsProxyAuthenticate = value.GetValueOrDefault();

                OnPropertyChanged(nameof(ProxyAuthenticate));
            }
        }

        public string ProxyUser
        {
            get { return Settings.Default.optionsProxyUser; }
            set
            {
                Settings.Default.optionsProxyUser = value;

                OnPropertyChanged(nameof(ProxyUser));
            }
        }

        public string ProxyPassword
        {
            get
            {
                using (var secureStr = Utils.DecryptString(Settings.Default.optionsProxyPassword))
                {
                    return Utils.ToInsecureString(secureStr);
                }
            }

            set
            {
                var encryptedPassword = Utils.EncryptString(proxyPassword.SecurePassword);
                if (Settings.Default.optionsProxyPassword != encryptedPassword)
                    Settings.Default.optionsProxyPassword = encryptedPassword;
            }
        }

        public void ShowAboutTab()
        {
            TabControl.SelectedIndex = TabControl.Items.IndexOf(TabItemAbout);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (!Utils.LaunchUri(e.Uri.ToString()))
                MessageBox.Show(Application.Current.MainWindow, "Unable to detect web browser to open link",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void buttonSupportThisProject_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.LaunchUri(@"http://www.little-apps.com/?donate"))
                MessageBox.Show(Application.Current.MainWindow, "Unable to detect web browser to open link",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void buttonWebsite_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.LaunchUri(@"http://www.little-apps.com/"))
                MessageBox.Show(Application.Current.MainWindow, "Unable to detect web browser to open link",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDlg = new FolderBrowserDialog())
            {
                folderBrowserDlg.Description = "Select the folder where the log files will be placed";
                folderBrowserDlg.SelectedPath = TextBoxLog.Text;
                folderBrowserDlg.ShowNewFolderButton = true;

                if (folderBrowserDlg.ShowDialog() == DialogResult.OK)
                    LogDirectory = folderBrowserDlg.SelectedPath;
            }
        }

        private void proxyPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            var encryptedPassword = Utils.EncryptString(proxyPassword.SecurePassword);
            if (Settings.Default.optionsProxyPassword != encryptedPassword)
                Settings.Default.optionsProxyPassword = encryptedPassword;
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged implementation
    }
}
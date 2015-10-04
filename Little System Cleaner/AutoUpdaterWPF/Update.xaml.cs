using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Timers;
using System.Windows;
using Microsoft.Win32;

namespace Little_System_Cleaner.AutoUpdaterWPF
{
    /// <summary>
    /// Interaction logic for Update.xaml
    /// </summary>
    public partial class Update
    {
        private Timer _timer;

        public Update(bool remindLater = false)
        {
            if (!remindLater)
            {
                try
                {
                    InitializeComponent();
                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
                
                Text = AutoUpdater.DialogTitle;
                TextBlockUpdate.Text = string.Format(TextBlockUpdate.Text, AutoUpdater.AppTitle);
                TextBlockDescription.Text =
                    string.Format(TextBlockDescription.Text,
                        AutoUpdater.AppTitle, AutoUpdater.CurrentVersion, AutoUpdater.InstalledVersion);
            }
        }

        public string Text
        {
            get { return Title; }
            set { Title = value; }
        }

        private void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AutoUpdater.ChangeLogUrl))
            {
                Dispatcher.InvokeAsync(() => MessageBox.Show(this, $"The change log cannot be displayed as the URL is empty.", AutoUpdater.DialogTitle, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }

            try
            {
                WebBrowser.Navigate(AutoUpdater.ChangeLogUrl);
            }
            catch (Exception ex)
            {
                Dispatcher.InvokeAsync(() => MessageBox.Show(this, $"The following error occurred trying to navigate to the change log: {ex.Message}", AutoUpdater.DialogTitle, MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AutoUpdater.DownloadUrl))
            {
                MessageBox.Show(this, $"The update cannot be done as the download URL is empty.", AutoUpdater.DialogTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AutoUpdater.OpenDownloadPage)
            {
                var processStartInfo = new ProcessStartInfo(AutoUpdater.DownloadUrl);

                Process.Start(processStartInfo);
            }
            else
            {
                var downloadDialog = new DownloadUpdate(AutoUpdater.DownloadUrl);

                try
                {
                    downloadDialog.ShowDialog();
                }
                catch (TargetInvocationException)
                {
                }
            }
        }

        private void buttonRemindLater_Click(object sender, RoutedEventArgs e)
        {
            if (AutoUpdater.LetUserSelectRemindLater)
            {
                var remindLaterForm = new RemindLater();

                var dialogResult = remindLaterForm.ShowDialog();

                if (dialogResult == true)
                {
                    AutoUpdater.RemindLaterTimeSpan = remindLaterForm.RemindLaterFormat;
                    AutoUpdater.RemindLaterAt = remindLaterForm.RemindLaterAt;
                }
                else if (dialogResult == false)
                {
                    var downloadDialog = new DownloadUpdate(AutoUpdater.DownloadUrl);

                    try
                    {
                        downloadDialog.ShowDialog();
                    }
                    catch (TargetInvocationException)
                    {
                        return;
                    }
                    return;
                }
                else
                {
                    DialogResult = null;
                    return;
                }
            }

            RegistryKey updateKey = Registry.CurrentUser.CreateSubKey(AutoUpdater.RegistryLocation);
            if (updateKey != null)
            {
                updateKey.SetValue("version", AutoUpdater.CurrentVersion);
                updateKey.SetValue("skip", 0);
                DateTime remindLaterDateTime = DateTime.Now;
                switch (AutoUpdater.RemindLaterTimeSpan)
                {
                    case RemindLaterFormat.Days:
                        remindLaterDateTime = DateTime.Now + TimeSpan.FromDays(AutoUpdater.RemindLaterAt);
                        break;
                    case RemindLaterFormat.Hours:
                        remindLaterDateTime = DateTime.Now + TimeSpan.FromHours(AutoUpdater.RemindLaterAt);
                        break;
                    case RemindLaterFormat.Minutes:
                        remindLaterDateTime = DateTime.Now + TimeSpan.FromMinutes(AutoUpdater.RemindLaterAt);
                        break;

                }
                updateKey.SetValue("remindlater", remindLaterDateTime.ToString(CultureInfo.CreateSpecificCulture("en-US")));
                SetTimer(remindLaterDateTime);
                updateKey.Close();
            }
        }

        private void buttonSkip_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey updateKey = Registry.CurrentUser.CreateSubKey(AutoUpdater.RegistryLocation);
            if (updateKey != null)
            {
                updateKey.SetValue("version", AutoUpdater.CurrentVersion.ToString());
                updateKey.SetValue("skip", 1);
                updateKey.Close();
            }
        }

        public void SetTimer(DateTime remindLater)
        {
            TimeSpan timeSpan = remindLater - DateTime.Now;
            _timer = new Timer
            {
                Interval = (int)timeSpan.TotalMilliseconds
            };
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            AutoUpdater.Start();
        }

        private void UpdateWindow_Closed(object sender, EventArgs e)
        {
            AutoUpdater.Running = false;
        }
    }
}

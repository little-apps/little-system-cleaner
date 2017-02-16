using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Timers;
using System.Windows;

namespace AutoUpdaterWPF
{
    /// <summary>
    ///     Interaction logic for Update.xaml
    /// </summary>
    public partial class Update
    {
        private Timer _timer;

        public Update(bool remindLater = false)
        {
            if (remindLater)
                return;

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

        public string Text
        {
            get { return Title; }
            set { Title = value; }
        }

        private void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AutoUpdater.ChangeLogUrl))
            {
                Dispatcher.InvokeAsync(
                    () =>
                        MessageBox.Show(this, "The change log cannot be displayed as the URL is empty.",
                            AutoUpdater.DialogTitle, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }

            try
            {
                WebBrowser.Navigate(AutoUpdater.ChangeLogUrl);
            }
            catch (Exception ex)
            {
                Dispatcher.InvokeAsync(
                    () =>
                        MessageBox.Show(this,
                            $"The following error occurred trying to navigate to the change log: {ex.Message}",
                            AutoUpdater.DialogTitle, MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AutoUpdater.DownloadUrl))
            {
                MessageBox.Show(this, "The update cannot be done as the download URL is empty.", AutoUpdater.DialogTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AutoUpdater.OpenDownloadPage)
            {
                var processStartInfo = new ProcessStartInfo(AutoUpdater.DownloadUrl);

                Process.Start(processStartInfo);
            }
            else
            {
                var downloadDialog = new global::AutoUpdaterWPF.DownloadUpdate(AutoUpdater.DownloadUrl);

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
                var remindLaterForm = new global::AutoUpdaterWPF.RemindLater();

                var dialogResult = remindLaterForm.ShowDialog();

                switch (dialogResult)
                {
                    case true:
                        AutoUpdater.RemindLaterTimeSpan = remindLaterForm.RemindLaterFormat;
                        AutoUpdater.RemindLaterAt = remindLaterForm.RemindLaterAt;
                        break;

                    case false:
                        var downloadDialog = new global::AutoUpdaterWPF.DownloadUpdate(AutoUpdater.DownloadUrl);

                        try
                        {
                            downloadDialog.ShowDialog();
                        }
                        catch (TargetInvocationException)
                        {
                            return;
                        }
                        return;

                    default:
                        DialogResult = null;
                        return;
                }
            }

            var updateKey = Registry.CurrentUser.CreateSubKey(AutoUpdater.RegistryLocation);
            if (updateKey == null)
                return;

            updateKey.SetValue("version", AutoUpdater.CurrentVersion);
            updateKey.SetValue("skip", 0);

            var remindLaterDateTime = DateTime.Now;
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

        private void buttonSkip_Click(object sender, RoutedEventArgs e)
        {
            var updateKey = Registry.CurrentUser.CreateSubKey(AutoUpdater.RegistryLocation);

            if (updateKey == null)
                return;

            updateKey.SetValue("version", AutoUpdater.CurrentVersion.ToString());
            updateKey.SetValue("skip", 1);
            updateKey.Close();
        }

        public void SetTimer(DateTime remindLater)
        {
            var timeSpan = remindLater - DateTime.Now;
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
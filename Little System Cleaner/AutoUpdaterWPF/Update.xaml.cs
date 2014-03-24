using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

namespace Little_System_Cleaner.AutoUpdaterWPF
{
    /// <summary>
    /// Interaction logic for Update.xaml
    /// </summary>
    public partial class Update : Window
    {
        private System.Timers.Timer _timer;

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
                
                this.Text = AutoUpdater.DialogTitle;
                this.textBlockUpdate.Text = string.Format(this.textBlockUpdate.Text, AutoUpdater.AppTitle);
                this.textBlockDescription.Text =
                    string.Format(this.textBlockDescription.Text,
                        AutoUpdater.AppTitle, AutoUpdater.CurrentVersion, AutoUpdater.InstalledVersion);
            }
        }

        public string Text
        {
            get { return base.Title; }
            set { base.Title = value; }
        }

        private void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.webBrowser.Navigate(AutoUpdater.ChangeLogURL);
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (AutoUpdater.OpenDownloadPage)
            {
                var processStartInfo = new ProcessStartInfo(AutoUpdater.DownloadURL);

                Process.Start(processStartInfo);
            }
            else
            {
                var downloadDialog = new DownloadUpdate(AutoUpdater.DownloadURL);

                try
                {
                    downloadDialog.ShowDialog();
                }
                catch (System.Reflection.TargetInvocationException)
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
                    var downloadDialog = new DownloadUpdate(AutoUpdater.DownloadURL);

                    try
                    {
                        downloadDialog.ShowDialog();
                    }
                    catch (System.Reflection.TargetInvocationException)
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
            _timer = new System.Timers.Timer
            {
                Interval = (int)timeSpan.TotalMilliseconds
            };
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            AutoUpdater.Start();
        }
    }
}

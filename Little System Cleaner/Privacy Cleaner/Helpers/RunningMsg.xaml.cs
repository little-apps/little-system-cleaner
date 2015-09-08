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
using System.Diagnostics;
using System.Threading;
using System.Timers;
using System.Windows;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Timer = System.Timers.Timer;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers
{
    /// <summary>
    /// Interaction logic for RunningMsg.xaml
    /// </summary>
    public partial class RunningMsg
    {
        private readonly string _procName;
        private readonly string _scannerName;

        private readonly Timer _timer = new Timer(100);

        public RunningMsg(string name, string proc)
        {
            InitializeComponent();

            _procName = proc;
            _scannerName = name;

            _timer.Elapsed += timer_Elapsed;
            _timer.Start();
        }

        private void checkBoxDontShow_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.privacyCleanerAutoSkipScanner = checkBoxDontShow.IsChecked.GetValueOrDefault();
        }

        internal static bool? DisplayRunningMsg(string scannerName, string procName)
        {
            if (Application.Current.Dispatcher.Thread != Thread.CurrentThread)
            {
                return (bool?)Application.Current.Dispatcher.Invoke(new Func<string, string, bool?>(DisplayRunningMsg), scannerName, procName);
            }

            if (!MiscFunctions.IsProcessRunning(procName))
                return true;

            if (Settings.Default.privacyCleanerAutoSkipScanner)
            {
                MessageBox.Show(Application.Current.MainWindow, $"Automatically skipping the scanning for {scannerName}...", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            RunningMsg dlgResult = new RunningMsg(scannerName, procName);
            return dlgResult.ShowDialog();
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new EventHandler<ElapsedEventArgs>(timer_Elapsed), sender, e);
                return;
            }

            // Update list box
            listBox.Items.Clear();
            foreach (Process p in Process.GetProcessesByName(_procName))
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                    listBox.Items.Add(p.MainWindowTitle);
            }

            // Check if process is running
            if (MiscFunctions.IsProcessRunning(_procName))
                return;

            DialogResult = true;
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (DialogResult.GetValueOrDefault() == false)
            {
                MessageBox.Show(this, $"Skipping the scanning for {_scannerName}...", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

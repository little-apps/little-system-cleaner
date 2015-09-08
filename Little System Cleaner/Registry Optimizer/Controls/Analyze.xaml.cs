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
using System.Threading;
using System.Windows;
using System.Windows.Shell;
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Registry_Optimizer.Helpers;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Analyze
    {
        Thread _threadCurrent, _threadScan;

        public Analyze()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Increase total number of scans
            Settings.Default.totalScans++;

            // Zero last scan errors found + fixed and elapsed
            Settings.Default.lastScanErrors = 0;
            Settings.Default.lastScanErrorsFixed = 0;
            Settings.Default.lastScanElapsed = 0;

            // Set last scan date
            Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            // Set taskbar progress bar
            Little_System_Cleaner.Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
            Little_System_Cleaner.Main.TaskbarProgressValue = 0;

            // Set progress bar
            progressBar1.Minimum = 0;
            progressBar1.Maximum = Wizard.RegistryHives.Count;
            progressBar1.Value = 0;

            _threadScan = new Thread(AnalyzeHives);
            _threadScan.Start();
        }

        private void AnalyzeHives()
        {
            DateTime dtStart = DateTime.Now;

            Thread.BeginCriticalRegion();

            foreach (Hive h in Wizard.RegistryHives)
            {
                IncrementProgressBar(h.RegistryHive);

                // Analyze Hive
                _threadCurrent = new Thread(() => h.AnalyzeHive(this));
                _threadCurrent.Start();
                _threadCurrent.Join();
            }

            Thread.EndCriticalRegion();

            TimeSpan timeSpan = DateTime.Now.Subtract(dtStart);

            Little_System_Cleaner.Main.Watcher.EventPeriod("Registry Optimizer", "Analyze", (int)timeSpan.TotalSeconds, true);

            Settings.Default.lastScanElapsed = timeSpan.Ticks;

            Dispatcher.BeginInvoke(new Action(() => {
                Little_System_Cleaner.Main.TaskbarProgressState = TaskbarItemProgressState.None;
                Close();
            }));
        }

        private void IncrementProgressBar(string currentHive)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new Action<string>(IncrementProgressBar), currentHive);
                return;
            }

            progressBar1.Value++;
            textBlockStatus.Text = $"Analyzing: {currentHive}";
        }

        private void progressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (progressBar1.Maximum != 0)
                Little_System_Cleaner.Main.TaskbarProgressValue = (e.NewValue / progressBar1.Maximum);
        }
    }
}

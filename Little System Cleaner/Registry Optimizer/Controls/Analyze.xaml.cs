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
using System.Collections.Generic;
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
using System.Threading;
using CommonTools;
using Little_System_Cleaner.Registry_Optimizer.Helpers;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Analyze : Window
    {
        Thread threadCurrent, threadScan;

        public Analyze()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set start time
            Wizard.ScanStartTime = DateTime.Now;

            // Set last scan date
            Properties.Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            // Increase total number of scans
            Properties.Settings.Default.totalScans++;

            // Zero last scan errors found + fixed
            Properties.Settings.Default.lastScanErrors = 0;
            Properties.Settings.Default.lastScanErrorsFixed = 0;

            // Set progress bar
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = Wizard.RegistryHives.Count;
            this.progressBar1.Value = 0;

            threadScan = new Thread(new ThreadStart(AnalyzeHives));
            threadScan.Start();
        }

        private void AnalyzeHives()
        {
            Thread.BeginCriticalRegion();

            foreach (Hive h in Wizard.RegistryHives)
            {
                IncrementProgressBar(h.RegistryHive);;

                // Analyze Hive
                threadCurrent = new Thread(new ThreadStart(h.AnalyzeHive));
                threadCurrent.Start();
                threadCurrent.Join();
            }

            Thread.EndCriticalRegion();

            this.Dispatcher.BeginInvoke(new Action(this.Close));
        }

        private void IncrementProgressBar(string currentHive)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action<string>(IncrementProgressBar), currentHive);
                return;
            }

            this.progressBar1.Value++;
            this.textBlockStatus.Text = string.Format("Analyzing: {0}", currentHive);
        }
    }
}

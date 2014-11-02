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
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Little_System_Cleaner.Registry_Cleaner.Scanners;
using CommonTools;
using System.Drawing;
using System.Windows.Media.Imaging;
using CommonTools.WpfAnimatedGif;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Little_System_Cleaner.Misc;
using System.Diagnostics;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
	public partial class Scan : UserControl
	{
        static List<ScannerBase> enabledScanners = new List<ScannerBase>();
        Wizard scanBase;
        System.Timers.Timer timerUpdate = new System.Timers.Timer(200);
        DateTime dateTimeStart = DateTime.MinValue;
        Thread threadScan;
        int currentListViewIndex = -1;
        ObservableCollection<lviScanner> _SectionCollection = new ObservableCollection<lviScanner>();
        private static bool AbortScan;

        private static string currentItemScanned;
        internal static int TotalItemsScanned = -1;

        /// <summary>
        /// Gets the enabled scanners
        /// </summary>
        internal static List<ScannerBase> EnabledScanners
        {
            get { return enabledScanners; }
        }

        /// <summary>
        /// Gets the total problems
        /// </summary>
        internal static int TotalProblems
        {
            get { return Wizard.badRegKeyArray.Count; }
        }

        /// <summary>
        /// Sets the currently scanned item and increments the total
        /// </summary>
        internal static string CurrentItem
        {
            get { return currentItemScanned; }
            set
            {
                TotalItemsScanned++;
                currentItemScanned = string.Copy(value);
            }
        }
        
        public lviScanner CurrentListViewItem
        {
            get { return this.SectionsCollection[currentListViewIndex] as lviScanner; }
        }

        public ObservableCollection<lviScanner> SectionsCollection
        {
            get { return _SectionCollection; }
        }

        public Scan(Wizard sb)
        {
            this.InitializeComponent();

            this.Focus();

            this.scanBase = sb;

            // Reset AbortScan
            AbortScan = false;

            // Zero last scan errors found + fixed and elapsed
            Properties.Settings.Default.lastScanErrors = 0;
            Properties.Settings.Default.lastScanErrorsFixed = 0;
            Properties.Settings.Default.lastScanElapsed = 0;

            // Set last scan date
            Properties.Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            // Start timer
            this.timerUpdate.Elapsed += new System.Timers.ElapsedEventHandler(timerUpdate_Elapsed);
            this.timerUpdate.Start();

            // Set taskbar progress bar
            Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            Main.TaskbarProgressValue = 0;

            // Set the progress bar
            this.progressBar.Minimum = 0;
            this.progressBar.Maximum = Scan.EnabledScanners.Count;
            this.progressBar.Value = 0;

            // Populate ListView
            foreach (ScannerBase scanBase in Scan.EnabledScanners)
            {
                this._SectionCollection.Add(new lviScanner(scanBase.ScannerName));
            }

            Wizard.ScanThread = new Thread(new ThreadStart(StartScanning));
            Wizard.ScanThread.Start();
        }

        public void AbortScanThread()
        {
            if (Wizard.ScanThread.IsAlive)
            {
                Wizard.ScanThread.Interrupt();
                Wizard.ScanThread.Abort();
            }

            // In case ScanThread failed to abort the child thread
            if (this.threadScan.IsAlive)
                this.threadScan.Abort();
        }

        void timerUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(timerUpdate_Elapsed), new object[] { sender, e });
                return;
            }

            if (this.currentListViewIndex != -1)
            {
                this.CurrentListViewItem.Errors = string.Format("{0} Errors", Wizard.badRegKeyArray.Problems(this.CurrentListViewItem.Section));
                this.listView.Items.Refresh();
            }
        }

        /// <summary>
        /// Begins scanning for errors in the registry
        /// </summary>
        private void StartScanning()
        {
            // Set scan start time
            this.dateTimeStart = DateTime.Now;

            // Create log file + Write Header
            Wizard.CreateNewLogFile();

            // Begin Critical Region
            Thread.BeginCriticalRegion();

            try
            {
                Wizard.Report.WriteLine("Started scan at " + DateTime.Now.ToString());
                Wizard.Report.WriteLine();

                // Begin Scanning
                foreach (ScannerBase scanner in Scan.EnabledScanners)
                {
                    invokeCurrentSection(scanner.ScannerName);

                    Wizard.Report.WriteLine("Starting scanning: " + scanner.ScannerName);

                    this.StartScanner(scanner);

                    if (AbortScan)
                        break;

                    Wizard.Report.WriteLine("Finished scanning: " + scanner.ScannerName);
                    Wizard.Report.WriteLine();
                }
            }
            catch (ThreadAbortException )
            {
                // Scanning was aborted
                AbortScan = true;

                Wizard.Report.Write("User aborted scan... ");

                if (this.threadScan.IsAlive)
                    this.threadScan.Abort();

                Wizard.Report.WriteLine("Exiting.\r\n");
            }
            finally
            {
                // Compute time between start and end of scan
                TimeSpan ts = DateTime.Now.Subtract(dateTimeStart);

                // Report to Little Software Stats
                Main.Watcher.EventPeriod("Registry Cleaner", "Scan", (int)ts.TotalSeconds, true);

                // Set last scan elapsed time (in ticks)
                Properties.Settings.Default.lastScanElapsed = ts.Ticks;

                // Increase total number of scans
                Properties.Settings.Default.totalScans++;

                // Stop timer
                this.timerUpdate.Stop();

                // Write scan stats to log file
                Wizard.Report.WriteLine(string.Format("Total time elapsed: {0} minutes {0} seconds", ts.Minutes, ts.Seconds));
                Wizard.Report.WriteLine(string.Format("Total problems found: {0}", TotalProblems));
                Wizard.Report.WriteLine(string.Format("Total objects scanned: {0}", TotalItemsScanned));
                Wizard.Report.WriteLine();
                Wizard.Report.WriteLine("Finished scan at " + DateTime.Now.ToString());

                // End Critical Region
                Thread.EndCriticalRegion();

                // Reset taskbar progress bar
                this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None));

                if (!AbortScan)
                    this.scanBase.MoveNext();
            }
        }

        private void StartScanner(ScannerBase scanner)
        {
            System.Reflection.MethodInfo mi = scanner.GetType().GetMethod("Scan", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (mi == null)
            {
                Debug.WriteLine("Unable to get method info for " + scanner.ScannerName);
                return;
            }

            Action objScan = (Action)Delegate.CreateDelegate(typeof(Action), mi);

            // Start thread
            this.threadScan = new Thread(new ThreadStart(objScan));

            try
            {
                threadScan.Start();
                threadScan.Join();
            }
            catch (ThreadInterruptedException)
            {
                if (!AbortScan)
                    AbortScan = true;

                if (threadScan.IsAlive)
                    threadScan.Abort();
            }
        }

        /// <summary>
        /// Sets the current section and increments the progress bar
        /// </summary>
        private void invokeCurrentSection(string sectionName)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action<string>(invokeCurrentSection), new object[] { sectionName });
                return;
            }

            if (this.currentListViewIndex != -1)
            {
                this.CurrentListViewItem.Status = "Finished";
                this.CurrentListViewItem.UnloadGif();
            }

            this.progressBar.Value++;
            this.currentListViewIndex++;

            Wizard.currentScannerName = sectionName;
            this.currentSection.Content = "Section: " + sectionName;

            this.CurrentListViewItem.Status = "Scanning";
            this.CurrentListViewItem.LoadGif();

            this.listView.Items.Refresh();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(App.Current.MainWindow, "Would you like to cancel the scan thats in progress?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // AbortScanThread will be called via Unloaded event
                this.scanBase.MoveFirst();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.AbortScanThread();
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.progressBar.Maximum != 0)
                Main.TaskbarProgressValue = (e.NewValue / this.progressBar.Maximum);
        }
       
	}
}
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

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
	public partial class Scan : UserControl
	{
        static List<ScannerBase> enabledScanners = new List<ScannerBase>();
        ScanWizard scanBase;
        System.Timers.Timer timerUpdate = new System.Timers.Timer(200);
        DateTime dateTimeStart = DateTime.MinValue;
        Thread threadScan;
        int currentListViewIndex = -1;
        ObservableCollection<lviScanner> _SectionCollection = new ObservableCollection<lviScanner>();

        private static string currentItemScanned;
        public static int TotalItemsScanned = -1;

        /// <summary>
        /// Gets the enabled scanners
        /// </summary>
        public static List<ScannerBase> EnabledScanners
        {
            get { return enabledScanners; }
        }

        /// <summary>
        /// Gets the total problems
        /// </summary>
        public static int TotalProblems
        {
            get { return ScanWizard.badRegKeyArray.Count; }
        }

        /// <summary>
        /// Sets the currently scanned item and increments the total
        /// </summary>
        public static string CurrentItem
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

        public Scan(ScanWizard sb)
        {
            this.InitializeComponent();

            this.Focus();

            this.scanBase = sb;

            // Set last scan date
            Properties.Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            // Start timer
            this.timerUpdate.Elapsed += new System.Timers.ElapsedEventHandler(timerUpdate_Elapsed);
            this.timerUpdate.Start();

            // Set the progress bar
            this.progressBar.Minimum = 0;
            this.progressBar.Maximum = Scan.EnabledScanners.Count;
            this.progressBar.Value = 0;

            // Populate ListView
            foreach (ScannerBase scanBase in Scan.EnabledScanners)
            {
                this._SectionCollection.Add(new lviScanner(scanBase.ScannerName));
            }

            ScanWizard.ScanThread = new Thread(new ThreadStart(StartScanning));
            ScanWizard.ScanThread.Start();
        }

        void timerUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new System.Timers.ElapsedEventHandler(timerUpdate_Elapsed), new object[] { sender, e });
                return;
            }


            if (this.currentListViewIndex != -1)
            {
                this.CurrentListViewItem.Errors = string.Format("{0} Errors", ScanWizard.badRegKeyArray.Problems(this.CurrentListViewItem.Section));
                this.listView.Items.Refresh();
            }
        }

        /// <summary>
        /// Begins scanning for errors in the registry
        /// </summary>
        private void StartScanning()
        {
            bool scanAborted;

            // Set scan start time
            this.dateTimeStart = DateTime.Now;

            // Create log file + Write Header
            ScanWizard.CreateNewLogFile();

            // Begin Critical Region
            Thread.BeginCriticalRegion();

            try
            {
                ScanWizard.logger.WriteLine("Started scan at " + DateTime.Now.ToString());
                ScanWizard.logger.WriteLine();

                // Begin Scanning
                foreach (ScannerBase scanner in Scan.EnabledScanners)
                {
                    invokeCurrentSection(scanner.ScannerName);

                    ScanWizard.logger.WriteLine("Starting scanning: " + scanner.ScannerName);

                    this.StartScanner(scanner);

                    ScanWizard.logger.WriteLine("Finished scanning: " + scanner.ScannerName);
                    ScanWizard.logger.WriteLine();
                }

                // Scan was successful
                scanAborted = false;
            }
            catch (ThreadAbortException )
            {
                // Scanning was aborted
                scanAborted = true;
                
                ScanWizard.logger.Write("User aborted scan... ");

                if (this.threadScan.IsAlive)
                    this.threadScan.Abort();

                ScanWizard.logger.WriteLine("Exiting.\r\n");
            }
            finally
            {
                // Compute time between start and end of scan
                TimeSpan ts = DateTime.Now.Subtract(dateTimeStart);

                // Set last scan elapsed time (in ticks)
                Properties.Settings.Default.lastScanElapsed = ts.Ticks;

                // Increase total number of scans
                Properties.Settings.Default.totalScans++;

                // Stop timer
                this.timerUpdate.Stop();

                // Write scan stats to log file
                ScanWizard.logger.WriteLine(string.Format("Total time elapsed: {0} minutes {0} seconds", ts.Minutes, ts.Seconds));
                ScanWizard.logger.WriteLine(string.Format("Total problems found: {0}", TotalProblems));
                ScanWizard.logger.WriteLine(string.Format("Total objects scanned: {0}", TotalItemsScanned));
                ScanWizard.logger.WriteLine();
                ScanWizard.logger.WriteLine("Finished scan at " + DateTime.Now.ToString());
            }

            // End Critical Region
            Thread.EndCriticalRegion();

            if (!scanAborted)
                this.Dispatcher.Invoke(new Action(this.scanBase.MoveNext));
        }

        /// <summary>
        /// Sets the current section and increments the progress bar
        /// </summary>
        private void invokeCurrentSection(string sectionName)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action<string>(invokeCurrentSection), new object[] { sectionName });
                return;
            }

            if (this.currentListViewIndex != -1)
            {
                this.CurrentListViewItem.Status = "Finished";
                this.CurrentListViewItem.UnloadGif();
            }

            this.progressBar.Value++;
            this.currentListViewIndex++;

            ScanWizard.currentScannerName = sectionName;
            this.currentSection.Text = "Section: " + sectionName;

            this.CurrentListViewItem.Status = "Scanning";
            this.CurrentListViewItem.LoadGif();

            this.listView.Items.Refresh();
        }

        private void StartScanner(ScannerBase scanner)
        {
            System.Reflection.MethodInfo mi = scanner.GetType().GetMethod("Scan", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Action objScan = (Action)Delegate.CreateDelegate(typeof(Action), mi);

            // Start thread
            this.threadScan = new Thread(new ThreadStart(objScan));

            threadScan.Start();
            threadScan.Join();

            // Wait 250ms
            Thread.Sleep(250);
        }

        private void buttonResults_Click(object sender, RoutedEventArgs e)
        {
            this.scanBase.MoveNext();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Would you like to cancel the scan thats in progress?", "Little Registry Cleaner", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ScanWizard.ScanThread.Abort();
                this.threadScan.Abort();
                this.scanBase.MoveFirst();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ScanWizard.ScanThread.Abort();
        }
       
	}

    public class lviScanner
    {
        public string Section { get; set; }
        public string Status { get; set; }
        public string Errors { get; set; }

        public System.Windows.Controls.Image Image { get; private set; }
        public Uri bMapImg { get; private set; }

        public lviScanner(string section)
        {
            Section = section;
            Status = "Queued";
            Errors = "0 Errors";
        }

        public void LoadGif()
        {
            this.Image = new System.Windows.Controls.Image();

            BitmapSource gif = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.ajax_loader.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            ImageBehavior.SetAnimatedSource(this.Image, gif);
        }

        public void UnloadGif()
        {
            this.Image = null;
            
            this.Image = new System.Windows.Controls.Image();
            this.Image.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.Repair.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }
    }
}
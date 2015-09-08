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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Shell;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Little_System_Cleaner.Registry_Cleaner.Scanners;
using ThreadState = System.Threading.ThreadState;
using Timer = System.Timers.Timer;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
	public partial class Scan
	{
	    readonly Wizard _scanBase;
	    readonly Timer _timerUpdate = new Timer(200);
        DateTime _dateTimeStart = DateTime.MinValue;
        Thread _threadScan;
        int _currentListViewIndex = -1;
	    readonly ObservableCollection<lviScanner> _sectionCollection = new ObservableCollection<lviScanner>();
        private static bool _abortScan;

        private static string _currentItemScanned;
        internal static int TotalItemsScanned = -1;

        /// <summary>
        /// Gets the enabled scanners
        /// </summary>
        internal static List<ScannerBase> EnabledScanners { get; } = new List<ScannerBase>();

	    /// <summary>
        /// Gets the total problems
        /// </summary>
        internal static int TotalProblems => Wizard.badRegKeyArray.Count;

	    /// <summary>
        /// Sets the currently scanned item and increments the total
        /// </summary>
        internal static string CurrentItem
        {
            get { return _currentItemScanned; }
            set
            {
                TotalItemsScanned++;
                _currentItemScanned = string.Copy(value);
            }
        }
        
        public lviScanner CurrentListViewItem => SectionsCollection[_currentListViewIndex];

	    public ObservableCollection<lviScanner> SectionsCollection => _sectionCollection;

	    public Scan(Wizard sb)
        {
            InitializeComponent();

            Focus();

            _scanBase = sb;

            // Reset AbortScan
            _abortScan = false;

            // Zero last scan errors found + fixed and elapsed
            Settings.Default.lastScanErrors = 0;
            Settings.Default.lastScanErrorsFixed = 0;
            Settings.Default.lastScanElapsed = 0;

            // Set last scan date
            Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            // Start timer
            _timerUpdate.Interval = 250;
            _timerUpdate.Elapsed += timerUpdate_Elapsed;
            _timerUpdate.Start();

            // Set taskbar progress bar
            Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
            Main.TaskbarProgressValue = 0;

            // Set the progress bar
            progressBar.Minimum = 0;
            progressBar.Maximum = EnabledScanners.Count;
            progressBar.Value = 0;

            // Populate ListView
            foreach (var lvi in EnabledScanners.Select(scanBase => new lviScanner(scanBase.ScannerName)))
            {
                _sectionCollection.Add(lvi);
            }

            Wizard.ScanThread = new Thread(StartScanning);
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
            if (_threadScan.IsAlive)
                _threadScan.Abort();
        }

        void timerUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new ElapsedEventHandler(timerUpdate_Elapsed), sender, e);
                return;
            }

            if (_currentListViewIndex != -1)
            {
                CurrentListViewItem.Errors = $"{Wizard.badRegKeyArray.Problems(CurrentListViewItem.Section)} Errors";
                listView.Items.Refresh();
            }
        }

        /// <summary>
        /// Begins scanning for errors in the registry
        /// </summary>
        private void StartScanning()
        {
            // Set scan start time
            _dateTimeStart = DateTime.Now;

            // Create log file + Write Header
            Wizard.CreateNewLogFile();

            // Begin Critical Region
            Thread.BeginCriticalRegion();

            try
            {
                Wizard.Report.WriteLine("Started scan at " + DateTime.Now);
                Wizard.Report.WriteLine();

                // Begin Scanning
                foreach (ScannerBase scanner in EnabledScanners)
                {
                    InvokeCurrentSection(scanner.ScannerName);

                    Wizard.Report.WriteLine("Starting scanning: " + scanner.ScannerName);

                    StartScanner(scanner);

                    if (_abortScan)
                        break;

                    Wizard.Report.WriteLine("Finished scanning: " + scanner.ScannerName);
                    Wizard.Report.WriteLine();
                }
            }
            catch (ThreadAbortException )
            {
                // Scanning was aborted
                _abortScan = true;

                Wizard.Report.Write("User aborted scan... ");

                if (_threadScan.IsAlive && _threadScan.ThreadState != ThreadState.AbortRequested)
                    _threadScan.Abort();

                Wizard.Report.WriteLine("Exiting.\r\n");

                Thread.ResetAbort();
            }
            finally
            {
                // Compute time between start and end of scan
                TimeSpan ts = DateTime.Now.Subtract(_dateTimeStart);

                // Report to Little Software Stats
                Main.Watcher.EventPeriod("Registry Cleaner", "Scan", (int)ts.TotalSeconds, true);

                // Set last scan elapsed time (in ticks)
                Settings.Default.lastScanElapsed = ts.Ticks;

                // Increase total number of scans
                Settings.Default.totalScans++;

                // Stop timer
                _timerUpdate.Stop();

                // Write scan stats to log file
                Wizard.Report.WriteLine("Total time elapsed: {0} minutes {1} seconds", ts.Minutes, ts.Seconds);
                Wizard.Report.WriteLine("Total problems found: {0}", TotalProblems);
                Wizard.Report.WriteLine("Total objects scanned: {0}", TotalItemsScanned);
                Wizard.Report.WriteLine();
                Wizard.Report.WriteLine("Finished scan at " + DateTime.Now);

                // End Critical Region
                Thread.EndCriticalRegion();

                // Reset taskbar progress bar
                Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.None));

                if (!_abortScan)
                    _scanBase.MoveNext();
            }
        }

        private void StartScanner(ScannerBase scanner)
        {
            MethodInfo mi = scanner.GetType().GetMethod("Scan", BindingFlags.NonPublic | BindingFlags.Static);

            if (mi == null)
            {
                Debug.WriteLine("Unable to get method info for " + scanner.ScannerName);
                return;
            }

            Action objScan = (Action)Delegate.CreateDelegate(typeof(Action), mi);

            // Start thread
            _threadScan = new Thread(new ThreadStart(objScan));

            try
            {
                _threadScan.Start();
                _threadScan.Join();
            }
            catch (Exception ex)
            {
                if (ex is ThreadInterruptedException)
                {
                    if (!_abortScan)
                        _abortScan = true;

                    if (_threadScan.IsAlive)
                        _threadScan.Abort();
                }
                else
                {
                    Debug.WriteLine(ex.Message);
                }
                
            }
        }

        /// <summary>
        /// Sets the current section and increments the progress bar
        /// </summary>
        private void InvokeCurrentSection(string sectionName)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new Action<string>(InvokeCurrentSection), sectionName);
                return;
            }

            if (_currentListViewIndex != -1)
            {
                // Update number of errors in case it wasn't updated yet
                CurrentListViewItem.Errors = $"{Wizard.badRegKeyArray.Problems(CurrentListViewItem.Section)} Errors";

                CurrentListViewItem.Status = "Finished";
                CurrentListViewItem.UnloadGif();
            }

            progressBar.Value++;
            _currentListViewIndex++;

            Wizard.CurrentScannerName = sectionName;
            currentSection.Content = "Section: " + sectionName;

            CurrentListViewItem.Status = "Scanning";
            CurrentListViewItem.LoadGif();

            CurrentListViewItem.Errors = "0 Errors";

            listView.Items.Refresh();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Application.Current.MainWindow, "Would you like to cancel the scan that's in progress?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // AbortScanThread will be called via Unloaded event
                _scanBase.MoveFirst();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            AbortScanThread();
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (progressBar.Maximum != 0)
                Main.TaskbarProgressValue = (e.NewValue / progressBar.Maximum);
        }
       
	}
}
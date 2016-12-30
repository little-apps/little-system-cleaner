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
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Little_System_Cleaner.Registry_Cleaner.Scanners;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Shell;
using Timer = System.Timers.Timer;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
    public partial class Scan
    {
        private static string _currentItemScanned;
        private static int _totalItemsScanned = -1;

        private readonly Task _mainTaskScan;
        private readonly Wizard _scanBase;
        private readonly Timer _timerUpdate = new Timer(200);
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private int _currentListViewIndex = -1;
        private DateTime _dateTimeStart = DateTime.MinValue;

        public Scan(Wizard sb)
        {
            InitializeComponent();

            Focus();

            _scanBase = sb;

            // Start timer
            _timerUpdate.Interval = 250;
            _timerUpdate.Elapsed += timerUpdate_Elapsed;
            _timerUpdate.Start();

            // Set taskbar progress bar
            Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
            Main.TaskbarProgressValue = 0;

            // Set the progress bar
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = EnabledScanners.Count;
            ProgressBar.Value = 0;

            // Populate ListView
            foreach (var lvi in EnabledScanners.Select(scanBase => new ScannerListViewItem(scanBase.ScannerName)))
            {
                SectionsCollection.Add(lvi);
            }

            _mainTaskScan = new Task(StartScanning, _cancellationTokenSource.Token);
            _mainTaskScan.Start();
        }

        /// <summary>
        ///     Gets the enabled scanners
        /// </summary>
        internal static List<ScannerBase> EnabledScanners { get; } = new List<ScannerBase>();

        /// <summary>
        ///     Gets the total problems
        /// </summary>
        private static int TotalProblems => Wizard.BadRegKeyArray.Count;

        /// <summary>
        ///     Sets the currently scanned item and increments the total
        /// </summary>
        internal static string CurrentItem
        {
            get { return _currentItemScanned; }
            set
            {
                _totalItemsScanned++;
                _currentItemScanned = string.Copy(value);
            }
        }

        private ScannerListViewItem CurrentListViewItem => SectionsCollection[_currentListViewIndex];

        private ObservableCollection<ScannerListViewItem> SectionsCollection { get; } =
            new ObservableCollection<ScannerListViewItem>();

        public void AbortScanThread()
        {
            _cancellationTokenSource?.Cancel();
            ScannerBase.CancellationToken?.Cancel();
        }

        private void timerUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new ElapsedEventHandler(timerUpdate_Elapsed), sender, e);
                return;
            }

            if (_currentListViewIndex != -1)
            {
                CurrentListViewItem.Errors = $"{Wizard.BadRegKeyArray.Problems(CurrentListViewItem.Section)} Errors";
                ListView.Items.Refresh();
            }
        }

        /// <summary>
        ///     Begins scanning for errors in the registry
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
                foreach (var scanner in EnabledScanners)
                {
                    InvokeCurrentSection(scanner.ScannerName);

                    Wizard.Report.WriteLine("Starting scanning: " + scanner.ScannerName);

                    StartScanner(scanner);

                    if (_cancellationTokenSource.IsCancellationRequested)
                        _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    Wizard.Report.WriteLine("Finished scanning: " + scanner.ScannerName);
                    Wizard.Report.WriteLine();
                }
            }
            catch (OperationCanceledException)
            {
                // Scanning was aborted
                Wizard.Report.Write("User aborted scan... ");

                Wizard.Report.WriteLine("Exiting.\r\n");
            }
            finally
            {
                // Compute time between start and end of scan
                var ts = DateTime.Now.Subtract(_dateTimeStart);

                // Report to Little Software Stats
                Main.Watcher.EventPeriod("Registry Cleaner", "Scan", (int)ts.TotalSeconds, true);

                // Stop timer
                _timerUpdate.Stop();

                // Write scan stats to log file
                Wizard.Report.WriteLine("Total time elapsed: {0} minutes {1} seconds", ts.Minutes, ts.Seconds);
                Wizard.Report.WriteLine("Total problems found: {0}", TotalProblems);
                Wizard.Report.WriteLine("Total objects scanned: {0}", _totalItemsScanned);
                Wizard.Report.WriteLine();
                Wizard.Report.WriteLine("Finished scan at " + DateTime.Now);

                // End Critical Region
                Thread.EndCriticalRegion();

                // Reset taskbar progress bar
                Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.None));

                if (!_cancellationTokenSource.IsCancellationRequested)
                    _scanBase.MoveNext();

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void StartScanner(ScannerBase scanner)
        {
            // Start task
            ScannerBase.CancellationToken = new CancellationTokenSource();

            var childScanTask = new Task(scanner.Scan, ScannerBase.CancellationToken.Token);
            childScanTask.RunSynchronously();

            ScannerBase.CancellationToken.Dispose();
            ScannerBase.CancellationToken = null;
        }

        /// <summary>
        ///     Sets the current section and increments the progress bar
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
                CurrentListViewItem.Errors = $"{Wizard.BadRegKeyArray.Problems(CurrentListViewItem.Section)} Errors";

                CurrentListViewItem.Status = "Finished";
                CurrentListViewItem.UnloadGif();
            }

            ProgressBar.Value++;
            _currentListViewIndex++;

            Wizard.CurrentScannerName = sectionName;
            CurrentSection.Content = "Section: " + sectionName;

            CurrentListViewItem.Status = "Scanning";
            CurrentListViewItem.LoadGif();

            CurrentListViewItem.Errors = "0 Errors";

            ListView.Items.Refresh();
        }

        private async void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(Application.Current.MainWindow, "Would you like to cancel the scan that's in progress?",
                    Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AbortScanThread();

                await _mainTaskScan;

                _scanBase.MoveFirst();
            }
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(ProgressBar.Maximum) > 0)
                Main.TaskbarProgressValue = e.NewValue / ProgressBar.Maximum;
        }
    }
}
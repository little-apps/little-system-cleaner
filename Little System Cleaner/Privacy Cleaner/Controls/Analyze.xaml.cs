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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Shell;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using Little_System_Cleaner.Properties;
using Timer = System.Timers.Timer;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Analyze : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
        
        readonly Wizard _scanBase;
        readonly Timer _timerUpdate = new Timer(200);
        int _currentListViewParentIndex = -1;
        int _currentListViewIndex = -1;
        Thread _threadScan;

        public ScannerBase CurrentListViewItem => SectionsCollection[_currentListViewParentIndex].Children[_currentListViewIndex];

        private int CurrentSectionProblems
        {
            get
            {
                string currentSection = (CurrentListViewItem.Parent != null ? CurrentListViewItem.Parent.Section : CurrentListViewItem.Section);

                return Wizard.ResultArray.Where(n => currentSection == n.Section).Select(n => n.Children.Count).FirstOrDefault();
            }
        }

        public ObservableCollection<ScannerBase> SectionsCollection => _scanBase.Model.RootChildren;

        public Analyze(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;

            listView.ItemsSource = SectionsCollection;

            // Increase total number of scans
            Settings.Default.totalScans++;

            // Zero last scan errors found + fixed and elapsed
            Settings.Default.lastScanErrors = 0;
            Settings.Default.lastScanErrorsFixed = 0;
            Settings.Default.lastScanElapsed = 0;

            // Set last scan date
            Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            // Start timer
            _timerUpdate.Elapsed += timerUpdate_Elapsed;
            _timerUpdate.Start();

            // Set the progress bar
            SetProgressBar();

            Wizard.ScanThread = new Thread(StartScanning);
            Wizard.ScanThread.Start();
        }

        private void SetProgressBar()
        {
            int max = SectionsCollection.Where(n => n.IsChecked.GetValueOrDefault()).SelectMany(n => n.Children).Count(child => child.IsChecked.GetValueOrDefault());

            // Set task bar progress bar
            Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
            Main.TaskbarProgressValue = 0;

            // Set progress bar
            progressBar.Value = 0;
            progressBar.Minimum = 0;
            progressBar.Maximum = max;
        }

        private void StartScanning()
        {
            bool scanAborted = false;
            int currentParent = -1;

            DateTime dtStart = DateTime.Now;

            try
            {
                // Begin critical region
                Thread.BeginCriticalRegion();

                foreach (ScannerBase n in SectionsCollection)
                {
                    currentParent++;
                    _currentListViewIndex = -1;

                    if (n.IsChecked.GetValueOrDefault() == false)
                        continue;

                    Wizard.CurrentScanner = n;

                    if (n.Children.Count > 0) // Should always have children, but just in case
                    {
                        foreach (ScannerBase child in n.Children)
                        {
                            InvokeCurrentSection(child.Section, currentParent);

                            StartScanner(n, child);
                        }

                        if (n.Results.Children.Count > 0)
                        {
                            Wizard.ResultArray.Add(n.Results);
                            Settings.Default.lastScanErrors += n.Results.Children.Count;
                        }
                    }
                    else
                    {
                        InvokeCurrentSection(n.Section, currentParent);

                        StartScanner(n);

                        if (n.Results.Children.Count > 0)
                        {
                            Wizard.ResultArray.Add(n.Results);
                            Settings.Default.lastScanErrors += n.Results.Children.Count;
                        }
                            
                    }

                    // Update info before going to next section (or exiting) 
                    Dispatcher.Invoke(new Action(() => {
                        n.Errors = $"{CurrentSectionProblems} Errors";
                        n.Status = "Finished";
                        n.UnloadGif();
                    }));
                }

                scanAborted = true;
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException || ex is ThreadInterruptedException)
                {
                    scanAborted = false;

                    if (_threadScan.IsAlive)
                        _threadScan.Abort();

                    if (ex is ThreadAbortException)
                        Thread.ResetAbort();
                }
                else
                    throw;
            }
            finally
            {
                // End critical region
                Thread.EndCriticalRegion();

                Main.Watcher.EventPeriod("Privacy Cleaner", "Analyze", (int)DateTime.Now.Subtract(dtStart).TotalSeconds, true);

                Settings.Default.lastScanElapsed = DateTime.Now.Subtract(dtStart).Ticks;

                Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = TaskbarItemProgressState.None));

                if (scanAborted)
                    _scanBase.MoveNext();
            }
        }

        /// <summary>
        /// Sets the current section and increments the progress bar
        /// </summary>
        private void InvokeCurrentSection(string sectionName, int parentSection)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new Action<string, int>(InvokeCurrentSection), sectionName, parentSection);
                return;
            }

            if (_currentListViewParentIndex != parentSection)
                _currentListViewParentIndex = parentSection;

            progressBar.Value++;
            _currentListViewIndex++;

            Wizard.CurrentSectionName = sectionName;
            currentSectionLabel.Content = "Section: " + sectionName;

            CurrentListViewItem.Status = "Scanning " + sectionName;
            CurrentListViewItem.LoadGif();

            Utils.AutoResizeColumns(listView);

            listView.Items.Refresh();
        }

        private void StartScanner(ScannerBase parent, ScannerBase child)
        {
            if (!string.IsNullOrEmpty(parent.ProcessName))
            {
                if (parent.Skipped)
                    return;

                bool? ret = RunningMsg.DisplayRunningMsg(parent.Name, parent.ProcessName);

                if (ret.GetValueOrDefault() == false)
                {
                    // Skip plugin
                    if (Dispatcher.Thread != Thread.CurrentThread)
                    {
                        Dispatcher.BeginInvoke(new Action(() => progressBar.Value++));
                    }
                    else
                    {
                        progressBar.Value++;
                    }

                    parent.Skipped = true;
                    
                    return;
                }
            }

            _threadScan = new Thread(() => TryScanMethod(() => parent.Scan(child)));

            try
            {
                _threadScan.Start();
                _threadScan.Join();
            }
            catch (ThreadInterruptedException)
            {

            }


        }

        private void StartScanner(ScannerBase parent)
        {
            if (!string.IsNullOrEmpty(parent.ProcessName))
            {
                if (parent.Skipped)
                    return;

                bool? ret = RunningMsg.DisplayRunningMsg(parent.Name, parent.ProcessName);

                if (ret.GetValueOrDefault() == false)
                {
                    // Skip plugin
                    if (Dispatcher.Thread != Thread.CurrentThread)
                    {
                        Dispatcher.BeginInvoke(new Action(() => progressBar.Value++));
                    }
                    else
                    {
                        progressBar.Value++;
                    }

                    parent.Skipped = true;

                    return;
                }
            }

            _threadScan = new Thread(() => TryScanMethod(parent.Scan));

            try
            {
                _threadScan.Start();
                _threadScan.Join();
            }
            catch (ThreadInterruptedException)
            {

            }
        }

        private void TryScanMethod(Action action)
        {
            try
            {
                action();
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            
        }

        void timerUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new ElapsedEventHandler(timerUpdate_Elapsed), sender, e);
                return;
            }


            if (_currentListViewIndex != -1)
            {
                CurrentListViewItem.Errors = $"{CurrentSectionProblems} Errors";
                listView.Items.Refresh();
            }
        }

        public void AbortScanThread()
        {
            if (Wizard.ScanThread.IsAlive)
            {
                Wizard.ScanThread.Interrupt();
                Wizard.ScanThread.Abort();
            }

            if (_threadScan.IsAlive)
                _threadScan.Abort();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Would you like to cancel the scan that's in progress?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AbortScanThread();
                _scanBase.MoveFirst();
            }
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (progressBar.Maximum != 0)
            {
                Main.TaskbarProgressValue = (e.NewValue / progressBar.Maximum);
            }
        }
    }
}

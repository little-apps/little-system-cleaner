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
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Analyze : UserControl
    {
        static List<ScannerBase> enabledScanners = new List<ScannerBase>();
        Wizard scanBase;
        System.Timers.Timer timerUpdate = new System.Timers.Timer(200);
        int currentListViewParentIndex = -1;
        int currentListViewIndex = -1;
        Thread threadScan;

        public ScannerBase CurrentListViewItem
        {
            get { return this.SectionsCollection[currentListViewParentIndex].Children[currentListViewIndex] as ScannerBase; }
        }

        public ObservableCollection<ScannerBase> SectionsCollection
        {
            get 
            {
                if (this.scanBase != null)
                    return this.scanBase.Model.RootChildren;

                return new ObservableCollection<ScannerBase>();
            }
        }

        public Analyze(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            // Set last scan date
            Properties.Settings.Default.lastScanDate = DateTime.Now.ToBinary();

            // Start timer
            this.timerUpdate.Elapsed += new System.Timers.ElapsedEventHandler(timerUpdate_Elapsed);
            this.timerUpdate.Start();

            // Set the progress bar
            this.SetProgressBar();

            Wizard.ScanThread = new Thread(new ThreadStart(StartScanning));
            Wizard.ScanThread.Start();
        }

        private void SetProgressBar()
        {
            int max = 0;

            foreach (ScannerBase n in this.SectionsCollection)
            {
                if (n.IsChecked.GetValueOrDefault() == false)
                    continue;

                foreach (ScannerBase child in n.Children)
                {
                    if (child.IsChecked.GetValueOrDefault() != false)
                        max++;
                }
            }

            this.progressBar.Value = 0;
            this.progressBar.Minimum = 0;
            this.progressBar.Maximum = max;
        }

        private void StartScanning()
        {
            bool scanAborted = false;
            int currentParent = -1;

            // Begin critical region
            Thread.BeginCriticalRegion();

            try
            {
                foreach (ScannerBase n in this.SectionsCollection)
                {
                    currentParent++;
                    currentListViewIndex = -1;

                    if (n.IsChecked.GetValueOrDefault() == false)
                        continue;

                    Wizard.CurrentScanner = n;

                    if (n.Children.Count > 0) // Should always have children, but just in case
                    {
                        foreach (ScannerBase child in n.Children)
                        {
                            InvokeCurrentSection(child.Section, currentParent);

                            this.StartScanner(n, child);
                        }

                        if (n.Results.Children.Count > 0)
                            Wizard.ResultArray.Add(n.Results);
                    }
                    else
                    {
                        InvokeCurrentSection(n.Section, currentParent);

                        this.StartScanner(n);

                        if (n.Results.Children.Count > 0)
                            Wizard.ResultArray.Add(n.Results);
                    }
                }

                scanAborted = true;
            }
            catch (ThreadAbortException)
            {
                scanAborted = false;

                if (this.threadScan.IsAlive)
                    this.threadScan.Abort();
            }
            finally
            {

            }

            // End critical region
            Thread.EndCriticalRegion();

            if (scanAborted)
                this.scanBase.MoveNext();
        }

        /// <summary>
        /// Sets the current section and increments the progress bar
        /// </summary>
        private void InvokeCurrentSection(string sectionName, int parentSection)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action<string, int>(InvokeCurrentSection), new object[] { sectionName, parentSection });
                return;
            }

            if (this.currentListViewIndex != -1)
            {
                this.CurrentListViewItem.Status = "Finished";
                this.CurrentListViewItem.UnloadGif();
            }

            if (currentListViewParentIndex != parentSection)
                currentListViewParentIndex = parentSection;

            this.progressBar.Value++;
            this.currentListViewIndex++;

            Wizard.CurrentSectionName = sectionName;
            this.currentSection.Text = "Section: " + sectionName;

            this.CurrentListViewItem.Status = "Scanning";
            this.CurrentListViewItem.LoadGif();

            this.listView.Items.Refresh();
        }

        private void StartScanner(ScannerBase parent, ScannerBase child)
        {
            if (!string.IsNullOrEmpty(parent.ProcessName))
            {
                bool? ret = RunningMsg.DisplayRunningMsg(parent.Name, parent.ProcessName);

                if (ret.GetValueOrDefault() == false)
                {
                    // Skip plugin
                    if (this.Dispatcher.Thread != Thread.CurrentThread)
                    {
                        this.Dispatcher.Invoke(new Action(() => this.progressBar.Value++));
                    }
                    else
                    {
                        this.progressBar.Value++;
                    }
                    
                    return;
                }
            }

            threadScan = new Thread(() => parent.Scan(child));
            threadScan.Start();
            threadScan.Join();
        }

        private void StartScanner(ScannerBase parent)
        {
            if (!string.IsNullOrEmpty(parent.ProcessName))
            {
                bool? ret = RunningMsg.DisplayRunningMsg(parent.Name, parent.ProcessName);

                if (ret.GetValueOrDefault() == false)
                {
                    // Skip plugin
                    if (this.Dispatcher.Thread != Thread.CurrentThread)
                    {
                        this.Dispatcher.Invoke(new Action(() => this.progressBar.Value++));
                    }
                    else
                    {
                        this.progressBar.Value++;
                    }

                    return;
                }
            }

            threadScan = new Thread(parent.Scan);
            threadScan.Start();
            threadScan.Join();
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
                this.CurrentListViewItem.Errors = string.Format("{0} Errors", Wizard.ResultArray.Problems(this.CurrentListViewItem.Section));
                this.listView.Items.Refresh();
            }
        }

        public void AbortScanThread()
        {
            if (Wizard.ScanThread.IsAlive)
                Wizard.ScanThread.Abort();

            if (this.threadScan.IsAlive)
                this.threadScan.Abort();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Would you like to cancel the scan thats in progress?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.AbortScanThread();
                this.scanBase.MoveFirst();
            }
        }
    }
}

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
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public partial class Analyze : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        static List<ScannerBase> enabledScanners = new List<ScannerBase>();
        Wizard scanBase;
        System.Timers.Timer timerUpdate = new System.Timers.Timer(200);
        int currentListViewParentIndex = -1;
        int currentListViewIndex = -1;
        Thread threadScan;
        private ObservableCollection<ScannerBase> _sectionsCollection;

        public ScannerBase CurrentListViewItem
        {
            get { return this.SectionsCollection[currentListViewParentIndex].Children[currentListViewIndex] as ScannerBase; }
        }

        private int CurrentSectionProblems
        {
            get
            {
                string currentSection = (this.CurrentListViewItem.Parent != null ? this.CurrentListViewItem.Parent.Section : this.CurrentListViewItem.Section);

                foreach (ResultNode n in Wizard.ResultArray)
                {
                    if (currentSection == n.Section)
                        return n.Children.Count;
                }

                return 0;
            }
        }

        public ObservableCollection<ScannerBase> SectionsCollection
        {
            get { return this.scanBase.Model.RootChildren; }
            //get { return this._sectionsCollection; }
            //set
            //{
            //    this._sectionsCollection = value;

            //    this.OnPropertyChanged("SectionsCollection");
            //}
        }

        public Analyze(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            this.listView.ItemsSource = this.SectionsCollection;

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

            // Set task bar progress bar
            Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            Main.TaskbarProgressValue = 0;

            // Set progress bar
            this.progressBar.Value = 0;
            this.progressBar.Minimum = 0;
            this.progressBar.Maximum = max;
        }

        private void StartScanning()
        {
            bool scanAborted = false;
            int currentParent = -1;

            try
            {
                // Begin critical region
                Thread.BeginCriticalRegion();

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

                    // Update info before going to next section (or exiting) 
                    this.Dispatcher.Invoke(new Action(() => {
                        n.Errors = string.Format("{0} Errors", this.CurrentSectionProblems);
                        n.Status = "Finished";
                        n.UnloadGif();
                    }));
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
                // End critical region
                Thread.EndCriticalRegion();

                this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None));

                if (scanAborted)
                    this.scanBase.MoveNext();
            }
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

            if (currentListViewParentIndex != parentSection)
                currentListViewParentIndex = parentSection;

            this.progressBar.Value++;
            this.currentListViewIndex++;

            Wizard.CurrentSectionName = sectionName;
            this.currentSection.Content = "Section: " + sectionName;

            this.CurrentListViewItem.Status = "Scanning " + sectionName;
            this.CurrentListViewItem.LoadGif();

            Utils.AutoResizeColumns(this.listView);

            this.listView.Items.Refresh();
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
                    if (this.Dispatcher.Thread != Thread.CurrentThread)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() => this.progressBar.Value++));
                    }
                    else
                    {
                        this.progressBar.Value++;
                    }

                    parent.Skipped = true;
                    
                    return;
                }
            }

            threadScan = new Thread(() => parent.Scan(child));

            try
            {
                threadScan.Start();
                threadScan.Join();
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
                    if (this.Dispatcher.Thread != Thread.CurrentThread)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() => this.progressBar.Value++));
                    }
                    else
                    {
                        this.progressBar.Value++;
                    }

                    parent.Skipped = true;

                    return;
                }
            }

            threadScan = new Thread(parent.Scan);

            try
            {
                threadScan.Start();
                threadScan.Join();
            }
            catch (ThreadInterruptedException)
            {

            }
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
                this.CurrentListViewItem.Errors = string.Format("{0} Errors", this.CurrentSectionProblems);
                this.listView.Items.Refresh();
            }
        }

        public void AbortScanThread()
        {
            if (Wizard.ScanThread.IsAlive)
            {
                Wizard.ScanThread.Interrupt();
                Wizard.ScanThread.Abort();
            }

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

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.progressBar.Maximum != 0)
            {
                Main.TaskbarProgressValue = (e.NewValue / this.progressBar.Maximum);
            }
        }
    }
}

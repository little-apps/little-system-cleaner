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
using System.Collections.ObjectModel;
using CommonTools.TreeListView.Tree;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using System.ComponentModel;
using Little_System_Cleaner.Misc;
using System.Threading;
using Little_System_Cleaner.Registry_Cleaner.Helpers.Backup;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
    public partial class Results : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private Wizard scanWiz;
        private Thread _fixThread;
        private int _progressBarValue = 0;
        private string _progressBarText;

        public int ProgressBarValue
        {
            get { return this._progressBarValue; }
            set
            {
                if (Thread.CurrentThread != this.Dispatcher.Thread)
                {
                    this.Dispatcher.BeginInvoke(new Action(() => this.ProgressBarValue = value));
                    return;
                }

                int val = value;

                this._progressBarValue = val;

                if (this.progressBar1.Maximum != 0)
                    Main.TaskbarProgressValue = (val / this.progressBar1.Maximum);

                this.OnPropertyChanged("ProgressBarValue");
            }
        }

        public string ProgressBarText
        {
            get { return this._progressBarText; }
            set
            {
                if (Thread.CurrentThread != this.Dispatcher.Thread)
                {
                    this.Dispatcher.BeginInvoke(new Action(() => this.ProgressBarText = value));
                    return;
                }

                this._progressBarText = value;
                this.OnPropertyChanged("ProgressBarText");
            }
        }

        public Thread FixThread
        {
            get { return this._fixThread; }
            private set { this._fixThread = value; }
        }

		public Results(Wizard scanBase)
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
            this.scanWiz = scanBase;

            this._tree.Model = ResultModel.CreateResultModel();
            this._tree.ExpandAll();


            if ((this._tree.Model as ResultModel).Root.Children.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "There were no errors found!", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Wizard.Report.DisplayLogFile((Properties.Settings.Default.registryCleanerOptionsShowLog && !Properties.Settings.Default.registryCleanerOptionsAutoRepair));

                // Set last scan errors found
                Properties.Settings.Default.lastScanErrors = 0;

                foreach (BadRegistryKey badRegKeyRoot in (this._tree.Model as ResultModel).Root.Children)
                {
                    foreach (BadRegistryKey child in badRegKeyRoot.Children)
                    {
                        Properties.Settings.Default.lastScanErrors++;
                    }
                }

                // Set total errors found
                Properties.Settings.Default.totalErrorsFound += Properties.Settings.Default.lastScanErrors;

                if (Properties.Settings.Default.registryCleanerOptionsAutoRepair)
                    this.FixProblems();
            }
		}

        private List<BadRegistryKey> GetSelectedRegKeys()
        {
            List<BadRegistryKey> badRegKeyArr = new List<BadRegistryKey>();

            foreach (BadRegistryKey badRegKeyRoot in (this._tree.Model as ResultModel).Root.Children)
            {
                foreach (BadRegistryKey child in badRegKeyRoot.Children)
                {
                    if ((child.IsChecked.HasValue) && child.IsChecked.Value)
                    {
                        badRegKeyArr.Add(child);
                    }
                }
            }

            return badRegKeyArr;
        }

        private void SetCheckedItems(Nullable<bool> isChecked)
        {
            foreach (BadRegistryKey badRegKeyRoot in (this._tree.Model as ResultModel).Root.Children)
            {
                foreach (BadRegistryKey child in badRegKeyRoot.Children)
                {
                    if (!isChecked.HasValue)
                        child.IsChecked = !child.IsChecked;
                    else
                        child.IsChecked = isChecked.Value;
                }
            }

            return;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.scanWiz.MoveFirst();
        }

        private void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            int selectedCount = this.GetSelectedRegKeys().Count;

            if (selectedCount == 0)
            {
                MessageBox.Show(App.Current.MainWindow, "You must select registry keys to be removed first.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ask to remove registry keys
            if (!Properties.Settings.Default.registryCleanerOptionsAutoRepair)
                if (MessageBox.Show("Would you like to fix all selected problems?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

            // Report
            Main.Watcher.Event("Registry Cleaner", "Fix Problems");

            // Disable buttons
            this.buttonCancel.IsEnabled = false;
            this.buttonFix.IsEnabled = false;

            // Set taskbar progress bar
            Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            Main.TaskbarProgressValue = 0;

            // Set progress bar range
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = selectedCount;
            this.progressBar1.Value = 0;

            this.FixThread = new Thread(new ThreadStart(this.FixProblems));
            this.FixThread.Start();
        }

        private void FixProblems()
        {
            bool cancelled = false;

            try
            {
                List<BadRegistryKey> badRegKeys = GetSelectedRegKeys();

                long lSeqNum = 0;
                BackupRegistry backupReg;

                if (SysRestore.SysRestoreAvailable())
                {
                    // Create Restore Point
                    this.ProgressBarText = "Creating system restore point";

                    try
                    {
                        SysRestore.StartRestore("Before Little Registry Cleaner Registry Fix", out lSeqNum);
                    }
                    catch (Win32Exception ex)
                    {
                        string message = string.Format("Unable to create system restore point.\nThe following error occurred: {0}", ex.Message);
                        this.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error)));
                    }
                }

                // Generate filename to backup registry
                this.ProgressBarText = "Creating backup file";
                string strBackupFile = string.Format("{0}\\{1:yyyy}_{1:MM}_{1:dd}_{1:HH}{1:mm}{1:ss}.bakx", Properties.Settings.Default.optionsBackupDir, DateTime.Now);

                try
                {
                    backupReg = new BackupRegistry(strBackupFile);
                    backupReg.Open(false);
                }
                catch (Exception ex)
                {
                    string message = string.Format("Unable to create backup file ({0}).\nError: {1}", strBackupFile, ex.Message);
                    this.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error)));
                    return;
                }

                Properties.Settings.Default.lastScanErrorsFixed = 0;

                foreach (BadRegistryKey brk in badRegKeys)
                {
                    bool skip = false;

                    // Backup key
                    if (!backupReg.Store(brk))
                    {
                        string message = string.Format("An error occurred trying to backup registry key ({0}).\nWould you like to remove it (not recommended)?", brk.RegKeyPath);

                        MessageBoxResult msgBoxResult = (MessageBoxResult)this.Dispatcher.Invoke(new Func<MessageBoxResult>(() => {
                            return MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Exclamation); 
                        }));

                        if (msgBoxResult != MessageBoxResult.Yes)
                            skip = true;
                    }

                    if (!skip)
                    {
                        // Delete key/value
                        if (!brk.Delete())
                        {
                            string message = string.Format("An error occurred trying to remove registry key {0}", brk.RegKeyPath);
                            this.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error)));
                        }
                        else
                        {
                            // Set last scan erors fixed
                            Properties.Settings.Default.lastScanErrorsFixed++;
                        }
                    }
                    
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Set icon to check mark
                        brk.bMapImg = new Image();
                        brk.bMapImg.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.finished_scanning.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                        this._tree.Items.Refresh();

                        // Increase & Update progress bar
                        this.ProgressBarText = string.Format("Items Repaired: {0}/{1}", ++this.progressBar1.Value, this.progressBar1.Maximum);
                    }));
                }

                // Store data as file
                backupReg.Serialize();

                // Set total errors fixed
                Properties.Settings.Default.totalErrorsFixed += Properties.Settings.Default.lastScanErrorsFixed;

                if (SysRestore.SysRestoreAvailable())
                {
                    // Finish creating restore point
                    if (lSeqNum != 0)
                    {
                        try
                        {
                            SysRestore.EndRestore(lSeqNum);
                        }
                        catch (Win32Exception ex)
                        {
                            string message = string.Format("Unable to create system restore point.\nThe following error occurred: {0}", ex.Message);
                            this.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error)));
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                cancelled = true;
            }
            finally
            {
                if (cancelled)
                {
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        // Enable buttons
                        this.buttonCancel.IsEnabled = true;
                        this.buttonFix.IsEnabled = true;

                        // Reset taskbar progress bar
                        Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                        Main.TaskbarProgressValue = 0;

                        // Reset progress bar
                        this.ProgressBarValue = 0;
                        this.ProgressBarText = "";
                    }));
                }
                else
                {
                    this.Dispatcher.BeginInvoke(new Action(() => Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None));

                    // If power user option selected -> automatically exit program
                    if (Properties.Settings.Default.registryCleanerOptionsAutoExit)
                    {
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        // Display message box and go back to first control
                        this.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Removed problems from registry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information)));

                        if (Properties.Settings.Default.registryCleanerOptionsAutoRescan)
                            this.scanWiz.Rescan();
                        else
                            this.scanWiz.MoveFirst();
                    }
                }
            }
            
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // If no errors found -> Go to first control
            if ((this._tree.Model as ResultModel).Root.Children.Count == 0)
                this.scanWiz.MoveFirst();

            this._tree.AutoResizeColumns();
        }

        private void contextMenuResults_Clicked(object sender, RoutedEventArgs e)
        {
            switch ((string)(sender as MenuItem).Header)
            {
                case "Select All":
                    {
                        SetCheckedItems(true);
                        break;
                    }
                case "Select None":
                    {
                        SetCheckedItems(false);
                        break;
                    }
                case "Invert Selection":
                    {
                        SetCheckedItems(null);
                        break;
                    }
                case "Exclude Selection":
                    {
                        if (this._tree.SelectedNode == null)
                        {
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, "No registry key is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        string regKeyPath = (this._tree.SelectedNode.Tag as BadRegistryKey).RegKeyPath;

                        ExcludeItem excludeItem = new ExcludeItem() { RegistryPath = regKeyPath };
                        if (!Properties.Settings.Default.arrayExcludeList.Contains(excludeItem))
                        {
                            Properties.Settings.Default.arrayExcludeList.Add(excludeItem);
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("Added registry key ({0}) to the exclude list", regKeyPath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                            this._tree.RemoveNode(this._tree.SelectedNode);
                        }
                        else
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("Registry key ({0}) already exists", regKeyPath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }
                case "View In RegEdit":
                    {
                        if (this._tree.SelectedNode == null)
                        {
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, "No registry key is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        try
                        {
                            string regKeyPath = (this._tree.SelectedNode.Tag as BadRegistryKey).RegKeyPath;
                            string regKeyValueName = (this._tree.SelectedNode.Tag as BadRegistryKey).ValueName;

                            RegEditGo.GoTo(regKeyPath, regKeyValueName);
                        }
                        catch (Exception ex)
                        {
                            string message = string.Format("Unable to open registry key in RegEdit.\nThe following error occurred: {0}", ex.Message);

                            MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        
                        break;
                    }
            }
        }
        
	}
}
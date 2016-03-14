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
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Little_System_Cleaner.Registry_Cleaner.Helpers.Backup;
using Little_System_Cleaner.Registry_Cleaner.Helpers.BadRegistryKeys;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
    public partial class Results : INotifyPropertyChanged
    {
        private readonly Wizard _scanWiz;
        private BitmapSource _bitmapSrcFinishedScanning;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private string _progressBarText;
        private int _progressBarValue;
        private Task<bool> _taskClean;

        public Results(Wizard scanBase)
        {
            InitializeComponent();

            // Insert code required on object creation below this point.
            _scanWiz = scanBase;

            Tree.Model = ResultModel.CreateResultModel();
            Tree.ExpandAll();

            var resultModel = Tree.Model as ResultModel;
            if (resultModel != null && resultModel.Root.Children.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "There were no errors found!", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Wizard.Report.DisplayLogFile(Settings.Default.registryCleanerOptionsShowLog &&
                                             !Settings.Default.registryCleanerOptionsAutoRepair);

                // Set last scan errors found
                var model = Tree.Model as ResultModel;
                if (model != null)
                    Settings.Default.lastScanErrors =
                        model.Root.Children
                            .SelectMany(badRegKeyRoot => badRegKeyRoot.Children)
                            .Count();

                // Set total errors found
                Settings.Default.totalErrorsFound += Settings.Default.lastScanErrors;

                if (Settings.Default.registryCleanerOptionsAutoRepair)
                    FixProblems();
            }
        }

        public int ProgressBarValue
        {
            get { return _progressBarValue; }
            set
            {
                if (Thread.CurrentThread != Dispatcher.Thread)
                {
                    Dispatcher.BeginInvoke(new Action(() => ProgressBarValue = value));
                    return;
                }

                var val = value;

                _progressBarValue = val;

                if (Math.Abs(ProgressBar.Maximum) > 0)
                    Main.TaskbarProgressValue = val/ProgressBar.Maximum;

                OnPropertyChanged(nameof(ProgressBarValue));
            }
        }

        public string ProgressBarText
        {
            get { return _progressBarText; }
            set
            {
                if (Thread.CurrentThread != Dispatcher.Thread)
                {
                    Dispatcher.BeginInvoke(new Action(() => ProgressBarText = value));
                    return;
                }

                _progressBarText = value;
                OnPropertyChanged(nameof(ProgressBarText));
            }
        }

        public bool FixProblemsRunning => (_taskClean != null) && _taskClean.Status == TaskStatus.Running;

        /// <summary>
        ///     The finished scanning bitmap is converted once to a BitmapSource and stored so it can be used again in order to
        ///     save resources
        /// </summary>
        public BitmapSource BitmapSrcFinishedScanning => _bitmapSrcFinishedScanning ??
                                                         (_bitmapSrcFinishedScanning =
                                                             Imaging.CreateBitmapSourceFromHBitmap(
                                                                 Properties.Resources.finished_scanning.GetHbitmap(),
                                                                 IntPtr.Zero, Int32Rect.Empty,
                                                                 BitmapSizeOptions.FromEmptyOptions()));

        private List<BadRegistryKey> GetSelectedRegKeys()
        {
            return (Tree.Model as ResultModel)?.Root.Children
                .SelectMany(badRegKeyRoot => badRegKeyRoot.Children)
                .Where(child => child.IsChecked.HasValue && child.IsChecked.Value)
                .ToList();
        }

        private void SetCheckedItems(bool? isChecked)
        {
            var badRegistryKeys =
                (Tree.Model as ResultModel)?.Root.Children.SelectMany(badRegKeyRoot => badRegKeyRoot.Children);

            if (badRegistryKeys == null)
                return;

            foreach (var child in badRegistryKeys)
            {
                if (!isChecked.HasValue)
                    child.IsChecked = !child.IsChecked;
                else
                    child.IsChecked = isChecked.Value;
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            _scanWiz.MoveFirst();
        }

        private async void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            var selectedCount = GetSelectedRegKeys().Count;

            if (selectedCount == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "You must select registry keys to be removed first.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ask to remove registry keys
            if (!Settings.Default.registryCleanerOptionsAutoRepair)
                if (
                    MessageBox.Show("Would you like to fix all selected problems?", Utils.ProductName,
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

            // Report
            Main.Watcher.Event("Registry Cleaner", "Fix Problems");

            // Disable buttons
            ButtonCancel.IsEnabled = false;
            ButtonFix.IsEnabled = false;

            // Set taskbar progress bar
            Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
            Main.TaskbarProgressValue = 0;

            // Set progress bar range
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = selectedCount;
            ProgressBarValue = 0;

            _taskClean = new Task<bool>(FixProblems, _cancellationTokenSource.Token);
            _taskClean.Start();
            if (await _taskClean)
            {
                Main.TaskbarProgressState = TaskbarItemProgressState.None;

                // If power user option selected -> automatically exit program
                if (Settings.Default.registryCleanerOptionsAutoExit)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    // Display message box and go back to first control
                    MessageBox.Show(Application.Current.MainWindow, "Removed problems from registry", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (Settings.Default.registryCleanerOptionsAutoRescan)
                        _scanWiz.Rescan();
                    else
                        _scanWiz.MoveFirst();
                }
            }
            else
            {
                // Enable buttons
                ButtonCancel.IsEnabled = true;
                ButtonFix.IsEnabled = true;

                // Reset taskbar progress bar
                Main.TaskbarProgressState = TaskbarItemProgressState.None;
                Main.TaskbarProgressValue = 0;

                // Reset progress bar
                ProgressBarValue = 0;
                ProgressBarText = "";
            }
        }

        /// <summary>
        ///     Fixes registry problems
        /// </summary>
        /// <returns>Returns true if fix was successful</returns>
        private bool FixProblems()
        {
            var cancelled = false;

            try
            {
                var badRegKeys = GetSelectedRegKeys();

                long lSeqNum = 0;
                BackupRegistry backupReg;

                if (SysRestore.SysRestoreAvailable())
                {
                    // Create Restore Point
                    ProgressBarText = "Creating system restore point";

                    try
                    {
                        SysRestore.StartRestore("Before Little System Cleaner (Registry Cleaner) Fix", out lSeqNum);
                    }
                    catch (Win32Exception ex)
                    {
                        string message =
                            $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                        Utils.MessageBoxThreadSafe(message, Utils.ProductName, MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }

                // Generate filename to backup registry
                ProgressBarText = "Creating backup file";
                var backupFile = string.Format("{0}\\{1:yyyy}_{1:MM}_{1:dd}_{1:HH}{1:mm}{1:ss}.bakx",
                    Settings.Default.OptionsBackupDir, DateTime.Now);

                try
                {
                    backupReg = new BackupRegistry(backupFile);
                    backupReg.Open(false);
                }
                catch (Exception ex)
                {
                    string message = $"Unable to create backup file ({backupFile}).\nError: {ex.Message}";
                    Utils.MessageBoxThreadSafe(message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                Settings.Default.lastScanErrorsFixed = 0;

                foreach (var brk in badRegKeys)
                {
                    var skip = false;

                    if (_cancellationTokenSource.IsCancellationRequested)
                        break;

                    // Backup key
                    if (!backupReg.Store(brk))
                    {
                        if (Settings.Default.registryCleanerOptionsShowErrors)
                        {
                            string message =
                                $"An error occurred trying to backup registry key ({brk.RegKeyPath}).\nWould you like to remove it (not recommended)?";

                            if (
                                Utils.MessageBoxThreadSafe(message, Utils.ProductName, MessageBoxButton.YesNo,
                                    MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
                                skip = true;
                        }
                        else
                        {
                            skip = !Settings.Default.registryCleanerOptionsDeleteOnBackupError;
                        }
                    }

                    if (!skip)
                    {
                        // Delete key/value
                        if (!brk.Delete())
                        {
                            if (Settings.Default.registryCleanerOptionsShowErrors)
                            {
                                string message = $"An error occurred trying to remove registry key {brk.RegKeyPath}";
                                Utils.MessageBoxThreadSafe(message, Utils.ProductName, MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            // Set last scan erors fixed
                            Settings.Default.lastScanErrorsFixed++;
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        // Set icon to check mark
                        brk.BitmapImg = new Image {Source = BitmapSrcFinishedScanning};

                        Tree.Items.Refresh();

                        // Increase & Update progress bar
                        ProgressBarValue++;
                        ProgressBarText = $"Items Repaired: {ProgressBarValue}/{ProgressBar.Maximum}";
                    });
                }

                if (!_cancellationTokenSource.IsCancellationRequested)
                    // Store data as file
                    backupReg.Serialize();

                // Set total errors fixed
                Settings.Default.totalErrorsFixed += Settings.Default.lastScanErrorsFixed;

                if (SysRestore.SysRestoreAvailable())
                {
                    // Finish creating restore point
                    if (lSeqNum != 0)
                    {
                        try
                        {
                            if (!_cancellationTokenSource.IsCancellationRequested)
                                SysRestore.EndRestore(lSeqNum);
                            else
                                SysRestore.CancelRestore(lSeqNum);
                        }
                        catch (Win32Exception ex)
                        {
                            string message =
                                $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                            Utils.MessageBoxThreadSafe(message, Utils.ProductName, MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }

                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            return !cancelled;
        }

        public void CancelFixIfRunning()
        {
            if (FixProblemsRunning)
                _cancellationTokenSource?.Cancel();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // If no errors found -> Go to first control
            var resultModel = Tree.Model as ResultModel;
            if (resultModel != null && resultModel.Root.Children.Count == 0)
                _scanWiz.MoveFirst();

            Tree.AutoResizeColumns();
        }

        private void contextMenuResults_Clicked(object sender, RoutedEventArgs e)
        {
            switch ((string) (sender as MenuItem)?.Header)
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
                    if (Tree.SelectedNode == null)
                    {
                        MessageBox.Show(Application.Current.MainWindow, "No registry key is selected", Utils.ProductName,
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var regKeyPath = (Tree.SelectedNode.Tag as BadRegistryKey)?.RegKeyPath;

                    var excludeItem = new ExcludeItem {RegistryPath = regKeyPath};
                    if (!Settings.Default.ArrayExcludeList.Contains(excludeItem))
                    {
                        Settings.Default.ArrayExcludeList.Add(excludeItem);
                        MessageBox.Show(Application.Current.MainWindow,
                            $"Added registry key ({regKeyPath}) to the exclude list", Utils.ProductName,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        Tree.RemoveNode(Tree.SelectedNode);
                    }
                    else
                        MessageBox.Show(Application.Current.MainWindow, $"Registry key ({regKeyPath}) already exists",
                            Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
                case "View In RegEdit":
                {
                    if (Tree.SelectedNode == null)
                    {
                        MessageBox.Show(Application.Current.MainWindow, "No registry key is selected", Utils.ProductName,
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        var regKeyPath = (Tree.SelectedNode.Tag as BadRegistryKey)?.RegKeyPath;
                        var regKeyValueName = (Tree.SelectedNode.Tag as BadRegistryKey)?.ValueName;

                        RegEditGo.GoTo(regKeyPath, regKeyValueName);
                    }
                    catch (Exception ex)
                    {
                        string message =
                            $"Unable to open registry key in RegEdit.\nThe following error occurred: {ex.Message}";

                        MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }

                    break;
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
    }
}
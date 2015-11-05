using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using Little_System_Cleaner.Duplicate_Finder.Helpers;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    /// Interaction logic for Results.xaml
    /// </summary>
    public partial class Results : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly Wizard _scanBase;
        private Task _taskScan;
        private string _progressBarText;

        public string ProgressBarText
        {
            get { return _progressBarText; }
            set
            {
                if (Dispatcher.Thread != Thread.CurrentThread)
                {
                    Dispatcher.Invoke(() => ProgressBarText = value);
                    return;
                }

                _progressBarText = value;
                OnPropertyChanged("ProgressBarText");
            }
        }

        public ResultModel DuplicateFiles => (Tree.Model as ResultModel);

        public Results(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;

            var loadingResults = new LoadingResults(_scanBase, Tree);

            var windowResult = loadingResults.ShowDialog();

            if (windowResult.GetValueOrDefault(false))
            {
                Utils.AutoResizeColumns(Tree);
            }
            else
            {
                Utils.MessageBoxThreadSafe("The results could not be prepared. Going back to start screen.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                _scanBase.MoveFirst();
            }

            //this._tree.Model = this.scanBase.Results;
            //this._tree.ExpandAll();

            //Utils.AutoResizeColumns(this._tree);
        }

        private async void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            //List<FileEntry> files = (from resParent in (this._tree.Model as ResultModel).Root.Children where resParent.Children.Count > 0 from resChild in resParent.Children where resChild.IsChecked.GetValueOrDefault() select resChild.FileEntry).ToList();

            var resultModel = Tree.Model as ResultModel;

            if (resultModel == null)
                return;

            var files = resultModel.Root.Children.Where(resParent => resParent.Children.Count > 0)
                .SelectMany(resParent => resParent.Children)
                .Where(resChild => resChild.IsChecked.GetValueOrDefault())
                .Select(resChild => resChild.FileEntry)
                .ToList();

            if (files.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "No files were selected to be removed. If you would like to not remove any files, please click cancel.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (
                MessageBox.Show(Application.Current.MainWindow,
                    "Are you sure you want to remove the selected files?\nYou may not be able to get them back once they're deleted.",
                    Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Main.Watcher.Event("Duplicate Finder", "Remove Duplicates");

            Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
            Main.TaskbarProgressValue = 0;

            ProgressBar.Value = 0;
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = (SysRestore.SysRestoreAvailable() ? files.Count + 2 : files.Count);

            _taskScan = new Task(() => FixDuplicates(files));
            _taskScan.Start();
            await _taskScan;

            MessageBox.Show(Application.Current.MainWindow, "Removed duplicate files from computer", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            Main.TaskbarProgressState = TaskbarItemProgressState.None;
            Main.TaskbarProgressValue = 0;

            _scanBase.MoveFirst();
        }

        private void FixDuplicates(IEnumerable<FileEntry> files)
        {
            long seqNum = 0;
            var sysRestoreAvailable = SysRestore.SysRestoreAvailable();

            if (sysRestoreAvailable)
            {
                ProgressBarText = "Creating System Restore Point";

                try
                {
                    SysRestore.StartRestore("Before Little System Cleaner (Duplicate Finder) Clean", out seqNum);
                }
                catch (Win32Exception ex)
                {
                    Utils.MessageBoxThreadSafe(Application.Current.MainWindow, "The following error occurred trying to create a system restore point: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Dispatcher.Invoke(() => ProgressBar.Value++);
            }

            foreach (var fileEntry in files)
            {
                var filePath = fileEntry.FilePath;

                var percent = Dispatcher.Invoke(() => ProgressBar.Value/ProgressBar.Maximum*100);
                ProgressBarText = Dispatcher.Invoke(() => $"{ProgressBar.Value}/{ProgressBar.Maximum} ({percent:0.##}%)");

                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    string message = $"Unable to remove file ({filePath}).\nThe following error occurred: {ex.Message}";
                    Utils.MessageBoxThreadSafe(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Dispatcher.Invoke(() => ProgressBar.Value++);
                Settings.Default.lastScanErrorsFixed++;
            }

            Settings.Default.totalErrorsFixed += Settings.Default.lastScanErrorsFixed;

            if (!sysRestoreAvailable)
                return;

            ProgressBarText = "Finalizing system restore point";

            if (seqNum != 0)
            {
                try
                {
                    SysRestore.EndRestore(seqNum);
                }
                catch (Win32Exception ex)
                {
                    Utils.MessageBoxThreadSafe(Application.Current.MainWindow, "Unable to create system restore point.\nThe following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            Dispatcher.Invoke(() => ProgressBar.Value++);
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_taskScan.Status == TaskStatus.Running)
            {
                MessageBox.Show(Application.Current.MainWindow, "Please wait for duplicate files to be fixed.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _scanBase.MoveFirst();
            }
        }

        private void _tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowDetails();
        }

        private void ShowDetails()
        {
            if (Tree.SelectedNode == null)
                return;

            var resultNode = Tree.SelectedNode.Tag as Result;

            if (resultNode != null && (resultNode.Children.Count > 0 || resultNode.FileEntry == null))
                return;

            if (resultNode != null)
                _scanBase.ShowFileInfo(resultNode.FileEntry);
        }

        #region Context Menu Events
        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            SetCheckedItems(true);
        }

        private void selectNone_Click(object sender, RoutedEventArgs e)
        {
            SetCheckedItems(false);
        }

        private void selectInvert_Click(object sender, RoutedEventArgs e)
        {
            SetCheckedItems(null);
        }

        private void viewFileInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowDetails();
        }

        private void SetCheckedItems(bool? isChecked)
        {
            var resultModel = Tree.Model as ResultModel;

            if (resultModel == null)
                return;

            foreach (var child in resultModel.Root.Children.SelectMany(root => root.Children))
            {
                if (!isChecked.HasValue)
                    child.IsChecked = !child.IsChecked;
                else
                    child.IsChecked = isChecked.Value;
            }
        }

        #endregion

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(ProgressBar.Maximum) > 0)
            {
                Main.TaskbarProgressValue = (e.NewValue / ProgressBar.Maximum);
            }
        }
    }
}

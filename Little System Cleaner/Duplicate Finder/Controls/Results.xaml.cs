using Little_System_Cleaner.Duplicate_Finder.Helpers;
using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    /// Interaction logic for Results.xaml
    /// </summary>
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

        private Wizard scanBase;
        private string _progressBarText;

        public string ProgressBarText
        {
            get { return this._progressBarText; }
            set
            {
                this._progressBarText = value;
                this.OnPropertyChanged("ProgressBarText");
            }
        }

        public ResultModel DuplicateFiles
        {
            get { return (this._tree.Model as ResultModel); }
        }

        public Results(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            this._tree.Model = ResultModel.CreateResultModel(sb);
            this._tree.ExpandAll();

            Utils.AutoResizeColumns(this._tree);
        }

        private void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            List<FileEntry> files = new List<FileEntry>();

            foreach (Result resParent in (this._tree.Model as ResultModel).Root.Children)
            {
                if (resParent.Children.Count > 0)
                {
                    foreach (Result resChild in resParent.Children)
                    {
                        if (resChild.IsChecked.GetValueOrDefault())
                            files.Add(resChild.FileEntry);
                    }
                }
            }

            if (files.Count == 0)
            {
                MessageBox.Show(App.Current.MainWindow, "No files were selected to be removed. If you would like to not remove any files, please click cancel.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(App.Current.MainWindow, "Are you sure you want to remove the selected files?\nYou may not be able to get them back once they're deleted.", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                long seqNum = 0;
                bool sysRestoreAvailable = SysRestore.SysRestoreAvailable();

                Main.Watcher.Event("Duplicate Finder", "Remove Duplicates");

                Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                Main.TaskbarProgressValue = 0;

                this.progressBar.Value = 0;
                this.progressBar.Minimum = 0;
                this.progressBar.Maximum = (sysRestoreAvailable ? files.Count + 2 : files.Count);

                if (sysRestoreAvailable)
                {
                    this.ProgressBarText = "Creating System Restore Point";

                    try
                    {
                        SysRestore.StartRestore("Before Little System Cleaner (Duplicate Finder) Clean", out seqNum);
                    }
                    catch (Win32Exception ex)
                    {
                        MessageBox.Show(App.Current.MainWindow, "The following error occurred trying to create a system restore point: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    this.progressBar.Value++;
                }
                
                foreach (FileEntry fileEntry in files)
                {
                    string filePath = fileEntry.FilePath;

                    double percent = ((this.progressBar.Value / this.progressBar.Maximum) * 100);
                    this.ProgressBarText = string.Format("{0}/{1} ({2:0.##}%)", this.progressBar.Value, this.progressBar.Maximum, percent);

                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("Unable to remove file ({0}).\nThe following error occurred: {1}", filePath, ex.Message);
                        MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    this.progressBar.Value++;
                    Properties.Settings.Default.lastScanErrorsFixed++;
                }

                Properties.Settings.Default.totalErrorsFixed += Properties.Settings.Default.lastScanErrorsFixed;

                if (sysRestoreAvailable)
                {
                    this.ProgressBarText = "Finalizing system restore point";

                    if (seqNum != 0)
                    {
                        try
                        {
                            SysRestore.EndRestore(seqNum);
                        }
                        catch (Win32Exception ex)
                        {
                            MessageBox.Show(App.Current.MainWindow, "Unable to create system restore point.\nThe following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    this.progressBar.Value++;
                }

                MessageBox.Show(App.Current.MainWindow, "Removed duplicate files from computer", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                Main.TaskbarProgressValue = 0;

                this.scanBase.MoveFirst();
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(App.Current.MainWindow, "Are you sure you want to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.scanBase.MoveFirst();
            }
        }

        private void _tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.ShowDetails();
        }

        private void ShowDetails()
        {
            if (this._tree.SelectedNode == null)
            {
                MessageBox.Show(App.Current.MainWindow, "Nothing is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result resultNode = this._tree.SelectedNode.Tag as Result;

            if (resultNode.Children.Count > 0 || resultNode.FileEntry == null)
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, "Selected row cannot be opened", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.scanBase.ShowFileInfo(resultNode.FileEntry);
        }

        #region Context Menu Events
        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            this.SetCheckedItems(true);
        }

        private void selectNone_Click(object sender, RoutedEventArgs e)
        {
            this.SetCheckedItems(false);
        }

        private void selectInvert_Click(object sender, RoutedEventArgs e)
        {
            this.SetCheckedItems(null);
        }

        private void viewFileInfo_Click(object sender, RoutedEventArgs e)
        {
            this.ShowDetails();
        }

        private void SetCheckedItems(Nullable<bool> isChecked)
        {
            foreach (Result root in (this._tree.Model as ResultModel).Root.Children)
            {
                foreach (Result child in root.Children)
                {
                    if (!isChecked.HasValue)
                        child.IsChecked = !child.IsChecked;
                    else
                        child.IsChecked = isChecked.Value;
                }
            }

            return;
        }
        #endregion

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.progressBar.Maximum != 0)
            {
                Main.TaskbarProgressValue = (e.NewValue / this.progressBar.Maximum);
            }
        }
    }
}

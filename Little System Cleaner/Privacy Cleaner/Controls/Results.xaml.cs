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
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Results
    {
        readonly Wizard _scanBase;

        private Task _cleanTask;

        public Results(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;

            Tree.Model = ResultModel.CreateResultModel();
            Tree.ExpandAll();
        }

        private void ShowDetails()
        {
            if (Tree.SelectedNode == null)
                return;

            ResultNode resultNode = Tree.SelectedNode.Tag as ResultNode;

            if (resultNode is RootNode)
                return;

            if (resultNode is ResultDelegate)
                return;

            _scanBase.ShowDetails(resultNode);
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Tree.SelectedNode == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "Nothing is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                ShowDetails();
            }
        }

        private async void buttonClean_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Main.Watcher.Event("Privacy Cleaner", "Clean Files");

            _cleanTask = new Task(Clean);
            _cleanTask.Start();
            await _cleanTask;

            MessageBox.Show(Application.Current.MainWindow, "Successfully Cleaned Disk", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            _scanBase.MoveFirst();
        }

        private void Clean()
        {
            long lSeqNum = 0;
            Report report = Report.CreateReport(Settings.Default.privacyCleanerLog);

            // Create system restore point
            try
            {
                SysRestore.StartRestore("Before Little System Cleaner (Privacy Cleaner) Cleaning", out lSeqNum);
            }
            catch (Win32Exception ex)
            {
                string message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                Utils.MessageBoxThreadSafe(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            foreach (ResultNode parent in (Tree.Model as ResultModel).Root.Children)
            {
                foreach (ResultNode n in parent.Children)
                {
                    if (n.IsChecked.GetValueOrDefault() != true)
                        continue;

                    report.WriteLine("Section: {0}", parent.Section);

                    n.Clean(report);
                }
            }

            Settings.Default.totalErrorsFixed += Settings.Default.lastScanErrorsFixed;

            report.WriteLine("Successfully Cleaned Disk @ " + DateTime.Now.ToLongTimeString());
            report.DisplayLogFile(Settings.Default.privacyCleanerDisplayLog);

            if (lSeqNum != 0)
            {
                try
                {
                    SysRestore.EndRestore(lSeqNum);
                }
                catch (Win32Exception ex)
                {
                    string message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                    Utils.MessageBoxThreadSafe(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cleanTask.Status == TaskStatus.Running)
            {
                MessageBox.Show(Application.Current.MainWindow, "Please wait for privacy cleaning to finish.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(Application.Current.MainWindow, "Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _scanBase.MoveFirst();
            }
        }
    }
}

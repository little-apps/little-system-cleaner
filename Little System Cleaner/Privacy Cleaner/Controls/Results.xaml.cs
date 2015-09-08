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
using System.Windows;
using System.Windows.Controls;
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

        public Results(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;

            _tree.Model = ResultModel.CreateResultModel();
            _tree.ExpandAll();
        }

        private void ShowDetails()
        {
            if (_tree.SelectedNode == null)
                return;

            ResultNode resultNode = _tree.SelectedNode.Tag as ResultNode;

            if (resultNode is RootNode)
                return;

            if (resultNode is ResultDelegate)
                return;

            _scanBase.ShowDetails(resultNode);
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_tree.SelectedNode == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "Nothing is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                ShowDetails();
            }
        }

        private void buttonClean_Click(object sender, RoutedEventArgs e)
        {
            long lSeqNum = 0;

            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Main.Watcher.Event("Privacy Cleaner", "Clean Files");

            Report report = Report.CreateReport(Settings.Default.privacyCleanerLog);

            // Create system restore point
            try
            {
                SysRestore.StartRestore("Before Little System Cleaner (Privacy Cleaner) Cleaning", out lSeqNum);
            }
            catch (Win32Exception ex)
            {
                string message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            foreach (ResultNode parent in (_tree.Model as ResultModel).Root.Children)
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
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MessageBox.Show(Application.Current.MainWindow, "Successfully Cleaned Disk", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            _scanBase.MoveFirst();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Application.Current.MainWindow, "Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _scanBase.MoveFirst();
            }
        }
    }
}

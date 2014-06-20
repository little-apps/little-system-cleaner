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
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using System.Xml;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Results : UserControl
    {
        Wizard scanBase;

        public Results(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            this._tree.Model = ResultModel.CreateResultModel();
            this._tree.ExpandAll();
        }

        private void ShowDetails()
        {
            if (this._tree.SelectedNode == null)
                return;

            ResultNode resultNode = this._tree.SelectedNode.Tag as ResultNode;

            if (resultNode is RootNode)
                return;

            if (resultNode is ResultDelegate)
                return;

            this.scanBase.ShowDetails(resultNode);
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this._tree.SelectedNode == null)
            {
                MessageBox.Show(App.Current.MainWindow, "Nothing is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                this.ShowDetails();
            }
        }

        private void buttonClean_Click(object sender, RoutedEventArgs e)
        {
            long seqNum = 0;

            if (MessageBox.Show(App.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Report report = Report.CreateReport(Properties.Settings.Default.privacyCleanerLog);

#if (!DEBUG)
            // Create system restore point
            if (Properties.Settings.Default.optionsSysRestore)
                SysRestore.StartRestore("Before Little Privacy Cleaner Fix", out seqNum);
#endif

            foreach (ResultNode parent in (this._tree.Model as ResultModel).Root.Children)
            {
                foreach (ResultNode n in parent.Children)
                {
                    if (n.IsChecked.GetValueOrDefault() != true)
                        continue;

                    report.WriteLine(string.Format("Section: {0}", parent.Section));

                    n.Clean(report);
                }
            }

            report.WriteLine("Successfully Cleaned Disk @ " + DateTime.Now.ToLongTimeString());
            report.DisplayLogFile(Properties.Settings.Default.privacyCleanerDisplayLog);

#if (!DEBUG)
            // End restore point
            SysRestore.EndRestore(seqNum);
#endif

            MessageBox.Show(App.Current.MainWindow, "Successfully Cleaned Disk", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            this.scanBase.MoveFirst();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(App.Current.MainWindow, "Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.scanBase.MoveFirst();
            }
        }
    }
}

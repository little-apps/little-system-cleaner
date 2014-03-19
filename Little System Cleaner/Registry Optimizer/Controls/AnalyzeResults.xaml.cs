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
using System.Linq;
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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls.DataVisualization.Charting;
using Little_System_Cleaner.Registry_Optimizer.Helpers;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class AnalyzeResults : UserControl
    {
        Wizard scanBase;

        public AnalyzeResults(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            double oldRegistrySize = Utils.GetOldRegistrySize(), newRegistrySize = Utils.GetNewRegistrySize();

            decimal oldRegistrySizeMB = decimal.Round(Convert.ToDecimal(oldRegistrySize) / 1024 / 1024, 2);
            decimal diffRegistrySizeMB = decimal.Round((Convert.ToDecimal(oldRegistrySize - newRegistrySize)) / 1024 / 1024, 2);

            ((PieSeries)mcChart.Series[0]).ItemsSource = new KeyValuePair<string, decimal>[] { 
                new KeyValuePair<string, decimal>(string.Format("Registry Size ({0}MB)", oldRegistrySizeMB), oldRegistrySizeMB - diffRegistrySizeMB),
                new KeyValuePair<string, decimal>(string.Format("Saving ({0}MB)", diffRegistrySizeMB), diffRegistrySizeMB) };

            if ((100 - ((newRegistrySize / oldRegistrySize) * 100)) >= 5) {
                // Set errors to number of registry hives
                Properties.Settings.Default.lastScanErrors = Wizard.RegistryHives.Count;

                this.mcChart.Title = "The Windows Registry Needs To Be Compacted";
            } else {
                // Set errors to zero
                Properties.Settings.Default.lastScanErrors = 0;
                this.mcChart.Title = "The Windows Registry Does Not Need To Be Compacted";
                this.buttonCompact.IsEnabled = false;
            }
        }

        private void buttonCompact_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to compact your registry?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            SecureDesktop secureDesktop = new SecureDesktop();
            secureDesktop.Show();

            Compact compactWnd = new Compact();
            compactWnd.ShowDialog();

            secureDesktop.Close();

            // Set errors fixed to number of registry hives
            Properties.Settings.Default.lastScanErrorsFixed = Wizard.RegistryHives.Count;

            if (MessageBox.Show(Application.Current.MainWindow, "You must restart your computer before the new setting will take effect. Do you want to restart your computer now?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                // Restart computer
                PInvoke.ExitWindowsEx(0x02, PInvoke.MajorOperatingSystem | PInvoke.MinorReconfig | PInvoke.FlagPlanned);

            this.scanBase.MoveFirst();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.scanBase.MoveFirst();
        }
    }

}

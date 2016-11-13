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
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Registry_Optimizer.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.DataVisualization.Charting;
using PInvoke = Little_System_Cleaner.Registry_Optimizer.Helpers.PInvoke;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    ///     Interaction logic for Analyze.xaml
    /// </summary>
    public partial class AnalyzeResults
    {
        private readonly Wizard _scanBase;

        public AnalyzeResults(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;

            double oldRegistrySize = HiveManager.GetOldRegistrySize(),
                newRegistrySize = HiveManager.GetNewRegistrySize();

            var oldRegistrySizeMb = decimal.Round(Convert.ToDecimal(oldRegistrySize) / 1024 / 1024, 2);
            var diffRegistrySizeMb = decimal.Round(Convert.ToDecimal(oldRegistrySize - newRegistrySize) / 1024 / 1024, 2);

            ((PieSeries)McChart.Series[0]).ItemsSource =
                new[]
                {
                    new KeyValuePair<string, decimal>($"Registry Size ({oldRegistrySizeMb}MB)",
                        oldRegistrySizeMb - diffRegistrySizeMb),
                    new KeyValuePair<string, decimal>($"Saving ({diffRegistrySizeMb}MB)", diffRegistrySizeMb)
                };

            if (100 - newRegistrySize / oldRegistrySize * 100 >= 5)
            {
                // Set errors to number of registry hives
                Settings.Default.lastScanErrors = Wizard.RegistryHives.Count;

                McChart.Title = "The Windows Registry Needs To Be Compacted";
            }
            else
            {
                // Properties.Settings.Default.lastScanErrors will still equal 0

                McChart.Title = "The Windows Registry Does Not Need To Be Compacted";
                ButtonCompact.IsEnabled = false;
            }
        }

        private void buttonCompact_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to compact your registry?",
                    Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            Wizard.IsBusy = true;

            var secureDesktop = new SecureDesktop();
            secureDesktop.Show();

            var compactWnd = new Compact();
            compactWnd.ShowDialog();

            secureDesktop.Close();

            // Set errors fixed to number of registry hives
            Settings.Default.lastScanErrorsFixed = Wizard.RegistryHives.Count;
            Settings.Default.totalErrorsFixed += Settings.Default.lastScanErrorsFixed;

            Wizard.IsBusy = false;

            if (
                MessageBox.Show(Application.Current.MainWindow,
                    "You must restart your computer before the new setting will take effect. Do you want to restart your computer now?",
                    Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                // Restart computer
                Misc.PInvoke.ExitWindowsEx(Misc.PInvoke.ShutdownFlags.Reboot, Misc.PInvoke.ShutdownReasons.MajorOperatingsystem | Misc.PInvoke.ShutdownReasons.MinorReconfig | Misc.PInvoke.ShutdownReasons.FlagsPlanned);

            _scanBase.MoveFirst();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Wizard.IsBusy)
            {
                MessageBox.Show(Application.Current.MainWindow, "Cannot cancel while the registry is being compacted",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            _scanBase.MoveFirst();
        }
    }
}
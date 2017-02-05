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

using Little_System_Cleaner.Registry_Optimizer.Helpers;
using System;
using System.Windows;
using Shared;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    ///     Interaction logic for Main.xaml
    /// </summary>
    public partial class Main
    {
        private readonly Wizard _scanBase;

        public Main(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;
        }

        /// <summary>
        ///     True if the registry has been compacted and is waiting for a reboot
        /// </summary>
        internal static bool IsCompacted { get; set; }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.IsAssemblyLoaded("System.Windows.Controls.DataVisualization.Toolkit", new Version(3, 5, 0), true))
                return;

            MessageBox.Show(Application.Current.MainWindow,
                "It appears that System.Windows.Controls.DataVisualization.Toolkit.dll is not loaded, because of this, the registry cannot be optimized.\n\nPlease ensure that the file is located in the same folder as Little System Cleaner and that the version is at least 3.5.0.",
                Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

            ButtonAnalyze.IsEnabled = false;
        }

        private void buttonAnalyze_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(Application.Current.MainWindow,
                    "You must close running programs before optimizing the registry.\nPlease save your work and close any running programs now.",
                    Utils.ProductName, MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK)
                return;

            Wizard.IsBusy = true;

            var secureDesktop = new SecureDesktop();
            secureDesktop.Show();

            var analyzeWnd = new Analyze();
            analyzeWnd.ShowDialog();

            secureDesktop.Close();

            Wizard.IsBusy = false;

            // Check registry size before continuing
            if (HiveManager.GetNewRegistrySize() <= 0 || IsCompacted)
            {
                MessageBox.Show(Application.Current.MainWindow,
                    "It appears that the registry has already been compacted.", Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            _scanBase.MoveNext();
        }
    }
}
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers
{
    /// <summary>
    /// Interaction logic for RunningMsg.xaml
    /// </summary>
    public partial class RunningMsg : Window
    {
        private readonly string procName;
        private readonly string scannerName;

        private Timer timer = new Timer(100);

        public RunningMsg(string name, string proc)
        {
            InitializeComponent();

            this.procName = proc;
            this.scannerName = name;

            this.timer.Elapsed += timer_Elapsed;
            this.timer.Start();
        }

        private void checkBoxDontShow_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.privacyCleanerAutoSkipScanner = this.checkBoxDontShow.IsChecked.GetValueOrDefault();
        }

        public static bool? DisplayRunningMsg(string scannerName, string procName)
        {
            if (!Utils.IsProcessRunning(procName))
                return true;

            if (Properties.Settings.Default.privacyCleanerAutoSkipScanner)
            {
                MessageBox.Show(string.Format("Automatically skipping the scanning for {0}...", scannerName), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            RunningMsg dlgResult = new RunningMsg(scannerName, procName);
            return dlgResult.ShowDialog();
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Update list box
            this.listBox.Items.Clear();
            foreach (Process p in Process.GetProcessesByName(this.procName))
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle))
                    this.listBox.Items.Add(p.MainWindowTitle);
            }

            // Check if process is running
            if (!Utils.IsProcessRunning(procName))
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.DialogResult.GetValueOrDefault() == false)
            {
                MessageBox.Show(this, string.Format("Skipping the scanning for {0}...", scannerName), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

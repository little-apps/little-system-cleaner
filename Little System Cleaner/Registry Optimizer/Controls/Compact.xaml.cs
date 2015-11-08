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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using Little_System_Cleaner.Misc;
using PInvoke = Little_System_Cleaner.Registry_Optimizer.Helpers.PInvoke;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    ///     Interaction logic for Compact.xaml
    /// </summary>
    public partial class Compact
    {
        private Thread _threadScan;
        private Thread _threadCurrent;

        public Compact()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set task bar progress bar
            Little_System_Cleaner.Main.TaskbarProgressState = TaskbarItemProgressState.Normal;
            Little_System_Cleaner.Main.TaskbarProgressValue = 0;

            // Set progress bar
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = Wizard.RegistryHives.Count;
            ProgressBar.Value = 0;

            CompactRegistry();
        }

        private async void CompactRegistry()
        {
            long lSeqNum = 0;

            Little_System_Cleaner.Main.Watcher.Event("Registry Optimizer", "Compact Registry");

            Thread.BeginCriticalRegion();

            SetShutdownBlockReason(true);

            try
            {
                SysRestore.StartRestore("Before Little System Cleaner Registry Optimization", out lSeqNum);
            }
            catch (Win32Exception ex)
            {
                string message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            foreach (var h in Wizard.RegistryHives)
            {
                if (h.SkipCompact)
                {
                    TextBlockStatus.Text = $"Skipping: {h.RegistryHive}";
                }
                else
                {
                    TextBlockStatus.Text = $"Compacting: {h.RegistryHive}";

                    await Task.Run(() => h.CompactHive(this));
                }

                ProgressBar.Value++;
            }

            if (lSeqNum != 0)
            {
                try
                {
                    SysRestore.EndRestore(lSeqNum);
                }
                catch (Win32Exception ex)
                {
                    string message =
                        $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            SetShutdownBlockReason(false);

            Thread.EndCriticalRegion();

            // Set IsCompacted
            Main.IsCompacted = true;

            DialogResult = true;

            Little_System_Cleaner.Main.TaskbarProgressState = TaskbarItemProgressState.None;
            Close();
        }

        /// <summary>
        ///     Enables/Disables the shutdown block reason
        /// </summary>
        /// <param name="enable">True to enable the shutdown block reason</param>
        private bool SetShutdownBlockReason(bool enable)
        {
            // The shutdown block will only succeed if it is called from the main thread
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                return (bool) Dispatcher.Invoke(new Func<bool, bool>(SetShutdownBlockReason), enable);
            }

            var hWnd = Process.GetCurrentProcess().MainWindowHandle;

            var ret = enable
                ? PInvoke.ShutdownBlockReasonCreate(hWnd, "The Windows Registry Is Being Compacted")
                : PInvoke.ShutdownBlockReasonDestroy(hWnd);

            return ret;
        }

        private void progressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(ProgressBar.Maximum) > 0)
                Little_System_Cleaner.Main.TaskbarProgressValue = e.NewValue/ProgressBar.Maximum;
        }
    }
}
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
using System.Windows;
using System.Windows.Shell;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Registry_Optimizer.Helpers;
using PInvoke = Little_System_Cleaner.Registry_Optimizer.Helpers.PInvoke;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    /// Interaction logic for Compact.xaml
    /// </summary>
    public partial class Compact : Window
    {
        Thread _threadScan, _threadCurrent;

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
            progressBar1.Minimum = 0;
            progressBar1.Maximum = Wizard.RegistryHives.Count;
            progressBar1.Value = 0;

            _threadScan = new Thread(CompactRegistry);
            _threadScan.Start();
        }

        private void CompactRegistry()
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
                MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            foreach (Hive h in Wizard.RegistryHives)
            {
                if (h.SkipCompact)
                {
                    SetStatusText($"Skipping: {h.RegistryHive}");
                }
                else
                {
                    SetStatusText($"Compacting: {h.RegistryHive}");

                    _threadCurrent = new Thread(() => h.CompactHive(this));
                    _threadCurrent.Start();
                    _threadCurrent.Join();
                }

                IncrementProgressBar();
            }

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

            SetShutdownBlockReason(false);
            
            Thread.EndCriticalRegion();

            // Set IsCompacted
            Main.IsCompacted = true;

            SetDialogResult(true);

            Dispatcher.BeginInvoke(new Action(() => {
                Little_System_Cleaner.Main.TaskbarProgressState = TaskbarItemProgressState.None;
                Close();
            }));
        }

        private void SetDialogResult(bool bResult)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new Action<bool>(SetDialogResult), bResult);
                return;
            }

            DialogResult = bResult;
        }

        private void SetStatusText(string statusText)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new Action<string>(SetStatusText), statusText);
                return;
            }

            textBlockStatus.Text = statusText;
        }

        private void IncrementProgressBar()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new Action(IncrementProgressBar));
                return;
            }

            progressBar1.Value++;
        }

        /// <summary>
        /// Enables/Disables the shutdown block reason
        /// </summary>
        /// <param name="enable">True to enable the shutdown block reason</param>
        private bool SetShutdownBlockReason(bool enable)
        {
            // The shutdown block will only succeed if it is called from the main thread
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                return (bool)Dispatcher.Invoke(new Func<bool, bool>(SetShutdownBlockReason), enable);
            }

            IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;

            var ret = enable ? PInvoke.ShutdownBlockReasonCreate(hWnd, "The Windows Registry Is Being Compacted") : PInvoke.ShutdownBlockReasonDestroy(hWnd);

            return ret;
        }

        private void progressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (progressBar1.Maximum != 0)
                Little_System_Cleaner.Main.TaskbarProgressValue = (e.NewValue / progressBar1.Maximum);
        }
    }
}

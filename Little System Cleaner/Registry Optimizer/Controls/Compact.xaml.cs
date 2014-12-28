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
using System.Windows.Shapes;
using CommonTools;
using System.Threading;
using System.Runtime.InteropServices;
using Little_System_Cleaner.Registry_Optimizer.Helpers;
using System.ComponentModel;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    /// Interaction logic for Compact.xaml
    /// </summary>
    public partial class Compact : Window
    {
        Thread threadScan, threadCurrent;

        public Compact()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set task bar progress bar
            Little_System_Cleaner.Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            Little_System_Cleaner.Main.TaskbarProgressValue = 0;

            // Set progress bar
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = Wizard.RegistryHives.Count;
            this.progressBar1.Value = 0;

            this.threadScan = new Thread(new ThreadStart(CompactRegistry));
            this.threadScan.Start();
        }

        private void CompactRegistry()
        {
            long lSeqNum = 0;
            long lSpaceSaved = 0;

            Little_System_Cleaner.Main.Watcher.Event("Registry Optimizer", "Compact Registry");

            Thread.BeginCriticalRegion();

            this.SetShutdownBlockReason(true);

            try
            {
                SysRestore.StartRestore("Before Little System Cleaner Registry Optimization", out lSeqNum);
            }
            catch (Win32Exception ex)
            {
                string message = string.Format("Unable to create system restore point.\nThe following error occurred: {0}", ex.Message);
                MessageBox.Show(App.Current.MainWindow, message, Little_System_Cleaner.Misc.Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            foreach (Hive h in Wizard.RegistryHives)
            {
                SetStatusText(string.Format("Compacting: {0}", h.RegistryHive));

                try
                {
                    threadCurrent = new Thread(new ThreadStart(h.CompactHive));
                    threadCurrent.Start();
                    threadCurrent.Join();

                    lSpaceSaved += h.OldHiveSize - h.NewHiveSize;
                }
                catch (Exception ex)
                {
                    string message = string.Format("Unable to compact registry hive: {0}\nThe following error occurred: {1}", h.RegistryHive, ex.Message);
                    MessageBox.Show(App.Current.MainWindow, message, Little_System_Cleaner.Misc.Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
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
                    string message = string.Format("Unable to create system restore point.\nThe following error occurred: {0}", ex.Message);
                    MessageBox.Show(App.Current.MainWindow, message, Little_System_Cleaner.Misc.Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            this.SetShutdownBlockReason(false);
            
            Thread.EndCriticalRegion();

            // Set IsCompacted
            Main.IsCompacted = true;

            SetDialogResult(true);

            this.Dispatcher.BeginInvoke(new Action(() => {
                Little_System_Cleaner.Main.TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                this.Close();
            }));
        }

        private void SetDialogResult(bool bResult)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action<bool>(SetDialogResult), bResult);
                return;
            }

            this.DialogResult = bResult;
        }

        private void SetStatusText(string statusText)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action<string>(SetStatusText), statusText);
                return;
            }

            this.textBlockStatus.Text = statusText;
        }

        private void IncrementProgressBar()
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action(IncrementProgressBar));
                return;
            }

            this.progressBar1.Value++;
        }

        /// <summary>
        /// Enables/Disables the shutdown block reason
        /// </summary>
        /// <param name="enable">True to enable the shutdown block reason</param>
        private bool SetShutdownBlockReason(bool enable)
        {
            // The shutdown block will only succeed if it is called from the main thread
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                return (bool)this.Dispatcher.Invoke(new Func<bool, bool>(SetShutdownBlockReason), enable);
            }

            bool ret;
            IntPtr hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            if (enable)
                ret = PInvoke.ShutdownBlockReasonCreate(hWnd, "The Windows Registry Is Being Compacted");
            else
                ret = PInvoke.ShutdownBlockReasonDestroy(hWnd);

            return ret;
        }

        private void progressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.progressBar1.Maximum != 0)
                Little_System_Cleaner.Main.TaskbarProgressValue = (e.NewValue / this.progressBar1.Maximum);
        }
    }
}

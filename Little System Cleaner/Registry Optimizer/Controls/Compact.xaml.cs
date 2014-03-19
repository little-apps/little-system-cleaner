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

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    /// Interaction logic for Compact.xaml
    /// </summary>
    public partial class Compact : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string reason);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

        Thread threadScan, threadCurrent;

        public Compact()
        {
            InitializeComponent();
        }

        private void CompactRegistry()
        {
            long lSeqNum = 0;
            long lSpaceSaved = 0;

            Thread.BeginCriticalRegion();

            SysRestore.StartRestore("Before Registry Optimization", out lSeqNum);

            foreach (Hive h in Wizard.RegistryHives)
            {
                SetStatusText(string.Format("Compacting: {0}", h.RegistryHive));

                threadCurrent = new Thread(new ThreadStart(h.CompactHive));
                threadCurrent.Start();
                threadCurrent.Join();

                lSpaceSaved += h.OldHiveSize - h.NewHiveSize;

                IncrementProgressBar();
            }

            SysRestore.EndRestore(lSeqNum);
            Thread.EndCriticalRegion();

            // Set IsCompacted
            Main.IsCompacted = true;

            // Update last scan stats
            long elapsedTime = DateTime.Now.Subtract(Wizard.ScanStartTime).Ticks;

            Properties.Settings.Default.lastScanElapsed = elapsedTime;

            // Update total scans
            Properties.Settings.Default.totalScans++;

            SetDialogResult(true);
            this.Dispatcher.BeginInvoke(new Action(this.Close));
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
        private void SetShutdownBlockReason(bool enable)
        {
            if (this.Dispatcher.Thread != Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action<bool>(SetShutdownBlockReason), enable);
                return;
            }

            IntPtr hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            if (enable)
                ShutdownBlockReasonCreate(hWnd, "The Windows Registry Is Being Compacted");
            else
                ShutdownBlockReasonDestroy(hWnd);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = Wizard.RegistryHives.Count;
            this.progressBar1.Value = 0;

            this.loadingImg.Content = new AnimatedImage();
            (this.loadingImg.Content as AnimatedImage).LoadGif(Properties.Resources.ajax_loader_blue);

            this.threadScan = new Thread(new ThreadStart(CompactRegistry));
            this.threadScan.Start();
        }
    }
}

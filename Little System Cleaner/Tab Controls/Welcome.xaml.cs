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
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Welcome.xaml
    /// </summary>
    public partial class Welcome : UserControl
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(this);
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);


        public Welcome()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            RegistryKey regKey = null;

            if (Properties.Settings.Default.lastScanDate != 0)
                this.lastDateTime.Text = DateTime.FromBinary(Properties.Settings.Default.lastScanDate).ToString();
            else
                this.lastDateTime.Text = "Unknown";
            this.errorsFound.Text = string.Format("{0} errors found", Properties.Settings.Default.lastScanErrors);
            this.errorsRepaired.Text = string.Format("{0} errors fixed", Properties.Settings.Default.lastScanErrorsFixed);
            if (Properties.Settings.Default.lastScanElapsed != 0)
            {
                TimeSpan ts = TimeSpan.FromTicks(Properties.Settings.Default.lastScanElapsed);
                this.elapsedTime.Text = string.Format("{0} seconds", Convert.ToInt32(ts.TotalSeconds));
            }
            else 
                this.elapsedTime.Text = "Unknown";

            this.totalScans.Text = string.Format("{0} scans performed", Properties.Settings.Default.totalScans);
            this.totalErrors.Text = string.Format("{0} errors found", Properties.Settings.Default.totalErrorsFound);
            this.totalErrorsFixed.Text = string.Format("{0} errors fixed", Properties.Settings.Default.totalErrorsFixed);

            this.cpuType.Text = "Unknown";

            try
            {
                regKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");

                if (regKey != null)
                {
                    string procName = regKey.GetValue("ProcessorNameString") as string;

                    if (!string.IsNullOrEmpty(procName))
                        this.cpuType.Text = procName;
                }
            }
            catch (Exception)
            {
                this.cpuType.Text = "Unknown";
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            if (GlobalMemoryStatusEx(memStatus))
                this.totalRAM.Text = string.Format("{0} total memory", Utils.ConvertSizeToString(Convert.ToInt64(memStatus.ullTotalPhys)));
            else
                this.totalRAM.Text = "Unknown";

            this.osVersion.Text = OSVersion.GetOSVersion();
        }
    }
}

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
using System.Threading;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : UserControl
    {
        #region GlobalMemoryStatusEx
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
        #endregion
       
        /// <summary>
        /// True if the registry has been compacted and is waiting for a reboot
        /// </summary>
        public static bool IsCompacted { get; set; }

        Wizard scanBase;

        public Main(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;
        }

        private void buttonAnalyze_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Application.Current.MainWindow, "You must close running programs before optimizing the registry.\nPlease save your work and close any running programs now.", Utils.ProductName, MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK)
                return;

            Wizard.IsBusy = true;

            SecureDesktop secureDesktop = new SecureDesktop();
            secureDesktop.Show();

            Analyze analyzeWnd = new Analyze();
            analyzeWnd.ShowDialog();

            secureDesktop.Close();

            Wizard.IsBusy = false;

            // Check registry size before continuing
            if (Utils.GetNewRegistrySize() <= 0 || IsCompacted)
            {
                MessageBox.Show(Application.Current.MainWindow, "It appears that the registry has already been compacted.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.scanBase.MoveNext();
        }

        
    }
}

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
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Input;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Xceed.Wpf.Toolkit;

namespace Little_System_Cleaner
{
    public partial class Main
	{
        public static bool IsTabsEnabled { get; set;}

        System.Timers.Timer timerCheck = new System.Timers.Timer(500);

		public Main()
		{
			this.InitializeComponent();

            //this.Title = string.Format("Little Registry Cleaner v{0}", System.Windows.Forms.Application.ProductVersion);

            //// Disk Cleaner functions
            PopulateDiskDrives();
            PopulateIncludeFolders();
		}

        private void PopulateIncludeFolders()
        {
            Properties.Settings.Default.diskCleanerIncludedFolders = new System.Collections.Specialized.StringCollection();

            Properties.Settings.Default.diskCleanerIncludedFolders.Add(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User));
            Properties.Settings.Default.diskCleanerIncludedFolders.Add(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine));
            Properties.Settings.Default.diskCleanerIncludedFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
            Properties.Settings.Default.diskCleanerIncludedFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache));
        }

        private void PopulateDiskDrives()
        {
            if (Properties.Settings.Default.diskCleanerDiskDrives == null)
                Properties.Settings.Default.diskCleanerDiskDrives = new System.Collections.ArrayList();
            else
                Properties.Settings.Default.diskCleanerDiskDrives.Clear();

            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (!driveInfo.IsReady || driveInfo.DriveType != DriveType.Fixed)
                    continue;

                string freeSpace = Utils.ConvertSizeToString(driveInfo.TotalFreeSpace);
                string totalSpace = Utils.ConvertSizeToString(driveInfo.TotalSize);

                bool isChecked = false;
                if (winDir.Contains(driveInfo.Name))
                    isChecked = true;

                Little_System_Cleaner.Disk_Cleaner.Controls.lviDrive listViewItem = new Little_System_Cleaner.Disk_Cleaner.Controls.lviDrive(isChecked, driveInfo.Name, driveInfo.DriveFormat, totalSpace, freeSpace, driveInfo);

                // Store as listviewitem cause im too lazy 
                Properties.Settings.Default.diskCleanerDiskDrives.Add(listViewItem);
            }
        }

        void timerCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new EventHandler<System.Timers.ElapsedEventArgs>(timerCheck_Elapsed), new object[] { sender, e });
                return;
            }

            this.tabItemWelcome.IsEnabled = this.tabItemOptions.IsEnabled = this.tabItemRestore.IsEnabled = this.tabItemStartupMgr.IsEnabled = this.tabItemUninstallMgr.IsEnabled = IsTabsEnabled;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();

            base.OnMouseLeftButtonDown(e);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            IsTabsEnabled = true;

            this.timerCheck.Elapsed += new System.Timers.ElapsedEventHandler(timerCheck_Elapsed);
            this.timerCheck.Start();

            // See if we have the current version
            if (Properties.Settings.Default.optionsUpdate)
            {
                string strVersion = "", strChangeLogURL = "", strDownloadURL = "", strReleaseDate = "";
                if (Utils.FindUpdate(ref strVersion, ref strReleaseDate, ref strChangeLogURL, ref strDownloadURL, true))
                    if (System.Windows.MessageBox.Show(this, "A newer version(" + strVersion + ") has been found. Would you like to update now?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                        Utils.LaunchURI(strDownloadURL);
            }
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (System.Windows.MessageBox.Show(this, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ScanWizard.CancelScan();
                this.ctrlOptions.UpdateSettings();
            }
            else
                e.Cancel = true;
        }

        private void imageHelp_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ContextMenu contextMenu = new ContextMenu();

            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.Help, "Help"));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.internet, "Visit Website"));
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.update, "Check for updates"));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.icon, "About..."));

            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// Used to create menu items for context menu
        /// </summary>
        /// <param name="bMapImg">Icon or null if theres none</param>
        /// <param name="header">Text to display</param>
        /// <returns>MenuItem</returns>
        private MenuItem CreateMenuItem(System.Drawing.Bitmap bMapImg, string header)
        {
            MenuItem menuItem = new MenuItem();

            // Create icon
            Image imgCtrl = new Image();
            if (bMapImg != null)
            {
                imgCtrl.Height = imgCtrl.Width = 16;
                imgCtrl.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bMapImg.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }

            menuItem.Icon = imgCtrl;
            menuItem.Header = header;
            menuItem.Click += new RoutedEventHandler(menuItem_Click);

            return menuItem;
        }

        void menuItem_Click(object sender, RoutedEventArgs e)
        {
            switch ((string)(sender as MenuItem).Header)
            {
                case "Help":
                    {
                        if (!File.Exists("Little System Cleaner.chm"))
                        {
                            System.Windows.MessageBox.Show(this, "No help file could be found", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        System.Windows.Forms.Help.ShowHelp(null, "Little System Cleaner.chm");
                        break;
                    }
                case "Visit Website":
                    {
                        Utils.LaunchURI(@"http://www.little-apps.com/little-system-cleaner/");
                        break;
                    }
                case "Check for updates":
                    {
                        string strVersion = "", strChangeLogURL = "", strDownloadURL = "", strReleaseDate = "";
                        if (Utils.FindUpdate(ref strVersion, ref strReleaseDate, ref strChangeLogURL, ref strDownloadURL, false))
                        {
                            if (System.Windows.MessageBox.Show(this, "A newer version(" + strVersion + ") has been found. Would you like to update now?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                Utils.LaunchURI(strDownloadURL);
                        } 
                        else
                            System.Windows.MessageBox.Show(this, "You already have the latest version", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }
                case "About...":
                    {
                        if (IsTabsEnabled)
                        {
                            this.tabControl.SelectedIndex = this.tabControl.Items.IndexOf(this.tabItemOptions);
                            this.ctrlOptions.ShowAboutTab();
                        }
                        break;
                    }

                default:
                    break;
            }
        }

        private void imageHelp_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void imageHelp_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void comboBoxTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.tabControl != null)
                this.tabControl.SelectedIndex = this.comboBoxTab.SelectedIndex;
        }
	}
}
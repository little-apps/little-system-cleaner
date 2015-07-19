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
using Little_System_Cleaner.Misc;
using System.Windows.Shell;
using System.Reflection;

namespace Little_System_Cleaner
{
    public partial class Main
	{
        internal static bool IsTabsEnabled { get; set; }

        internal static TaskbarItemProgressState TaskbarProgressState
        {
            get { return (App.Current.MainWindow as Main).taskBarItemInfo.ProgressState; }
            set 
            {
                if (App.Current != null)
                {
                    Main currentWindow = App.Current.MainWindow as Main;

                    if (currentWindow != null)
                    {
                        TaskbarItemInfo taskBarItemInfo = currentWindow.taskBarItemInfo;

                        if (taskBarItemInfo != null)
                            taskBarItemInfo.ProgressState = value;
                    }
                }
                
            }
        }

        internal static double TaskbarProgressValue
        {
            get { return (App.Current.MainWindow as Main).taskBarItemInfo.ProgressValue; }
            set
            {
                TaskbarItemInfo taskBarItemInfo = (App.Current.MainWindow as Main).taskBarItemInfo;

                if (taskBarItemInfo != null)
                    taskBarItemInfo.ProgressValue = value; 
            }
        }

        private static LittleSoftwareStats.Watcher _watcher;
        internal static LittleSoftwareStats.Watcher Watcher
        {
            get { return _watcher; }
        }

        System.Timers.Timer timerCheck = new System.Timers.Timer(500);
        private bool ignoreSetTabControl = false;

		public Main()
		{
			this.InitializeComponent();

            //this.Title = string.Format("Little Registry Cleaner v{0}", System.Windows.Forms.Application.ProductVersion);
		}

        void timerCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new EventHandler<System.Timers.ElapsedEventArgs>(timerCheck_Elapsed), new object[] { sender, e });
                return;
            }

            this.tabItemWelcome.IsEnabled = this.tabItemOptions.IsEnabled = this.tabItemStartupMgr.IsEnabled = this.tabItemUninstallMgr.IsEnabled = IsTabsEnabled;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();

            base.OnMouseLeftButtonDown(e);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Send usage data to Little Software Stats
            _watcher = new LittleSoftwareStats.Watcher();
            LittleSoftwareStats.Config.Enabled = Properties.Settings.Default.optionsUsageStats;

            string appVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Watcher.Start("922492147b2e47744961de5b9a5d0886", appVer);

            IsTabsEnabled = true;

            this.timerCheck.Elapsed += new System.Timers.ElapsedEventHandler(timerCheck_Elapsed);
            this.timerCheck.Start();

            // See if we have the current version
            if (Properties.Settings.Default.updateAuto)
            {
                AutoUpdaterWPF.AutoUpdater.MainDispatcher = this.Dispatcher;
                AutoUpdaterWPF.AutoUpdater.Start(Properties.Settings.Default.updateURL);
            }

            this.taskBarItemInfo.Description = Utils.ProductName;
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (System.Windows.MessageBox.Show(this, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                bool? canExit = null;

                UserControl lastCtrl = (this.tabControl.SelectedContent as UserControl);
                System.Reflection.MethodBase methodUnload = lastCtrl.GetType().GetMethod("OnUnloaded");
                if (methodUnload != null)
                    canExit = (bool)methodUnload.Invoke(lastCtrl, new object[] { true });

                if ((canExit.HasValue) && canExit.Value == false)
                {
                    // NOTE: Invoked function is responsible for displaying message on why it can't exit
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
            }

            if (!e.Cancel)
                Watcher.Stop();
        }

        private void imageHelp_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ContextMenu contextMenu = new ContextMenu();

            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.Help, "Help"));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.internet, "Visit Website"));
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.update, "Check for updates"));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.little_system_cleaner.ToBitmap(), "About..."));

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
                            MessageBox.Show(this, "No help file could be found", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        System.Windows.Forms.Help.ShowHelp(null, "Little System Cleaner.chm");
                        break;
                    }
                case "Visit Website":
                    {
                        if (!Utils.LaunchURI(@"http://www.little-apps.com/little-system-cleaner/"))
                            MessageBox.Show(this, "Unable to detect web browser to open link", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                        break;
                    }
                case "Check for updates":
                    {
                        AutoUpdaterWPF.AutoUpdater.MainDispatcher = this.Dispatcher;
                        AutoUpdaterWPF.AutoUpdater.Start(true);
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
            if (this.ignoreSetTabControl)
            {
                this.ignoreSetTabControl = false;

                return;
            }

            this.setTabControl(this.comboBoxTab.SelectedIndex);
        }

        private void setTabControl(int index)
        {
            if (this.tabControl == null)
                return;

            bool? bUnload = null;

            UserControl lastCtrl = (this.tabControl.SelectedContent as UserControl);
            System.Reflection.MethodBase methodUnload = lastCtrl.GetType().GetMethod("OnUnloaded");
            if (methodUnload != null)
                bUnload = (bool?)methodUnload.Invoke(lastCtrl, new object[] { false });

            if (bUnload == true || !bUnload.HasValue)
            {
                this.tabControl.SelectedIndex = index;

                UserControl nextCtrl = (this.tabControl.SelectedContent as UserControl);
                System.Reflection.MethodBase methodLoad = nextCtrl.GetType().GetMethod("OnLoaded");
                if (methodLoad != null)
                    methodLoad.Invoke(nextCtrl, new object[] { });
            }
            else
            {
                // Change combobox back
                this.ignoreSetTabControl = true;
                this.comboBoxTab.SelectedIndex = this.tabControl.SelectedIndex;
            }
        }
	}
}
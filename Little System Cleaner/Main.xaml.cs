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

using Little_System_Cleaner.AutoUpdaterWPF;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Tab_Controls.Options;
using LittleSoftwareStats;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Timer = System.Timers.Timer;
using UserControl = System.Windows.Controls.UserControl;

namespace Little_System_Cleaner
{
    public partial class Main
    {
        public static Main Instance { get; private set; }
        
        private bool _ignoreSetTabControl;

        public Main()
        {
            InitializeComponent();

            Instance = this;
            //this.Title = string.Format("Little Registry Cleaner v{0}", System.Windows.Forms.Application.ProductVersion);
        }

        /// <summary>
        /// Gets the progress state of the window icon in the task bar
        /// </summary>
        internal static TaskbarItemProgressState TaskbarProgressState
        {
            get
            {
                var main = Application.Current.MainWindow as Main;
                return main?.TaskBarItemInfo.ProgressState ?? TaskbarItemProgressState.None;
            }
            set
            {
                if (Application.Current == null)
                    return;
                var currentWindow = Application.Current.MainWindow as Main;

                var taskBarItemInfo = currentWindow?.TaskBarItemInfo;

                if (taskBarItemInfo != null)
                    taskBarItemInfo.ProgressState = value;
            }
        }

        /// <summary>
        /// Gets the progress value of the window icon in the task bar
        /// </summary>
        internal static double TaskbarProgressValue
        {
            get
            {
                var main = Application.Current.MainWindow as Main;
                if (main != null)
                    return main.TaskBarItemInfo.ProgressValue;

                throw new NullReferenceException();
            }
            set
            {
                var main = Application.Current.MainWindow as Main;
                var taskBarItemInfo = main?.TaskBarItemInfo;

                if (taskBarItemInfo != null)
                    taskBarItemInfo.ProgressValue = value;
            }
        }

        /// <summary>
        /// Watcher object used by Little Software Stats
        /// </summary>
        internal static Watcher Watcher { get; private set; }

        /// <summary>
        /// Hack that allows user to drag window when left click is held on window
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            DragMove();

            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Performs operations when window is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            MoveToTabIndex(0);

            // Send usage data to Little Software Stats
            Watcher = new Watcher();
            Config.Enabled = Settings.Default.optionsUsageStats;

            var appVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Watcher.Start("922492147b2e47744961de5b9a5d0886", appVer);

            // See if we have the current version
            if (Settings.Default.updateAuto)
            {
                AutoUpdater.MainDispatcher = Dispatcher;
                AutoUpdater.Start(Settings.Default.updateURL);
            }

            TaskBarItemInfo.Description = Utils.ProductName;
        }

        /// <summary>
        /// Prompts user to exit and exits cleanly if they choose yes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClose(object sender, CancelEventArgs e)
        {
            if (
                MessageBox.Show(this, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                TabControl.ForceExit = true;
                
                var canExit = TabControl.CanUnload(TabControl.SelectedContent as ContentControl);

                if (!canExit)
                {
                    // NOTE: Invoked function is responsible for displaying message on why it can't exit
                    e.Cancel = true;
                    TabControl.ForceExit = false;
                }
            }
            else
            {
                e.Cancel = true;
            }

            if (!e.Cancel)
            {
                Watcher.Stop();
                GarbageCollectAndFinalize();
            }
        }

        /// <summary>
        /// Displays context menu when Help button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imageHelp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var contextMenu = new ContextMenu();

            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.Help, "Help"));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.internet, "Visit Website"));
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.update, "Check for updates"));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(Properties.Resources.little_system_cleaner.ToBitmap(), "About..."));

            contextMenu.IsOpen = true;
        }

        /// <summary>
        ///     Used to create menu items for context menu
        /// </summary>
        /// <param name="bitmapImg">Icon or null if theres none</param>
        /// <param name="header">Text to display</param>
        /// <returns>MenuItem</returns>
        private MenuItem CreateMenuItem(Bitmap bitmapImg, string header)
        {
            var menuItem = new MenuItem();

            // Create icon
            var imgCtrl = new Image();
            if (bitmapImg != null)
            {
                imgCtrl.Height = imgCtrl.Width = 16;
                imgCtrl.Source = Imaging.CreateBitmapSourceFromHBitmap(bitmapImg.GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            menuItem.Icon = imgCtrl;
            menuItem.Header = header;
            menuItem.Click += menuItem_Click;

            return menuItem;
        }

        /// <summary>
        /// Handles the item that user clicked in context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_Click(object sender, RoutedEventArgs e)
        {
            switch ((string)(sender as MenuItem)?.Header)
            {
                case "Help":
                    {
                        if (!File.Exists("Little System Cleaner.chm"))
                        {
                            MessageBox.Show(this, "No help file could be found", Utils.ProductName, MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }

                        Help.ShowHelp(null, "Little System Cleaner.chm");
                        break;
                    }
                case "Visit Website":
                    {
                        if (!Utils.LaunchUri(@"http://www.little-apps.com/little-system-cleaner/"))
                            MessageBox.Show(this, "Unable to detect web browser to open link", Utils.ProductName,
                                MessageBoxButton.OK, MessageBoxImage.Error);

                        break;
                    }
                case "Check for updates":
                    {
                        AutoUpdater.MainDispatcher = Dispatcher;
                        AutoUpdater.Start(true);
                        break;
                    }
                case "About...":
                {
                    ComboBoxTab.SelectedItem = ComboBoxItemOptions;

                    var options = TabItemOptions.Content as Options;
                    options?.ShowAboutTab();

                    break;
                }
            }
        }

        /// <summary>
        /// Changes cursor to hand when user enters help button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imageHelp_MouseEnter(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Changes cursor to default arrow when mouse leaves help button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imageHelp_MouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Changes tab control when user selects new item in combo box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabControl == null)
                return;

            if (_ignoreSetTabControl)
            {
                _ignoreSetTabControl = false;

                return;
            }

            if (MoveToTabIndex(ComboBoxTab.SelectedIndex))
                return;

            // If tab index not changed -> reset combobox index
            _ignoreSetTabControl = true;
            ComboBoxTab.SelectedIndex = TabControl.SelectedIndex;
        }

        /// <summary>
        /// Unloads current tab control and then loads new tab control
        /// </summary>
        /// <param name="index">Index to change to</param>
        /// <returns>False if the tab could not be unloaded</returns>
        private bool MoveToTabIndex(int index)
        {
            TabControl.SelectedIndex = index;
            
            return TabControl.SelectedIndex == ComboBoxTab.SelectedIndex;
        }

        /// <summary>
        ///     Calls GC.Collect() and GC.WaitForPendingFinalizers()
        /// </summary>
        private static void GarbageCollectAndFinalize()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
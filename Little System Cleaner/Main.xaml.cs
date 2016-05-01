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
using LittleSoftwareStats;
using Little_System_Cleaner.AutoUpdaterWPF;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Tab_Controls.Options;
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
        private readonly Timer _timerCheck = new Timer(500);
        private bool _ignoreSetTabControl;

        public Main()
        {
            InitializeComponent();

            //this.Title = string.Format("Little Registry Cleaner v{0}", System.Windows.Forms.Application.ProductVersion);
        }

        internal static bool IsTabsEnabled { get; set; }

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

        internal static Watcher Watcher { get; private set; }

        private void timerCheck_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new EventHandler<ElapsedEventArgs>(timerCheck_Elapsed), sender, e);
                return;
            }

            TabItemWelcome.IsEnabled =
                TabItemOptions.IsEnabled = TabItemStartupMgr.IsEnabled = TabItemUninstallMgr.IsEnabled = IsTabsEnabled;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            DragMove();

            base.OnMouseLeftButtonDown(e);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Send usage data to Little Software Stats
            Watcher = new Watcher();
            Config.Enabled = Settings.Default.optionsUsageStats;

            var appVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Watcher.Start("922492147b2e47744961de5b9a5d0886", appVer);

            IsTabsEnabled = true;

            _timerCheck.Elapsed += timerCheck_Elapsed;
            _timerCheck.Start();

            // See if we have the current version
            if (Settings.Default.updateAuto)
            {
                AutoUpdater.MainDispatcher = Dispatcher;
                AutoUpdater.Start(Settings.Default.updateURL);
            }

            TaskBarItemInfo.Description = Utils.ProductName;
        }

        private void OnClose(object sender, CancelEventArgs e)
        {
            if (
                MessageBox.Show(this, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var lastCtrl = GetLastControl();
                var canExit = CallOnUnloaded(lastCtrl, true);

                if (canExit.HasValue && canExit.Value == false)
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
            {
                Watcher.Stop();
                GarbageCollectAndFinalize();
            }
        }

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

        private void menuItem_Click(object sender, RoutedEventArgs e)
        {
            switch ((string) (sender as MenuItem)?.Header)
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
                    if (IsTabsEnabled)
                    {
                        TabControl.SelectedIndex = TabControl.Items.IndexOf(TabItemOptions);

                        var options = TabItemOptions.Content as Options;
                        options?.ShowAboutTab();
                    }
                    break;
                }
            }
        }

        private void imageHelp_MouseEnter(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void imageHelp_MouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        private void comboBoxTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreSetTabControl)
            {
                _ignoreSetTabControl = false;

                return;
            }

            SetTabControl(ComboBoxTab.SelectedIndex);
        }

        private void SetTabControl(int index)
        {
            if (TabControl == null)
                return;

            var lastCtrl = GetLastControl();

            var canExit = CallOnUnloaded(lastCtrl, false);

            GarbageCollectAndFinalize();

            if (canExit == true || !canExit.HasValue)
            {
                // If DynamicUserControl -> clear Content
                (lastCtrl as DynamicUserControl)?.ClearUserControl();

                TabControl.SelectedIndex = index;

                var selectedContent = TabControl.SelectedContent;

                var control = selectedContent as DynamicUserControl;
                var nextCtrl = control != null
                    ? control.InitUserControl()
                    : TabControl.SelectedContent as UserControl;

                var methodLoad = nextCtrl?.GetType().GetMethod("OnLoaded");
                methodLoad?.Invoke(nextCtrl, new object[] {});
            }
            else
            {
                // Change combobox back
                _ignoreSetTabControl = true;
                ComboBoxTab.SelectedIndex = TabControl.SelectedIndex;
            }
        }

        private UserControl GetLastControl()
        {
            var lastCtrl = TabControl.SelectedContent as UserControl;

            if (lastCtrl is DynamicUserControl)
                lastCtrl = (UserControl) (lastCtrl as DynamicUserControl).Content;

            return lastCtrl;
        }

        private static bool? CallOnUnloaded(UserControl lastCtrl, bool forceExit)
        {
            bool? canExit = null;

            var methodUnload = lastCtrl?.GetType().GetMethod("OnUnloaded");
            if (methodUnload != null)
                canExit = (bool?) methodUnload.Invoke(lastCtrl, new object[] {forceExit});

            return canExit;
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
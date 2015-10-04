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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Little_System_Cleaner.Misc;
using System.Windows.Shell;
using System.Reflection;
using Little_System_Cleaner.Tab_Controls.Options;

namespace Little_System_Cleaner
{
    public partial class Main
	{
        internal static bool IsTabsEnabled { get; set; }

        internal static TaskbarItemProgressState TaskbarProgressState
        {
            get { return (Application.Current.MainWindow as Main).TaskBarItemInfo.ProgressState; }
            set 
            {
                if (Application.Current != null)
                {
                    Main currentWindow = Application.Current.MainWindow as Main;

                    TaskbarItemInfo taskBarItemInfo = currentWindow?.TaskBarItemInfo;

                    if (taskBarItemInfo != null)
                        taskBarItemInfo.ProgressState = value;
                }
                
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
                TaskbarItemInfo taskBarItemInfo = main?.TaskBarItemInfo;

                if (taskBarItemInfo != null)
                    taskBarItemInfo.ProgressValue = value;
            }
        }

        private static LittleSoftwareStats.Watcher _watcher;
        internal static LittleSoftwareStats.Watcher Watcher => _watcher;

        private readonly System.Timers.Timer _timerCheck = new System.Timers.Timer(500);
        private bool _ignoreSetTabControl;

		public Main()
		{
			InitializeComponent();

            //this.Title = string.Format("Little Registry Cleaner v{0}", System.Windows.Forms.Application.ProductVersion);
		}

        void timerCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new EventHandler<System.Timers.ElapsedEventArgs>(timerCheck_Elapsed), sender, e);
                return;
            }

            TabItemWelcome.IsEnabled = TabItemOptions.IsEnabled = TabItemStartupMgr.IsEnabled = TabItemUninstallMgr.IsEnabled = IsTabsEnabled;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            DragMove();

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

            _timerCheck.Elapsed += timerCheck_Elapsed;
            _timerCheck.Start();

            // See if we have the current version
            if (Properties.Settings.Default.updateAuto)
            {
                AutoUpdaterWPF.AutoUpdater.MainDispatcher = Dispatcher;
                AutoUpdaterWPF.AutoUpdater.Start(Properties.Settings.Default.updateURL);
            }

            TaskBarItemInfo.Description = Utils.ProductName;
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var lastCtrl = GetLastControl();
                var canExit = CallOnUnloaded(lastCtrl, true);

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
            {
                Watcher.Stop();
                GarbageCollectAndFinalize();
            }
        }

        private void imageHelp_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            menuItem.Click += menuItem_Click;

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
                        if (!Utils.LaunchUri(@"http://www.little-apps.com/little-system-cleaner/"))
                            MessageBox.Show(this, "Unable to detect web browser to open link", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                        break;
                    }
                case "Check for updates":
                    {
                        AutoUpdaterWPF.AutoUpdater.MainDispatcher = Dispatcher;
                        AutoUpdaterWPF.AutoUpdater.Start(true);
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

                var nextCtrl = selectedContent is DynamicUserControl
                    ? (selectedContent as DynamicUserControl).InitUserControl()
                    : TabControl.SelectedContent as UserControl;
                
                MethodBase methodLoad = nextCtrl?.GetType().GetMethod("OnLoaded");
                methodLoad?.Invoke(nextCtrl, new object[] { });
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
            UserControl lastCtrl = (TabControl.SelectedContent as UserControl);

            if (lastCtrl is DynamicUserControl)
                lastCtrl = (UserControl)(lastCtrl as DynamicUserControl).Content;

            return lastCtrl;
        }

        private bool? CallOnUnloaded(UserControl lastCtrl, bool forceExit)
        {
            bool? canExit = null;

            MethodBase methodUnload = lastCtrl?.GetType().GetMethod("OnUnloaded");
            if (methodUnload != null)
                canExit = (bool?)methodUnload.Invoke(lastCtrl, new object[] { forceExit });

            return canExit;
        }

        /// <summary>
        /// Calls GC.Collect() and GC.WaitForPendingFinalizers()
        /// </summary>
        private static void GarbageCollectAndFinalize()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
	}
}
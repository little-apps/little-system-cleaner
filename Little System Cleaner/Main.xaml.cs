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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Little_System_Cleaner.Tab_Controls.Tools;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using Label = System.Windows.Controls.Label;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Orientation = System.Windows.Controls.Orientation;
using UserControl = System.Windows.Controls.UserControl;

namespace Little_System_Cleaner
{
    public partial class Main
    {
        public static Main Instance { get; private set; }
        
        private bool _ignoreSetTabControl;

        internal DynamicTabControl TabControl { get; private set; }

        public Main()
        {
            InitializeComponent();

            BuildControls();

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
        /// Builds the ComboBox and TabControl
        /// </summary>
        private void BuildControls()
        {
            TabControl = new DynamicTabControl {Name = "TabControl"};

            TabControl.SetValue(Grid.RowProperty, 1);
            TabControl.SetValue(MarginProperty, new Thickness(0));
            TabControl.SetValue(Selector.IsSynchronizedWithCurrentItemProperty, true);
            TabControl.SetValue(StyleProperty, FindResource("WindowTabCtrl") as Style);

            var style = FindResource("WindowTabItem") as Style;

            AddComboTabItem(CreateComboBoxItem("Registry Cleaner"),
                CreateDynamicTabItem("TabItemRegCleaner", typeof(Registry_Cleaner.Controls.Wizard), style));

            AddComboTabItem(CreateComboBoxItem("Registry Optimizer"),
                CreateDynamicTabItem("TabItemRegOptimizer", typeof(Registry_Optimizer.Controls.Wizard), style));

            AddComboTabItem(CreateComboBoxItem("Disk Cleaner"),
                CreateDynamicTabItem("TabItemDiskCleaner", typeof(Disk_Cleaner.Controls.Wizard), style));

            AddComboTabItem(CreateComboBoxItem("Duplicate Cleaner"),
                CreateDynamicTabItem("TabItemDuplicateFinder", typeof(Duplicate_Finder.Controls.Wizard), style));

            AddComboTabItem(CreateComboBoxItem("Tools"),
                CreateDynamicTabItem("TabItemTools", typeof(Tools), style));

            AddComboTabItem(CreateComboBoxItem("Options"),
                 CreateTabItem("TabItemOptions", new Options(), style));

            (Content as Grid)?.Children.Add(TabControl);
        }

        /// <summary>
        /// Adds <see cref="ComboBoxItem"/> and <see cref="TabItem"/> to window
        /// </summary>
        /// <remarks>The ComboBoxItem.Tag is set to the TabItem and the TabItem.Tag is set to the ComboBoxItem</remarks>
        /// <param name="comboBoxItem">ComboBoxItem</param>
        /// <param name="tabItem">TabItem</param>
        /// <exception cref="ArgumentNullException">Thrown if ComboBoxItem or TabItem is null</exception>
        private void AddComboTabItem(FrameworkElement comboBoxItem, FrameworkElement tabItem)
        {
            if (comboBoxItem == null)
                throw new ArgumentNullException(nameof(comboBoxItem));

            if (tabItem == null)
                throw new ArgumentNullException(nameof(tabItem));

            comboBoxItem.Tag = tabItem;
            tabItem.Tag = comboBoxItem;

            ComboBoxTab.Items.Add(comboBoxItem);
            TabControl.Items.Add(tabItem);
        }

        /// <summary>
        /// Creates tab item with <see cref="DynamicUserControl"/>
        /// </summary>
        /// <param name="name">Name of tab item</param>
        /// <param name="type">Type of user control</param>
        /// <param name="style">Style to use for tab item</param>
        /// <returns>TabItem</returns>
        private static TabItem CreateDynamicTabItem(string name, Type type, Style style)
        {
            var userCntrl = new DynamicUserControl();
            DynamicUserControl.SetType(userCntrl, type);

            var tabItem = new TabItem
            {
                Name = name,
                Style = style,
                Content = userCntrl
            };

            return tabItem;
        }

        /// <summary>
        /// Creates a tab item with user control
        /// </summary>
        /// <param name="name">Name of tab item</param>
        /// <param name="userControl">User control inside tab item</param>
        /// <param name="style">Style to use for tab item</param>
        /// <returns>TabItem</returns>
        private static TabItem CreateTabItem(string name, UserControl userControl, Style style)
        {
            var tabItem = new TabItem
            {
                Name = name,
                Style = style,
                Content = userControl
            };

            return tabItem;
        }

        /// <summary>
        /// Creates a <see cref="ComboBoxItem"/> for a tab item
        /// </summary>
        /// <param name="iconSrc">Source of icon</param>
        /// <param name="text">Text for combo box item</param>
        /// <param name="isSelected">True if combo box item is selected</param>
        /// <returns>Combo box item</returns>
        private static ComboBoxItem CreateComboBoxItem(string iconSrc, string text, bool isSelected = false)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var icon = new Image
            {
                Source = new BitmapImage(new Uri(iconSrc, UriKind.RelativeOrAbsolute)),
                Width = 52,
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(icon);

            var label = new Label
            {
                Width = 261,
                Height = 47,
                Margin = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Content = text
            };

            stackPanel.Children.Add(label);

            var comboBoxItem = new ComboBoxItem
            {
                Content = stackPanel,
                IsSelected = isSelected
            };

            return comboBoxItem;
        }



        /// <summary>
        /// Creates a <see cref="ComboBoxItem"/> for a tab item, but without an icon and different Combo Box Item style
        /// </summary>
        /// <param name="text">Text for combo box item</param>
        /// <param name="isSelected">True if combo box item is selected</param>
        /// <returns>Combo box item</returns>
        private static ComboBoxItem CreateComboBoxItem(string text, bool isSelected = false)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };


            var label = new Label
            {
                Width = 261,
                Height = 47,
                Margin = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                Content = text
            };

            stackPanel.Children.Add(label);

            var comboBoxItem = new ComboBoxItem
            {
                Content = stackPanel,
                IsSelected = isSelected
            };

            return comboBoxItem;
        }

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
            ComboBoxTab.SelectedIndex = 0;
            MoveToTabControl(TabControl.Items[0] as TabItem);

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

            try
            {
                var comboBoxItem = e.AddedItems.Cast<ComboBoxItem>().First();

                if (comboBoxItem.Tag == null)
                    throw new NullReferenceException("ComboBoxItem.Tag is null");

                var tabItem = comboBoxItem.Tag as TabItem;

                if (tabItem == null)
                    throw new InvalidCastException("Cannot cast ComboBoxItem.Tag to TabItem");

                MoveToTabControl(tabItem);
            }
            catch
            {
                // If tab index not changed -> reset combobox index
                _ignoreSetTabControl = true;
                ComboBoxTab.SelectedIndex = TabControl.SelectedIndex;
            }
        }

        /// <summary>
        /// Changes the tab control
        /// </summary>
        /// <param name="tabControl">TabControl to change to</param>
        /// <exception cref="Exception">Thrown if TabControl.SelectedItem does not match current combobox item</exception>
        private void MoveToTabControl(TabItem tabControl)
        {
            TabControl.SelectedItem = tabControl;

            if (!ComboBoxTab.Items.Cast<ComboBoxItem>()
                .Any(comboItem => ReferenceEquals(comboItem.Tag, TabControl.SelectedItem)))
                throw new Exception("Tab was not changed");
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
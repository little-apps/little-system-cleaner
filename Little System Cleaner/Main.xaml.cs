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

using AutoUpdaterWPF;
using Little_System_Cleaner.Misc;
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
using Little_System_Cleaner.Tab_Controls.Tools;
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
using System.Windows.Shell;
using Shared;

namespace Little_System_Cleaner
{
    public partial class Main
    {
        private struct ControlInfo
        {
            public string Icon;
            public string Name;
            public string TabItemName;
            public string ControlTypeName;
            public string Assembly;
        }

        public static Main Instance { get; private set; }
        
        private bool _ignoreSetTabControl;

        public TaskbarItemInfo TaskBarItemInfoPublic => TaskBarItemInfo;

        internal DynamicTabControl TabControl { get; private set; }

        private static readonly ControlInfo[] ExternalControlInfos =
        {
            new ControlInfo
            {
                Icon = "Resources/icon.png",
                Name = "Registry Cleaner",
                TabItemName = "TabItemRegCleaner",
                ControlTypeName = "Registry_Cleaner.Controls.Wizard",
                Assembly = "Registry Cleaner.dll"
            },
            new ControlInfo
            {
                Icon = "Resources/optimizer.png",
                Name = "Registry Optimizer",
                TabItemName = "TabItemRegOptimizer",
                ControlTypeName = "Registry_Optimizer.Controls.Wizard",
                Assembly = "Registry Optimizer.dll"
            },
            new ControlInfo
            {
                Icon = "Resources/disk cleaner/icon.png",
                Name = "Disk Cleaner",
                TabItemName = "TabItemDiskCleaner",
                ControlTypeName = "Disk_Cleaner.Controls.Wizard",
                Assembly = "Disk Cleaner.dll"
            },
            new ControlInfo
            {
                Icon = "Resources/duplicate finder/icon.png",
                Name = "Duplicate Finder",
                TabItemName = "TabItemDuplicateFinder",
                ControlTypeName = "Duplicate_Finder.Controls.Wizard",
                Assembly = "Duplicate Finder.dll"
            }
        };

        public Main()
        {
            InitializeComponent();
            
            BuildControls();

            Utils.MainWindowInstance = this;
            Instance = this;
            //this.Title = string.Format("Little Registry Cleaner v{0}", System.Windows.Forms.Application.ProductVersion);
        }

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

            foreach (var controlInfo in ExternalControlInfos)
            {
                try
                {
                    var asm = Assembly.LoadFile($"{Environment.CurrentDirectory}\\{controlInfo.Assembly}");
                    var type = asm.GetType(controlInfo.ControlTypeName);

                    AddComboTabItem(CreateComboBoxItem(controlInfo.Icon, controlInfo.Name),
                        CreateDynamicTabItem(controlInfo.TabItemName, type, style));
                }
                catch (Exception)
                {
                    // Unable to load assembly, skip..
                }
            }

            AddComboTabItem(CreateComboBoxItem("Resources/Tools.png", "Tools"),
                CreateDynamicTabItem("TabItemTools", typeof(Tools), style));

            AddComboTabItem(CreateComboBoxItem("Resources/Options.png", "Options"),
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
            Utils.Watcher = new Watcher();
            Config.Enabled = Settings.Default.optionsUsageStats;

            var appVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Utils.Watcher.Start("922492147b2e47744961de5b9a5d0886", appVer);

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
                Utils.Watcher.Stop();
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
            var header = ((MenuItem) sender).Header as string;

            switch (header)
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
                        var tabItem = TabControl.GetTabItem("TabItemOptions");
                        var comboBoxItem = tabItem.Tag as ComboBoxItem;
                        
                        ComboBoxTab.SelectedItem = comboBoxItem;

                        var options = tabItem.Content as Options;
                        options?.ShowAboutTab();

                        break;
                    }
                default:
                    {
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
﻿/*
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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Little_System_Cleaner.Uninstall_Manager.Helpers;
using Shared;
using Shared.Uninstall_Manager;
using ProgramInfoSorter = Little_System_Cleaner.Uninstall_Manager.Helpers.ProgramInfoSorter;

namespace Little_System_Cleaner.Uninstall_Manager.Controls
{
    public partial class UninstallManager
    {
        private GridViewColumn _lastColumnClicked;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public UninstallManager()
        {
            InitializeComponent();
        }

        public ObservableCollection<ProgramInfoListViewItem> ProgramInfos { get; } =
            new ObservableCollection<ProgramInfoListViewItem>();

        public void OnLoaded()
        {
            PopulateListView();

            // Manually sort listview
            Sort((ListViewProgs.View as GridView)?.Columns[0], _lastDirection);
        }

        public bool OnUnloaded(bool forceExit)
        {
            if (ProgramInfos.Count > 0)
                ProgramInfos.Clear();

            return true;
        }

        private void PopulateListView()
        {
            var listProgInfo = new List<ProgramInfoListViewItem>();
            RegistryKey regKey = null;

            // Clear listview
            ProgramInfos.Clear();

            // Turn textbox into regex pattern
            var regex = new Regex("", RegexOptions.IgnoreCase);

            if (TextBoxSearch.HasText)
            {
                var result = new StringBuilder();
                foreach (var str in TextBoxSearch.Text.Split(' '))
                {
                    result.Append(Regex.Escape(str));
                    result.Append(".*");
                }

                regex = new Regex(result.ToString(), RegexOptions.IgnoreCase);
            }

            // Get the program info list
            try
            {
                regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

                if (regKey != null)
                {
                    foreach (var subKeyName in regKey.GetSubKeyNames())
                    {
                        RegistryKey subKey = null;

                        try
                        {
                            subKey = regKey.OpenSubKey(subKeyName);

                            if (subKey != null)
                                listProgInfo.Add(new ProgramInfoListViewItem(subKey));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("The following error occurred: " + ex.Message +
                                            "\nSkipping uninstall entry for " + regKey + "\\" + subKeyName + "...");
                        }
                        finally
                        {
                            subKey?.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to open " +
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            }
            finally
            {
                regKey?.Close();
            }

            // (x64 registry keys)
            if (Utils.Is64BitOs)
            {
                try
                {
                    regKey =
                        Registry.LocalMachine.OpenSubKey(
                            @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");

                    if (regKey != null)
                    {
                        foreach (var subKeyName in regKey.GetSubKeyNames())
                        {
                            RegistryKey subKey = null;

                            try
                            {
                                subKey = regKey.OpenSubKey(subKeyName);

                                if (subKey != null)
                                    listProgInfo.Add(new ProgramInfoListViewItem(subKey));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("The following error occurred: " + ex.Message +
                                                "\nSkipping uninstall entry for " + regKey + "\\" + subKeyName +
                                                "...");
                            }
                            finally
                            {
                                subKey?.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\n" +
                                    @"Unable to open SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                }
                finally
                {
                    regKey?.Close();
                }
            }

            // Populate list view
            ProgramInfos.AddRange(
                listProgInfo.Where(
                    progInfo =>
                        !string.IsNullOrEmpty(progInfo.DisplayName) && string.IsNullOrEmpty(progInfo.ParentKeyName) &&
                        !progInfo.SystemComponent).Where(progInfo => regex.IsMatch(progInfo.Program)));

            // Resize columns
            ListViewProgs.AutoResizeColumns();
        }

        private void SearchTextBox_Search(object sender, RoutedEventArgs e)
        {
            PopulateListView();
            Sort(_lastColumnClicked, _lastDirection);
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    ListSortDirection direction;
                    if (!Equals(headerClicked.Column, _lastColumnClicked))
                        direction = ListSortDirection.Ascending;
                    else
                        direction = _lastDirection == ListSortDirection.Ascending
                            ? ListSortDirection.Descending
                            : ListSortDirection.Ascending;

                    Sort(headerClicked.Column, direction);
                }
            }
        }

        private void Sort(GridViewColumn column, ListSortDirection direction)
        {
            /*ICollectionView dataView = CollectionViewSource.GetDefaultView(ListViewProgs.Items);
            string sortBy = column.Header as string;

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();*/

            var dataView = (ListCollectionView)CollectionViewSource.GetDefaultView(ListViewProgs.ItemsSource);
            dataView.CustomSort = new ProgramInfoSorter(column, direction);
            dataView.Refresh();

            if (direction == ListSortDirection.Ascending)
                column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
            else
                column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;

            // Remove arrow from previously sorted header
            if (_lastColumnClicked != null && !Equals(_lastColumnClicked, column))
                _lastColumnClicked.HeaderTemplate = null;

            _lastColumnClicked = column;
            _lastDirection = direction;
        }

        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewProgs.SelectedItems.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "No entry selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var progInfo = ListViewProgs.SelectedItems[0] as ProgramInfo;

            if (
                MessageBox.Show(Application.Current.MainWindow,
                    "Are you sure you want to remove this program from the registry?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Utils.Watcher.Event("Uninstall Manager", "Remove from registry");
                progInfo?.RemoveFromRegistry();

                PopulateListView();

                // Manually sort listview
                Sort((ListViewProgs.View as GridView)?.Columns[0], _lastDirection);
            }
        }

        private void buttonUninstall_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewProgs.SelectedItems.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "No entry selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var progInfo = ListViewProgs.SelectedItems[0] as ProgramInfo;

            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to remove this program?",
                    Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Utils.Watcher.Event("Uninstall Manager", "Uninstall");
                progInfo?.Uninstall();

                PopulateListView();

                // Manually sort listview
                Sort((ListViewProgs.View as GridView)?.Columns[0], _lastDirection);
            }
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateListView();

            // Manually sort listview
            Sort((ListViewProgs.View as GridView)?.Columns[0], _lastDirection);
        }
    }
}
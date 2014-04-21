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
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections.ObjectModel;
using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Xml;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using Little_System_Cleaner.Registry_Cleaner.Helpers;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
	public partial class Results : UserControl
	{
        ScanWizard scanWiz;

		public Results(ScanWizard scanBase)
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
            this.scanWiz = scanBase;

            this._tree.Model = ResultModel.CreateResultModel();
            this._tree.ExpandAll();


            if ((this._tree.Model as ResultModel).Root.Children.Count == 0)
            {
                MessageBox.Show(Application.Current.MainWindow, "There were no errors found!", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ScanWizard.logger.DisplayLogFile((Properties.Settings.Default.registryCleanerOptionsShowLog && !Properties.Settings.Default.registryCleanerOptionsAutoRepair));

                // Set last scan errors found
                Properties.Settings.Default.lastScanErrors = 0;

                foreach (BadRegistryKey badRegKeyRoot in (this._tree.Model as ResultModel).Root.Children)
                {
                    foreach (BadRegistryKey child in badRegKeyRoot.Children)
                    {
                        Properties.Settings.Default.lastScanErrors++;
                    }
                }

                // Set total errors found
                Properties.Settings.Default.totalErrorsFound += Properties.Settings.Default.lastScanErrors;

                if (Properties.Settings.Default.registryCleanerOptionsAutoRepair)
                    this.FixProblems();
            }
		}

        private List<BadRegistryKey> GetSelectedRegKeys()
        {
            List<BadRegistryKey> badRegKeyArr = new List<BadRegistryKey>();

            foreach (BadRegistryKey badRegKeyRoot in (this._tree.Model as ResultModel).Root.Children)
            {
                foreach (BadRegistryKey child in badRegKeyRoot.Children)
                {
                    if ((child.IsChecked.HasValue) && child.IsChecked.Value)
                    {
                        badRegKeyArr.Add(child);
                    }
                }
            }

            return badRegKeyArr;
        }

        private void SetCheckedItems(Nullable<bool> isChecked)
        {
            List<BadRegistryKey> badRegKeyArr = new List<BadRegistryKey>();

            foreach (BadRegistryKey badRegKeyRoot in (this._tree.Model as ResultModel).Root.Children)
            {
                foreach (BadRegistryKey child in badRegKeyRoot.Children)
                {
                    if (!isChecked.HasValue)
                        child.IsChecked = !child.IsChecked;
                    else
                        child.IsChecked = isChecked.Value;
                }
            }

            return;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.scanWiz.MoveFirst();
        }

        private void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            FixProblems();
        }

        private void FixProblems()
        {
            List<BadRegistryKey> badRegKeys = GetSelectedRegKeys();

            long lSeqNum = 0;
            xmlWriter w = new xmlWriter();
            xmlRegistry xmlReg = new xmlRegistry();

            // Ask to remove registry keys
            if (!Properties.Settings.Default.registryCleanerOptionsAutoRepair)
                if (MessageBox.Show("Would you like to fix all selected problems?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

            // Disable buttons
            this.buttonCancel.IsEnabled = false;
            this.buttonFix.IsEnabled = false;

            // Set progress bar range
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = badRegKeys.Count;
            this.progressBar1.Value = 0;

            // Create Restore Point
            this.progressBarText.Text = "Creating system restore point";
            SysRestore.StartRestore("Before Little Registry Cleaner Registry Fix", out lSeqNum);

            // Generate filename to backup registry
            this.progressBarText.Text = "Creating backup file";
            string strBackupFile = string.Format("{0}\\{1:yyyy}_{1:MM}_{1:dd}_{1:HH}{1:mm}{1:ss}.bakx", Properties.Settings.Default.optionsBackupDir, DateTime.Now);

            // Write opening tags to Backup File
            if (!w.open(strBackupFile))
                return;

            xmlElement wroot = new xmlElement(xmlRegistry.XML_ROOT);
            wroot.write(w, 1, false, true);

            Properties.Settings.Default.lastScanErrorsFixed = 0;

            foreach (BadRegistryKey brk in badRegKeys)
            {
                // Backup & Delete key
                xmlReg.deleteAsXml(brk, w);

                // Set icon to check mark
                brk.bMapImg = new Image();
                brk.bMapImg.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.finished_scanning.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                this._tree.Items.Refresh();

                // Increase & Update progress bar
                this.progressBarText.Text = string.Format("Items Repaired: {0}/{1}", ++this.progressBar1.Value, this.progressBar1.Maximum);

                // Set last scan erors fixed
                Properties.Settings.Default.lastScanErrorsFixed++;
            }

            // Set total errors fixed
            Properties.Settings.Default.totalErrorsFixed += Properties.Settings.Default.lastScanErrorsFixed;

            // Write Closing Tag to Backup File
            wroot.writeClosingTag(w, -1, false, true);
            w.close();

            // Finish creating restore point
            SysRestore.EndRestore(lSeqNum);

            // If power user option selected -> automatically exit program
            if (Properties.Settings.Default.registryCleanerOptionsAutoExit)
            {
                Application.Current.Shutdown();
                return;
            }

            // Display message box and go back to first control
            MessageBox.Show("Removed problems from registry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            if (Properties.Settings.Default.registryCleanerOptionsAutoRescan)
                this.scanWiz.Rescan();
            else
                this.scanWiz.MoveFirst();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // If no errors found -> Go to first control
            if ((this._tree.Model as ResultModel).Root.Children.Count == 0)
                this.scanWiz.MoveFirst();

            this._tree.AutoResizeColumns();
        }

        private void contextMenuResults_Clicked(object sender, RoutedEventArgs e)
        {
            switch ((string)(sender as MenuItem).Header)
            {
                case "Select All":
                    {
                        SetCheckedItems(true);
                        break;
                    }
                case "Select None":
                    {
                        SetCheckedItems(false);
                        break;
                    }
                case "Invert Selection":
                    {
                        SetCheckedItems(null);
                        break;
                    }
                case "Exclude Selection":
                    {
                        if (this._tree.SelectedNode == null)
                        {
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, "No registry key is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        string regKeyPath = (this._tree.SelectedNode.Tag as BadRegistryKey).RegKeyPath;

                        ExcludeItem excludeItem = new ExcludeItem() { RegistryPath = regKeyPath };
                        if (!Properties.Settings.Default.arrayExcludeList.Contains(excludeItem))
                        {
                            Properties.Settings.Default.arrayExcludeList.Add(excludeItem);
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("Added registry key ({0}) to the exclude list", regKeyPath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                            this._tree.RemoveNode(this._tree.SelectedNode);
                        }
                        else
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, string.Format("Registry key ({0}) already exists", regKeyPath), Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }
                case "View In RegEdit":
                    {
                        if (this._tree.SelectedNode == null)
                        {
                            MessageBox.Show(System.Windows.Application.Current.MainWindow, "No registry key is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        string regKeyPath = (this._tree.SelectedNode.Tag as BadRegistryKey).RegKeyPath;
                        string regKeyValueName = (this._tree.SelectedNode.Tag as BadRegistryKey).ValueName;

                        RegEditGo.GoTo(regKeyPath, regKeyValueName);
                        break;
                    }
            }
        }
        
	}
}
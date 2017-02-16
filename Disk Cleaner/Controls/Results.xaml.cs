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

using Disk_Cleaner.Helpers;
using Shared;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Disk_Cleaner.Controls
{
    /// <summary>
    ///     Interaction logic for Results.xaml
    /// </summary>
    public partial class Results
    {
        private readonly Task _fixTask;

        public Wizard ScanBase;

        public Results(Wizard sb)
        {
            InitializeComponent();

            _fixTask = new Task(FixProblems);

            ScanBase = sb;

            ResetInfo();

            ListViewFiles.AutoResizeColumns();
        }

        public ObservableCollection<ProblemFile> ProblemsCollection => Wizard.FileList;

        private void listViewFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewFiles.SelectedItem != null)
            {
                var problemFile = ListViewFiles.SelectedItem as ProblemFile;

                if (problemFile == null)
                    return;

                var fileInfo = problemFile.FileInfo;

                // Get icon
                var fileIcon = System.Drawing.Icon.ExtractAssociatedIcon(fileInfo.FullName) ?? SystemIcons.Application;

                Icon.Source = Imaging.CreateBitmapSourceFromHBitmap(fileIcon.ToBitmap().GetHbitmap(), IntPtr.Zero,
                    Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                FileName.Text = fileInfo.Name;
                FileSize.Text = Utils.ConvertSizeToString(fileInfo.Length);
                Location.Text = fileInfo.DirectoryName;
                LastAccessed.Text = fileInfo.LastAccessTime.ToLongDateString();
            }
            else
            {
                ResetInfo();
            }
        }

        private void ResetInfo()
        {
            Icon.Source = Imaging.CreateBitmapSourceFromHBitmap(SystemIcons.Application.ToBitmap().GetHbitmap(),
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            FileName.Text = "N/A";
            FileSize.Text = "N/A";
            Location.Text = "N/A";
            LastAccessed.Text = "N/A";
        }

        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            ProblemsCollection.ToList().ForEach(lvi => lvi.Checked = true);

            ListViewFiles.Items.Refresh();
        }

        private void selectNone_Click(object sender, RoutedEventArgs e)
        {
            ProblemsCollection.ToList().ForEach(lvi => lvi.Checked = false);

            ListViewFiles.Items.Refresh();
        }

        private void selectInvert_Click(object sender, RoutedEventArgs e)
        {
            ProblemsCollection.ToList().ForEach(lvi => lvi.Checked = !lvi.Checked);

            ListViewFiles.Items.Refresh();
        }

        private async void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            var uncheckedFiles = ProblemsCollection.Count(lvi => !lvi.Checked.GetValueOrDefault());

            if (uncheckedFiles == ProblemsCollection.Count)
            {
                MessageBox.Show(Application.Current.MainWindow, "No files are selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Settings.Default.diskCleanerAutoClean)
                if (
                    MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to remove these files?",
                        Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

            Utils.Watcher.Event("Disk Cleaner", "Remove Files");

            _fixTask.Start();
            await _fixTask;

            MessageBox.Show(Application.Current.MainWindow, "Successfully cleaned files from disk", Utils.ProductName,
                MessageBoxButton.OK, MessageBoxImage.Information);

            ScanBase.MoveFirst();
        }

        private void FixProblems()
        {
            long lSeqNum = 0;

            try
            {
                SysRestore.StartRestore("Before Little System Cleaner (Disk Cleaner) Cleaning", out lSeqNum);
            }
            catch (Win32Exception ex)
            {
                string message = $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                Utils.MessageBoxThreadSafe(Application.Current.MainWindow, message, Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            foreach (var lvi in ProblemsCollection.Where(lvi => lvi.Checked.GetValueOrDefault()))
            {
                try
                {
                    var fileInfo = lvi.FileInfo;

                    // Make sure file exists
                    if (!fileInfo.Exists)
                        continue;

                    switch (Settings.Default.diskCleanerRemoveMode)
                    {
                        case 0:
                            // Remove permanately
                            fileInfo.Delete();
                            break;

                        case 1:
                            // Recycle file
                            SendFileToRecycleBin(fileInfo.FullName);
                            break;

                        default:
                            // Move file to specified directory
                            if (!Directory.Exists(Settings.Default.diskCleanerMoveFolder))
                                Directory.CreateDirectory(Settings.Default.diskCleanerMoveFolder);

                            File.Move(fileInfo.FullName, $@"{Settings.Default.diskCleanerMoveFolder}\{fileInfo.Name}");
                            break;
                    }
                }
                catch (Exception)
                {
                    //this.m_watcher.Exception(ex);
                }
            }

            if (lSeqNum != 0)
            {
                try
                {
                    SysRestore.EndRestore(lSeqNum);
                }
                catch (Win32Exception ex)
                {
                    string message =
                        $"Unable to create system restore point.\nThe following error occurred: {ex.Message}";
                    Utils.MessageBoxThreadSafe(Application.Current.MainWindow, message, Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_fixTask.Status == TaskStatus.Running)
            {
                MessageBox.Show(Application.Current.MainWindow, "Please wait for the problems to be fixed.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure you want to cancel?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            ResetInfo();
            Wizard.FileList.Clear();

            ScanBase.MoveFirst();
        }

        private static void SendFileToRecycleBin(string filePath)
        {
            var shf = new PInvoke.ShFileOpStruct
            {
                wFunc = PInvoke.FoDelete,
                fFlags = PInvoke.FofAllowundo | PInvoke.FofNoconfirmation,
                pFrom = filePath
            };
            PInvoke.SHFileOperation(ref shf);
        }
    }
}
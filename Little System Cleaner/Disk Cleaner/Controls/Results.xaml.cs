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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Little_System_Cleaner.Disk_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Results.xaml
    /// </summary>
    public partial class Results : UserControl
    {
        public Wizard scanBase;

        public ObservableCollection<ProblemFile> ProblemsCollection {
            get 
            {
                return Wizard.fileList;
            }
        }

        public Results(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            // Update last scan stats
            long elapsedTime = DateTime.Now.Subtract(Wizard.ScanStartTime).Ticks;

            Properties.Settings.Default.lastScanElapsed = elapsedTime;

            this.ResetInfo();

            Utils.AutoResizeColumns(this.listViewFiles);
        }

        private void listViewFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.listViewFiles.SelectedItem != null)
            {
                FileInfo fileInfo = (this.listViewFiles.SelectedItem as ProblemFile).FileInfo;

                // Get icon
                Icon fileIcon = System.Drawing.Icon.ExtractAssociatedIcon(fileInfo.FullName);
                if (fileIcon == null)
                    fileIcon = SystemIcons.Application;

                this.Icon.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(fileIcon.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                this.FileName.Text = "File Name: " + fileInfo.Name;
                this.FileSize.Text = "File Size: " + Utils.ConvertSizeToString(fileInfo.Length);
                this.Location.Text = "Location: " + fileInfo.DirectoryName;
                this.LastAccessed.Text = "Last Accessed: " + fileInfo.LastAccessTime.ToLongDateString();
            }
            else
            {
                this.ResetInfo();
            }
        }

        private void ResetInfo()
        {
            this.Icon.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(System.Drawing.SystemIcons.Application.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            this.FileName.Text = "File Name: N/A";
            this.FileSize.Text = "File Size: N/A";
            this.Location.Text = "Location: N/A";
            this.LastAccessed.Text = "Last Accessed: N/A";
        }

        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (ProblemFile lvi in this.ProblemsCollection)
            {
                lvi.Checked = true;
            }

            this.listViewFiles.Items.Refresh();
        }

        private void selectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (ProblemFile lvi in this.ProblemsCollection)
            {
                lvi.Checked = false;
            }

            this.listViewFiles.Items.Refresh();
        }

        private void selectInvert_Click(object sender, RoutedEventArgs e)
        {
            foreach (ProblemFile lvi in this.ProblemsCollection)
            {
                lvi.Checked = !(lvi.Checked);
            }

            this.listViewFiles.Items.Refresh();
        }

        private void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            int uncheckedFiles = 0;

            foreach (ProblemFile lvi in this.ProblemsCollection)
            {
                if (!lvi.Checked.GetValueOrDefault())
                {
                    uncheckedFiles++;
                }
            }

            if (uncheckedFiles == this.ProblemsCollection.Count)
            {
                MessageBox.Show(Window.GetWindow(this), "No files are selected", System.Windows.Forms.Application.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Properties.Settings.Default.diskCleanerAutoClean)
                if (MessageBox.Show(Window.GetWindow(this), "Are you sure you want to remove these files?", System.Windows.Forms.Application.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                    return;

            long lSeqNum = 0;
            SysRestore.StartRestore("Before Little Disk Cleaner Cleaning", out lSeqNum);

            foreach (ProblemFile lvi in this.ProblemsCollection)
            {
                if (!lvi.Checked.GetValueOrDefault())
                    continue;

                try
                {
                    FileInfo fileInfo = lvi.FileInfo;

                    // Set last scan erors fixed
                    Properties.Settings.Default.lastScanErrorsFixed++;

                    // Make sure file exists
                    if (!fileInfo.Exists)
                        continue;

                    if (Properties.Settings.Default.diskCleanerRemoveMode == 0)
                    {
                        // Remove permanately
                        fileInfo.Delete();
                    }
                    else if (Properties.Settings.Default.diskCleanerRemoveMode == 1)
                    {
                        // Recycle file
                        Utils.SendFileToRecycleBin(fileInfo.FullName);
                    }
                    else
                    {
                        // Move file to specified directory
                        if (!Directory.Exists(Properties.Settings.Default.diskCleanerMoveFolder))
                            Directory.CreateDirectory(Properties.Settings.Default.diskCleanerMoveFolder);

                        File.Move(fileInfo.FullName, string.Format(@"{0}\{1}", Properties.Settings.Default.diskCleanerMoveFolder, fileInfo.Name));
                    }
                }
                catch (Exception ex)
                {
                    //this.m_watcher.Exception(ex);
                }
            }

            if (lSeqNum != 0)
            {
                SysRestore.EndRestore(lSeqNum);
            }

            MessageBox.Show(Window.GetWindow(this), "Successfully cleaned files from disk", System.Windows.Forms.Application.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

            this.scanBase.MoveFirst();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.ResetInfo();
            Wizard.fileList.Clear();

            this.scanBase.MoveFirst();
        }
    }

    public class ProblemFile
    {
        private FileInfo _fileInfo;

        public bool? Checked
        {
            get;
            set;
        }

        public string Name
        {
            get
            {
                return this.FileInfo.Name;
            }
        }

        public string Location
        {
            get
            {
                return this.FileInfo.DirectoryName;
            }
        }

        public string Size
        {
            get
            {
                return Utils.ConvertSizeToString(this.FileInfo.Length);
            }
        }

        public FileInfo FileInfo
        {
            get
            {
                return this._fileInfo;
            }
        }

        public ProblemFile(FileInfo fi)
        {
            this.Checked = true;
            this._fileInfo = fi;
        }
    }
}

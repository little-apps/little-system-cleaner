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
using System.Diagnostics;
using Little_System_Cleaner.Xml;

namespace Little_System_Cleaner.Controls
{
	public partial class Restore
	{
        ObservableCollection<RestoreFile> _restoreFiles = new ObservableCollection<RestoreFile>();

        public ObservableCollection<RestoreFile> RestoreFiles {
            get { return _restoreFiles; }
        }

		public Restore()
		{
			this.InitializeComponent();

            this.listViewFiles.ItemsSource = RestoreFiles;

            PopulateListView();
		}

        private void PopulateListView()
        {
            DirectoryInfo di = new DirectoryInfo(Properties.Settings.Default.optionsBackupDir);

            // If directory doesnt exist -> create it
            if (!di.Exists)
                di.Create();

            // If list is already populated -> clear it
            if (RestoreFiles.Count > 0)
                RestoreFiles.Clear();

            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.Extension.CompareTo(".bakx") == 0)
                {
                    this._restoreFiles.Add(new RestoreFile(fi));
                }
            }

            // Refresh listview
            this.listViewFiles.Items.Refresh();

            // Auto resize columns
            if (this.listViewFiles.Items.Count > 0)
                Utils.AutoResizeColumns(this.listViewFiles);
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Properties.Settings.Default.optionsBackupDir);
        }

        private void buttonRestore_Click(object sender, RoutedEventArgs e)
        {
            long lSeqNum = 0;
            xmlReader xmlReader = new xmlReader();
            xmlRegistry xmlReg = new xmlRegistry();

            if (this.listViewFiles.SelectedItem == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "No restore file selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            SysRestore.StartRestore("Before Little Registry Cleaner Restore", out lSeqNum);

            if (xmlReg.loadAsXml(xmlReader, (this.listViewFiles.SelectedItem as RestoreFile).FileInfo.FullName))
            {
                MessageBox.Show(Application.Current.MainWindow, "Successfully restored registry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                if (Properties.Settings.Default.registryCleanerOptionsDelBackup)
                {
                    // Delete file
                    (this.listViewFiles.SelectedItem as RestoreFile).FileInfo.Delete();

                    // Remove from listview and refresh
                    RestoreFiles.Remove(this.listViewFiles.SelectedItem as RestoreFile);
                    PopulateListView();
                }
            } else
                MessageBox.Show(Application.Current.MainWindow, "Error restoring the registry", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

            SysRestore.EndRestore(lSeqNum);
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateListView();
        }
	}

    public class RestoreFile 
    {
        FileInfo _fileInfo;
        string _file, _date, _size;

        public FileInfo FileInfo
        {
            get { return _fileInfo; }
        }

        public string File 
        { 
            get { return _file; }
        }
        public string Date 
        { 
            get { return _date; }
        }
        public string Size 
        { 
            get { return _size; }
        }

        public RestoreFile(FileInfo fileInfo) {
            _fileInfo = fileInfo;
            _file = fileInfo.Name;
            _date = fileInfo.CreationTime.ToString();
            _size = Utils.ConvertSizeToString((uint)fileInfo.Length);
        }
    }
}
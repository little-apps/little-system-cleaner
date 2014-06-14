using Little_System_Cleaner.Duplicate_Finder.Helpers;
using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : UserControl
    {
        private readonly Wizard scanBase;

        public Start(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            this.DataContext = this.scanBase.Options;

            if (this.scanBase.Options.Drives.Count > 0)
                this.scanBase.Options.Drives.Clear();

            if (this.scanBase.Options.IncFolders.Count > 0)
                this.scanBase.Options.IncFolders.Clear();

            try {
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    this.scanBase.Options.Drives.Add(new IncludeDrive(di));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine("The following error occurred: {0}\nUnable to get list of drives.", ex.Message);
            }

            this.scanBase.Options.SkipTempFiles = true;
            this.scanBase.Options.SkipSysAppDirs = true;
            this.scanBase.Options.SkipWindowsDir = true;

            this.scanBase.Options.OnPropertyChanged("SkipFilesGreaterThan");
            this.scanBase.Options.OnPropertyChanged("SkipFilesGreaterSize");
            this.scanBase.Options.OnPropertyChanged("SkipFilesGreaterUnit");

            this.scanBase.Options.HashAlgorithms = HashAlgorithm.CreateList();
            this.scanBase.Options.HashAlgorithm = this.scanBase.Options.HashAlgorithms[0];
        }

        private void excludeFolderAdd_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowser.Description = "Select a folder to exclude from check for duplicate files";

                if (folderBrowser.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) == System.Windows.Forms.DialogResult.OK)
                {
                    ExcludeFolder excFolder = new ExcludeFolder(folderBrowser.SelectedPath);

                    if (this.scanBase.Options.ExcludeFolders.Contains(excFolder))
                        MessageBox.Show(App.Current.MainWindow, "The selected folder is already excluded", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    else if (this.scanBase.Options.OnlySelectedFolders.GetValueOrDefault() == true && this.scanBase.Options.IncFolders.Contains(new IncludeFolder(folderBrowser.SelectedPath)))
                        MessageBox.Show(App.Current.MainWindow, "The selected folder cannot be in both the included and excluded folders", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                    {
                        this.scanBase.Options.ExcludeFolders.Add(excFolder);
                        MessageBox.Show(App.Current.MainWindow, "The selected folder has been excluded from the search", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void excludeFolderDel_Click(object sender, RoutedEventArgs e)
        {
            if (this.scanBase.Options.ExcludeFolderSelected == null)
            {
                MessageBox.Show(App.Current.MainWindow, "No folder is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.scanBase.Options.ExcludeFolderSelected.ReadOnly)
            {
                MessageBox.Show(App.Current.MainWindow, "This folder has been excluded in order to protect critical files from being deleted. Please uncheck the respective checkbox under Options in the Files tab to remove it.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                this.tabControl.SelectedIndex = 1;
            }
            else
            {
                if (MessageBox.Show(App.Current.MainWindow, "Are you sure you want to remove this directory from the excluded folders?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    string message = string.Format("The folder ({0}) has been removed from the excluded folders.", this.scanBase.Options.ExcludeFolderSelected.FolderPath);
                    this.scanBase.Options.ExcludeFolders.Remove(this.scanBase.Options.ExcludeFolderSelected);

                    MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            
        }

        private void addFolder_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowser.Description = "Select a folder to check for duplicate files";

                if (folderBrowser.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) == System.Windows.Forms.DialogResult.OK)
                {
                    IncludeFolder incFolder = new IncludeFolder(folderBrowser.SelectedPath);

                    if (this.scanBase.Options.IncFolders.Contains(incFolder))
                        MessageBox.Show(App.Current.MainWindow, "The selected folder is already included", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    else if (this.scanBase.Options.ExcludeFolders.Contains(new ExcludeFolder(folderBrowser.SelectedPath)))
                        MessageBox.Show(App.Current.MainWindow, "The selected folder cannot be in both the included and excluded folders", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                    {
                        this.scanBase.Options.IncFolders.Add(incFolder);
                        MessageBox.Show(App.Current.MainWindow, "The selected folder has been included in the search", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void removeFolder_Click(object sender, RoutedEventArgs e)
        {
            if (this.scanBase.Options.IncludeFolderSelected == null)
            {
                MessageBox.Show(App.Current.MainWindow, "No folder is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(App.Current.MainWindow, "Are you sure you want to remove this directory from the included folders?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string message = string.Format("The folder ({0}) has been removed from the included folders.", this.scanBase.Options.IncludeFolderSelected.Name);
                this.scanBase.Options.IncFolders.Remove(this.scanBase.Options.IncludeFolderSelected);

                MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            bool canContinue = false;

            if (this.scanBase.Options.OnlySelectedDrives.GetValueOrDefault())
            {
                if (this.scanBase.Options.Drives.Count == 0)
                {
                    MessageBox.Show(App.Current.MainWindow, "There seems to have been an error detecting drives to scan", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (IncludeDrive drive in this.scanBase.Options.Drives)
                {
                    if (drive.IsChecked.GetValueOrDefault())
                    {
                        canContinue = true;
                        break;
                    }
                }

                if (!canContinue)
                {
                    MessageBox.Show(App.Current.MainWindow, "You must select at least one drive in order to start the scan", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (this.scanBase.Options.OnlySelectedFolders.GetValueOrDefault())
            {
                if (this.scanBase.Options.IncFolders.Count == 0)
                    canContinue = false;
                else
                {
                    foreach (IncludeFolder dir in this.scanBase.Options.IncFolders)
                    {
                        if (dir.IsChecked.GetValueOrDefault())
                        {
                            canContinue = true;
                            break;
                        }
                    }
                }

                if (!canContinue)
                {
                    MessageBox.Show(App.Current.MainWindow, "You must select at least one folder in order to start the scan", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (this.scanBase.Options.CompareMusicTags.GetValueOrDefault())
            {
                if (!this.scanBase.Options.MusicTagAlbum.GetValueOrDefault()
                    && !this.scanBase.Options.MusicTagArtist.GetValueOrDefault()
                    && !this.scanBase.Options.MusicTagBitRate.GetValueOrDefault()
                    && !this.scanBase.Options.MusicTagDuration.GetValueOrDefault()
                    && !this.scanBase.Options.MusicTagGenre.GetValueOrDefault()
                    && !this.scanBase.Options.MusicTagTitle.GetValueOrDefault()
                    && !this.scanBase.Options.MusicTagTrackNo.GetValueOrDefault()
                    && !this.scanBase.Options.MusicTagYear.GetValueOrDefault())
                {
                    MessageBox.Show(App.Current.MainWindow, "You must select at least one music tag to compare in order to start the scan", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            this.scanBase.MoveNext();
        }

        
    }
}

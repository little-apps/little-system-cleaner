using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Duplicate_Finder.Helpers;
using Shared;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Duplicate_Finder.Controls
{
    /// <summary>
    ///     Interaction logic for Start.xaml
    /// </summary>
    public partial class Start
    {
        private readonly Wizard _scanBase;

        public Start(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;

            DataContext = _scanBase.Options;

            // Get drives that are checked
            var drivesChecked =
                _scanBase.Options.Drives.Where(includeDrive => includeDrive.IsChecked.GetValueOrDefault()).ToList();

            // Clear drives
            _scanBase.Options.Drives.Clear();

            try
            {
                _scanBase.Options.Drives.AddRange(DriveInfo.GetDrives()
                    .Select(di => new IncludeDrive(di))
                    // Iterate through drives and check drive if checked in previous list
                    .Select(
                        includeDriveToAdd => new IncludeDrive { IsChecked = drivesChecked.Contains(includeDriveToAdd), Name = includeDriveToAdd.Name }));
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine("The following error occurred: {0}\nUnable to get list of drives.", ex.Message);
            }

            // Only include directories that exist
            if (_scanBase.Options.IncFolders.Count > 0)
            {
                _scanBase.Options.IncFolders =
                    new ObservableCollection<IncludeFolder>(
                        _scanBase.Options.IncFolders.Where(includeFolder => Directory.Exists(includeFolder.Name)));
            }

            _scanBase.Options.SkipTempFiles = true;
            _scanBase.Options.SkipSysAppDirs = true;
            _scanBase.Options.SkipWindowsDir = true;

            _scanBase.Options.OnPropertyChanged(nameof(_scanBase.Options.SkipFilesGreaterThan));
            _scanBase.Options.OnPropertyChanged(nameof(_scanBase.Options.SkipFilesGreaterSize));
            _scanBase.Options.OnPropertyChanged(nameof(_scanBase.Options.SkipFilesGreaterUnit));

            _scanBase.Options.HashAlgorithms = HashAlgorithm.CreateList();
            _scanBase.Options.HashAlgorithm = _scanBase.Options.HashAlgorithms[2]; // SHA1
        }

        private void excludeFolderAdd_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.Description = "Select a folder to exclude from check for duplicate files";

                if (folderBrowser.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) != DialogResult.OK)
                    return;

                var excFolder = new ExcludeFolder(folderBrowser.SelectedPath);

                if (_scanBase.Options.ExcludeFolders.Contains(excFolder))
                    MessageBox.Show(Application.Current.MainWindow, "The selected folder is already excluded",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                else if (_scanBase.Options.OnlySelectedFolders.GetValueOrDefault() &&
                         _scanBase.Options.IncFolders.Contains(new IncludeFolder(folderBrowser.SelectedPath)))
                    MessageBox.Show(Application.Current.MainWindow,
                        "The selected folder cannot be in both the included and excluded folders", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    _scanBase.Options.ExcludeFolders.Add(excFolder);

                    MessageBox.Show(Application.Current.MainWindow,
                        "The selected folder has been excluded from the search", Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void excludeFolderDel_Click(object sender, RoutedEventArgs e)
        {
            if (_scanBase.Options.ExcludeFolderSelected == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "No folder is selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_scanBase.Options.ExcludeFolderSelected.ReadOnly)
            {
                MessageBox.Show(Application.Current.MainWindow,
                    "This folder has been excluded in order to protect critical files from being deleted. Please uncheck the respective checkbox under Options in the Files tab to remove it.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                TabControl.SelectedIndex = 1;
            }
            else
            {
                if (
                    MessageBox.Show(Application.Current.MainWindow,
                        "Are you sure you want to remove this directory from the excluded folders?", Utils.ProductName,
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                string message =
                    $"The folder ({_scanBase.Options.ExcludeFolderSelected.FolderPath}) has been removed from the excluded folders.";
                _scanBase.Options.ExcludeFolders.Remove(_scanBase.Options.ExcludeFolderSelected);

                MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void addFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.Description = "Select a folder to check for duplicate files";

                if (folderBrowser.ShowDialog(WindowWrapper.GetCurrentWindowHandle()) != DialogResult.OK)
                    return;

                var incFolder = new IncludeFolder(folderBrowser.SelectedPath);

                if (_scanBase.Options.IncFolders.Contains(incFolder))
                    MessageBox.Show(Application.Current.MainWindow, "The selected folder is already included",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                else if (_scanBase.Options.ExcludeFolders.Contains(new ExcludeFolder(folderBrowser.SelectedPath)))
                    MessageBox.Show(Application.Current.MainWindow,
                        "The selected folder cannot be in both the included and excluded folders", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    _scanBase.Options.IncFolders.Add(incFolder);

                    MessageBox.Show(Application.Current.MainWindow,
                        "The selected folder has been included in the search", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void removeFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_scanBase.Options.IncludeFolderSelected == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "No folder is selected", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (
                MessageBox.Show(Application.Current.MainWindow,
                    "Are you sure you want to remove this directory from the included folders?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            string message =
                $"The folder ({_scanBase.Options.IncludeFolderSelected.Name}) has been removed from the included folders.";
            _scanBase.Options.IncFolders.Remove(_scanBase.Options.IncludeFolderSelected);

            MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            bool canContinue;

            if (_scanBase.Options.OnlySelectedDrives.GetValueOrDefault())
            {
                if (_scanBase.Options.Drives.Count == 0)
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "There seems to have been an error detecting drives to scan", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                canContinue = _scanBase.Options.Drives.Any(drive => drive.IsChecked.GetValueOrDefault());

                if (!canContinue)
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "You must select at least one drive in order to start the scan", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (_scanBase.Options.OnlySelectedFolders.GetValueOrDefault())
            {
                canContinue = _scanBase.Options.IncFolders.Count != 0 &&
                              _scanBase.Options.IncFolders.Any(dir => dir.IsChecked.GetValueOrDefault());

                if (!canContinue)
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "You must select at least one folder in order to start the scan", Utils.ProductName,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (_scanBase.Options.CompareMusicTags.GetValueOrDefault())
            {
                if (!_scanBase.Options.MusicTagAlbum.GetValueOrDefault()
                    && !_scanBase.Options.MusicTagArtist.GetValueOrDefault()
                    && !_scanBase.Options.MusicTagBitRate.GetValueOrDefault()
                    && !_scanBase.Options.MusicTagDuration.GetValueOrDefault()
                    && !_scanBase.Options.MusicTagGenre.GetValueOrDefault()
                    && !_scanBase.Options.MusicTagTitle.GetValueOrDefault()
                    && !_scanBase.Options.MusicTagTrackNo.GetValueOrDefault()
                    && !_scanBase.Options.MusicTagYear.GetValueOrDefault())
                {
                    MessageBox.Show(Application.Current.MainWindow,
                        "You must select at least one music tag to compare in order to start the scan",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            UserOptions.StoreUserOptions(_scanBase.Options);

            _scanBase.MoveNext();
        }
    }
}
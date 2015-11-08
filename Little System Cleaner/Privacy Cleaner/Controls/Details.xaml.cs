using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Details.xaml
    /// </summary>
    public partial class Details
    {
        #region ShellExecuteEx
        internal static int SW_SHOW = 5;
        internal static uint SEE_MASK_INVOKEIDLIST = 12;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);
        #endregion

        public ObservableCollection<DetailItem> DetailItemCollection { get; } = new ObservableCollection<DetailItem>();

        private readonly Wizard _scanBase;

        public Details(Wizard scanBase, ResultNode resultNode)
        {
            InitializeComponent();

            if (resultNode == null)
                throw new ObjectDisposedException("resultNode");

            _scanBase = scanBase;

            if (resultNode.FilePaths != null)
            {
                DetailItemCollection.AddRange(resultNode.FilePaths.Select(
                    filePath =>
                        new DetailItem
                        {
                            Name = filePath,
                            Size = Utils.ConvertSizeToString(MiscFunctions.GetFileSize(filePath)),
                            AccessDate = Directory.GetLastAccessTime(filePath).ToString()
                        }));
            }

            if (resultNode.FolderPaths != null)
            {
                DetailItemCollection.AddRange(
                    resultNode.FolderPaths.Select(kvp => kvp.Key)
                        .Select(
                            folderPath =>
                                new DetailItem
                                {
                                    Name = folderPath,
                                    Size = Utils.ConvertSizeToString(MiscFunctions.GetFolderSize(folderPath, false)),
                                    AccessDate = Directory.GetLastAccessTime(folderPath).ToString()
                                }));
            }

            ListView.AutoResizeColumns();
        }

        private void ButtonGoBack_Click(object sender, RoutedEventArgs e)
        {
            _scanBase.HideDetails();
        }

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            var detailItem = ListView.SelectedItem as DetailItem;
            if (detailItem == null)
                return;

            var path = detailItem.Name;

            if (!File.Exists(path))
                return;

            if (
                MessageBox.Show(Application.Current.MainWindow, "Are you sure?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var startInfo = new ProcessStartInfo
            {
                ErrorDialog = true,
                FileName = path
            };

            Process.Start(startInfo);
        }

        private void ButtonLocate_Click(object sender, RoutedEventArgs e)
        {
            var detailItem = ListView.SelectedItem as DetailItem;
            if (detailItem == null)
                return;

            var path = detailItem.Name;

            Process.Start("explorer", Path.GetDirectoryName(path));
        }

        private void ButtonViewProperties_Click(object sender, RoutedEventArgs e)
        {
            var detailItem = ListView.SelectedItem as DetailItem;
            if (detailItem == null)
                return;

            var path = detailItem.Name;

            var info = new SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = (string)path.Clone();
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            ShellExecuteEx(ref info);
        }
    }
}

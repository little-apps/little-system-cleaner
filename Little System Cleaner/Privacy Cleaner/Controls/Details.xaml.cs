using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Details.xaml
    /// </summary>
    public partial class Details : UserControl
    {
        private ObservableCollection<DetailItem> _detailItemCollection = new ObservableCollection<DetailItem>();
        public ObservableCollection<DetailItem> DetailItemCollection
        {
            get { return _detailItemCollection; }
        }

        private Wizard _scanBase;

        public Details(Wizard scanBase, ResultNode resultNode)
        {
            InitializeComponent();

            if (resultNode == null)
                throw new ObjectDisposedException("resultNode");

            this._scanBase = scanBase;

            if (resultNode.FilePaths != null)
            {
                foreach (string filePath in resultNode.FilePaths)
                {
                    string fileSize = Little_System_Cleaner.Misc.Utils.ConvertSizeToString(MiscFunctions.GetFileSize(filePath));
                    string lastAccessDate = Directory.GetLastAccessTime(filePath).ToString();

                    this.DetailItemCollection.Add(new DetailItem() { Name = filePath, Size = fileSize, AccessDate = lastAccessDate });
                }
            }

            if (resultNode.FolderPaths != null)
            {
                foreach (KeyValuePair<string, bool> kvp in resultNode.FolderPaths)
                {
                    string folderPath = kvp.Key;
                    //SearchOption recurse = ((kvp.Value)?(SearchOption.AllDirectories):(SearchOption.TopDirectoryOnly));

                    string folderSize = Little_System_Cleaner.Misc.Utils.ConvertSizeToString(MiscFunctions.GetFolderSize(folderPath, false));
                    string lastAccessDate = Directory.GetLastAccessTime(folderPath).ToString();

                    this.DetailItemCollection.Add(new DetailItem() { Name = folderPath, Size = folderSize, AccessDate = lastAccessDate });
                }
            }

            Little_System_Cleaner.Misc.Utils.AutoResizeColumns(this.listView);
        }

        private void btnGoBack_Click(object sender, RoutedEventArgs e)
        {
            this._scanBase.HideDetails();
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (this.listView.SelectedItem == null)
                return;

            string path = (this.listView.SelectedItem as DetailItem).Name;

            if (!File.Exists(path))
                return;

            if (MessageBox.Show(App.Current.MainWindow, "Are you sure?", "Little Privacy Cleaner", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.ErrorDialog = true;
                startInfo.FileName = path;
                System.Diagnostics.Process.Start(startInfo);
            }
        }

        private void btnLocate_Click(object sender, RoutedEventArgs e)
        {
            if (this.listView.SelectedItem == null)
                return;

            string path = (this.listView.SelectedItem as DetailItem).Name;

            System.Diagnostics.Process.Start("explorer", Path.GetDirectoryName(path));
        }

        private void btnViewProperties_Click(object sender, RoutedEventArgs e)
        {
            if (this.listView.SelectedItem == null)
                return;

            string path = (this.listView.SelectedItem as DetailItem).Name;

            PInvoke.SHELLEXECUTEINFO info = new PInvoke.SHELLEXECUTEINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = (string)path.Clone();
            info.nShow = PInvoke.SW_SHOW;
            info.fMask = PInvoke.SEE_MASK_INVOKEIDLIST;
            PInvoke.ShellExecuteEx(ref info);
        }
    }
}

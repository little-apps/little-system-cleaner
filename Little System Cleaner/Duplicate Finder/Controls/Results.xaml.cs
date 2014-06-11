using Little_System_Cleaner.Duplicate_Finder.Helpers;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    /// Interaction logic for Results.xaml
    /// </summary>
    public partial class Results : UserControl
    {
        private Wizard scanBase;

        public ResultModel DuplicateFiles
        {
            get { return (this._tree.Model as ResultModel); }
        }

        public Results(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            this._tree.Model = ResultModel.CreateResultModel(sb);
            this._tree.ExpandAll();

            Utils.AutoResizeColumns(this._tree);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            List<FileEntry> files = new List<FileEntry>();

            foreach (Result resParent in (this._tree.Model as ResultModel).Root.Children)
            {
                if (resParent.Children.Count > 0)
                {
                    foreach (Result resChild in resParent.Children)
                    {
                        if (resChild.IsChecked.GetValueOrDefault())
                            files.Add(resChild.FileEntry);
                    }
                }
            }

            if (files.Count == 0)
            {
                MessageBox.Show(App.Current.MainWindow, "No files were selected to be removed. If you would like to not remove any files, please click cancel.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(App.Current.MainWindow, "Are you sure you want to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.scanBase.MoveFirst();
            }
        }

        private void _tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this._tree.SelectedNode == null)
            {
                MessageBox.Show(App.Current.MainWindow, "Nothing is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                this.ShowDetails();
            }
        }

        private void ShowDetails()
        {
            if (this._tree.SelectedNode == null)
                return;

            Result resultNode = this._tree.SelectedNode.Tag as Result;

            if (resultNode.Children.Count > 0 || resultNode.FileEntry == null)
                return;

            this.scanBase.ShowFileInfo(resultNode.FileEntry);
        }
    }
}

using Little_System_Cleaner.Duplicate_Finder.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    /// Interaction logic for LoadingResults.xaml
    /// </summary>
    public partial class LoadingResults : Window
    {
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        private Wizard _scanBase;
        private CommonTools.TreeListView.Tree.TreeList _tree;
        private ResultModel _resultModel = null;

        public ResultModel Model
        {
            get { return this._resultModel; }
        }

        public LoadingResults(Wizard scanBase, CommonTools.TreeListView.Tree.TreeList treeListView)
        {
            InitializeComponent();

            this._scanBase = scanBase;
            this._tree = treeListView;
        }

        private void LoadingResults_Loaded(object sender, RoutedEventArgs e)
        {
            this.backgroundWorker.DoWork += backgroundWorker_DoWork;
            this.backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;

            this.backgroundWorker.RunWorkerAsync(this._scanBase);
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null && (bool)e.Result)
            {
                this.DialogResult = true;
            }
            else
            {
                this.DialogResult = false;
            }

            this.Close();
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            this._resultModel = ResultModel.CreateResultModel((Wizard)e.Argument);

            this.Dispatcher.Invoke(new Action(() => this._tree.Model = this._resultModel));

            this._tree.ExpandAll();

            e.Result = true;
        }

        private void LoadingResults_Closing(object sender, CancelEventArgs e)
        {
            if (this.backgroundWorker.IsBusy && !this.backgroundWorker.CancellationPending)
            {
                this.backgroundWorker.CancelAsync();

                this.DialogResult = false;
            }
        }

        
    }
}

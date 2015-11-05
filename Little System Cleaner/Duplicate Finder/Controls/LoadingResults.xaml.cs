using System;
using System.ComponentModel;
using System.Windows;
using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Duplicate_Finder.Helpers;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    /// <summary>
    /// Interaction logic for LoadingResults.xaml
    /// </summary>
    public partial class LoadingResults
    {
        private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
        private readonly Wizard _scanBase;
        private readonly TreeList _tree;

        public ResultModel Model { get; private set; }

        public LoadingResults(Wizard scanBase, TreeList treeListView)
        {
            this.HideIcon();

            InitializeComponent();

            _scanBase = scanBase;
            _tree = treeListView;
        }

        private void LoadingResults_Loaded(object sender, RoutedEventArgs e)
        {
            this.HideCloseButton();

            _backgroundWorker.DoWork += backgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;

            _backgroundWorker.RunWorkerAsync(_scanBase);
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null && (bool)e.Result)
            {
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }

            Close();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Model = ResultModel.CreateResultModel((Wizard)e.Argument);

            Dispatcher.Invoke(new Action(() => _tree.Model = Model));

            _tree.ExpandAll();

            e.Result = true;
        }

        private void LoadingResults_Closing(object sender, CancelEventArgs e)
        {
            if (!_backgroundWorker.IsBusy || _backgroundWorker.CancellationPending)
                return;

            _backgroundWorker.CancelAsync();

            DialogResult = false;
        }

        
    }
}

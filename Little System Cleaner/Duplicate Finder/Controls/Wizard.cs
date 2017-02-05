using Little_System_Cleaner.Duplicate_Finder.Helpers;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Shared;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    public class Wizard : WizardBase
    {
        private Dictionary<string, List<FileEntry>> _filesGroupedByFilename;
        private Dictionary<string, List<FileEntry>> _filesGroupedByHash;
        private UserControl _savedControl;

        public Wizard()
        {
            Options = UserOptions.GetUserOptions();

            Controls.Add(typeof(Start));
            Controls.Add(typeof(Scan));
            Controls.Add(typeof(Results));
        }

        public UserOptions Options { get; }

        public Dictionary<string, List<FileEntry>> FilesGroupedByFilename => _filesGroupedByFilename ??
                                                                             (_filesGroupedByFilename =
                                                                                 new Dictionary<string, List<FileEntry>>
                                                                                     ());

        public Dictionary<string, List<FileEntry>> FilesGroupedByHash => _filesGroupedByHash ??
                                                                         (_filesGroupedByHash =
                                                                             new Dictionary<string, List<FileEntry>>());

        public ResultModel Results { get; set; }

        public override void OnLoaded()
        {
            SetCurrentControl(0);
        }

        public override bool OnUnloaded(bool forceExit)
        {
            bool exit;

            var start = CurrentControl as Start;
            if (start != null)
            {
                UserOptions.StoreUserOptions(Options);

                return true;
            }

            var scan = CurrentControl as Scan;
            if (scan != null)
            {
                exit = forceExit ||
                       MessageBox.Show("Would you like to cancel the scan that's in progress?", Utils.ProductName,
                           MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (!exit)
                    return false;

                scan.AbortScanTask();

                return true;
            }

            if (!(CurrentControl is Results))
                return true;

            exit = forceExit ||
                   MessageBox.Show("Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo,
                       MessageBoxImage.Question) == MessageBoxResult.Yes;

            return exit;
        }

        public void ShowFileInfo(FileEntry fileEntry)
        {
            if (CurrentControl is Details)
                HideFileInfo();

            _savedControl = CurrentControl;

            var fileInfoCntrl = new Details(this, fileEntry);
            Content = fileInfoCntrl;
        }

        public void HideFileInfo()
        {
            if (CurrentControl is Results)
                return;

            if (_savedControl == null)
            {
                MessageBox.Show(Application.Current.MainWindow,
                    "An error occurred going back to the results. The scan process will need to be restarted.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                MoveFirst();

                return;
            }

            Content = _savedControl;

            _savedControl = null;
        }
    }
}
using Little_System_Cleaner.Duplicate_Finder.Helpers;
using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    public class Wizard : WizardBase
    {
        private UserControl savedControl;
        private readonly UserOptions _options;

        private Dictionary<string, List<FileEntry>> _filesGroupedByFilename;
        private Dictionary<string, List<FileEntry>> _filesGroupedByHash;

        public UserOptions Options
        {
            get { return this._options; }
        }

        public Dictionary<string, List<FileEntry>> FilesGroupedByFilename
        {
            get
            {
                if (this._filesGroupedByFilename == null)
                    this._filesGroupedByFilename = new Dictionary<string, List<FileEntry>>();

                return this._filesGroupedByFilename;
            }
        }
        public Dictionary<string, List<FileEntry>> FilesGroupedByHash
        {
            get
            {
                if (this._filesGroupedByHash == null)
                    this._filesGroupedByHash = new Dictionary<string, List<FileEntry>>();

                return this._filesGroupedByHash;
            }
        }

        public Wizard()
        {
            this._options = new UserOptions();

            this.Controls.Add(typeof(Start));
            this.Controls.Add(typeof(Scan));
            this.Controls.Add(typeof(Results));
        }

        public override void OnLoaded()
        {
            this.SetCurrentControl(0);
        }

        public override bool OnUnloaded(bool forceExit)
        {
            bool exit;

            if (this.CurrentControl is Scan)
            {
                exit = (forceExit ? true : MessageBox.Show("Would you like to cancel the scan that's in progress?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);

                if (exit)
                {
                    (this.CurrentControl as Scan).AbortScanThread();

                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (this.CurrentControl is Results)
            {
                exit = (forceExit ? true : MessageBox.Show("Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);

                if (exit)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }


            return true;
        }

        public void ShowFileInfo(FileEntry fileEntry)
        {
            if (this.CurrentControl is Details)
                this.HideFileInfo();

            this.savedControl = this.CurrentControl;

            Details fileInfoCntrl = new Details(this, fileEntry);
            this.Content = fileInfoCntrl;
        }

        public void HideFileInfo()
        {
            if (this.CurrentControl is Results)
                return;

            if (this.savedControl == null)
            {
                MessageBox.Show(App.Current.MainWindow, "An error occurred going back to the results. The scan process will need to be restarted.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                this.MoveFirst();

                return;
            }

            this.Content = this.savedControl;

            this.savedControl = null;
        }
    }
}

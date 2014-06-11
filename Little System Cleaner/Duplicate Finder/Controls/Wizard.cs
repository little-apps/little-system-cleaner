using Little_System_Cleaner.Duplicate_Finder.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Little_System_Cleaner.Duplicate_Finder.Controls
{
    public class Wizard : UserControl
    {
        private List<Type> arrayControls = new List<Type>();
        private int currentControl = 0;
        private UserControl savedControl;
        private readonly UserOptions _options;

        private Dictionary<string, List<FileEntry>> _filesGroupedByFilename;
        private Dictionary<string, List<FileEntry>> _filesGroupedByHash;

        public UserControl userControl
        {
            get { return (UserControl)this.Content; }
        }

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

            this.arrayControls.Add(typeof(Start));
            this.arrayControls.Add(typeof(Scan));
            this.arrayControls.Add(typeof(Results));
        }

        public void OnLoaded()
        {
            this.SetCurrentControl(0);
        }

        public bool OnUnloaded()
        {
            if (this.userControl is Scan)
            {
                if (MessageBox.Show("Would you like to cancel the scan thats in progress?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    (this.userControl as Scan).AbortScanThread();

                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (this.userControl is Results)
            {
                if (MessageBox.Show("Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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

        /// <summary>
        /// Changes the current control
        /// </summary>
        /// <param name="index">Index of control in list</param>
        private void SetCurrentControl(int index)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new Action(() => SetCurrentControl(index)));
                return;
            }

            if (this.userControl != null)
                this.userControl.RaiseEvent(new RoutedEventArgs(UserControl.UnloadedEvent, this.userControl));

            this.Content = Activator.CreateInstance(this.arrayControls[index], this);
        }

        /// <summary>
        /// Moves to the next control
        /// </summary>
        public void MoveNext()
        {
            SetCurrentControl(++currentControl);
        }

        /// <summary>
        /// Moves to the previous control
        /// </summary>
        public void MovePrev()
        {
            SetCurrentControl(--currentControl);
        }

        /// <summary>
        /// Moves to the first control
        /// </summary>
        public void MoveFirst()
        {
            currentControl = 0;

            SetCurrentControl(currentControl);
        }

        public void ShowFileInfo(FileEntry fileEntry)
        {
            if (this.userControl is Details)
                this.HideFileInfo();

            this.savedControl = this.userControl;

            Details fileInfoCntrl = new Details(this, fileEntry);
            this.Content = fileInfoCntrl;
        }

        public void HideFileInfo()
        {
            if (this.userControl is Results)
                return;

            if (this.savedControl == null)
            {
                MessageBox.Show(App.Current.MainWindow, "An error occured going back to the results. The scan process will need to be restarted.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                this.MoveFirst();

                return;
            }

            this.Content = this.savedControl;

            this.savedControl = null;
        }
    }
}

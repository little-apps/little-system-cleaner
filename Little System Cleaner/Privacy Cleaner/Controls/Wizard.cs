/*
    Little System Cleaner
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    public class Wizard : UserControl
    {
        List<Type> arrayControls = new List<Type>();
        int currentControl = 0;

        public static ScannerBase CurrentScanner;
        static int ProblemsFound = 0;

        /// <summary>
        /// Gets/Sets the current file or registry key being scanned
        /// <remarks>Please set this with every scan function</remarks>
        /// </summary>
        public static string CurrentFile
        {
            get;
            set;
        }

        public static string CurrentSectionName
        {
            get;
            set;
        }

        public static ResultArray ResultArray = new ResultArray();

        public static Thread ScanThread { get; set; }

        public SectionModel Model
        {
            get;
            set;
        }

        private Results StoredResults { get; set; }

        public UserControl userControl
        {
            get { return (UserControl)this.Content; }
        }

        public Wizard()
        {
            this.arrayControls.Add(typeof(Start));
            this.arrayControls.Add(typeof(Analyze));
            this.arrayControls.Add(typeof(Results));
        }

        public void OnLoaded()
        {
            this.SetCurrentControl(0);
        }

        public bool OnUnloaded()
        {
            if (this.userControl is Analyze)
            {
                if (MessageBox.Show("Would you like to cancel the scan thats in progress?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    (this.userControl as Analyze).AbortScanThread();
                    Wizard.ResultArray.Clear();

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
                    Wizard.ResultArray.Clear();

                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public void ShowDetails(ResultNode resultNode)
        {
            // Store current control
            this.StoredResults = this.userControl as Results;

            Details ctrlDetails = new Details(this, resultNode);
            this.Content = ctrlDetails;
        }

        public void HideDetails()
        {
            if (this.StoredResults == null)
            {
                MessageBox.Show(App.Current.MainWindow, "An error occured going back to the results. The scan process will need to be restarted.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                this.MoveFirst();

                return;
            }

            this.Content = this.StoredResults;
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

            Wizard.ResultArray.Clear();

            SetCurrentControl(currentControl);
        }

        /// <summary>
        /// Adds a clean delegate to the result array
        /// </summary>
        /// <param name="cleanDelegate">Delegate</param>
        /// <param name="desc">Description</param>
        /// <param name="size">Size of file or files in bytes (optional)</param>
        public static bool StoreCleanDelegate(CleanDelegate cleanDelegate, string desc, long size)
        {
            if (cleanDelegate == null || string.IsNullOrEmpty(desc))
                return false;

            CurrentScanner.Results.Children.Add(new ResultDelegate(cleanDelegate, desc, size));

            return true;
        }

        /// <summary>
        /// Gets the size of the files and stores the files in the result array
        /// </summary>
        /// <param name="filePath">File Path</param>
        public static bool StoreBadFileList(string desc, string[] filePaths)
        {
            long fileSize = 0;

            // Check for null parameters
            if (filePaths == null || filePaths.Length == 0 || string.IsNullOrEmpty(desc))
                return false;

            // Calculate total file size
            foreach (string filePath in filePaths)
                fileSize += MiscFunctions.GetFileSize(filePath);

            CurrentScanner.Results.Children.Add(new ResultFiles(desc, filePaths, fileSize));

            return true;
        }

        /// <summary>
        /// Stores the folder paths in the result array
        /// </summary>
        /// <param name="filePath">File Path</param>
        public static bool StoreBadFolderList(string desc, Dictionary<string, bool> folderPaths)
        {
            // Check for null parameters
            if (folderPaths == null || folderPaths.Count == 0 || string.IsNullOrEmpty(desc))
                return false;

            CurrentScanner.Results.Children.Add(new ResultFolders(desc, folderPaths));

            return true;
        }

        /// <summary>
        /// Stores a bad file path in the result array
        /// </summary>
        /// <param name="filePath">File Path</param>
        /// <param name="fileSize">File Size</param>
        [Obsolete]
        public static bool StoreBadFileList(string desc, string[] filePaths, long fileSize)
        {
            // Check for null parameters
            if (filePaths == null || filePaths.Length == 0 || string.IsNullOrEmpty(desc))
                return false;

            // Make sure files pass check
            //foreach (string file in filePaths)
            //    if (!Utils.IsFileValid(file))
            //        return false;

            CurrentScanner.Results.Children.Add(new ResultFiles(desc, filePaths, fileSize));

            return true;
        }

        /// <summary>
        /// Stores a registry key in the result array
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="regKeys">Dictionary containing a registry key (must be writeable) and a list of value names</param>
        /// <returns>True or false if the description and/or dictionary is empty</returns>
        public static bool StoreBadRegKeyValueNames(string desc, Dictionary<RegistryKey, string[]> regKeys)
        {
            if (string.IsNullOrEmpty(desc) || regKeys == null || regKeys.Count == 0)
                return false;

            foreach (KeyValuePair<RegistryKey, string[]> kvp in regKeys)
            {
                if (kvp.Key == null || (kvp.Value == null || kvp.Value.Length <= 0))
                    return false;
            }

            CurrentScanner.Results.Children.Add(new ResultRegKeys(desc, regKeys));

            return true;
        }

        /// <summary>
        /// Stores a registry key in the result array
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="regKeys">Dictionary containing a registry subkey (must be writeable) and a whether to remove to the whole subkey</param>
        /// <returns>True or false if the description and/or dictionary is empty</returns>
        public static bool StoreBadRegKeySubKeys(string desc, Dictionary<RegistryKey, bool> regKeys)
        {
            if (string.IsNullOrEmpty(desc) || regKeys == null || regKeys.Count == 0)
                return false;

            CurrentScanner.Results.Children.Add(new ResultRegKeys(desc, regKeys));

            return true;
        }

        public static bool StoreINIKeys(string desc, INIInfo[] iniInfo)
        {
            if (string.IsNullOrEmpty(desc) || iniInfo == null)
                return false;

            CurrentScanner.Results.Children.Add(new ResultINI(desc, iniInfo));

            return true;
        }

        public static bool StoreXML(string desc, Dictionary<string, List<string>> xmlPaths)
        {
            if (string.IsNullOrEmpty(desc) || xmlPaths == null || xmlPaths.Count == 0)
                return false;

            CurrentScanner.Results.Children.Add(new ResultXML(desc, xmlPaths));

            return true;
        }
    }
}

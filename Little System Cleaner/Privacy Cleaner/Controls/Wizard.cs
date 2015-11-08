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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Helpers.Results;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using Microsoft.Win32;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    public class Wizard : WizardBase
    {
        private static readonly object LockObj = new object();
        internal static ScannerBase CurrentScanner;
        internal static bool SqLiteLoaded = true;

        private static readonly ObservableCollection<ResultNode> _resultArray = new ObservableCollection<ResultNode>();

        public Wizard()
        {
            Controls.Add(typeof (Start));
            Controls.Add(typeof (Analyze));
            Controls.Add(typeof (Results));
        }

        /// <summary>
        ///     Gets/Sets the current file or registry key being scanned
        ///     <remarks>Please set this with every scan function</remarks>
        /// </summary>
        internal static string CurrentFile { get; set; }

        internal static string CurrentSectionName { get; set; }

        internal static ObservableCollection<ResultNode> ResultArray
        {
            get
            {
                lock (LockObj)
                {
                    return _resultArray;
                }
            }
        }

        public SectionModel Model { get; set; }

        private Results StoredResults { get; set; }

        public override void OnLoaded()
        {
            SetCurrentControl(0);
        }

        public override bool OnUnloaded(bool forceExit)
        {
            bool exit;

            var analyze = CurrentControl as Analyze;
            if (analyze != null)
            {
                exit = forceExit ||
                       MessageBox.Show("Would you like to cancel the scan that's in progress?", Utils.ProductName,
                           MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (exit)
                {
                    analyze.AbortScanThread();
                    ResultArray.Clear();

                    return true;
                }
                return false;
            }

            if (!(CurrentControl is Results))
                return true;

            exit = forceExit ||
                   MessageBox.Show("Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo,
                       MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!exit)
                return false;

            ResultArray.Clear();

            return true;
        }

        public void ShowDetails(ResultNode resultNode)
        {
            // Store current control
            StoredResults = CurrentControl as Results;

            var ctrlDetails = new Details(this, resultNode);
            Content = ctrlDetails;
        }

        public void HideDetails()
        {
            if (StoredResults == null)
            {
                MessageBox.Show(Application.Current.MainWindow,
                    "An error occurred going back to the results. The scan process will need to be restarted.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                MoveFirst();

                return;
            }

            Content = StoredResults;
        }

        /// <summary>
        ///     Moves to the first control
        /// </summary>
        public override void MoveFirst(bool autoMove = true)
        {
            // Clear results before going back
            ResultArray.Clear();

            base.MoveFirst(autoMove);
        }

        /// <summary>
        ///     Adds a clean delegate to the result array
        /// </summary>
        /// <param name="cleanDelegate">Delegate</param>
        /// <param name="desc">Description</param>
        /// <param name="size">Size of file or files in bytes (optional)</param>
        internal static bool StoreCleanDelegate(CleanDelegate cleanDelegate, string desc, long size)
        {
            if (cleanDelegate == null || string.IsNullOrEmpty(desc))
                return false;

            CurrentScanner.Results.Children.Add(new ResultDelegate(cleanDelegate, desc, size));

            return true;
        }

        /// <summary>
        ///     Gets the size of the files and stores the files in the result array
        /// </summary>
        /// <param name="desc">Description of bad files</param>
        /// <param name="filePaths">File Paths</param>
        internal static bool StoreBadFileList(string desc, string[] filePaths)
        {
            // Check for null parameters
            if (filePaths == null || filePaths.Length == 0 || string.IsNullOrEmpty(desc))
                return false;

            // Calculate total file size
            var fileSize = filePaths.Sum(filePath => MiscFunctions.GetFileSize(filePath));

            CurrentScanner.Results.Children.Add(new ResultFiles(desc, filePaths, fileSize));

            return true;
        }

        /// <summary>
        ///     Stores the folder paths in the result array
        /// </summary>
        /// <param name="desc">Description of bad files</param>
        /// <param name="folderPaths">Folder Paths</param>
        internal static bool StoreBadFolderList(string desc, Dictionary<string, bool> folderPaths)
        {
            // Check for null parameters
            if (folderPaths == null || folderPaths.Count == 0 || string.IsNullOrEmpty(desc))
                return false;

            CurrentScanner.Results.Children.Add(new ResultFolders(desc, folderPaths));

            return true;
        }

        /// <summary>
        ///     Stores a bad file path in the result array
        /// </summary>
        /// <param name="desc">Description of bad files</param>
        /// <param name="filePaths">File Paths</param>
        /// <param name="fileSize">File Size</param>
        [Obsolete]
        internal static bool StoreBadFileList(string desc, string[] filePaths, long fileSize)
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
        ///     Stores a registry key in the result array
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="regKeys">Dictionary containing a registry key (must be writeable) and a list of value names</param>
        /// <returns>True or false if the description and/or dictionary is empty</returns>
        internal static bool StoreBadRegKeyValueNames(string desc, Dictionary<RegistryKey, string[]> regKeys)
        {
            if (string.IsNullOrEmpty(desc) || regKeys == null || regKeys.Count == 0)
                return false;

            if (regKeys.Any(kvp => kvp.Key == null || kvp.Value == null || kvp.Value.Length <= 0))
                return false;

            CurrentScanner.Results.Children.Add(new ResultRegKeys(desc, regKeys));

            return true;
        }

        /// <summary>
        ///     Stores a registry key in the result array
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="regKeys">
        ///     Dictionary containing a registry subkey (must be writeable) and a whether to remove to the whole
        ///     subkey
        /// </param>
        /// <returns>True or false if the description and/or dictionary is empty</returns>
        internal static bool StoreBadRegKeySubKeys(string desc, Dictionary<RegistryKey, bool> regKeys)
        {
            if (string.IsNullOrEmpty(desc) || regKeys == null || regKeys.Count == 0)
                return false;

            CurrentScanner.Results.Children.Add(new ResultRegKeys(desc, regKeys));

            return true;
        }

        internal static bool StoreIniKeys(string desc, IniInfo[] iniInfo)
        {
            if (string.IsNullOrEmpty(desc) || iniInfo == null)
                return false;

            CurrentScanner.Results.Children.Add(new ResultIni(desc, iniInfo));

            return true;
        }

        internal static bool StoreXml(string desc, Dictionary<string, List<string>> xmlPaths)
        {
            if (string.IsNullOrEmpty(desc) || xmlPaths == null || xmlPaths.Count == 0)
                return false;

            CurrentScanner.Results.Children.Add(new ResultXml(desc, xmlPaths));

            return true;
        }
    }
}
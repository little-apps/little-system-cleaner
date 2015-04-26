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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Little_System_Cleaner.Registry_Cleaner.Scanners;
using System.Threading;
using System.Security.Permissions;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Little_System_Cleaner.Misc;
using Microsoft.Win32;
using System.Security;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
    public class Wizard : WizardBase
    {
        List<ScannerBase> arrayScanners = new List<ScannerBase>();
        SectionModel _model = null;
        static BadRegKeyArray _badRegKeyArray = new BadRegKeyArray();

        public SectionModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                ParseModelChild(_model);
            }
        }

        internal static string currentScannerName
        {
            get;
            set;
        }

        internal static BadRegKeyArray badRegKeyArray
        {
            get { return _badRegKeyArray; }
        }

        public List<ScannerBase> Scanners
        {
            get { return arrayScanners; }
        }

        private static Report _report;
        internal static Report Report
        {
            get { return _report; }
        }

        internal static bool CreateNewLogFile() 
        {
            string fileName;

            try
            {
                fileName = System.IO.Path.GetTempFileName();
            }
            catch (System.IO.IOException ex)
            {
                MessageBox.Show(App.Current.MainWindow, "The following error occurred: " + ex.Message + "\nDue to this, a log of the scan will not be created.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            _report = Report.CreateReport(Properties.Settings.Default.registryCleanerOptionsLog);

            return true;
        }

        internal static DateTime ScanStartTime { get; set; }

        internal static Thread ScanThread { get; set; }

        public Wizard()
        {
            this.Controls.Add(typeof(Start));
            this.Controls.Add(typeof(Scan));
            this.Controls.Add(typeof(Results));

            this.arrayScanners.Add(new ApplicationInfo());
            this.arrayScanners.Add(new ApplicationPaths());
            this.arrayScanners.Add(new ApplicationSettings());
            this.arrayScanners.Add(new ActivexComObjects());
            this.arrayScanners.Add(new SharedDLLs());
            this.arrayScanners.Add(new SystemDrivers());
            this.arrayScanners.Add(new WindowsFonts());
            this.arrayScanners.Add(new WindowsHelpFiles());
            this.arrayScanners.Add(new RecentDocs());
            this.arrayScanners.Add(new WindowsSounds());
            this.arrayScanners.Add(new StartupFiles());
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
                    Wizard.badRegKeyArray.Clear();
                    Scan.EnabledScanners.Clear();

                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (this.CurrentControl is Results)
            {
                if ((!forceExit && (this.CurrentControl as Results).FixThread != null) && (this.CurrentControl as Results).FixThread.IsAlive)
                    return false;

                exit = (forceExit ? true : MessageBox.Show("Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);

                if (exit)
                {
                    // Forced to exit -> abort fix thread
                    if (((this.CurrentControl as Results).FixThread != null) && (this.CurrentControl as Results).FixThread.IsAlive)
                        (this.CurrentControl as Results).FixThread.Abort();

                    Wizard.badRegKeyArray.Clear();
                    Scan.EnabledScanners.Clear();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            

            return true;
        }

        public override void MoveFirst(bool autoMove = true)
        {
            badRegKeyArray.Clear();
            Scan.EnabledScanners.Clear();

            base.MoveFirst(autoMove);
        }

        /// <summary>
        /// Go back to the scan control
        /// </summary>
        public void Rescan()
        {
            badRegKeyArray.Clear();

            base.SetCurrentControl(1);
        }

        internal void ParseModelChild(SectionModel model)
        {
            if (model.Root.Children[0].Children.Count <= 0)
                throw new ArgumentException("model must contain children", "model");

            foreach (Little_System_Cleaner.Registry_Cleaner.Helpers.Section child in model.Root.Children[0].Children)
            {
                foreach (ScannerBase scanner in arrayScanners)
                {
                    if (child.SectionName == scanner.ScannerName)
                    {
                        if (child.IsChecked.HasValue && child.IsChecked.Value == true)
                        {
                            scanner.bMapImg = child.bMapImg;
                            scanner.IsEnabled = true;
                            Scan.EnabledScanners.Add(scanner);
                        }
                        else
                            scanner.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// <para>Stores an invalid registry key to array list</para>
        /// <para>Use IsOnIgnoreList to check for ignored registry keys and paths</para>
        /// </summary>
        /// <param name="Problem">Reason its invalid</param>
        /// <param name="Path">The path to registry key (including registry hive)</param>
        /// <returns>True if it was added</returns>
        internal static bool StoreInvalidKey(string Problem, string Path)
        {
            return StoreInvalidKey(Problem, Path, "");
        }

        /// <summary>
        /// <para>Stores an invalid registry key to array list</para>
        /// <para>Use IsOnIgnoreList to check for ignored registry keys and paths</para>
        /// </summary>
        /// <param name="problem">Reason its invalid</param>
        /// <param name="regPath">The path to registry key (including registry hive)</param>
        /// <param name="valueName">Value name (See remarks)</param>
        /// <remarks>Set value name to null/empty for no value name or (default) to use default value</remarks>
        /// <returns>True if it was added. Otherwise, false.</returns>
        internal static bool StoreInvalidKey(string problem, string regPath, string valueName)
        {
            string baseKey, subKey;

            // Check for null parameters
            if (string.IsNullOrEmpty(problem) || string.IsNullOrEmpty(regPath))
                return false;

            // Make sure registry key isnt already in array
            if (_badRegKeyArray.Contains(regPath, valueName))
                return false;

            // Make sure registry key exists
            if (!Utils.RegKeyExists(regPath))
                return false;

            // Parse registry key to base and subkey
            if (!Utils.ParseRegKeyPath(regPath, out baseKey, out subKey))
                return false;

            // Check for ignored registry path
            if (IsOnIgnoreList(regPath))
                return false;

            using (RegistryKey regKey = Utils.RegOpenKey(regPath, false))
            {
                // Can we get write access?
                if (regKey == null)
                    return false;

                // Can we delete it?
                if (!ScanFunctions.CanDeleteKey(regKey))
                    return false;
            }

            // If value name is specified, see if it exists
            if (!string.IsNullOrEmpty(valueName))
                if (!ScanFunctions.ValueNameExists(baseKey, subKey, valueName))
                    return false;

            int severity = 1;

            if (problem == Strings.InvalidFile)
            {
                severity = 5;
            }
            else if (problem == Strings.InvalidFileExt)
            {
                severity = 2;
            }
            else if (problem == Strings.InvalidInprocServer)
            {
                severity = 4;
            }
            else if (problem == Strings.InvalidInprocServer32)
            {
                severity = 4;
            }
            else if (problem == Strings.InvalidProgIDFileExt)
            {
                severity = 3;
            }
            else if (problem == Strings.InvalidRegKey)
            {
                severity = 2;
            }
            else if (problem == Strings.InvalidToolbar)
            {
                severity = 4;
            }
            else if (problem == Strings.MissingAppID)
            {
                severity = 5;
            }
            else if (problem == Strings.MissingCLSID)
            {
                severity = 5;
            }
            else if (problem == Strings.MissingProgID)
            {
                severity = 5;
            }
            else if (problem == Strings.NoRegKey)
            {
                severity = 1;
            }
            else if (problem == Strings.ObsoleteRegKey)
            {
                severity = 1;
            }

            _badRegKeyArray.Add(new BadRegistryKey(Wizard.currentScannerName, problem, baseKey, subKey, valueName, severity));

            if (!string.IsNullOrEmpty(valueName))
                Wizard.Report.WriteLine(string.Format("Bad Registry Value Found! Problem: \"{0}\" Path: \"{1}\" Value Name: \"{2}\"", problem, regPath, valueName));
            else
                Wizard.Report.WriteLine(string.Format("Bad Registry Key Found! Problem: \"{0}\" Path: \"{1}\"", problem, regPath));

            return true;
        }

        /// <summary>
        /// Checks for the path in ignore list
        /// </summary>
        /// <returns>true if it is on the ignore list, otherwise false</returns>
        internal static bool IsOnIgnoreList(string Path)
        {
            if (!string.IsNullOrEmpty(Path) && Properties.Settings.Default.arrayExcludeList.Count > 0)
            {
                string expandedPath = string.Empty;
                bool isPath = (!Path.ToUpper().StartsWith("HKEY"));

                foreach (ExcludeItem i in Properties.Settings.Default.arrayExcludeList)
                {
                    if (isPath && i.IsPath)
                    {
                        if (string.IsNullOrEmpty(expandedPath))
                        {
                            // Trim and change to lower case
                            expandedPath = Path.Trim().ToLower();

                            // Remove quotes
                            expandedPath = Utils.UnqouteSpaces(expandedPath);

                            // Remove environment variables
                            expandedPath = Environment.ExpandEnvironmentVariables(expandedPath);
                        }

                        if (!string.IsNullOrEmpty(expandedPath))
                        {
                            if (Utils.CompareWildcard(expandedPath, i.ToString()))
                                return true;
                        }
                    }

                    if (Utils.CompareWildcard(Path, i.ToString()))
                        return true;
                }
            }

            return false;
        }

    }
}

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
using System.Linq;
using System.Windows;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Little_System_Cleaner.Registry_Cleaner.Helpers.BadRegistryKeys;
using Little_System_Cleaner.Registry_Cleaner.Helpers.Sections;
using Little_System_Cleaner.Registry_Cleaner.Scanners;

namespace Little_System_Cleaner.Registry_Cleaner.Controls
{
    public class Wizard : WizardBase
    {
        private SectionModel _model;


        public Wizard()
        {
            Controls.Add(typeof (Start));
            Controls.Add(typeof (Scan));
            Controls.Add(typeof (Results));

            Scanners.Add(new ApplicationInfo());
            Scanners.Add(new ApplicationPaths());
            Scanners.Add(new ApplicationSettings());
            Scanners.Add(new ActivexComObjects());
            Scanners.Add(new SharedDLLs());
            Scanners.Add(new SystemDrivers());
            Scanners.Add(new WindowsFonts());
            Scanners.Add(new WindowsHelpFiles());
            Scanners.Add(new RecentDocs());
            Scanners.Add(new WindowsSounds());
            Scanners.Add(new StartupFiles());
        }

        public SectionModel Model
        {
            get { return _model; }
            set
            {
                _model = value;
                ParseModelChild(_model);
            }
        }

        internal static string CurrentScannerName { get; set; }

        internal static BadRegKeyArray BadRegKeyArray { get; } = new BadRegKeyArray();

        public List<ScannerBase> Scanners { get; } = new List<ScannerBase>();

        internal static Report Report { get; private set; }

        internal static DateTime ScanStartTime { get; set; }

        internal static bool CreateNewLogFile()
        {
            Report = Report.CreateReport(Settings.Default.registryCleanerOptionsLog);

            return true;
        }

        public override void OnLoaded()
        {
            SetCurrentControl(0);
        }

        public override bool OnUnloaded(bool forceExit)
        {
            bool exit;

            var scan = CurrentControl as Scan;
            if (scan != null)
            {
                exit = forceExit ||
                       MessageBox.Show("Would you like to cancel the scan that's in progress?", Utils.ProductName,
                           MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (!exit)
                    return false;

                scan.AbortScanThread();
                BadRegKeyArray.Clear();
                Scan.EnabledScanners.Clear();

                return true;
            }

            var results = CurrentControl as Results;
            if (results == null)
                return true;

            if (!forceExit && ((Results) CurrentControl).FixProblemsRunning)
                return false;

            exit = forceExit ||
                   MessageBox.Show("Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo,
                       MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!exit)
                return false;

            // Forced to exit -> abort fix task
            ((Results) CurrentControl).CancelFixIfRunning();

            BadRegKeyArray.Clear();
            Scan.EnabledScanners.Clear();

            return true;
        }

        public override void MoveFirst(bool autoMove = true)
        {
            BadRegKeyArray.Clear();
            Scan.EnabledScanners.Clear();

            base.MoveFirst(autoMove);
        }

        /// <summary>
        ///     Go back to the scan control
        /// </summary>
        public void Rescan()
        {
            BadRegKeyArray.Clear();

            SetCurrentControl(1);
        }

        internal void ParseModelChild(SectionModel model)
        {
            if (model.Root.Children[0].Children.Count <= 0)
                throw new ArgumentException("model must contain children", nameof(model));

            foreach (var child in model.Root.Children[0].Children)
            {
                foreach (var scanner in Scanners.Where(scanner => child.SectionName == scanner.ScannerName))
                {
                    if (child.IsChecked.HasValue && child.IsChecked.Value)
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

        /// <summary>
        ///     <para>Stores an invalid registry key to array list</para>
        ///     <para>Use IsOnIgnoreList to check for ignored registry keys and paths</para>
        /// </summary>
        /// <param name="problem">Reason its invalid</param>
        /// <param name="path">The path to registry key (including registry hive)</param>
        /// <returns>True if it was added</returns>
        internal static bool StoreInvalidKey(string problem, string path)
        {
            return StoreInvalidKey(problem, path, "");
        }

        /// <summary>
        ///     <para>Stores an invalid registry key to array list</para>
        ///     <para>Use IsOnIgnoreList to check for ignored registry keys and paths</para>
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
            if (BadRegKeyArray.Contains(regPath, valueName))
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

            using (var regKey = Utils.RegOpenKey(regPath, false))
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

            var severity = 1;

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

            BadRegKeyArray.Add(new BadRegistryKey(CurrentScannerName, problem, baseKey, subKey, valueName, severity));

            Report.WriteLine(!string.IsNullOrEmpty(valueName)
                ? $"Bad Registry Value Found! Problem: \"{problem}\" Path: \"{regPath}\" Value Name: \"{valueName}\""
                : $"Bad Registry Key Found! Problem: \"{problem}\" Path: \"{regPath}\"");

            return true;
        }

        /// <summary>
        ///     Checks for the path in ignore list
        /// </summary>
        /// <returns>true if it is on the ignore list, otherwise false</returns>
        internal static bool IsOnIgnoreList(string path)
        {
            if (string.IsNullOrEmpty(path) || Settings.Default.ArrayExcludeList.Count <= 0)
                return false;

            var expandedPath = string.Empty;
            var isPath = !path.ToUpper().StartsWith("HKEY");

            foreach (var i in Settings.Default.ArrayExcludeList)
            {
                if (isPath && i.IsPath)
                {
                    if (string.IsNullOrEmpty(expandedPath))
                    {
                        // Trim and change to lower case
                        expandedPath = path.Trim().ToLower();

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

                if (Utils.CompareWildcard(path, i.ToString()))
                    return true;
            }

            return false;
        }
    }
}
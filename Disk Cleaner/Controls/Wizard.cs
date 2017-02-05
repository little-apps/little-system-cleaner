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
using System.IO;
using System.Windows;
using Disk_Cleaner.Helpers;
using Shared;

namespace Disk_Cleaner.Controls
{
    public class Wizard : WizardBase
    {
        public Wizard()
        {
            Controls.Add(typeof(Start));
            Controls.Add(typeof(Analyze));
            Controls.Add(typeof(Results));
        }

        public List<DriveInfo> SelectedDrives { get; } = new List<DriveInfo>();

        internal static ObservableCollection<ProblemFile> FileList { get; set; }

        internal static ObservableCollection<LviDrive> DiskDrives { get; set; }

        internal static ObservableCollection<LviFolder> IncludeFolders { get; set; }

        internal static bool DrivesLoaded
        {
            get
            {
                if (DiskDrives == null || IncludeFolders == null)
                    return false;

                return DiskDrives.Count != 0 && IncludeFolders.Count != 0;
            }
        }

        internal static DateTime ScanStartTime { get; set; }

        public override void OnLoaded()
        {
            MoveFirst();
        }

        public override bool OnUnloaded(bool forceExit)
        {
            bool exit;

            var analyze = CurrentControl as Analyze;

            if (analyze != null)
            {
                exit = forceExit ||
                       MessageBox.Show(Application.Current.MainWindow,
                           "Scanning is currently in progress. Would you like to cancel?", Utils.ProductName,
                           MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (!exit)
                    return false;

                analyze.CancelAnalyze();

                return true;
            }

            if (!(CurrentControl is Results))
                return true;

            exit = forceExit ||
                   MessageBox.Show(Application.Current.MainWindow,
                       "Scanning results will be reset. Would you like to continue?", Utils.ProductName,
                       MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!exit)
                return false;

            FileList.Clear();
            return true;
        }
    }
}
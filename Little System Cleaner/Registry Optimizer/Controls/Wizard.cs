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
using Little_System_Cleaner.Registry_Optimizer.Helpers;
using System.Collections.ObjectModel;
using System.Windows;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    public class Wizard : WizardBase
    {
        public bool HivesLoaded = false;

        public Wizard()
        {
            Controls.Add(typeof(LoadHives));
            Controls.Add(typeof(Main));
            Controls.Add(typeof(AnalyzeResults));

            IsBusy = false;
        }

        internal static ObservableCollection<Hive> RegistryHives { get; set; }

        internal static bool IsBusy { get; set; }

        public override void OnLoaded()
        {
            MoveFirst();
        }

        public override bool OnUnloaded(bool forceExit)
        {
            if (!HivesLoaded)
            {
                // Registry hives not completely loaded, unload them
                RegistryHives.Clear();
            }

            if (IsBusy)
            {
                MessageBox.Show(Application.Current.MainWindow,
                    "The Windows Registry is currently being analyzed/compacted. The operation cannot be completed at the moment.",
                    Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (CurrentControl is AnalyzeResults)
            {
                var exit = forceExit ||
                           MessageBox.Show(Application.Current.MainWindow,
                               "Analyze results will be reset. Would you like to continue?", Utils.ProductName,
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (!exit)
                    return false;

                foreach (var h in RegistryHives)
                {
                    h.Reset();
                }

                return true;
            }

            return true;
        }

        public override void MoveFirst(bool autoMove = true)
        {
            SetCurrentControl(!HivesLoaded ? 0 : 1, autoMove);
        }
    }
}
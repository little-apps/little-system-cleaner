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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    public class Wizard : WizardBase
    {
        internal static ObservableCollection<Hive> RegistryHives { get; set; }

        internal static bool IsBusy { get; set; }

        public bool HivesLoaded = false;

        public Wizard() : base()
        {
            this.Controls.Add(typeof(LoadHives));
            this.Controls.Add(typeof(Main));
            this.Controls.Add(typeof(AnalyzeResults));

            IsBusy = false;
        }

        public override void OnLoaded()
        {
            this.MoveFirst();
        }

        public override bool OnUnloaded(bool forceExit)
        {
            if (!HivesLoaded)
            {
                // Registry hives not completely loaded, unload them
                Wizard.RegistryHives.Clear();
            }

            if (Wizard.IsBusy)
            {
                MessageBox.Show(Application.Current.MainWindow, "The Windows Registry is currently being analyzed/compacted. The operation cannot be completed at the moment.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            bool exit;

            if (this.CurrentControl is AnalyzeResults)
            {
                exit = (forceExit ? true : MessageBox.Show(App.Current.MainWindow, "Analyze results will be reset. Would you like to continue?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);

                if (exit)
                {
                    foreach (Hive h in Wizard.RegistryHives) {
                        h.Reset();
                    }

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
            if (!this.HivesLoaded)
                this.SetCurrentControl(0, autoMove);
            else
                this.SetCurrentControl(1, autoMove);
        }
    }
}

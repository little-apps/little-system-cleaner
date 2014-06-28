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
    public class Wizard : UserControl
    {
        List<Type> arrayControls = new List<Type>();
        int currentControl = 0;

        public UserControl userControl
        {
            get { return (UserControl)this.Content; }
        }

        internal static DateTime ScanStartTime { get; set; }

        internal static ObservableCollection<Hive> RegistryHives { get; set; }

        internal static bool IsBusy { get; set; }

        public bool HivesLoaded = false;

        public Wizard()
        {
            this.arrayControls.Add(typeof(LoadHives));
            this.arrayControls.Add(typeof(Main));
            this.arrayControls.Add(typeof(AnalyzeResults));

            IsBusy = false;
        }

        public void OnLoaded()
        {
            if (!this.HivesLoaded)
                this.SetCurrentControl(0);
            else
                this.SetCurrentControl(1);
        }

        public bool OnUnloaded(bool forceExit = false)
        {
            if (!HivesLoaded)
            {
                // Registry hives not completley loaded, unload them
                Wizard.RegistryHives.Clear();
            }

            if (Wizard.IsBusy)
            {
                MessageBox.Show(Application.Current.MainWindow, "The Windows Registry is currently being analyzed/compacted. The operation cannot be completed at the moment.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            bool exit;

            if (this.userControl is AnalyzeResults)
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

        /// <summary>
        /// Changes the current control
        /// </summary>
        /// <param name="index">Index of control in list</param>
        private void SetCurrentControl(int index)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.BeginInvoke(new Action(() => SetCurrentControl(index)));
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
            this.SetCurrentControl(++currentControl);
        }

        /// <summary>
        /// Moves to the previous control
        /// </summary>
        public void MovePrev()
        {
            this.SetCurrentControl(--currentControl);
        }

        /// <summary>
        /// Moves to the first control
        /// </summary>
        public void MoveFirst()
        {
            this.currentControl = 0;

            this.SetCurrentControl(currentControl);
        }
    }
}

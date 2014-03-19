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
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Little_System_Cleaner.Disk_Cleaner.Controls
{
    public class Wizard : UserControl
    {
        List<Type> arrayControls = new List<Type>();
        List<DriveInfo> selDrives = new List<DriveInfo>();
        int currentControl = 0;

        public UserControl userControl
        {
            get { return (UserControl)this.Content; }
        }

        public List<DriveInfo> selectedDrives
        {
            get
            {
                return this.selDrives;
            }
        }

        public static ObservableCollection<ProblemFile> fileList
        {
            get;
            set;
        }

        public static DateTime ScanStartTime { get; set; }

        public Wizard()
        {
            this.arrayControls.Add(typeof(Start));
            this.arrayControls.Add(typeof(Analyze));
            this.arrayControls.Add(typeof(Results));

            this.SetCurrentControl(0);
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

            if (index > 0)
                Main.IsTabsEnabled = false;
            else
                Main.IsTabsEnabled = true;

            System.Reflection.ConstructorInfo constructorInfo = this.arrayControls[index].GetConstructor(new Type[] { typeof(Wizard) });

            this.Content = constructorInfo.Invoke(new object[] { this });
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

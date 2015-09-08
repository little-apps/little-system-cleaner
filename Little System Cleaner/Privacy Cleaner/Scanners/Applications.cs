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

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public class Applications : ScannerBase
    {
        public Applications() 
        {
            Name = "Applications";

            //if (Registry.CurrentUser.OpenSubKey(@"Software\Adobe\Acrobat Reader") != null)
            //    this.Nodes.Add(new Applications("Adobe Reader"));
            //if (Registry.CurrentUser.OpenSubKey(@"Software\Adobe\Photoshop") != null)
            //    this.Nodes.Add(new Applications("Adobe Photoshop"));
            //if (Registry.CurrentUser.OpenSubKey(@"Software\Adobe\Dreamweaver CS3") != null)
            //    this.Nodes.Add(new Applications("Adobe Dreamweaver CS3"));
            //if (Registry.CurrentUser.OpenSubKey(@"Software\Adobe\Dreamweaver CS4") != null)
            //    this.Nodes.Add(new Applications("Adobe Dreamweaver CS4"));
            //if (Registry.CurrentUser.OpenSubKey(@"Software\OpenOffice.org\OpenOffice.org 3.1") != null)
            //    this.Nodes.Add(new Applications("OpenOffice 3.1"));
            //if (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Office\12.0") != null)
            //    this.Nodes.Add(new Applications("Microsoft Office 2007"));
        }

        public Applications(ScannerBase parent, string header)
        {
            Parent = parent;
            Name = header;
        }

        public override void Scan(ScannerBase child)
        {
            //if (!Children.Contains(child))
            //    return;

            //if (!child.IsChecked.GetValueOrDefault())
            //    return;
        }
    }
}
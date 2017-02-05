﻿/*
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

using System.ComponentModel;

namespace Registry_Optimizer.Controls
{
    /// <summary>
    ///     Interaction logic for SecureDesktop.xaml
    /// </summary>
    public partial class SecureDesktop
    {
        public SecureDesktop()
        {
            InitializeComponent();

            System.Windows.Forms.Cursor.Hide();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            System.Windows.Forms.Cursor.Show();

            base.OnClosing(e);
        }
    }
}
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
using System.Windows;
using System.Windows.Interop;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace Shared
{
    /// <summary>
    ///     Used to get IWin32Window from WPF Window
    /// </summary>
    public class WindowWrapper : IWin32Window
    {
        /// <summary>
        /// Constructor that takes in handle to window
        /// </summary>
        /// <param name="handle">Window handle</param>
        public WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Constructor that takes in WPF Window
        /// </summary>
        /// <param name="window">Window instance</param>
        public WindowWrapper(Window window)
        {
            var wih = new WindowInteropHelper(window);
            Handle = wih.Handle;
        }

        /// <summary>
        /// Gets the handle to the window
        /// </summary>
        public IntPtr Handle { get; }

        /// <summary>
        /// Gets WindowWrapper for main window
        /// </summary>
        /// <returns>WindowWrapper for main window</returns>
        public static WindowWrapper GetCurrentWindowHandle()
        {
            var winWrapper = new WindowWrapper(Application.Current.MainWindow);

            return winWrapper;
        }
    }
}
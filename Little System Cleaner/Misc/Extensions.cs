using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace Little_System_Cleaner.Misc
{
    internal static class Extensions
    {
        /// <summary>
        ///     Hides close (X) button in top right window
        /// </summary>
        /// <param name="window"></param>
        /// <param name="addClosingHook">
        ///     If true, adds delegate to Closing event which sets Cancel to true causing Window to note
        ///     close. Please note, calling Close() method will not close the window either.
        /// </param>
        internal static void HideCloseButton(this Window window, bool addClosingHook = false)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            PInvoke.SetWindowLong(hwnd, PInvoke.GwlStyle,
                PInvoke.GetWindowLong(hwnd, PInvoke.GwlStyle) & ~PInvoke.WsSysmenu);

            if (addClosingHook)
            {
                // Cancel Closing event (if it occurs)
                window.Closing += delegate (object sender, CancelEventArgs args) { args.Cancel = true; };
            }
        }

        /// <summary>
        ///     Hides icon for window.
        ///     If this is called before InitializeComponent() then the icon will be completely removed from the title bar
        ///     If this is called after InitializeComponent() then an empty image is used but there will be empty space between
        ///     window border and title
        /// </summary>
        /// <param name="window">Window class</param>
        internal static void HideIcon(this Window window)
        {
            if (window.IsInitialized)
            {
                window.Icon = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 0 }, 4);
            }
            else
            {
                window.SourceInitialized += delegate
                {
                    // Get this window's handle
                    var hwnd = new WindowInteropHelper(window).Handle;

                    // Change the extended window style to not show a window icon
                    var extendedStyle = PInvoke.GetWindowLong(hwnd, PInvoke.GwlExstyle);
                    PInvoke.SetWindowLong(hwnd, PInvoke.GwlExstyle, extendedStyle | PInvoke.WsExDlgmodalframe);

                    // Update the window's non-client area to reflect the changes
                    PInvoke.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                        PInvoke.SwpNomove | PInvoke.SwpNosize | PInvoke.SwpNozorder | PInvoke.SwpFramechanged);
                };
            }
        }

        /// <summary>
        ///     Converts a System.Drawing.Bitmap to a System.Controls.Image
        /// </summary>
        /// <param name="bitmap">Source</param>
        /// <returns>Image</returns>
        internal static Image CreateBitmapSourceFromBitmap(this Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            var hBitmap = bitmap.GetHbitmap();

            try
            {
                var bitmapImg = new Image
                {
                    Source =
                        Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions())
                };

                return bitmapImg;
            }
            finally
            {
                PInvoke.DeleteObject(hBitmap);
            }
        }

        /// <summary>
        ///     Adds a suffix to a given number (1st, 2nd, 3rd, ...)
        /// </summary>
        /// <param name="number">Number to add suffix to</param>
        /// <returns>Number with suffix</returns>
        internal static string GetNumberSuffix(this int number)
        {
            if (number <= 0)
                return number.ToString();

            var n = number % 100;

            // Skip the switch for as many numbers as possible.
            if (n > 3 && n < 21)
                return n + "th";

            // Determine the suffix for numbers ending in 1, 2 or 3, otherwise add a 'th'
            switch (n % 10)
            {
                case 1:
                    return n + "st";

                case 2:
                    return n + "nd";

                case 3:
                    return n + "rd";

                default:
                    return n + "th";
            }
        }

        /// <summary>
        ///     Auto resize columns
        /// </summary>
        internal static void AutoResizeColumns(this ListView listView)
        {
            var gv = listView.View as GridView;

            if (gv == null)
                return;

            foreach (var gvc in gv.Columns)
            {
                // Set width to max value because actual width doesn't include margins
                gvc.Width = double.MaxValue;

                // Set it to NaN to remove white space
                gvc.Width = double.NaN;
            }

            listView.UpdateLayout();
        }

        /// <summary>
        ///     Calculates size of directory
        /// </summary>
        /// <param name="directory">DirectoryInfo class</param>
        /// <param name="includeSubdirectories">Includes sub directories if true</param>
        /// <returns>Size of directory in bytes</returns>
        internal static long CalculateDirectorySize(this DirectoryInfo directory, bool includeSubdirectories)
        {
            // Examine all contained files.
            var files = directory.GetFiles();
            var totalSize = files.Sum(file => file.Length);

            // Examine all contained directories.
            if (!includeSubdirectories)
                return totalSize;

            var dirs = directory.GetDirectories();
            totalSize += dirs.Sum(dir => dir.CalculateDirectorySize(true));

            return totalSize;
        }

        internal static void AddRange<T>(this ObservableCollection<T> observableCollection, IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                observableCollection.Add(item);
            }
        }

        internal static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumeration)
        {
            return new ObservableCollection<T>(enumeration);
        }
    }
}
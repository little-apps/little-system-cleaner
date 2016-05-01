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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Misc
{
    /// <summary>
    ///     This log class is used for the scanner modules
    /// </summary>
    public sealed class Report : StreamWriter
    {
        private readonly object _lockObject = new object();

        public Report(Stream stream, bool isEnabled)
            : base(stream)
        {
            IsEnabled = isEnabled;

            if (!IsEnabled)
                return;

            try
            {
                // Flush the buffers automatically
                AutoFlush = true;

                // Create log directory if it doesnt exist
                if (!Directory.Exists(Settings.Default.OptionsLogDir))
                    Directory.CreateDirectory(Settings.Default.OptionsLogDir);

                lock (_lockObject)
                {
                    // Writes header to log file
                    WriteLine("Little System Cleaner " + Application.ProductVersion);
                    WriteLine("Website: http://www.little-apps.com/little-system-cleaner/");
                    WriteLine("Date & Time: " + DateTime.Now);
                    WriteLine("OS: " + OsVersion.GetOsVersion());
                    WriteLine();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        ///     Gets/Sets whether logging is enabled
        /// </summary>
        public bool IsEnabled { get; }

        public override Encoding Encoding => Encoding.ASCII;

        /// <summary>
        ///     Creates an instance of a report
        /// </summary>
        /// <param name="isEnabled">Whether logging is enabled or not</param>
        /// <returns></returns>
        internal static Report CreateReport(bool isEnabled)
        {
            var stream = new MemoryStream();
            var report = new Report(stream, isEnabled);
            return report;
        }

        /// <summary>
        ///     Moves the temp file to the log directory and opens it with the default viewer
        /// </summary>
        /// <returns>True if the file is displayed</returns>
        public bool DisplayLogFile(bool displayFile)
        {
            if (!IsEnabled)
                return false;

            var newFileName = string.Format("{0}\\{1:yyyy}_{1:MM}_{1:dd}_{1:HH}{1:mm}{1:ss}.txt",
                Settings.Default.OptionsLogDir, DateTime.Now);

            try
            {
                lock (_lockObject)
                {
                    using (var fileStream = new FileStream(newFileName, FileMode.Create, FileAccess.Write))
                    {
                        var memoryStream = BaseStream as MemoryStream;
                        memoryStream?.WriteTo(fileStream);
                    }

                    if (!displayFile)
                        return true;

                    var startInfo = new ProcessStartInfo("NOTEPAD.EXE", newFileName)
                    {
                        ErrorDialog = true
                    };

                    Process.Start(startInfo);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return false;
        }
    }
}
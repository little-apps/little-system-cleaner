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
using System.Windows.Forms;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Little_System_Cleaner
{
    /// <summary>
    /// This log class is used for the scanner modules
    /// </summary>
    public class Report : StreamWriter
    {
        private readonly bool _isEnabled;
        private readonly object lockObject = new object();

        /// <summary>
        /// Gets/Sets whether logging is enabled
        /// </summary>
        public bool IsEnabled
        {
            get { return this._isEnabled; }
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }

        /// <summary>
        /// Creates an instance of a report
        /// </summary>
        /// <param name="isEnabled">Whether logging is enabled or not</param>
        /// <returns></returns>
        public static Report CreateReport(bool isEnabled)
        {
            MemoryStream stream = new MemoryStream();
            Report report = new Report(stream, isEnabled);
            return report;
        }

        public Report(MemoryStream stream, bool isEnabled)
            : base(stream)
        {
            this._isEnabled = isEnabled;

            if (this.IsEnabled)
            {
                try
                {
                    // Flush the buffers automatically
                    this.AutoFlush = true;

                    // Create log directory if it doesnt exist
                    if (!Directory.Exists(Little_System_Cleaner.Properties.Settings.Default.optionsLogDir))
                        Directory.CreateDirectory(Little_System_Cleaner.Properties.Settings.Default.optionsLogDir);

                    lock (this.lockObject)
                    {
                        // Writes header to log file
                        this.WriteLine("Little System Cleaner " + Application.ProductVersion);
                        this.WriteLine("Website: http://www.little-apps.com/little-system-cleaner/");
                        this.WriteLine("Date & Time: " + DateTime.Now.ToString());
                        this.WriteLine("OS: " + OSVersion.GetOSVersion());
                        this.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Moves the temp file to the log directory and opens it with the default viewer
        /// </summary>
        /// <returns>True if the file is displayed</returns>
        public bool DisplayLogFile(bool displayFile)
        {
            if (this.IsEnabled)
            {
                string strNewFileName = string.Format("{0}\\{1:yyyy}_{1:MM}_{1:dd}_{1:HH}{1:mm}{1:ss}.txt", Little_System_Cleaner.Properties.Settings.Default.optionsLogDir, DateTime.Now);

                try
                {
                    lock (this.lockObject)
                    {
                        using (FileStream fileStream = new FileStream(strNewFileName, FileMode.Create, FileAccess.Write))
                        {
                            (this.BaseStream as MemoryStream).WriteTo(fileStream);
                        }

                        if (displayFile)
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo("NOTEPAD.EXE", strNewFileName);
                            startInfo.ErrorDialog = true;
                            Process.Start(startInfo);
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            return false;
        }

    }
}

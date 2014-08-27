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
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;
using Little_System_Cleaner.Misc;
using System.Reflection;

namespace Little_System_Cleaner
{
    /// <summary>
    /// Interaction logic for CrashReporter.xaml
    /// </summary>
    public partial class CrashReporter : Window
    {
        private Exception _exception;

        public System.Windows.Media.Imaging.BitmapSource ImageSource
        {
            get 
            {
                IntPtr hBitmap = System.Drawing.SystemIcons.Error.ToBitmap().GetHbitmap();
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
            }
        }


        public CrashReporter(Exception e)
        {
            InitializeComponent();

            this._exception = e;

            GenerateDialogReport();
        }

        /// <summary>
        /// Fills in text box with exception info
        /// </summary>
        /// <param name="e"></param>
        private void GenerateDialogReport()
        {
            StringBuilder sb = new StringBuilder();

            Process proc = Process.GetCurrentProcess();

            // dates and time
            sb.AppendLine(string.Format("Current Date/Time: {0}", DateTime.Now.ToString()));
            sb.AppendLine(string.Format("Exec. Date/Time: {0}", proc.StartTime.ToString()));
            sb.AppendLine(string.Format("Build Date: {0}", Properties.Settings.Default.strBuildTime));
            // os info
            sb.AppendLine(string.Format("OS: {0}", Environment.OSVersion.VersionString));
            sb.AppendLine(string.Format("Language: {0}", System.Windows.Forms.Application.CurrentCulture.ToString()));
            // uptime stats
            sb.AppendLine(string.Format("System Uptime: {0} Days {1} Hours {2} Mins {3} Secs", Math.Round((decimal)Environment.TickCount / 86400000), Math.Round((decimal)Environment.TickCount / 3600000 % 24), Math.Round((decimal)Environment.TickCount / 120000 % 60), Math.Round((decimal)Environment.TickCount / 1000 % 60)));
            sb.AppendLine(string.Format("Program Uptime: {0}", proc.TotalProcessorTime.ToString()));
            // process id
            sb.AppendLine(string.Format("PID: {0}", proc.Id));
            // exe name
            sb.AppendLine(string.Format("Executable: {0}", System.Windows.Forms.Application.ExecutablePath));
            sb.AppendLine(string.Format("Process Name: {0}", proc.ToString()));
            sb.AppendLine(string.Format("Main Module Name: {0}", proc.MainModule.ModuleName));
            // exe stats
            sb.AppendLine(string.Format("Module Count: {0}", proc.Modules.Count));
            sb.AppendLine(string.Format("Thread Count: {0}", proc.Threads.Count));
            sb.AppendLine(string.Format("Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));
            sb.AppendLine(string.Format("Is Admin: {0}", Permissions.IsUserAdministrator));
            sb.AppendLine(string.Format("Is Debugged: {0}", Debugger.IsAttached));
            // versions
            sb.AppendLine(string.Format("Version: {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            sb.AppendLine(string.Format("CLR Version: {0}", Environment.Version.ToString()));


            Exception ex = this._exception;
            for (int i = 0; ex != null; ex = ex.InnerException, i++)
            {
                sb.AppendLine();
                sb.AppendLine(string.Format("Type #{0} {1}", i, ex.GetType().ToString()));

                foreach (System.Reflection.PropertyInfo propInfo in ex.GetType().GetProperties())
                {
                    string fieldName = string.Format("{0} #{1}", propInfo.Name, i);
                    string fieldValue = string.Format("{0}", propInfo.GetValue(ex, null));

                    // Ignore stack trace + data
                    if (propInfo.Name == "StackTrace"
                        || propInfo.Name == "Data"
                        || string.IsNullOrEmpty(propInfo.Name)
                        || string.IsNullOrEmpty(fieldValue))
                        continue;

                    sb.AppendLine(string.Format("{0}: {1}", fieldName, fieldValue));
                }

                if (ex.Data != null)
                    foreach (DictionaryEntry de in ex.Data)
                        sb.AppendLine(string.Format("Dictionary Entry #{0}: Key: {1} Value: {2}", i, de.Key, de.Value));
            }

            sb.AppendLine();
            sb.AppendLine("StackTrace:");
            sb.AppendLine(this._exception.StackTrace);

            this.textBox1.Text = sb.ToString();
        }

        private void buttonDontSend_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            if (Main.Watcher != null)
                Main.Watcher.Exception(this._exception);

            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.checkBoxRestart.IsChecked.GetValueOrDefault())
            {
                System.Diagnostics.Process.Start(App.ResourceAssembly.Location, "/restart");
                Application.Current.Shutdown();
            }
        }
    }
}

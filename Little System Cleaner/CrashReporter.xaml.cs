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
using Little_System_Cleaner.Properties;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Little_System_Cleaner
{
    /// <summary>
    ///     Interaction logic for CrashReporter.xaml
    /// </summary>
    public partial class CrashReporter
    {
        private readonly Exception _exception;

        public CrashReporter(Exception e)
        {
            InitializeComponent();

            _exception = e;

            GenerateDialogReport();
        }

        public BitmapSource ImageSource
        {
            get
            {
                var hBitmap = SystemIcons.Error.ToBitmap().GetHbitmap();
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                    );
            }
        }

        /// <summary>
        ///     Opens crash report window properly
        /// </summary>
        /// <param name="ex">Exception</param>
        public static void ShowCrashReport(Exception ex)
        {
            // Are we in the UI thread?
            if (Thread.CurrentThread != Application.Current.Dispatcher.Thread)
            {
                Application.Current.Dispatcher.Invoke(new Action<Exception>(ShowCrashReport), ex);
                return;
            }

            var crashRep = new CrashReporter(ex);
            crashRep.Show();
        }

        /// <summary>
        ///     Fills in text box with exception info
        /// </summary>
        private void GenerateDialogReport()
        {
            var sb = new StringBuilder();

            var proc = Process.GetCurrentProcess();

            // dates and time
            sb.AppendLine($"Current Date/Time: {DateTime.Now}");
            sb.AppendLine($"Exec. Date/Time: {proc.StartTime}");
            sb.AppendLine($"Build Date: {Settings.Default.BuildTime}");
            // os info
            sb.AppendLine($"OS: {Environment.OSVersion.VersionString}");
            sb.AppendLine($"Language: {System.Windows.Forms.Application.CurrentCulture}");
            // uptime stats
            sb.AppendLine(
                $"System Uptime: {Math.Round((decimal)Environment.TickCount / 86400000)} Days {Math.Round((decimal)Environment.TickCount / 3600000 % 24)} Hours {Math.Round((decimal)Environment.TickCount / 120000 % 60)} Mins {Math.Round((decimal)Environment.TickCount / 1000 % 60)} Secs");
            sb.AppendLine($"Program Uptime: {proc.TotalProcessorTime}");
            // process id
            sb.AppendLine($"PID: {proc.Id}");
            // exe name
            sb.AppendLine($"Executable: {System.Windows.Forms.Application.ExecutablePath}");
            sb.AppendLine($"Process Name: {proc}");
            sb.AppendLine($"Main Module Name: {proc.MainModule.ModuleName}");
            // exe stats
            sb.AppendLine($"Module Count: {proc.Modules.Count}");
            sb.AppendLine($"Thread Count: {proc.Threads.Count}");
            sb.AppendLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            sb.AppendLine($"Is Admin: {Permissions.IsUserAdministrator}");
            sb.AppendLine($"Is Debugged: {Debugger.IsAttached}");
            // versions
            sb.AppendLine($"Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            sb.AppendLine($"CLR Version: {Environment.Version}");

            var ex = _exception;
            for (var i = 0; ex != null; ex = ex.InnerException, i++)
            {
                sb.AppendLine();
                sb.AppendLine($"Type #{i} {ex.GetType()}");

                foreach (var propInfo in ex.GetType().GetProperties())
                {
                    string fieldName = $"{propInfo.Name} #{i}";
                    string fieldValue = $"{propInfo.GetValue(ex, null)}";

                    // Ignore stack trace + data
                    if (propInfo.Name == "StackTrace"
                        || propInfo.Name == "Data"
                        || string.IsNullOrEmpty(propInfo.Name)
                        || string.IsNullOrEmpty(fieldValue))
                        continue;

                    sb.AppendLine($"{fieldName}: {fieldValue}");
                }

                foreach (DictionaryEntry de in ex.Data)
                    sb.AppendLine($"Dictionary Entry #{i}: Key: {de.Key} Value: {de.Value}");
            }

            sb.AppendLine();
            sb.AppendLine("StackTrace:");
            sb.AppendLine(_exception.StackTrace);

            TextBoxInfo.Text = sb.ToString();
        }

        private void buttonDontSend_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            if (Main.Watcher != null)
                Main.Watcher.Exception(_exception);

            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (CheckBoxRestart.IsChecked.GetValueOrDefault())
            {
                Process.Start(Application.ResourceAssembly.Location, "/restart");
                Application.Current.Shutdown();
            }
        }
    }
}
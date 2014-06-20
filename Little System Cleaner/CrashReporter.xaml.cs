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

namespace Little_System_Cleaner
{
    /// <summary>
    /// Interaction logic for CrashReporter.xaml
    /// </summary>
    public partial class CrashReporter : Window
    {
        MemoryStream memoryStream = new MemoryStream();

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

            GenerateDialogReport(e);
        }

        /// <summary>
        /// Fills in text box with exception info
        /// </summary>
        /// <param name="e"></param>
        private void GenerateDialogReport(Exception e)
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
            sb.AppendLine(string.Format("Version: {0}", System.Windows.Forms.Application.ProductVersion));
            sb.AppendLine(string.Format("CLR Version: {0}", Environment.Version.ToString()));


            Exception ex = e;
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
            sb.AppendLine(e.StackTrace);

            this.textBox1.Text = sb.ToString();

            byte[] b = Encoding.ASCII.GetBytes(sb.ToString());
            memoryStream.Write(b, 0, b.Length);
        }

        private void buttonDontSend_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            SendBugReport();
            this.Close();
        }

        /// <summary>
        /// Uses PHP to upload bug report and email it
        /// </summary>
        /// <returns>True if it was sent</returns>
        private bool SendBugReport()
        {
            string fileFormName = "uploadedfile";
            string contenttype = "application/octet-stream";
            string fileName = "bugreport.txt";

            string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
            HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(Properties.Settings.Default.bugReportAddr);
            webrequest.CookieContainer = new CookieContainer();
            webrequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webrequest.Method = "POST";

            // Build up the post message header

            StringBuilder sb = new StringBuilder();
            sb.Append("--");
            sb.Append(boundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"");
            sb.Append(fileFormName);
            sb.Append("\"; filename=\"");
            sb.Append(fileName);
            sb.Append("\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: ");
            sb.Append(contenttype);
            sb.Append("\r\n");
            sb.Append("\r\n");

            string postHeader = sb.ToString();
            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(postHeader);

            // Build the trailing boundary string as a byte array
            // ensuring the boundary appears on a line by itself

            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            //FileStream fileStream = new FileStream(uploadfile, FileMode.Open, FileAccess.Read);
            long length = postHeaderBytes.Length + this.memoryStream.Length + boundaryBytes.Length;
            webrequest.ContentLength = length;

            try
            {
                Stream requestStream = webrequest.GetRequestStream();

                // Write out our post header
                requestStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);

                // Write out the file contents
                byte[] buffer = this.memoryStream.ToArray();
                requestStream.Write(buffer, 0, buffer.Length);

                requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            }
            catch (WebException e)
            {
                if (MessageBox.Show(this, e.Message + "\nClick OK to retry", "Error", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    return SendBugReport();
                else
                    return false;
            }

            // Write out the trailing boundary
            HttpWebResponse response = (HttpWebResponse)webrequest.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                MessageBox.Show(this, "Sent bug report successfully", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            else
            {
                MessageBox.Show(this, "The bug report could not be sent", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.checkBoxRestart.IsChecked.GetValueOrDefault())
            {
                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown();
            }
        }
    }
}

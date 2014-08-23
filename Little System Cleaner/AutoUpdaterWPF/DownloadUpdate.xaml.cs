using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Little_System_Cleaner.AutoUpdaterWPF
{
    /// <summary>
    /// Interaction logic for DownloadUpdate.xaml
    /// </summary>
    internal partial class DownloadUpdate : Window
    {
        private readonly string _downloadURL;

        private string _tempPath;

        public DownloadUpdate(string downloadURL)
        {
            InitializeComponent();

            _downloadURL = downloadURL;
        }

        private void DownloadUpdateDialogLoad(object sender, RoutedEventArgs e)
        {
            var webClient = new WebClient();

            var uri = new Uri(_downloadURL);

            string fileName;

            if (!string.IsNullOrEmpty(AutoUpdater.LocalFileName))
            {
                fileName = AutoUpdater.LocalFileName;
            }
            else
            {
                fileName = GetFileName(_downloadURL);

                if (string.IsNullOrEmpty(fileName))
                {
                    Debug.WriteLine("Unable to get filename from {0}", new object[] { _downloadURL });

                    this.Close();
                    return;
                }
            }

            _tempPath = string.Format(@"{0}{1}", Path.GetTempPath(), fileName);

            webClient.DownloadProgressChanged += OnDownloadProgressChanged;
            webClient.DownloadFileCompleted += OnDownloadComplete;

            webClient.DownloadFileAsync(uri, _tempPath);

        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this, "Unable to download update.\n" + e.Error.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();

                return;
            }

            var processStartInfo = new ProcessStartInfo {FileName = _tempPath, UseShellExecute = true};

            Process.Start(processStartInfo);

            if (App.Current.Dispatcher.Thread == System.Threading.Thread.CurrentThread) // Check if were on the main thread
            {
                Application.Current.Shutdown();
            }
            else
            {
                Environment.Exit(1);
            }
        }

        private static string GetFileName(string url)
        {
            var fileName = string.Empty;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            httpWebRequest.Method = "HEAD";
            httpWebRequest.AllowAutoRedirect = false;

            HttpWebResponse httpWebResponse = null;

            try
            {
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (WebException ex)
            {
                string message = "Unable to download file.\n" + ex.Message;
                MessageBox.Show(App.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return string.Empty;
            }

            if (httpWebResponse.StatusCode.Equals(HttpStatusCode.Redirect) || httpWebResponse.StatusCode.Equals(HttpStatusCode.Moved) || httpWebResponse.StatusCode.Equals(HttpStatusCode.MovedPermanently))
            {
                if (httpWebResponse.Headers["Location"] != null)
                {
                    var location = httpWebResponse.Headers["Location"];
                    fileName = GetFileName(location);
                    return fileName;
                }
            }

            if (httpWebResponse.Headers["content-disposition"] != null)
            {
                var contentDisposition = httpWebResponse.Headers["content-disposition"];
                if (!string.IsNullOrEmpty(contentDisposition))
                {
                    const string lookForFileName = "filename=";
                    var index = contentDisposition.IndexOf(lookForFileName, StringComparison.CurrentCultureIgnoreCase);
                    if (index >= 0)
                        fileName = contentDisposition.Substring(index + lookForFileName.Length);

                    if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                    {
                        fileName = fileName.Substring(1, fileName.Length - 2);
                    }
                }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                var uri = new Uri(url);

                fileName = Path.GetFileName(uri.LocalPath);
            }

            return fileName;
        }
    }
}

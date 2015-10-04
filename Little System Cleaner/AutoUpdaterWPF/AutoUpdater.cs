using Little_System_Cleaner.Misc;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace Little_System_Cleaner.AutoUpdaterWPF
{
    public enum RemindLaterFormat
    {
        Minutes,
        Hours,
        Days
    }

    /// <summary>
    /// Main class that lets you auto update applications by setting some static fields and executing its Start method.
    /// </summary>
    internal static class AutoUpdater
    {
        internal static string DialogTitle = string.Empty;

        internal static string ChangeLogUrl = string.Empty;

        internal static string DownloadUrl = string.Empty;

        internal static string LocalFileName = string.Empty;

        internal static string RegistryLocation = string.Empty;

        internal static string AppTitle = string.Empty;

        internal static Version CurrentVersion;

        internal static Version InstalledVersion;

        internal static bool ForceCheck;

        internal static Dispatcher MainDispatcher;

        internal static bool Running;

        /// <summary>
        /// URL of the xml file that contains information about latest version of the application.
        /// </summary>
        /// 
        internal static string AppCastUrl;

        /// <summary>
        /// Opens the download url in default browser if true. Very usefull if you have portable application.
        /// </summary>
        internal static bool OpenDownloadPage = false;

        /// <summary>
        /// If this is true users see dialog where they can set remind later interval otherwise it will take the interval from RemindLaterAt and RemindLaterTimeSpan fields.
        /// </summary>
        internal static bool LetUserSelectRemindLater = true;

        /// <summary>
        /// Remind Later interval after user should be reminded of update.
        /// </summary>
        internal static int RemindLaterAt = 2;

        /// <summary>
        /// Set if RemindLaterAt interval should be in Minutes, Hours or Days.
        /// </summary>
        internal static RemindLaterFormat RemindLaterTimeSpan = RemindLaterFormat.Days;

        /// <summary>
        /// Start checking for new version of application and display dialog to the user if update is available.
        /// </summary>
        /// <param name="forceUpdate">If true, ignores remind later and checks for update right away</param>
        internal static void Start(bool forceUpdate = false)
        {
            Start(AppCastUrl, forceUpdate);
        }

        /// <summary>
        /// Start checking for new version of application and display dialog to the user if update is available.
        /// </summary>
        /// <param name="appCast">URL of the xml file that contains information about latest version of the application.</param>
        /// <param name="forceUpdate">If true, ignores remind later and checks for update right away</param>
        internal static void Start(string appCast, bool forceUpdate = false)
        {
            if (Running)
            {
                MessageBox.Show(Application.Current.MainWindow, "An update check is already in progress.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AppCastUrl = appCast;
            ForceCheck = forceUpdate;

            CheckForUpdate();

            Running = true;
        }

        private async static void CheckForUpdate()
        {
            var mainAssembly = Assembly.GetEntryAssembly();
            var companyAttribute = (AssemblyCompanyAttribute)GetAttribute(mainAssembly, typeof(AssemblyCompanyAttribute));
            var titleAttribute = (AssemblyTitleAttribute)GetAttribute(mainAssembly, typeof(AssemblyTitleAttribute));
            AppTitle = titleAttribute != null ? titleAttribute.Title : mainAssembly.GetName().Name;
            var appCompany = companyAttribute != null ? companyAttribute.Company : "";

            RegistryLocation = !string.IsNullOrEmpty(appCompany) ? $@"Software\{appCompany}\{AppTitle}\AutoUpdater" : $@"Software\{AppTitle}\AutoUpdater";

            RegistryKey updateKey = null;
            object skip = null;
            object applicationVersion = null;
            object remindLaterTime = null;

            try
            {
               updateKey = Registry.CurrentUser.OpenSubKey(RegistryLocation);

                if (updateKey != null)
                {
                    skip = updateKey.GetValue("skip");
                    applicationVersion = updateKey.GetValue("version");
                    remindLaterTime = updateKey.GetValue("remindlater");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following exception occurred trying to retrieve update settings: " + ex.Message);
            }
            finally
            {
                updateKey?.Close();
            }


            if (ForceCheck == false && remindLaterTime != null)
            {
                DateTime remindLater = Convert.ToDateTime(remindLaterTime.ToString(), CultureInfo.CreateSpecificCulture("en-US"));

                int compareResult = DateTime.Compare(DateTime.Now, remindLater);

                if (compareResult < 0)
                {
                    var updateForm = new Update(true);
                    updateForm.SetTimer(remindLater);
                    return;
                }
            }

            var fileVersionAttribute = (AssemblyFileVersionAttribute)GetAttribute(mainAssembly, typeof(AssemblyFileVersionAttribute));
            InstalledVersion = new Version(fileVersionAttribute.Version);

            WebRequest webRequest = WebRequest.Create(AppCastUrl);
            webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            webRequest.Proxy = Utils.GetProxySettings();

            WebResponse webResponse;

            try
            {
                webResponse = await Task.Run(() => webRequest.GetResponse());
            }
            catch (Exception ex)
            {
                if (MainDispatcher != null) // Make sure MainDispatcher is set
                {
                    if (ForceCheck)
                    {
                        // Only display errors if user requested update check

                        if (ex is WebException)
                            Utils.MessageBoxThreadSafe("An error occurred connecting to the update server. Please check that you're connected to the internet and (if applicable) your proxy settings are correct.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        else
                            Utils.MessageBoxThreadSafe("The following error occurred: " + ex.Message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                
                return;
            }

            UpdateXml updateXml;

            Stream appCastStream = null;
            XmlTextReader reader = null;

            try
            {
                appCastStream = await Task.Run(() => webResponse.GetResponseStream());

                if (appCastStream == null)
                    throw new Exception("Response stream from update server was null.");

                XmlSerializer serializer = new XmlSerializer(typeof(UpdateXml));

                reader = new XmlTextReader(appCastStream);
                
                if (serializer.CanDeserialize(reader))
                    updateXml = (UpdateXml)serializer.Deserialize(reader);
                else
                    throw new Exception("Update file is in the wrong format.");
            }
            catch (Exception ex)
            {
                string message = $"The following error occurred trying to read update file: {ex.Message}";

                Debug.WriteLine(message);
                Utils.MessageBoxThreadSafe(message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }
            finally
            {
                reader.Close();

                appCastStream.Close();

                webResponse.Close();
            }

            foreach (UpdateXml.Item item in updateXml.Items)
            {
                if (item.Version != null)
                {
                    if (item.Version <= InstalledVersion)
                        continue;

                    CurrentVersion = item.Version;
                }
                else
                    continue;

                DialogTitle = item.Title;
                ChangeLogUrl = item.ChangeLog;
                DownloadUrl = item.Url;
                LocalFileName = item.FileName;
            }

            if (CurrentVersion != null && CurrentVersion > InstalledVersion)
            {
                if (skip != null && applicationVersion != null)
                {
                    string skipValue = skip.ToString();
                    var skipVersion = new Version(applicationVersion.ToString());

                    if (skipValue.Equals("1") && CurrentVersion <= skipVersion)
                        return;

                    if (CurrentVersion > skipVersion)
                    {
                        RegistryKey updateKeyWrite = null;

                        try
                        {
                            updateKeyWrite = Registry.CurrentUser.CreateSubKey(RegistryLocation);

                            if (updateKeyWrite != null)
                            {
                                updateKeyWrite.SetValue("version", CurrentVersion.ToString());
                                updateKeyWrite.SetValue("skip", 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            string message = "The following error occurred trying to save update update settings: " + ex.Message;

                            Debug.WriteLine(message);
                            Utils.MessageBoxThreadSafe(message, Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            updateKeyWrite?.Close();
                        }
                    }
                }

                await Application.Current.Dispatcher.InvokeAsync(ShowUI);
            }
            else if (ForceCheck)
            {
                Utils.MessageBoxThreadSafe(Properties.Resources.updateLatest, Properties.Resources.updateTitle, MessageBoxButton.OK, MessageBoxImage.Information);

            }
        }

        private static void ShowUI()
        {
            var updateForm = new Update();

            updateForm.Show();

            // Focus window (so it's not hidden behind main window)
            updateForm.Topmost = true;
            updateForm.Topmost = false;
            updateForm.Focus();
        }

        private static Attribute GetAttribute(Assembly assembly, Type attributeType)
        {
            var attributes = assembly.GetCustomAttributes(attributeType, false);
            if (attributes.Length == 0)
            {
                return null;
            }
            return (Attribute)attributes[0];
        }
    }
}

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
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Shared;

namespace Little_System_Cleaner
{
    public class App : Application
    {
        /// <summary>
        /// </summary>
        public App()
        {
            StartupUri = new Uri("Main.xaml", UriKind.Relative);

            // Add resources
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("TreeStyles.xaml", UriKind.Relative)
            });
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("Themes/Generic.xaml", UriKind.Relative)
            });
        }

        /// <summary>
        /// </summary>
        [STAThread]
        private static void Main()
        {
            bool mutexCreated;

            var parseArgs = new ParseArgs(Environment.GetCommandLineArgs());

            var waitToExit = parseArgs.Arguments.ContainsKey("restart");

            using (var mutexMain = new Mutex(true, "Little System Cleaner", out mutexCreated))
            {
                if (!mutexCreated)
                {
                    if (waitToExit)
                    {
                        try
                        {
                            if (!mutexMain.WaitOne())
                            {
                                Debug.WriteLine("Unable to acquire mutex");
                                Environment.Exit(0);
                            }
                        }
                        catch (AbandonedMutexException)
                        {
                            Debug.WriteLine("Mutex was abandoned");
                        }
                    }
                    else
                    {
                        // If mutex isnt available, show message and exit...
                        MessageBox.Show("Another program seems to be already running...", Utils.ProductName,
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                if (!IsAssemblyLoaded("CommonTools", Utils.ProductVersion))
                {
                    MessageBox.Show(
                        "It appears that CommonTools.dll is not loaded, because of this, Little System Cleaner cannot be loaded.\n\nPlease ensure that the file is located in the same folder as Little System Cleaner and that the version is at least 1.0.",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!IsAssemblyLoaded("Xceed.Wpf.Toolkit", new Version(2, 0, 0, 0), true))
                {
                    MessageBox.Show(
                        "It appears that Xceed.Wpf.Toolkit.dll is not loaded, because of this, Little System Cleaner cannot be loaded.\n\nPlease ensure that the file is located in the same folder as Little System Cleaner and that the version is at least 2.0.",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var app = new App();

                Permissions.SetPrivileges(true);
                app.Run();
                Permissions.SetPrivileges(false);

                mutexMain.ReleaseMutex();
            }
        }

        /// <summary>
        ///     Checks if assembly is loaded or not
        /// </summary>
        /// <param name="assembly">
        ///     The name of the assembly (ie: System.Data.XYZ). This sometimes is (but not always) also the
        ///     namespace of the assembly.
        /// </param>
        /// <param name="ver">What the version of the assembly should be. Set to null for any version (default is null)</param>
        /// <param name="versionCanBeGreater">
        ///     If true, the version of the assembly can be the same or greater than the specified
        ///     version. Otherwise, the version must be the exact same as the assembly.
        /// </param>
        /// <param name="publicKeyToken">
        ///     What the public key token of the assembly should be. Set to null for any public key token
        ///     (default is null). This needs to be 8 bytes.
        /// </param>
        /// <returns>True if the assembly is loaded</returns>
        /// <remarks>
        ///     Please note that if versionCanBeGreater is set to true and publicKeyToken is not null, this function can
        ///     return false even though the the version of the assembly is greater. This is due to the fact that the public key
        ///     token is derived from the certificate used to sign the file and this certificate can change over time.
        /// </remarks>
        public static bool IsAssemblyLoaded(string assembly, Version ver = null, bool versionCanBeGreater = false,
            byte[] publicKeyToken = null)
        {
            if (string.IsNullOrWhiteSpace(assembly))
                throw new ArgumentNullException(nameof(assembly), "The assembly name cannot be null or empty");

            if ((publicKeyToken != null) && publicKeyToken.Length != 8)
                throw new ArgumentException("The public key token must be 8 bytes long", nameof(publicKeyToken));

            // Do not get Assembly from App because this function is called before App is initialized
            var asm = Assembly.GetExecutingAssembly();

            foreach (var asmLoaded in asm.GetReferencedAssemblies())
            {
                if (asmLoaded.Name == assembly)
                {
                    if (ver != null)
                    {
                        var n = asmLoaded.Version.CompareTo(ver);

                        if (n < 0)
                            // version cannot be less
                            continue;

                        if (!versionCanBeGreater && n > 0)
                            // version cannot be greater
                            continue;
                    }

                    var asmPublicKeyToken = asmLoaded.GetPublicKeyToken();
                    if ((publicKeyToken != null) && !publicKeyToken.SequenceEqual(asmPublicKeyToken))
                        continue;

                    return true;
                }
            }

            return false;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += Application_DispatcherUnhandledException;
            Exit += App_Exit;

            base.OnStartup(e);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CrashReporter.ShowCrashReport(e.ExceptionObject as Exception);
        }

        private static void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Handled)
                return;

            CrashReporter.ShowCrashReport(e.Exception);

            e.Handled = true;
        }

        private static void App_Exit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}
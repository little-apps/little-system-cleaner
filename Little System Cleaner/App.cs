﻿/*
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

                if (!Utils.IsAssemblyLoaded("CommonTools", Utils.ProductVersion))
                {
                    MessageBox.Show(
                        "It appears that CommonTools.dll is not loaded, because of this, Little System Cleaner cannot be loaded.\n\nPlease ensure that the file is located in the same folder as Little System Cleaner and that the version is at least 1.0.",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!Utils.IsAssemblyLoaded("Xceed.Wpf.Toolkit", new Version(2, 0, 0, 0), true))
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
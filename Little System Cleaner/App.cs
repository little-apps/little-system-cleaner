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
using System.IO;
using System.Net;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Threading;
using Microsoft.Win32;
using System.Xml;
using System.Diagnostics;

namespace Little_System_Cleaner
{
    public partial class App : System.Windows.Application
    {
        /// <summary>
        /// 
        /// </summary>
        [STAThread]
        static void Main()
        {
            int i = 0;

            bool bMutexCreated = false;
            Mutex mutexMain = new Mutex(true, "Little System Cleaner", out bMutexCreated);

            // If mutex isnt available, show message and exit...
            if (!bMutexCreated)
            {
                MessageBox.Show("Another program seems to be already running...", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Splasher.Splash = new SplashScreen.SplashScreen();
            //Splasher.ShowSplash();

            //Thread thread = new Thread(new ThreadStart(InitHives));
            //thread.Start();
            //thread.Join();

            new App();

            mutexMain.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        public App()
        {
            StartupUri = new System.Uri("Main.xaml", UriKind.Relative);

            // Add resources
            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new System.Uri("TreeStyles.xaml", UriKind.Relative) });
            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new System.Uri("Themes/Generic.xaml", UriKind.Relative) });
            
            Permissions.SetPrivileges(true);
            Run();
            Permissions.SetPrivileges(false);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Application_DispatcherUnhandledException);
            this.Exit += new ExitEventHandler(App_Exit);

            base.OnStartup(e);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CrashReporter crashReporter = new CrashReporter(e.ExceptionObject as Exception);
            crashReporter.Show();
        }

        void App_Exit(object sender, ExitEventArgs e)
        {
            Little_System_Cleaner.Properties.Settings.Default.Save();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            CrashReporter crashReporter = new CrashReporter(e.Exception);
            crashReporter.Show();
            e.Handled = true;
        }

        

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // TODO: Add startup code here...

        }
    }
}

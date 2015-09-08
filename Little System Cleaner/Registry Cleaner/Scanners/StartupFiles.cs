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

using Microsoft.Win32;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Misc;
using System.Threading;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class StartupFiles : ScannerBase
    {
        public override string ScannerName => Strings.StartupFiles;

        internal static void Scan()
        {
            try
            {
                // all user keys
                CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run"));
                CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce"));
                CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices"));
                CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"));
                CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup"));
                CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"));
                CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunEx"));
                CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"));

                // current user keys
                CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run"));
                CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce"));
                CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices"));
                CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"));
                CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup"));
                CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"));
                CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunEx"));
                CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"));

                if (Utils.Is64BitOs)
                {
                    // all user keys
                    CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run"));
                    CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce"));
                    CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices"));
                    CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"));
                    CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup"));
                    CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce"));
                    CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunEx"));
                    CheckAutoRun(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run"));

                    // current user keys
                    CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run"));
                    CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce"));
                    CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices"));
                    CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"));
                    CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup"));
                    CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce"));
                    CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunEx"));
                    CheckAutoRun(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run"));
                }
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }

        /// <summary>
        /// Checks for invalid files in startup registry key
        /// </summary>
        /// <param name="regKey">The registry key to scan</param>
        private static void CheckAutoRun(RegistryKey regKey)
        {
            if (regKey == null)
                return;

            Wizard.Report.WriteLine("Checking for invalid files in " + regKey.Name);

            foreach (string progName in regKey.GetValueNames())
            {
                string runPath = regKey.GetValue(progName) as string;

                if (!string.IsNullOrEmpty(runPath))
                {
                    // Check run path by itself
                    if (Utils.FileExists(runPath) || Wizard.IsOnIgnoreList(runPath))
                        continue;

                    // See if file exists (also checks if string is null)
                    string filePath, args;

                    if (Utils.ExtractArguments(runPath, out filePath, out args))
                        continue;

                    if (Wizard.IsOnIgnoreList(filePath))
                        continue;

                    if (Utils.ExtractArguments2(runPath, out filePath, out args))
                        continue;

                    if (Wizard.IsOnIgnoreList(filePath))
                        continue;

                    Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.Name, (string.IsNullOrWhiteSpace(progName) ? "(default)" : progName));
                }
            }

            regKey.Close();
        }
        
    }
}

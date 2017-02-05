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

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;
using LittleSoftwareStats;
using Microsoft.Win32;
using Application = System.Windows.Forms.Application;

namespace Shared
{
    public static class Utils
    {
        /// <summary>
        ///     Returns true if the OS is 64 bit
        /// </summary>
        public static bool Is64BitOs => Environment.Is64BitOperatingSystem;

        /// <summary>
        ///     Returns Little System Cleaner
        /// </summary>
        public static string ProductName => Application.ProductName;

        /// <summary>
        ///     Returns current version of Little System Cleaner
        /// </summary>
        public static Version ProductVersion
        {
            get
            {
                var currentApp = Assembly.GetEntryAssembly();
                var name = new AssemblyName(currentApp.FullName);

                return name.Version;
            }
        }

        public static Window MainWindowInstance { get; set; }

        /// <summary>
        ///     Returns thread safe main window variable
        /// </summary>
        public static Window MainWindowThreadSafe
        {
            get
            {
                return System.Windows.Application.Current.Dispatcher.Thread != Thread.CurrentThread
                    ? System.Windows.Application.Current.Dispatcher.Invoke(() => MainWindowThreadSafe)
                    : MainWindowInstance;
            }
        }

        /// <summary>
        /// Gets the progress state of the window icon in the task bar
        /// </summary>
        public static TaskbarItemProgressState TaskbarProgressState
        {
            get
            {
                dynamic main = MainWindowInstance ?? System.Windows.Application.Current.MainWindow;
                return main?.TaskBarItemInfoPublic.ProgressState ?? TaskbarItemProgressState.None;
            }
            set
            {
                if (System.Windows.Application.Current == null)
                    return;

                dynamic currentWindow = MainWindowInstance ?? System.Windows.Application.Current.MainWindow;

                var taskBarItemInfo = currentWindow?.TaskBarItemInfoPublic;

                if (taskBarItemInfo != null)
                    taskBarItemInfo.ProgressState = value;
            }
        }

        /// <summary>
        /// Gets the progress value of the window icon in the task bar
        /// </summary>
        public static double TaskbarProgressValue
        {
            get
            {
                dynamic main = MainWindowInstance ?? System.Windows.Application.Current.MainWindow;
                if (main != null)
                    return main.TaskBarItemInfoPublic.ProgressValue;

                throw new NullReferenceException();
            }
            set
            {
                dynamic main = MainWindowInstance ?? System.Windows.Application.Current.MainWindow;
                var taskBarItemInfo = main?.TaskBarItemInfoPublic;

                if (taskBarItemInfo != null)
                    taskBarItemInfo.ProgressValue = value;
            }
        }

        /// <summary>
        /// Watcher object used by Little Software Stats
        /// </summary>
        public static Watcher Watcher { get; set; }

        /// <summary>
        /// Gets proxy settings
        /// </summary>
        /// <returns>IWebProxy with proxy settings</returns>
        public static IWebProxy GetProxySettings()
        {
            var webProxy = new WebProxy();
            
            switch (Settings.Default.optionsUseProxy)
            {
                case 0:
                    return webProxy;

                case 1:
                    return WebRequest.DefaultWebProxy;

                default:
                    if (String.IsNullOrEmpty(Settings.Default.optionsProxyHost) ||
                        Settings.Default.optionsProxyPort <= 0 || Settings.Default.optionsProxyPort >= 65535)
                        return webProxy;

                    webProxy.Address =
                        new Uri("http://" + Settings.Default.optionsProxyHost + ":" + Settings.Default.optionsProxyPort);
                    webProxy.BypassProxyOnLocal = false;

                    if (!Settings.Default.optionsProxyAuthenticate)
                        return webProxy;

                    using (var pass = DecryptString(Settings.Default.optionsProxyPassword))
                    {
                        webProxy.Credentials = new NetworkCredential(Settings.Default.optionsProxyUser, pass);
                    }

                    return webProxy;
            }
        }

        /// <summary>
        ///     Extracts the large or small icon
        /// </summary>
        /// <param name="path">Path to icon</param>
        /// <returns>Large or small icon or null</returns>
        public static Icon ExtractIcon(string path)
        {
            var largeIcon = IntPtr.Zero;
            var smallIcon = IntPtr.Zero;

            path = UnqouteSpaces(path);

            PInvoke.ExtractIconExA(path, 0, ref largeIcon, ref smallIcon, 1);

            //Transform the bits into the icon image
            Icon returnIcon = null;
            if (smallIcon != IntPtr.Zero)
                returnIcon = (Icon)Icon.FromHandle(smallIcon).Clone();
            else if (largeIcon != IntPtr.Zero)
                returnIcon = (Icon)Icon.FromHandle(largeIcon).Clone();

            //clean up
            PInvoke.DestroyIcon(smallIcon);
            PInvoke.DestroyIcon(largeIcon);

            return returnIcon;
        }

        /// <summary>
        ///     Resolves path to .lnk shortcut
        /// </summary>
        /// <param name="shortcut">The path to the shortcut</param>
        /// <param name="filepath">Returns the file path</param>
        /// <param name="arguments">Returns the shortcuts arguments</param>
        /// <returns>Returns false if the filepath doesnt exist</returns>
        /// <remarks>
        ///     TODO: Read the .LNK file as documented at https://msdn.microsoft.com/en-us/library/dd871305.aspx
        /// </remarks>
        public static bool ResolveShortcut(string shortcut, out string filepath, out string arguments)
        {
            var link = new PInvoke.ShellLink();
            ((PInvoke.IPersistFile)link).Load(shortcut, PInvoke.StgmRead);
            // TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.
            // ((IShellLinkW)link).Resolve(hwnd, 0)
            var path = new StringBuilder(PInvoke.MaxPath);
            PInvoke.Win32FindDataWide data;
            ((PInvoke.IShellLinkW)link).GetPath(path, path.Capacity, out data, 0);

            var args = new StringBuilder(PInvoke.MaxPath);
            ((PInvoke.IShellLinkW)link).GetArguments(args, args.Capacity);

            filepath = path.ToString();
            arguments = args.ToString();

            return FileExists(filepath);
        }

        /// <summary>
        /// Sanitizes the file path (checks for environment variables, illegal characters, etc)
        /// </summary>
        /// <param name="filePath">File Path</param>
        /// <returns>Sanitized file path, or, an empty string</returns>
        public static string SanitizeFilePath(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                return String.Empty;

            var fileName = String.Copy(filePath.Trim().ToLower());

            // Remove quotes
            fileName = UnqouteSpaces(fileName);

            // Remove environment variables
            fileName = Environment.ExpandEnvironmentVariables(fileName);

            // Check for illegal characters
            return FindAnyIllegalChars(fileName) ? String.Empty : fileName;
        }

        /// <summary>
        ///     Sees if the file exists
        /// </summary>
        /// <remarks>
        ///     Always use this to check for files in the scanners! Also, be sure to check if file is ignored before adding it
        ///     as problematic
        /// </remarks>
        /// <param name="filePath">The filename (including path)</param>
        /// <returns>
        ///     True if it exists or if the path should be skipped. Otherwise, false if the file path is empty or doesnt exist
        /// </returns>
        public static bool FileExists(string filePath)
        {
            var fileName = SanitizeFilePath(filePath);

            // Now see if file exists
            return File.Exists(fileName) || PInvoke.PathFileExists(fileName) || SearchPath(fileName);
        }

        /// <summary>
        ///     Uses PathGetArgs and PathRemoveArgs API to extract file arguments
        /// </summary>
        /// <param name="cmdLine">Command Line</param>
        /// <param name="filePath">file path</param>
        /// <param name="fileArgs">arguments</param>
        /// <exception cref="ArgumentNullException">Thrown when cmdLine is null or empty</exception>
        /// <returns>False if the path doesnt exist</returns>
        public static bool ExtractArguments(string cmdLine, out string filePath, out string fileArgs)
        {
            var cmdLineStrBuilder = new StringBuilder(cmdLine.ToLower().Trim());

            filePath = fileArgs = "";

            if (String.IsNullOrEmpty(cmdLineStrBuilder.ToString()))
                throw new ArgumentNullException(nameof(cmdLine));

            fileArgs = Marshal.PtrToStringAuto(PInvoke.PathGetArgs(cmdLineStrBuilder.ToString()));

            PInvoke.PathRemoveArgs(cmdLineStrBuilder);

            filePath = String.Copy(cmdLineStrBuilder.ToString());

            return !String.IsNullOrEmpty(filePath) && FileExists(filePath);
        }

        /// <summary>
        ///     Parses the file location w/o windows API
        /// </summary>
        /// <param name="cmdLine">Command Line</param>
        /// <param name="filePath">file path</param>
        /// <param name="fileArgs">arguments</param>
        /// <exception cref="ArgumentNullException">Thrown when cmdLine is null or empty</exception>
        /// <returns>Returns true if file was located</returns>
        public static bool ExtractArguments2(string cmdLine, out string filePath, out string fileArgs)
        {
            var cmdLineCopy = String.Copy(cmdLine.ToLower().Trim());
            var ret = false;

            filePath = fileArgs = "";

            if (String.IsNullOrEmpty(cmdLineCopy))
                throw new ArgumentNullException(cmdLine);

            // Remove Quotes
            cmdLineCopy = UnqouteSpaces(cmdLineCopy);

            // Expand variables
            cmdLineCopy = Environment.ExpandEnvironmentVariables(cmdLineCopy);

            // Try to see file exists by combining parts
            var fileFullPath = new StringBuilder(260);
            var pos = 0;
            foreach (var ch in cmdLineCopy)
            {
                fileFullPath = fileFullPath.Append(ch);
                pos++;

                if (FindAnyIllegalChars(fileFullPath.ToString()))
                    break;

                // See if part exists
                if (!File.Exists(fileFullPath.ToString()))
                    continue;

                filePath = String.Copy(fileFullPath.ToString());
                ret = true;
                break;
            }

            if (ret && pos > 0)
                fileArgs = cmdLineCopy.Remove(0, pos).Trim();

            return ret;
        }

        /// <summary>
        ///     Use to safely call function with registry key being open in parameter
        /// </summary>
        /// <param name="action">Function call</param>
        public static void SafeOpenRegistryKey(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message);
            }
        }

        /// <summary>
        ///     Converts the size in bytes to a formatted string
        /// </summary>
        /// <param name="length">Size in bytes</param>
        /// <param name="shortFormat">If true, displays units in short form (ie: Bytes becomes B)</param>
        /// <returns>Formatted String</returns>
        public static string ConvertSizeToString(long length, bool shortFormat = true)
        {
            if (length < 0)
                return "";

            decimal size;
            string sizeFormatted, unit;

            if (length < 1000) // 1KB
            {
                size = length;
                unit = shortFormat ? " B" : " Bytes";
            }
            else if (length < 1000000) // 1MB
            {
                size = length / (decimal)0x400;
                unit = shortFormat ? " KB" : " Kilobytes";
            }
            else if (length < 1000000000) // 1GB
            {
                size = length / (decimal)0x100000;
                unit = shortFormat ? " MB" : " Megabytes";
            }
            else
            {
                size = length / (decimal)0x40000000;
                unit = shortFormat ? " GB" : " Gigabytes";
            }

            if (Decimal.Subtract(size, size) == Decimal.Zero)
                sizeFormatted = size.ToString("0");
            else if (size < 10)
                sizeFormatted = size.ToString("0.00");
            else if (size < 100)
                sizeFormatted = size.ToString("0.0");
            else
                sizeFormatted = size.ToString("0");

            return sizeFormatted + unit;
        }

        /// <summary>
        ///     Returns special folder path specified by CSIDL
        /// </summary>
        /// <param name="csidl">CSIDL</param>
        /// <returns>Special folder path</returns>
        public static string GetSpecialFolderPath(int csidl)
        {
            var path = new StringBuilder(260);

            return PInvoke.SHGetSpecialFolderPath(IntPtr.Zero, path, csidl, false) ? String.Copy(path.ToString()) : "";
        }

        /// <summary>
        /// Checks for filename in %PATH% variable
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>True if it was found</returns>
        public static bool SearchPath(string fileName)
        {
            string retPath;

            return SearchPath(fileName, null, out retPath);
        }

        /// <summary>
        /// Locates filename in %PATH% variable
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <param name="retPath">Full path of filename</param>
        /// <returns>True if it was found</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool SearchPath(string fileName, out string retPath)
        {
            return SearchPath(fileName, null, out retPath);
        }

        /// <summary>
        /// Checks for filename in specified path
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <param name="path">Path to search</param>
        /// <returns>True if it was found</returns>
        public static bool SearchPath(string fileName, string path)
        {
            string retPath;

            return SearchPath(fileName, path, out retPath);
        }

        /// <summary>
        ///     Checks for the file using the specified path and/or %PATH% variable
        /// </summary>
        /// <param name="fileName">The name of the file for which to search</param>
        /// <param name="path">The path to be searched for the file (searches %path% variable if null)</param>
        /// <param name="retPath">The path containing the file</param>
        /// <returns>True if it was found</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool SearchPath(string fileName, string path, out string retPath)
        {
            var buffer = new StringBuilder(260);

            var ret = PInvoke.SearchPath(!String.IsNullOrEmpty(path) ? path : null, fileName, null, 260, buffer, null);

            if (ret != 0 && !String.IsNullOrWhiteSpace(buffer.ToString()))
            {
                retPath = buffer.ToString();

                return true;
            }

            retPath = String.Empty;

            return false;
        }

        /// <summary>
        ///     Removes quotes from the path
        /// </summary>
        /// <param name="path">Path w/ quotes</param>
        /// <returns>Path w/o quotes</returns>
        public static string UnqouteSpaces(string path)
        {
            var sb = new StringBuilder(path);

            PInvoke.PathUnquoteSpaces(sb);

            return String.Copy(sb.ToString());
        }

        /// <summary>
        ///     Parses the path and checks for any illegal characters
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>Returns true if it contains illegal characters</returns>
        public static bool FindAnyIllegalChars(string path)
        {
            // Get directory portion of the path.
            var dirName = path;
            var fullFileName = "";
            int dirSepPosastPos;
            if ((dirSepPosastPos = path.LastIndexOf(Path.DirectorySeparatorChar)) >= 0)
            {
                dirName = path.Substring(0, dirSepPosastPos);

                // Get filename portion of the path.
                if (dirSepPosastPos >= 0 && dirSepPosastPos + 1 < path.Length)
                    fullFileName = path.Substring(dirSepPosastPos + 1);
            }

            // Find any characters in the directory that are illegal.
            if (dirName.IndexOfAny(Path.GetInvalidPathChars()) != -1) // Found invalid character in directory
                return true;

            // Find any characters in the filename that are illegal.
            if (!String.IsNullOrEmpty(fullFileName))
                if (fullFileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                    // Found invalid character in filename
                    return true;

            return false;
        }

        /// <summary>
        ///     Uses the FindExecutable API to search for the file that opens the specified document
        /// </summary>
        /// <param name="fileName">The document to search for</param>
        /// <returns>The file that opens the document</returns>
        public static string FindExecutable(string fileName)
        {
            var resultBuffer = new StringBuilder(1024);

            var ret = PInvoke.FindExecutableA(fileName, String.Empty, resultBuffer);

            return ret >= 32 ? resultBuffer.ToString() : $"Error: ({ret})";
        }

        /// <summary>
        ///     Shortens the registry hive path
        /// </summary>
        /// <param name="subKey">Path containing registry hive (EX: HKEY_CURRENT_USER/...) </param>
        /// <returns>Shortened registry path  (EX: HKCU/...) </returns>
        public static string PrefixRegPath(string subKey)
        {
            var subKeyUpper = subKey.ToUpper();

            if (subKeyUpper.StartsWith("HKEY_CLASSES_ROOT"))
            {
                subKey = subKey.Replace("HKEY_CLASSES_ROOT", "HKCR");
            }
            else if (subKeyUpper.StartsWith("HKEY_CURRENT_USER"))
            {
                subKey = subKey.Replace("HKEY_CURRENT_USER", "HKCU");
            }
            else if (subKeyUpper.StartsWith("HKEY_LOCAL_MACHINE"))
            {
                subKey = subKey.Replace("HKEY_LOCAL_MACHINE", "HKLM");
            }
            else if (subKeyUpper.StartsWith("HKEY_USERS"))
            {
                subKey = subKey.Replace("HKEY_USERS", "HKU");
            }
            else if (subKeyUpper.StartsWith("HKEY_CURRENT_CONFIG"))
            {
                subKey = subKey.Replace("HKEY_CURRENT_CONFIG", "HKCC");
            }

            return subKey;
        }

        /// <summary>
        ///     Checks for suitable browser then launches URI
        /// </summary>
        /// <param name="webAddress">The address to launch</param>
        public static bool LaunchUri(string webAddress)
        {
            // Try default application for http://
            try
            {
                var keyValue =
                    Registry.GetValue(@"HKEY_CURRENT_USER\Software\Classes\http\shell\open\command", "", null) as string;
                if (!String.IsNullOrEmpty(keyValue))
                {
                    var browserPath = keyValue.Replace("%1", webAddress);
                    Process.Start(browserPath);

                    return true;
                }
            }
            catch
            {
                // ignored
            }

            // Try to open using Process.Start
            try
            {
                Process.Start(webAddress);

                return true;
            }
            catch
            {
                // ignored
            }

            // Try to open with 'explorer.exe' (on newer Windows systems, this will open the default for http://)
            try
            {
                string browserPath;

                if (SearchPath("explorer.exe", out browserPath))
                {
                    var argUrl = "\"" + webAddress + "\"";

                    Process.Start(browserPath, argUrl);

                    return true;
                }
            }
            catch
            {
                // ignored
            }

            // Try to open with 'firefox.exe'
            try
            {
                var progFilesDir = Is64BitOs
                    ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                    : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var firefoxPath = progFilesDir + @"\Mozilla Firefox\firefox.exe";

                if (File.Exists(firefoxPath))
                {
                    var argUrl = "\"" + webAddress + "\"";

                    Process.Start(firefoxPath, argUrl);

                    return true;
                }
            }
            catch
            {
                // ignored
            }

            // Try to open with 'chrome.exe'
            try
            {
                var chromePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                                 @"\Google\Chrome\Application\chrome.exe";

                if (File.Exists(chromePath))
                {
                    var argUrl = "\"" + webAddress + "\"";

                    Process.Start(chromePath, argUrl);

                    return true;
                }
            }
            catch
            {
                // ignored
            }

            // return false, all failed
            return false;
        }

        /// <summary>
        ///     Converts the string representation to its equivalent GUID
        /// </summary>
        /// <param name="s">String containing the GUID to be converted</param>
        /// <param name="guid">If conversion is sucessful, this parameter is the GUID value of the string. Otherwise, it is empty.</param>
        /// <returns>True if the conversion succeeded</returns>
        public static bool TryParseGuid(string s, out Guid guid)
        {
            guid = Guid.Empty;

            try
            {
                if (String.IsNullOrEmpty(s))
                    return false;

                s = s.Trim();

                if (
                    !Regex.IsMatch(s,
                        @"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$"))
                    return false;

                guid = new Guid(s);
            }
            catch
            {
                return false;
            }

            return true;
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
            if (String.IsNullOrWhiteSpace(assembly))
                throw new ArgumentNullException(nameof(assembly), "The assembly name cannot be null or empty");

            if ((publicKeyToken != null) && publicKeyToken.Length != 8)
                throw new ArgumentException("The public key token must be 8 bytes long", nameof(publicKeyToken));

            // Do not get Assembly from App because this function is called before App is initialized
            var asm = Assembly.GetCallingAssembly();

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

        /// <summary>
        ///     Compares wildcard to string
        /// </summary>
        /// <param name="wildString">String to compare</param>
        /// <param name="mask">Wildcard mask (ex: *.jpg)</param>
        /// <param name="ignoreCase">Ignore case for comparison (default is true)</param>
        /// <returns>True if match found</returns>
        public static bool CompareWildcard(string wildString, string mask, bool ignoreCase = true)
        {
            // Cannot continue with Mask empty
            if (String.IsNullOrEmpty(mask))
                return false;

            // If WildString is null -> make it an empty string
            if (wildString == null)
                wildString = String.Empty;

            // If Mask is * and WildString isn't empty -> return true
            if (mask == "*" && !String.IsNullOrEmpty(wildString))
                return true;

            // If Mask is ? and WildString length is 1 -> return true
            if (mask == "?" && wildString.Length == 1)
                return true;

            // If WildString and Mask match -> no need to go any further
            if (String.Compare(wildString, mask, ignoreCase) == 0)
                return true;

            var regExPattern = "^" + Regex.Escape(mask).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            var regEx = new Regex(regExPattern,
                ignoreCase ? RegexOptions.Singleline | RegexOptions.IgnoreCase : RegexOptions.Singleline);

            return regEx.IsMatch(wildString);
        }
        
        /// <summary>
        ///     Displays message box with main window as owner window using thread safe method
        /// </summary>
        /// <param name="messageBoxText">Text to display in message box</param>
        /// <param name="caption">Caption for message box</param>
        /// <param name="button">Message box button(s)</param>
        /// <param name="icon">Message box icon</param>
        /// <returns>Returns MessageBoxResult (the button the user clicked)</returns>
        public static MessageBoxResult MessageBoxThreadSafe(string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBoxThreadSafe(MainWindowThreadSafe, messageBoxText, caption, button, icon);
        }

        /// <summary>
        ///     Displays message box using thread safe method
        /// </summary>
        /// <param name="owner">Owner window</param>
        /// <param name="messageBoxText">Text to display in message box</param>
        /// <param name="caption">Caption for message box</param>
        /// <param name="button">Message box button(s)</param>
        /// <param name="icon">Message box icon</param>
        /// <returns>Returns MessageBoxResult (the button the user clicked)</returns>
        public static MessageBoxResult MessageBoxThreadSafe(Window owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon)
        {
            Func<MessageBoxResult> showMsgBox = () => MessageBox.Show(owner, messageBoxText, caption, button, icon);

            return System.Windows.Application.Current.Dispatcher.Thread != Thread.CurrentThread
                ? System.Windows.Application.Current.Dispatcher.Invoke(showMsgBox)
                : showMsgBox();
        }

        /// <summary>
        ///     Displays message box asynchronously with main window as owner window using thread safe method
        /// </summary>
        /// <param name="messageBoxText">Text to display in message box</param>
        /// <param name="caption">Caption for message box</param>
        /// <param name="button">Message box button(s)</param>
        /// <param name="icon">Message box icon</param>
        /// <returns>Returns DispatcherOperation class</returns>
        public static DispatcherOperation MessageBoxThreadSafeAsync(string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBoxThreadSafeAsync(MainWindowThreadSafe, messageBoxText, caption, button, icon);
        }

        /// <summary>
        ///     Displays message box asynchronously using thread safe method
        /// </summary>
        /// <param name="owner">Owner window</param>
        /// <param name="messageBoxText">Text to display in message box</param>
        /// <param name="caption">Caption for message box</param>
        /// <param name="button">Message box button(s)</param>
        /// <param name="icon">Message box icon</param>
        /// <returns>Returns DispatcherOperation class</returns>
        public static DispatcherOperation MessageBoxThreadSafeAsync(Window owner, string messageBoxText,
            string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            Func<MessageBoxResult> showMsgBox = () => MessageBox.Show(owner, messageBoxText, caption, button, icon);

            return System.Windows.Application.Current.Dispatcher.BeginInvoke(showMsgBox);
        }

        #region SecureString Functions
        /// <summary>
        /// Gets hash specific to machine to encrypt strings
        /// </summary>
        private static byte[] GetMachineHash
        {
            get
            {
                var machineName = Environment.MachineName;

                var macId = "NOTFOUND";
                try
                {
                    var nics = NetworkInterface.GetAllNetworkInterfaces();

                    if (nics.Length > 0)
                    {
                        foreach (var nic in nics.Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                            )
                        {
                            macId = nic.GetPhysicalAddress().ToString();
                            break;
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                string[] hardDriveSerialNo = { "" };
                var mc = new ManagementClass("Win32_DiskDrive");
                foreach (
                    var o in
                        mc.GetInstances()
                            .Cast<ManagementBaseObject>()
                            .TakeWhile(o => String.IsNullOrEmpty(hardDriveSerialNo[0])))
                {
                    // Only get the first one
                    try
                    {
                        var mo = (ManagementObject)o;

                        hardDriveSerialNo[0] = mo["SerialNumber"].ToString();
                    }
                    catch
                    {
                        // ignored
                    }
                }

                MD5 md5 = new MD5CryptoServiceProvider();
                return md5.ComputeHash(Encoding.ASCII.GetBytes(machineName + macId + hardDriveSerialNo[0]));
            }
        }

        /// <summary>
        /// Encrypts SecureString
        /// </summary>
        /// <param name="input">SecureString</param>
        /// <returns>String encrypted with hash encoded in base64 or empty string if it couldnt be encrypted</returns>
        public static string EncryptString(SecureString input)
        {
            if (input.Length == 0)
                return String.Empty;

            var encryptedData = ProtectedData.Protect(
                Encoding.Unicode.GetBytes(ToInsecureString(input)),
                GetMachineHash,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Decrypts string into SecureString
        /// </summary>
        /// <param name="encryptedData">String encrypted with hash encoded in base64</param>
        /// <returns>SecureString or empty if it couldnt be decrypted</returns>
        public static SecureString DecryptString(string encryptedData)
        {
            if (String.IsNullOrWhiteSpace(encryptedData))
                return new SecureString();

            try
            {
                var decryptedData = ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData),
                    GetMachineHash,
                    DataProtectionScope.CurrentUser);
                return ToSecureString(Encoding.Unicode.GetString(decryptedData));
            }
            catch
            {
                return new SecureString();
            }
        }

        /// <summary>
        /// Converts string to SecureString
        /// </summary>
        /// <param name="input">string</param>
        /// <returns>SecureString</returns>
        /// <remarks>The input string should be destroyed after calling this in order for it be secured</remarks>
        private static SecureString ToSecureString(string input)
        {
            var secure = new SecureString();
            foreach (var c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        /// <summary>
        /// Converts SecureString to string
        /// </summary>
        /// <param name="input">SecureString</param>
        /// <returns>string</returns>
        public static string ToInsecureString(SecureString input)
        {
            string returnValue;

            var ptr = Marshal.SecureStringToBSTR(input);

            try
            {
                returnValue = Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }

            return returnValue;
        }

        #endregion SecureString Functions

        #region Registry Functions

        /// <summary>
        ///     Parses the registry key path and sees if exists
        /// </summary>
        /// <param name="inPath">The registry path (including hive)</param>
        /// <returns>True if it exists</returns>
        public static bool RegKeyExists(string inPath)
        {
            string baseKey, subKey;

            return ParseRegKeyPath(inPath, out baseKey, out subKey) && RegKeyExists(baseKey, subKey);
        }

        /// <summary>
        /// Checks if registry root and sub key exists 
        /// </summary>
        /// <param name="mainKey">Root key (HKEY_LOCAL_MACHINE, etc)</param>
        /// <param name="subKey">Sub key path</param>
        /// <returns>True if it exists</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool RegKeyExists(string mainKey, string subKey)
        {
            var reg = RegOpenKey(mainKey, subKey);

            if (reg == null)
                return false;

            reg.Close();

            return true;
        }

        /// <summary>
        ///     Opens a registry key
        /// </summary>
        /// <param name="regPath">Registry path (including hive)</param>
        /// <param name="openReadOnly">If true, opens the key with read-only access (default: true)</param>
        /// <param name="throwOnError">If true, throws an excpetion when an error occurs</param>
        /// <returns>Registry Key class</returns>
        public static RegistryKey RegOpenKey(string regPath, bool openReadOnly = true, bool throwOnError = false)
        {
            string mainKey, subKey;

            ParseRegKeyPath(regPath, out mainKey, out subKey, throwOnError);

            return RegOpenKey(mainKey, subKey, openReadOnly, throwOnError);
        }

        /// <summary>
        ///     Returns RegistryKey from specified hive and subkey
        /// </summary>
        /// <param name="mainKey">The hive (begins with HKEY)</param>
        /// <param name="subKey">The sub key (cannot be null or whitespace)</param>
        /// <param name="openReadOnly">If true, opens the key with read-only access (default: true)</param>
        /// <param name="throwOnError">If true, throws an excpetion when an error occurs</param>
        /// <returns>RegistryKey or null if error occurred</returns>
        public static RegistryKey RegOpenKey(string mainKey, string subKey, bool openReadOnly = true,
            bool throwOnError = false)
        {
            RegistryKey reg;

            try
            {
                switch (mainKey.ToUpper())
                {
                    case "HKEY_CLASSES_ROOT":
                        {
                            reg = Registry.ClassesRoot;
                            break;
                        }
                    case "HKEY_CURRENT_USER":
                        {
                            reg = Registry.CurrentUser;
                            break;
                        }
                    case "HKEY_LOCAL_MACHINE":
                        {
                            reg = Registry.LocalMachine;
                            break;
                        }
                    case "HKEY_USERS":
                        {
                            reg = Registry.Users;
                            break;
                        }
                    case "HKEY_CURRENT_CONFIG":
                        {
                            reg = Registry.CurrentConfig;
                            break;
                        }
                    default:
                        {
                            if (!throwOnError)
                                return null;

                            throw new Exception($"Unable to parse registry key.\nMain key: {mainKey}\nSub Key: {subKey}"); // break here
                        }
                }

                if (!String.IsNullOrWhiteSpace(subKey))
                    reg = reg.OpenSubKey(subKey, !openReadOnly);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred trying to open " + mainKey.ToUpper() + "/" + subKey + ": " +
                                ex.Message);

                if (throwOnError)
                    throw;

                return null;
            }

            return reg;
        }

        /// <summary>
        ///     Parses a registry key path and outputs the base and subkey to strings
        /// </summary>
        /// <param name="inPath">Registry key path</param>
        /// <param name="baseKey">Base Key (Hive name)</param>
        /// <param name="subKey">Sub Key Path</param>
        /// <param name="throwOnError">If true, throws an excpetion when an error occurs</param>
        /// <returns>True if the path was parsed successfully</returns>
        public static bool ParseRegKeyPath(string inPath, out string baseKey, out string subKey,
            bool throwOnError = false)
        {
            baseKey = subKey = "";

            if (String.IsNullOrEmpty(inPath))
                return false;

            var mainKeyName = inPath;

            try
            {
                var slashPos = mainKeyName.IndexOf("\\", StringComparison.Ordinal);
                if (slashPos > -1)
                {
                    baseKey = mainKeyName.Substring(0, slashPos);
                    subKey = mainKeyName.Substring(slashPos + 1);
                }
                else if (mainKeyName.ToUpper().StartsWith("HKEY"))
                {
                    baseKey = mainKeyName;
                }
                else
                {
                    if (throwOnError)
                        throw new Exception($"Unable to parse registry key ({inPath})");

                    return false;
                }
            }
            catch (Exception)
            {
                if (throwOnError)
                    throw;

                return false;
            }

            return true;
        }

        #endregion Registry Functions
    }
}
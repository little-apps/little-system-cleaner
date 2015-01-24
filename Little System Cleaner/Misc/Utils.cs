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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Registry_Cleaner.Scanners;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Security.Cryptography;
using System.Security;
using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using System.Windows.Shell;
using System.Linq;

namespace Little_System_Cleaner.Misc
{
    internal static class Utils
    {
        /// <summary>
        /// Returns true if the OS is 64 bit
        /// </summary>
        internal static bool Is64BitOS
        {
            get { return Environment.Is64BitOperatingSystem; }
        }

        internal static string ProductName
        {
            get { return Application.ProductName; }
        }

        internal static IWebProxy GetProxySettings()
        {
            WebProxy webProxy = new WebProxy();

            if (Properties.Settings.Default.optionsUseProxy == 0)
                return webProxy;
            else if (Properties.Settings.Default.optionsUseProxy == 1)
                return WebRequest.DefaultWebProxy;
            else {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.optionsProxyHost) && (Properties.Settings.Default.optionsProxyPort > 0 && Properties.Settings.Default.optionsProxyPort < 65535))
                {
                    webProxy.Address = new Uri("http://" + Properties.Settings.Default.optionsProxyHost + ":" + Properties.Settings.Default.optionsProxyPort);
                    webProxy.BypassProxyOnLocal = false;

                    if (Properties.Settings.Default.optionsProxyAuthenticate)
                    {
                        using (SecureString strPass = Utils.DecryptString(Properties.Settings.Default.optionsProxyPassword))
                        {
                            webProxy.Credentials = new NetworkCredential(Properties.Settings.Default.optionsProxyUser, strPass);
                        }
                    }

                    return webProxy;
                }
                else
                {
                    return webProxy;
                }
            }
        }

        #region SecureString Functions

        private static byte[] GetMachineHash {
            get
            {
                string machineName = Environment.MachineName;

                string macID = "NOTFOUND";
                try
                {
                    NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

                    if (nics != null && nics.Length > 0)
                    {
                        foreach (NetworkInterface nic in nics)
                        {
                            if (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                            {
                                macID = nic.GetPhysicalAddress().ToString();
                                break;
                            }
                        }
                    }

                }
                catch 
                {
                }

                string hardDriveSerialNo = "";
                System.Management.ManagementClass mc = new System.Management.ManagementClass("Win32_DiskDrive");
                foreach (System.Management.ManagementObject mo in mc.GetInstances())
                {
                    // Only get the first one
                    if (hardDriveSerialNo == "")
                    {
                        try
                        {
                            hardDriveSerialNo = mo["SerialNumber"].ToString();
                            break;
                        }
                        catch
                        {
                        }
                    }
                }

                MD5 md5 = new MD5CryptoServiceProvider();
                return md5.ComputeHash(Encoding.ASCII.GetBytes(machineName + macID + hardDriveSerialNo));
            }
        }

        internal static string EncryptString(SecureString input)
        {
            if (input.Length == 0)
                return string.Empty;

            byte[] encryptedData = ProtectedData.Protect(
                Encoding.Unicode.GetBytes(ToInsecureString(input)),
                Utils.GetMachineHash,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        internal static SecureString DecryptString(string encryptedData)
        {
            if (string.IsNullOrWhiteSpace(encryptedData))
                return new SecureString();

            try
            {
                byte[] decryptedData = ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData),
                    Utils.GetMachineHash,
                    DataProtectionScope.CurrentUser);
                return ToSecureString(Encoding.Unicode.GetString(decryptedData));
            }
            catch
            {
                return new SecureString();
            }
        }

        internal static SecureString ToSecureString(string input)
        {
            SecureString secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        internal static string ToInsecureString(SecureString input)
        {
            string returnValue = string.Empty;
            IntPtr ptr = Marshal.SecureStringToBSTR(input);
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
        #endregion

        #region Registry Functions
        /// <summary>
        /// Parses the registry key path and sees if exists
        /// </summary>
        /// <param name="InPath">The registry path (including hive)</param>
        /// <returns>True if it exists</returns>
        internal static bool RegKeyExists(string inPath)
        {
            string strBaseKey, strSubKey;

            if (!ParseRegKeyPath(inPath, out strBaseKey, out strSubKey))
                return false;

            return RegKeyExists(strBaseKey, strSubKey);
        }

        internal static bool RegKeyExists(string mainKey, string subKey)
        {
            bool bKeyExists = false;
            RegistryKey reg = RegOpenKey(mainKey, subKey);

            if (reg != null)
            {
                bKeyExists = true;
                reg.Close();
            }

            return bKeyExists;
        }

        /// <summary>
        /// Opens a registry key
        /// </summary>
        /// <param name="regPath">Registry path (including hive)</param>
        /// <param name="openReadOnly">If true, opens the key with read-only access (default: true)</param>
        /// <param name="throwOnError">If true, throws an excpetion when an error occurs</param>
        /// <returns>Registry Key class</returns>
        internal static RegistryKey RegOpenKey(string regPath, bool openReadOnly = true, bool throwOnError = false)
        {
            string mainKey = "", subKey = "";

            ParseRegKeyPath(regPath, out mainKey, out subKey, throwOnError);

            return RegOpenKey(mainKey, subKey, openReadOnly, throwOnError);
        }

        /// <summary>
        /// Returns RegistryKey from specified hive and subkey
        /// </summary>
        /// <param name="MainKey">The hive (begins with HKEY)</param>
        /// <param name="SubKey">The sub key (cannot be null or whitespace)</param>
        /// <param name="openReadOnly">If true, opens the key with read-only access (default: true)</param>
        /// <param name="throwOnError">If true, throws an excpetion when an error occurs</param>
        /// <returns>RegistryKey or null if error occurred</returns>
        internal static RegistryKey RegOpenKey(string MainKey, string SubKey, bool openReadOnly = true, bool throwOnError = false)
        {
            RegistryKey reg = null;

            try
            {
                if (MainKey.ToUpper().CompareTo("HKEY_CLASSES_ROOT") == 0)
                    reg = Registry.ClassesRoot;
                else if (MainKey.ToUpper().CompareTo("HKEY_CURRENT_USER") == 0)
                    reg = Registry.CurrentUser;
                else if (MainKey.ToUpper().CompareTo("HKEY_LOCAL_MACHINE") == 0)
                    reg = Registry.LocalMachine;
                else if (MainKey.ToUpper().CompareTo("HKEY_USERS") == 0)
                    reg = Registry.Users;
                else if (MainKey.ToUpper().CompareTo("HKEY_CURRENT_CONFIG") == 0)
                    reg = Registry.CurrentConfig;
                else
                {
                    if (throwOnError)
                    {
                        string message = string.Format("Unable to parse registry key.\nMain key: {0}\nSub Key: {1}", MainKey, SubKey);
                        throw new Exception(message);
                    }

                    return null; // break here
                }
                    

                if (!string.IsNullOrWhiteSpace(SubKey))
                    reg = reg.OpenSubKey(SubKey, (!openReadOnly));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred trying to open " + MainKey.ToUpper() + "/" + SubKey + ": " + ex.Message);

                if (throwOnError)
                    throw ex;

                return null;
            }

            return reg;
        }

        /// <summary>
        /// Parses a registry key path and outputs the base and subkey to strings
        /// </summary>
        /// <param name="inPath">Registry key path</param>
        /// <param name="baseKey">Base Key (Hive name)</param>
        /// <param name="subKey">Sub Key Path</param>
        /// <param name="throwOnError">If true, throws an excpetion when an error occurs</param>
        /// <returns>True if the path was parsed successfully</returns>
        internal static bool ParseRegKeyPath(string inPath, out string baseKey, out string subKey, bool throwOnError = false)
        {
            baseKey = subKey = "";

            if (string.IsNullOrEmpty(inPath))
                return false;

            string strMainKeyname = inPath;

            try
            {
                int nSlash = strMainKeyname.IndexOf("\\");
                if (nSlash > -1)
                {
                    baseKey = strMainKeyname.Substring(0, nSlash);
                    subKey = strMainKeyname.Substring(nSlash + 1);
                }
                else if (strMainKeyname.ToUpper().StartsWith("HKEY")) {
                    baseKey = strMainKeyname;
                }
                else
                {
                    if (throwOnError)
                    {
                        string message = string.Format("Unable to parse registry key ({0})", inPath);
                        throw new Exception(message);
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                if (throwOnError)
                    throw ex;

                return false;
            }

            return true;
        }
        #endregion

        internal enum VDTReturn
        {
            ValidDrive = 0,
            InvalidDrive = 1,
            SkipCheck = 3
        }

        /// <summary>
        /// Sees if path has valid drive type
        /// </summary>
        /// <param name="path">Path containing drive</param>
        /// <returns>ValidDriveTypeReturn enum</returns>
        internal static VDTReturn ValidDriveType(string path)
        {
            StringBuilder sb = new StringBuilder(path);
            if (PInvoke.PathStripToRoot(sb))
            {
                DriveType dt = PInvoke.GetDriveType(sb.ToString());

                if (Properties.Settings.Default.registryCleanerOptionsRemMedia)
                {
                    // Just return true if its on a removable media
                    if (dt == DriveType.Removable ||
                        dt == DriveType.Network ||
                        dt == DriveType.CDRom)
                        return VDTReturn.SkipCheck;
                }

                // Return false for unkown and no root dir
                if (dt == DriveType.NoRootDirectory ||
                    dt == DriveType.Unknown)
                    return VDTReturn.InvalidDrive;
            }

            return VDTReturn.ValidDrive;
        }


        /// <summary>
        /// Extracts the large or small icon
        /// </summary>
        /// <param name="Path">Path to icon</param>
        /// <returns>Large or small icon or null</returns>
        internal static Icon ExtractIcon(string Path)
        {
            IntPtr largeIcon = IntPtr.Zero;
            IntPtr smallIcon = IntPtr.Zero;

            string strPath = Utils.UnqouteSpaces(Path);

            PInvoke.ExtractIconExA(strPath, 0, ref largeIcon, ref smallIcon, 1);

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
        /// Resolves path to .lnk shortcut
        /// </summary>
        /// <param name="shortcut">The path to the shortcut</param>
        /// <param name="filepath">Returns the file path</param>
        /// <param name="arguments">Returns the shortcuts arguments</param>
        /// <returns>Returns false if the filepath doesnt exist</returns>
        internal static bool ResolveShortcut(string shortcut, out string filepath, out string arguments)
        {
            PInvoke.ShellLink link = new PInvoke.ShellLink();
            ((PInvoke.IPersistFile)link).Load(shortcut, PInvoke.STGM_READ);
            // TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.  
            // ((IShellLinkW)link).Resolve(hwnd, 0) 
            StringBuilder path = new StringBuilder(PInvoke.MAX_PATH);
            PInvoke.WIN32_FIND_DATAW data = new PInvoke.WIN32_FIND_DATAW();
            ((PInvoke.IShellLinkW)link).GetPath(path, path.Capacity, out data, 0);

            StringBuilder args = new StringBuilder(PInvoke.MAX_PATH);
            ((PInvoke.IShellLinkW)link).GetArguments(args, args.Capacity);

            filepath = path.ToString();
            arguments = args.ToString();

            if (!FileExists(filepath))
                return false;

            return true;
        }

        /// <summary>
        /// Sees if the file exists
        /// </summary>
        /// <remarks>Always use this to check for files in the scanners! Also, be sure to check if file is ignored before adding it as problematic</remarks>
        /// <param name="filePath">The filename (including path)</param>
        /// <returns>
        /// True if it exists or if the path should be skipped. Otherwise, false if the file path is empty or doesnt exist
        /// </returns>
        internal static bool FileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string strFileName = string.Copy(filePath.Trim().ToLower());

            // Remove quotes
            strFileName = Utils.UnqouteSpaces(strFileName);

            // Remove environment variables
            strFileName = Environment.ExpandEnvironmentVariables(strFileName);

            // Check for illegal characters
            if (Utils.FindAnyIllegalChars(strFileName))
                return false;

            // Check Drive Type
            Utils.VDTReturn ret = Utils.ValidDriveType(strFileName);
            if (ret == Utils.VDTReturn.InvalidDrive)
                return false;
            else if (ret == Utils.VDTReturn.SkipCheck)
                return true;

            // Now see if file exists
            if (File.Exists(strFileName))
                return true;

            if (PInvoke.PathFileExists(strFileName))
                return true;

            if (Utils.SearchPath(strFileName))
                return true;

            return false;
        }

        /// <summary>
        /// Uses PathGetArgs and PathRemoveArgs API to extract file arguments
        /// </summary>
        /// <param name="cmdLine">Command Line</param>
        /// <param name="filePath">file path</param>
        /// <param name="fileArgs">arguments</param>
        /// <exception cref="ArgumentNullException">Thrown when cmdLine is null or empty</exception>
        /// <returns>False if the path doesnt exist</returns>
        internal static bool ExtractArguments(string cmdLine, out string filePath, out string fileArgs)
        {
            StringBuilder strCmdLine = new StringBuilder(cmdLine.ToLower().Trim());

            filePath = fileArgs = "";

            if (string.IsNullOrEmpty(strCmdLine.ToString()))
                throw new ArgumentNullException("cmdLine");

            fileArgs = Marshal.PtrToStringAuto(PInvoke.PathGetArgs(strCmdLine.ToString()));

            PInvoke.PathRemoveArgs(strCmdLine);

            filePath = string.Copy(strCmdLine.ToString());

            if (!string.IsNullOrEmpty(filePath))
                if (FileExists(filePath))
                    return true;

            return false;
        }

        /// <summary>
        /// Parses the file location w/o windows API
        /// </summary>
        /// <param name="cmdLine">Command Line</param>
        /// <param name="filePath">file path</param>
        /// <param name="fileArgs">arguments</param>
        /// <exception cref="ArgumentNullException">Thrown when cmdLine is null or empty</exception>
        /// <returns>Returns true if file was located</returns>
        internal static bool ExtractArguments2(string cmdLine, out string filePath, out string fileArgs)
        {
            string strCmdLine = string.Copy(cmdLine.ToLower().Trim());
            bool bRet = false;

            filePath = fileArgs = "";

            if (string.IsNullOrEmpty(strCmdLine))
                throw new ArgumentNullException(cmdLine);

            // Remove Quotes
            strCmdLine = Utils.UnqouteSpaces(strCmdLine);

            // Expand variables
            strCmdLine = Environment.ExpandEnvironmentVariables(strCmdLine);

            // Try to see file exists by combining parts
            StringBuilder strFileFullPath = new StringBuilder(260);
            int nPos = 0;
            foreach (char ch in strCmdLine.ToCharArray())
            {
                strFileFullPath = strFileFullPath.Append(ch);
                nPos++;

                if (Utils.FindAnyIllegalChars(strFileFullPath.ToString()))
                    break;

                // See if part exists
                if (File.Exists(strFileFullPath.ToString()))
                {
                    filePath = string.Copy(strFileFullPath.ToString());
                    bRet = true;
                    break;
                }
            }

            if (bRet && nPos > 0)
                fileArgs = strCmdLine.Remove(0, nPos).Trim();

            return bRet;
        }

        /// <summary>
        /// Use to safely call function with registry key being open in parameter
        /// </summary>
        /// <param name="action">Function call</param>
        internal static void SafeOpenRegistryKey(Action action)
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
        /// Converts the size in bytes to a formatted string
        /// </summary>
        /// <param name="Length">Size in bytes</param>
        /// <param name="shortFormat">If true, displays units in short form (ie: Bytes becomes B)</param>
        /// <returns>Formatted String</returns>
        internal static string ConvertSizeToString(long Length, bool shortFormat = true)
        {
            if (Length < 0)
                return "";

            float nSize;
            string strSizeFmt, strUnit = "";

            if (Length < 1000)             // 1KB
            {
                nSize = Length;
                strUnit = (shortFormat ? " B" : " Bytes");
            }
            else if (Length < 1000000)     // 1MB
            {
                nSize = Length / (float)0x400;
                strUnit = (shortFormat ? " KB" : " Kilobytes");
            }
            else if (Length < 1000000000)   // 1GB
            {
                nSize = Length / (float)0x100000;
                strUnit = (shortFormat ? " MB" : " Megabytes");
            }
            else
            {
                nSize = Length / (float)0x40000000;
                strUnit = (shortFormat ? " GB" : " Gigabytes");
            }

            if (nSize == (int)nSize)
                strSizeFmt = nSize.ToString("0");
            else if (nSize < 10)
                strSizeFmt = nSize.ToString("0.00");
            else if (nSize < 100)
                strSizeFmt = nSize.ToString("0.0");
            else
                strSizeFmt = nSize.ToString("0");

            return strSizeFmt + strUnit;
        }

        /// <summary>
        /// Calculates size of directory
        /// </summary>
        /// <param name="directory">DirectoryInfo class</param>
        /// <param name="includeSubdirectories">Includes sub directories if true</param>
        /// <returns>Size of directory in bytes</returns>
        internal static long CalculateDirectorySize(DirectoryInfo directory, bool includeSubdirectories)
        {
            long totalSize = 0;

            // Examine all contained files.
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                totalSize += file.Length;
            }

            // Examine all contained directories.
            if (includeSubdirectories)
            {
                DirectoryInfo[] dirs = directory.GetDirectories();
                foreach (DirectoryInfo dir in dirs)
                {
                    totalSize += CalculateDirectorySize(dir, true);
                }
            }

            return totalSize;
        }


        /// <summary>
        /// Returns special folder path specified by CSIDL
        /// </summary>
        /// <param name="CSIDL">CSIDL</param>
        /// <returns>Special folder path</returns>
        internal static string GetSpecialFolderPath(int CSIDL)
        {
            StringBuilder path = new StringBuilder(260);

            if (PInvoke.SHGetSpecialFolderPath(IntPtr.Zero, path, CSIDL, false))
                return string.Copy(path.ToString());

            return "";
        }

        internal static bool SearchPath(string fileName)
        {
            string retPath = "";

            return SearchPath(fileName, null, out retPath);
        }

        internal static bool SearchPath(string fileName, out string retPath)
        {
            return SearchPath(fileName, null, out retPath);
        }

        internal static bool SearchPath(string fileName, string Path)
        {
            string retPath = "";

            return SearchPath(fileName, Path, out retPath);
        }

        /// <summary>
        /// Checks for the file using the specified path and/or %PATH% variable
        /// </summary>
        /// <param name="fileName">The name of the file for which to search</param>
        /// <param name="Path">The path to be searched for the file (searches %path% variable if null)</param>
        /// <param name="retPath">The path containing the file</param>
        /// <returns>True if it was found</returns>
        internal static bool SearchPath(string fileName, string Path, out string retPath)
        {
            StringBuilder strBuffer = new StringBuilder(260);

            int ret = PInvoke.SearchPath(((!string.IsNullOrEmpty(Path)) ? (Path) : (null)), fileName, null, 260, strBuffer, null);

            if (ret != 0 && !string.IsNullOrWhiteSpace(strBuffer.ToString()))
            {
                retPath = strBuffer.ToString();

                return true;
            }
            else
            {
                retPath = string.Empty;
            }
            
            return false;
        }

        /// <summary>
        /// Removes quotes from the path
        /// </summary>
        /// <param name="Path">Path w/ quotes</param>
        /// <returns>Path w/o quotes</returns>
        internal static string UnqouteSpaces(string Path)
        {
            StringBuilder sb = new StringBuilder(Path);

            PInvoke.PathUnquoteSpaces(sb);

            return string.Copy(sb.ToString());
        }

        
        /// <summary>
        /// Parses the path and checks for any illegal characters
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>Returns true if it contains illegal characters</returns>
        internal static bool FindAnyIllegalChars(string path)
        {
            // Get directory portion of the path.
            string dirName = path;
            string fullFileName = "";
            int pos = 0;
            if ((pos = path.LastIndexOf(Path.DirectorySeparatorChar)) >= 0)
            {
                dirName = path.Substring(0, pos);

                // Get filename portion of the path.
                if (pos >= 0 && (pos + 1) < path.Length)
                    fullFileName = path.Substring(pos + 1);
            }

            // Find any characters in the directory that are illegal.
            if (dirName.IndexOfAny(Path.GetInvalidPathChars()) != -1) // Found invalid character in directory
                return true;

            // Find any characters in the filename that are illegal.
            if (!string.IsNullOrEmpty(fullFileName))
                if (fullFileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) // Found invalid character in filename
                    return true;

            return false;
        }

        /// <summary>
        /// Uses the FindExecutable API to search for the file that opens the specified document
        /// </summary>
        /// <param name="strFilename">The document to search for</param>
        /// <returns>The file that opens the document</returns>
        internal static string FindExecutable(string strFilename)
        {
            StringBuilder strResultBuffer = new StringBuilder(1024);

            long nResult = PInvoke.FindExecutableA(strFilename, string.Empty, strResultBuffer);

            if (nResult >= 32)
            {
                return strResultBuffer.ToString();
            }

            return string.Format("Error: ({0})", nResult);
        }

        /// <summary>
        /// Shortens the registry hive path
        /// </summary>
        /// <param name="SubKey">Path containing registry hive (EX: HKEY_CURRENT_USER/...) </param>
        /// <returns>Shortened registry path  (EX: HKCU/...) </returns>
        internal static string PrefixRegPath(string SubKey)
        {
            string strSubKey = string.Copy(SubKey);

            if (strSubKey.ToUpper().StartsWith("HKEY_CLASSES_ROOT"))
            {
                strSubKey = strSubKey.Replace("HKEY_CLASSES_ROOT", "HKCR");
            }
            else if (strSubKey.ToUpper().StartsWith("HKEY_CURRENT_USER"))
            {
                strSubKey = strSubKey.Replace("HKEY_CURRENT_USER", "HKCU");
            }
            else if (strSubKey.ToUpper().StartsWith("HKEY_LOCAL_MACHINE"))
            {
                strSubKey = strSubKey.Replace("HKEY_LOCAL_MACHINE", "HKLM");
            }
            else if (strSubKey.ToUpper().StartsWith("HKEY_USERS"))
            {
                strSubKey = strSubKey.Replace("HKEY_USERS", "HKU");
            }
            else if (strSubKey.ToUpper().StartsWith("HKEY_CURRENT_CONFIG"))
            {
                strSubKey = strSubKey.Replace("HKEY_CURRENT_CONFIG", "HKCC");
            }

            return strSubKey;
        }

        /// <summary>
        /// Checks for suitable browser then launches URI
        /// </summary>
        /// <param name="WebAddress">The address to launch</param>
        internal static bool LaunchURI(string WebAddress)
        {
            // Try default application for http://
            try
            {
                string keyValue = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Classes\http\shell\open\command", "", null) as string;
                if (!string.IsNullOrEmpty(keyValue))
                {
                    string browserPath = keyValue.Replace("%1", WebAddress);
                    Process.Start(browserPath);

                    return true;
                }
            }
            catch
            {
            }

            // Try to open using Process.Start
            try
            {
                Process.Start(WebAddress);

                return true;
            }
            catch
            {
            }

            // Try to open with 'explorer.exe' (on newer Windows systems, this will open the default for http://)
            try
            {
                string browserPath;

                if (SearchPath("explorer.exe", out browserPath))
                {
                    string argUrl = "\"" + WebAddress + "\"";

                    Process.Start(browserPath, argUrl);

                    return true;
                }

            }
            catch
            {
            }

            // Try to open with 'firefox.exe'
            try
            {
                string progFilesDir = (Utils.Is64BitOS ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
                string firefoxPath = progFilesDir + @"\Mozilla Firefox\firefox.exe";

                if (File.Exists(firefoxPath))
                {
                    string argUrl = "\"" + WebAddress + "\"";

                    Process.Start(firefoxPath, argUrl);

                    return true;
                }
            }
            catch
            {
            }

            // Try to open with 'chrome.exe'
            try
            {
                string chromePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\Application\chrome.exe";

                if (File.Exists(chromePath))
                {
                    string argUrl = "\"" + WebAddress + "\"";

                    Process.Start(chromePath, argUrl);

                    return true;
                }
            }
            catch
            {

            }

            // return false, all failed
            return false;
        }

        /// <summary>
        /// Converts the string representation to its equivalent GUID
        /// </summary>
        /// <param name="s">String containing the GUID to be converted</param>
        /// <param name="guid">If conversion is sucessful, this parameter is the GUID value of the string. Otherwise, it is empty.</param>
        /// <returns>True if the conversion succeeded</returns>
        internal static bool TryParseGuid(string s, out Guid guid)
        {
            guid = Guid.Empty;

            try
            {
                if (string.IsNullOrEmpty(s))
                    return false;

                s = s.Trim();

                if (!Regex.IsMatch(s, @"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$"))
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
        /// Auto resize columns
        /// </summary>
        internal static void AutoResizeColumns(System.Windows.Controls.ListView listView)
        {
            System.Windows.Controls.GridView gv = listView.View as System.Windows.Controls.GridView;

            if (gv != null)
            {
                foreach (System.Windows.Controls.GridViewColumn gvc in gv.Columns)
                {
                    // Set width to max value because actual width doesn't include margins
                    gvc.Width = double.MaxValue;

                    // Set it to NaN to remove white space
                    gvc.Width = double.NaN;
                }

                listView.UpdateLayout();
            }
        }

        /// <summary>
        /// Adds a suffix to a given number (1st, 2nd, 3rd, ...)
        /// </summary>
        /// <param name="number">Number to add suffix to</param>
        /// <returns>Number with suffix</returns>
        internal static string GetNumberSuffix(int number)
        {
            if (number <= 0)
                return number.ToString();

            int n = number % 100;

            // Skip the switch for as many numbers as possible.
            if (n > 3 && n < 21)
                return n.ToString() + "th";

            // Determine the suffix for numbers ending in 1, 2 or 3, otherwise add a 'th'
            switch (n % 10)
            {
                case 1: return n.ToString() + "st";
                case 2: return n.ToString() + "nd";
                case 3: return n.ToString() + "rd";
                default: return n.ToString() + "th";
            }
        }

        /// <summary>
        /// Converts a System.Drawing.Bitmap to a System.Controls.Image
        /// </summary>
        /// <param name="bitmap">Source</param>
        /// <returns>Image</returns>
        internal static System.Windows.Controls.Image CreateBitmapSourceFromBitmap(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            IntPtr hBitmap = bitmap.GetHbitmap();

            try
            {
                System.Windows.Controls.Image bMapImg = new System.Windows.Controls.Image();
                bMapImg.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );

                return bMapImg;
            }
            finally
            {
                if (hBitmap != null)
                    PInvoke.DeleteObject(hBitmap);
            }
        }

        /// <summary>
        /// Compares wildcard to string
        /// </summary>
        /// <param name="WildString">String to compare</param>
        /// <param name="Mask">Wildcard mask (ex: *.jpg)</param>
        /// <returns>True if match found</returns>
        internal static bool CompareWildcard(string WildString, string Mask, bool IgnoreCase = true)
        {
            // Cannot continue with Mask empty
            if (string.IsNullOrEmpty(Mask))
                return false;

            // If WildString is null -> make it an empty string
            if (WildString == null)
                WildString = string.Empty;

            // If Mask is * and WildString isn't empty -> return true
            if (string.Compare(Mask, "*") == 0 && !string.IsNullOrEmpty(WildString))
                return true;

            // If Mask is ? and WildString length is 1 -> return true
            if (string.Compare(Mask, "?") == 0 && WildString.Length == 1)
                return true;

            // If WildString and Mask match -> no need to go any further
            if (string.Compare(WildString, Mask, IgnoreCase) == 0)
                return true;

            string regExPattern = "^" + Regex.Escape(Mask).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            Regex regEx = new Regex(regExPattern, (IgnoreCase ? RegexOptions.Singleline | RegexOptions.IgnoreCase : RegexOptions.Singleline));
            
            return regEx.IsMatch(WildString);
        }

        /// <summary>
        /// Checks if assembly is loaded or not
        /// </summary>
        /// <param name="assembly">The name of the assembly (ie: System.Data.XYZ). This sometimes is (but not always) also the namespace of the assembly.</param>
        /// <param name="ver">What the version of the assembly should be. Set to null for any version (default is null)</param>
        /// <param name="versionCanBeGreater">If true, the version of the assembly can be greater than the specified version. Otherwise, the version must be the exact same as the assembly.</param>
        /// <param name="publicKeyToken">What the public key token of the assembly should be. Set to null for any public key token (default is null). This needs to be 8 bytes.</param>
        /// <returns>True if the assembly is loaded</returns>
        /// <remarks>Please note that if versionCanBeGreater is set to true and publicKeyToken is not null, this function can return false even though the the version of the assembly is greater. This is due to the fact that the public key token is derived from the certificate used to sign the file and this certificate can change over time.</remarks>
        internal static bool IsAssemblyLoaded(string assembly, Version ver = null, bool versionCanBeGreater = false, byte[] publicKeyToken = null) 
        {
            if (string.IsNullOrWhiteSpace(assembly))
                throw new ArgumentNullException("assembly", "The assembly name cannot be null or empty");

            if ((publicKeyToken != null) && publicKeyToken.Length != 8)
                throw new ArgumentException("The public key token must be 8 bytes long", "publicKeyToken");

            Assembly asm = Assembly.GetExecutingAssembly();

            foreach (AssemblyName asmLoaded in asm.GetReferencedAssemblies())
            {
                if (string.Compare(asmLoaded.Name, assembly) == 0)
                {
                    if (ver != null)
                    {
                        int n = asmLoaded.Version.CompareTo(ver);

                        if (!versionCanBeGreater && n != 0)
                            continue;
                        else if (versionCanBeGreater && n < 0)
                            continue;
                    }

                    byte[] asmPublicKeyToken = asmLoaded.GetPublicKeyToken();
                    if ((publicKeyToken != null) && !publicKeyToken.SequenceEqual(asmPublicKeyToken))
                        continue;

                    return true;
                }
            }

            return false;
        }
    }
}

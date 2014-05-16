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
using Microsoft.VisualBasic.FileIO;
using System.Net;
using System.Net.NetworkInformation;

namespace Little_System_Cleaner
{
    static class Utils
    {
        #region Signatures imported from http://pinvoke.net

        [DllImport("kernel32.dll")]
        public static extern int SearchPath(string strPath, string strFileName, string strExtension, uint nBufferLength, StringBuilder strBuffer, string strFilePart);
        [DllImport("kernel32.dll")]
        public static extern DriveType GetDriveType([MarshalAs(UnmanagedType.LPStr)] string lpRootPathName);
        [DllImport("kernel32.dll")]
        public static extern uint QueryDosDevice([In, Optional] string lpDeviceName, [Out] StringBuilder lpTargetPath, [In] int ucchMax);

        
        [DllImport("shell32.dll")]
        public static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);
        [DllImport("shell32.dll", EntryPoint = "FindExecutable")]
        public static extern long FindExecutableA(string lpFile, string lpDirectory, StringBuilder lpResult);
        [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
        public static extern int ExtractIconExA(string lpszFile, int nIconIndex, ref IntPtr phiconLarge, ref IntPtr phiconSmall, int nIcons);

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr PathGetArgs(string path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern void PathRemoveArgs([In, Out] StringBuilder path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int PathParseIconLocation([In, Out] StringBuilder path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern void PathUnquoteSpaces([In, Out] StringBuilder path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PathFileExists(string path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PathStripToRoot([In, Out] StringBuilder path);
        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PathRemoveFileSpec([In, Out] StringBuilder path);

        [DllImport("user32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int smIndex);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        // Used for SHGetSpecialFolderPath
        public const int CSIDL_STARTUP = 0x0007; // All Users\Startup
        public const int CSIDL_COMMON_STARTUP = 0x0018; // Common Users\Startup
        public const int CSIDL_PROGRAMS = 0x0002;   // All Users\Start Menu\Programs
        public const int CSIDL_COMMON_PROGRAMS = 0x0017;   // Start Menu\Programs

        #endregion
        #region Interop (IShellLink and IPersistFile)
        [Flags()]
        enum SLGP_FLAGS
        {
            /// <summary>Retrieves the standard short (8.3 format) file name</summary>
            SLGP_SHORTPATH = 0x1,
            /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
            SLGP_UNCPRIORITY = 0x2,
            /// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
            SLGP_RAWPATH = 0x4
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public long ftCreationTime;
            public long ftLastAccessTime;
            public long ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [Flags()]

        enum SLR_FLAGS
        {
            /// <summary>
            /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
            /// the high-order word of fFlags can be set to a time-out value that specifies the
            /// maximum amount of time to be spent resolving the link. The function returns if the
            /// link cannot be resolved within the time-out duration. If the high-order word is set
            /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            /// duration, in milliseconds.
            /// </summary>
            SLR_NO_UI = 0x1,
            /// <summary>Obsolete and no longer used</summary>
            SLR_ANY_MATCH = 0x2,
            /// <summary>If the link object has changed, update its path and list of identifiers.
            /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
            /// whether or not the link object has changed.</summary>
            SLR_UPDATE = 0x4,
            /// <summary>Do not update the link information</summary>
            SLR_NOUPDATE = 0x8,
            /// <summary>Do not execute the search heuristics</summary>
            SLR_NOSEARCH = 0x10,
            /// <summary>Do not use distributed link tracking</summary>
            SLR_NOTRACK = 0x20,
            /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
            /// removable media across multiple devices based on the volume name. It also uses the
            /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
            SLR_NOLINKINFO = 0x40,
            /// <summary>Call the Microsoft Windows Installer</summary>
            SLR_INVOKE_MSI = 0x80
        }


        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        interface IShellLinkW
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);
            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            void GetIDList(out IntPtr ppidl);
            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            void SetIDList(IntPtr pidl);
            /// <summary>Retrieves the description string for a Shell link object</summary>
            void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            /// <summary>Sets the command-line arguments for a Shell link object</summary>
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            /// <summary>Retrieves the hot key for a Shell link object</summary>
            void GetHotkey(out short pwHotkey);
            /// <summary>Sets a hot key for a Shell link object</summary>
            void SetHotkey(short wHotkey);
            /// <summary>Retrieves the show command for a Shell link object</summary>
            void GetShowCmd(out int piShowCmd);
            /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
            void SetShowCmd(int iShowCmd);
            /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
            void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                int cchIconPath, out int piIcon);
            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            /// <summary>Sets the relative path to the Shell link object</summary>
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);
            /// <summary>Sets the path and file name of a Shell link object</summary>
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);

        }

        [ComImport, Guid("0000010c-0000-0000-c000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersist
        {
            [PreserveSig]
            void GetClassID(out Guid pClassID);
        }


        [ComImport, Guid("0000010b-0000-0000-C000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPersistFile : IPersist
        {
            new void GetClassID(out Guid pClassID);
            [PreserveSig]
            int IsDirty();

            [PreserveSig]
            void Load([In, MarshalAs(UnmanagedType.LPWStr)]
            string pszFileName, uint dwMode);

            [PreserveSig]
            void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                [In, MarshalAs(UnmanagedType.Bool)] bool fRemember);

            [PreserveSig]
            void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

            [PreserveSig]
            void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
        }

        const uint STGM_READ = 0;
        const int MAX_PATH = 260;

        // CLSID_ShellLink from ShlGuid.h 
        [
            ComImport(),
            Guid("00021401-0000-0000-C000-000000000046")
        ]
        public class ShellLink
        {
        }

        #endregion

        /// <summary>
        /// Returns true if the OS is 64 bit
        /// </summary>
        public static bool Is64BitOS
        {
            get { return Environment.Is64BitOperatingSystem; }
        }

        public static string ProductName
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
        /// Parses a registry key path and outputs the base and subkey to strings
        /// </summary>
        /// <param name="inPath">Registry key path</param>
        /// <param name="baseKey">Base Key (Hive name)</param>
        /// <param name="subKey">Sub Key Path</param>
        /// <returns>True if the path was parsed successfully</returns>
        public static bool ParseRegKeyPath(string inPath, out string baseKey, out string subKey)
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
                else if (strMainKeyname.ToUpper().StartsWith("HKEY"))
                    baseKey = strMainKeyname;
                else
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses the registry key path and sees if exists
        /// </summary>
        /// <param name="InPath">The registry path (including hive)</param>
        /// <returns>True if it exists</returns>
        public static bool RegKeyExists(string inPath)
        {
            string strBaseKey, strSubKey;

            if (!ParseRegKeyPath(inPath, out strBaseKey, out strSubKey))
                return false;

            return RegKeyExists(strBaseKey, strSubKey);
        }

        public static bool RegKeyExists(string mainKey, string subKey)
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
        /// Checks if registry key + value name exist
        /// </summary>
        /// <param name="inPath">Registry Key</param>
        /// <param name="valueName">Value name</param>
        /// <returns>True if it exists</returns>
        public static bool ValueNameExists(string inPath, string valueName)
        {
            string baseKey, subKey;

            if (!ParseRegKeyPath(inPath, out baseKey, out subKey))
                return false;

            return ValueNameExists(baseKey, subKey, valueName);
        }


        /// <summary>
        /// Checks if registry key + value name exist
        /// </summary>
        /// <param name="mainKey">Registry Hive</param>
        /// <param name="subKey">Registry Sub Key</param>
        /// <param name="valueName">Value Name</param>
        /// <returns>True if value name exists in registry key</returns>
        public static bool ValueNameExists(string mainKey, string subKey, string valueName)
        {
            bool bKeyExists = false;
            RegistryKey reg = RegOpenKey(mainKey, subKey);

            try
            {
                if (reg != null)
                {
                    if (reg.GetValue(valueName) != null)
                        bKeyExists = true;
                    reg.Close();
                }
            }
            catch
            {
                return false;
            }

            return bKeyExists;
        }

        /// <summary>
        /// Opens a registry key
        /// </summary>
        /// <param name="regPath">Registry path (including hive)</param>
        /// <returns>Registry Key class</returns>
        public static RegistryKey RegOpenKey(string regPath)
        {
            string mainKey = "", subKey = "";

            ParseRegKeyPath(regPath, out mainKey, out subKey);

            return RegOpenKey(mainKey, subKey);
        }

        /// <summary>
        /// Returns RegistryKey from specified hive and subkey
        /// </summary>
        /// <param name="MainKey">The hive (begins with HKEY)</param>
        /// <param name="SubKey">The sub key (cannot be null or whitespace)</param>
        /// <returns>RegistryKey or null if error occurred</returns>
        public static RegistryKey RegOpenKey(string MainKey, string SubKey)
        {
            RegistryKey reg = null;

            if (string.IsNullOrWhiteSpace(SubKey))
            {
                return null;
            }

            try
            {
                if (MainKey.ToUpper().CompareTo("HKEY_CLASSES_ROOT") == 0)
                {
                    reg = Registry.ClassesRoot.OpenSubKey(SubKey, true);
                }
                else if (MainKey.ToUpper().CompareTo("HKEY_CURRENT_USER") == 0)
                {
                    reg = Registry.CurrentUser.OpenSubKey(SubKey, true);
                }
                else if (MainKey.ToUpper().CompareTo("HKEY_LOCAL_MACHINE") == 0)
                {
                    reg = Registry.LocalMachine.OpenSubKey(SubKey, true);
                }
                else if (MainKey.ToUpper().CompareTo("HKEY_USERS") == 0)
                {
                    reg = Registry.Users.OpenSubKey(SubKey, true);
                }
                else if (MainKey.ToUpper().CompareTo("HKEY_CURRENT_CONFIG") == 0)
                {
                    reg = Registry.CurrentConfig.OpenSubKey(SubKey, true);
                }
                else
                    return null; // break here
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred trying to open " + MainKey.ToUpper() + "/" + SubKey + ": " + ex.Message);
                return null;
            }

            return reg;
        }

        /// <summary>
        /// Gets the value kind and converts it accordingly
        /// </summary>
        /// <returns>Registry value formatted to a string</returns>
        public static string RegConvertXValueToString(RegistryKey regKey, string valueName)
        {
            string strRet = "";

            if (regKey == null)
                return strRet;

            try
            {

                switch (regKey.GetValueKind(valueName))
                {
                    case RegistryValueKind.MultiString:
                        {
                            string strValue = "";
                            string[] strValues = (string[])regKey.GetValue(valueName);

                            for (int i = 0; i < strValues.Length; i++)
                            {
                                if (i != 0)
                                    strValue = string.Concat(strValue, ",");

                                strValue = string.Format("{0} {1}", strValue, strValues[i]);
                            }

                            strRet = string.Copy(strValue);

                            break;
                        }
                    case RegistryValueKind.Binary:
                        {
                            string strValue = "";

                            foreach (byte b in (byte[])regKey.GetValue(valueName))
                                strValue = string.Format("{0} {1:X2}", strValue, b);

                            strRet = string.Copy(strValue);

                            break;
                        }
                    case RegistryValueKind.DWord:
                    case RegistryValueKind.QWord:
                        {
                            strRet = string.Format("0x{0:X} ({0:D})", regKey.GetValue(valueName));
                            break;
                        }
                    default:
                        {
                            strRet = string.Format("{0}", regKey.GetValue(valueName));
                            break;
                        }

                }
            }
            catch
            {
                return "";
            }

            return strRet;
        }

        internal static object TryGetValue(RegistryKey regKey, string valueName, object defaultValue = null)
        {
            object value = defaultValue;

            try
            {
                value = regKey.GetValue(valueName);

                if (value == null && defaultValue != null)
                    value = defaultValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to get registry value for " + valueName + " in " + regKey.ToString());
            }

            return value;
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
        #endregion

        /// <summary>
        /// Uses PathGetArgs and PathRemoveArgs API to extract file arguments
        /// </summary>
        /// <param name="cmdLine">Command Line</param>
        /// <param name="filePath">file path</param>
        /// <param name="fileArgs">arguments</param>
        /// <exception cref="ArgumentNullException">Thrown when cmdLine is null or empty</exception>
        /// <returns>False if the path doesnt exist</returns>
        public static bool ExtractArguments(string cmdLine, out string filePath, out string fileArgs)
        {
            StringBuilder strCmdLine = new StringBuilder(cmdLine.ToLower().Trim());

            filePath = fileArgs = "";

            if (string.IsNullOrEmpty(strCmdLine.ToString()))
                throw new ArgumentNullException("cmdLine");

            fileArgs = Marshal.PtrToStringAuto(PathGetArgs(strCmdLine.ToString()));

            PathRemoveArgs(strCmdLine);

            filePath = string.Copy(strCmdLine.ToString());

            if (!string.IsNullOrEmpty(filePath))
                if (Utils.FileExists(filePath))
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
        public static bool ExtractArguments2(string cmdLine, out string filePath, out string fileArgs)
        {
            string strCmdLine = string.Copy(cmdLine.ToLower().Trim());
            bool bRet = false;

            filePath = fileArgs = "";

            if (string.IsNullOrEmpty(strCmdLine))
                throw new ArgumentNullException(cmdLine);

            // Remove Quotes
            strCmdLine = UnqouteSpaces(strCmdLine);

            // Expand variables
            strCmdLine = Environment.ExpandEnvironmentVariables(strCmdLine);

            // Try to see file exists by combining parts
            StringBuilder strFileFullPath = new StringBuilder(260);
            int nPos = 0;
            foreach (char ch in strCmdLine.ToCharArray())
            {
                strFileFullPath = strFileFullPath.Append(ch);
                nPos++;

                if (FindAnyIllegalChars(strFileFullPath.ToString()))
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
        /// Resolves path to .lnk shortcut
        /// </summary>
        /// <param name="shortcut">The path to the shortcut</param>
        /// <param name="filepath">Returns the file path</param>
        /// <param name="arguments">Returns the shortcuts arguments</param>
        /// <returns>Returns false if the filepath doesnt exist</returns>
        public static bool ResolveShortcut(string shortcut, out string filepath, out string arguments)
        {
            ShellLink link = new ShellLink();
            ((IPersistFile)link).Load(shortcut, STGM_READ);
            // TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.  
            // ((IShellLinkW)link).Resolve(hwnd, 0) 
            StringBuilder path = new StringBuilder(MAX_PATH);
            WIN32_FIND_DATAW data = new WIN32_FIND_DATAW();
            ((IShellLinkW)link).GetPath(path, path.Capacity, out data, 0);

            StringBuilder args = new StringBuilder(MAX_PATH);
            ((IShellLinkW)link).GetArguments(args, args.Capacity);

            filepath = path.ToString();
            arguments = args.ToString();

            if (!Utils.FileExists(filepath))
                return false;

            return true;
        }

        /// <summary>
        /// Creates .lnk shortcut to filename
        /// </summary>
        /// <param name="filename">.lnk shortcut</param>
        /// <param name="path">path for filename</param>
        /// <param name="arguments">arguments for shortcut (can be null)</param>
        /// <returns>True if shortcut was created</returns>
        public static bool CreateShortcut(string filename, string path, string arguments)
        {
            ShellLink link = new ShellLink();
            ((IShellLinkW)link).SetPath(path);
            if (!string.IsNullOrEmpty(arguments))
                ((IShellLinkW)link).SetArguments(arguments);
            ((IPersistFile)link).Save(filename, false);

            return (File.Exists(filename));
        }

        /// <summary>
        /// Converts FILETIME structure to DateTime structure
        /// </summary>
        /// <param name="ft">FILETIME structure</param>
        /// <returns>DateTime structure</returns>
        public static DateTime FileTime2DateTime(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            DateTime dt = DateTime.MaxValue;
            long hFT2 = (((long)ft.dwHighDateTime) << 32) + ft.dwLowDateTime;

            try
            {
                dt = DateTime.FromFileTimeUtc(hFT2);
            }
            catch (ArgumentOutOfRangeException)
            {
                dt = DateTime.MaxValue;
            }

            return dt;
        }

        /// <summary>
        /// Converts the size in bytes to a formatted string
        /// </summary>
        /// <param name="Length">Size in bytes</param>
        /// <returns>Formatted String</returns>
        public static string ConvertSizeToString(long Length)
        {
            if (Length < 0)
                return "";

            float nSize;
            string strSizeFmt, strUnit = "";

            if (Length < 1000)             // 1KB
            {
                nSize = Length;
                strUnit = " B";
            }
            else if (Length < 1000000)     // 1MB
            {
                nSize = Length / (float)0x400;
                strUnit = " KB";
            }
            else if (Length < 1000000000)   // 1GB
            {
                nSize = Length / (float)0x100000;
                strUnit = " MB";
            }
            else
            {
                nSize = Length / (float)0x40000000;
                strUnit = " GB";
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
        public static long CalculateDirectorySize(DirectoryInfo directory, bool includeSubdirectories)
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
        public static string GetSpecialFolderPath(int CSIDL)
        {
            StringBuilder path = new StringBuilder(260);

            if (Utils.SHGetSpecialFolderPath(IntPtr.Zero, path, CSIDL, false))
                return string.Copy(path.ToString());

            return "";
        }

        public static bool SearchPath(string fileName)
        {
            string retPath = "";

            return SearchPath(fileName, null, out retPath);
        }

        public static bool SearchPath(string fileName, string Path)
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
        public static bool SearchPath(string fileName, string Path, out string retPath)
        {
            StringBuilder strBuffer = new StringBuilder(260);

            int ret = SearchPath(((!string.IsNullOrEmpty(Path)) ? (Path) : (null)), fileName, null, 260, strBuffer, null);

            if (ret != 0)
            {
                retPath = strBuffer.ToString();
                return true;
            }
            else
                retPath = "";

            return false;
        }

        /// <summary>
        /// Removes quotes from the path
        /// </summary>
        /// <param name="Path">Path w/ quotes</param>
        /// <returns>Path w/o quotes</returns>
        private static string UnqouteSpaces(string Path)
        {
            StringBuilder sb = new StringBuilder(Path);

            PathUnquoteSpaces(sb);

            return string.Copy(sb.ToString());
        }

        /// <summary>
        /// Gets the icon path and sees if it exists
        /// </summary>
        /// <param name="IconPath">The icon path</param>
        /// <returns>True if it exists</returns>
        public static bool IconExists(string IconPath)
        {
            string strFileName = string.Copy(IconPath.Trim().ToLower());

            // Remove quotes
            strFileName = UnqouteSpaces(strFileName);

            // Remove starting @
            if (strFileName.StartsWith("@"))
                strFileName = strFileName.Substring(1);

            // Return true if %1
            if (strFileName == "%1")
                return true;

            // Get icon path
            int nSlash = strFileName.IndexOf(',');
            if (nSlash > -1)
            {
                strFileName = strFileName.Substring(0, nSlash);

                return Utils.FileExists(strFileName);
            }
            else
            {
                StringBuilder sb = new StringBuilder(strFileName);
                if (PathParseIconLocation(sb) >= 0)
                    if (!string.IsNullOrEmpty(sb.ToString()))
                        return Utils.FileExists(sb.ToString());
            }

            return false;
        }

        /// <summary>
        /// Extracts the large or small icon
        /// </summary>
        /// <param name="Path">Path to icon</param>
        /// <returns>Large or small icon or null</returns>
        public static Icon ExtractIcon(string Path)
        {
            IntPtr largeIcon = IntPtr.Zero;
            IntPtr smallIcon = IntPtr.Zero;

            string strPath = UnqouteSpaces(Path);

            ExtractIconExA(strPath, 0, ref largeIcon, ref smallIcon, 1);

            //Transform the bits into the icon image
            Icon returnIcon = null;
            if (smallIcon != IntPtr.Zero)
                returnIcon = (Icon)Icon.FromHandle(smallIcon).Clone();
            else if (largeIcon != IntPtr.Zero)
                returnIcon = (Icon)Icon.FromHandle(largeIcon).Clone();

            //clean up
            DestroyIcon(smallIcon);
            DestroyIcon(largeIcon);

            return returnIcon;
        }

        enum VDTReturn
        {
            ValidDrive = 0,
            InvalidDrive = 1,
            SkipCheck = 3
        }

        /// <summary>
        /// Sees if path has valid type
        /// </summary>
        /// <param name="path">Path containing drive</param>
        /// <returns>ValidDriveTypeReturn enum</returns>
        private static VDTReturn ValidDriveType(string path)
        {
            StringBuilder sb = new StringBuilder(path);
            if (PathStripToRoot(sb))
            {
                DriveType dt = GetDriveType(sb.ToString());

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
        /// Sees if the file exists
        /// </summary>
        /// <remarks>Always use this to check for files in the scanners!</remarks>
        /// <param name="filePath">The filename (including path)</param>
        /// <returns>
        /// True if it exists or if the path should be skipped. Otherwise, false if the file path is empty or doesnt exist
        /// </returns>
        public static bool FileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string strFileName = string.Copy(filePath.Trim().ToLower());

            // Remove quotes
            strFileName = UnqouteSpaces(strFileName);

            // Remove environment variables
            strFileName = Environment.ExpandEnvironmentVariables(strFileName);

            // Check for illegal characters
            if (FindAnyIllegalChars(strFileName))
                return false;

            // Check Drive Type
            VDTReturn ret = ValidDriveType(strFileName);
            if (ret == VDTReturn.InvalidDrive)
                return false;
            else if (ret == VDTReturn.SkipCheck)
                return true;

            // See if it is on exclude list
            if (ScanWizard.IsOnIgnoreList(strFileName))
                return true;

            // Now see if file exists
            if (File.Exists(strFileName))
                return true;

            if (PathFileExists(strFileName))
                return true;

            if (SearchPath(strFileName))
                return true;

            return false;
        }

        /// <summary>
        /// Sees if the directory exists
        /// </summary>
        /// <remarks>Always use this to check for directories in the scanners!</remarks>
        /// <param name="dirPath">The directory</param>
        /// <returns>True if it exists or if the path should be skipped. Otherwise, false if the file path is empty or doesnt exist</returns>
        public static bool DirExists(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
                return false;

            string strDirectory = string.Copy(dirPath.Trim().ToLower());

            // Remove quotes
            strDirectory = UnqouteSpaces(strDirectory);

            // Expand enviroment variables
            strDirectory = Environment.ExpandEnvironmentVariables(strDirectory);

            // Check drive type
            VDTReturn ret = ValidDriveType(strDirectory);
            if (ret == VDTReturn.InvalidDrive)
                return false;
            else if (ret == VDTReturn.SkipCheck)
                return true;

            // Check for illegal chars
            if (FindAnyIllegalChars(strDirectory))
                return false;

            // See if it is on the exclude list
            if (ScanWizard.IsOnIgnoreList(strDirectory))
                return true;

            // Remove filename.ext and trailing backslash from path
            StringBuilder sb = new StringBuilder(strDirectory);
            if (PathRemoveFileSpec(sb))
                if (Directory.Exists(sb.ToString()))
                    return true;

            if (Directory.Exists(strDirectory))
                return true;

            return false;
        }

        /// <summary>
        /// Parses the path and checks for any illegal characters
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>Returns true if it contains illegal characters</returns>
        private static bool FindAnyIllegalChars(string path)
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
        public static string FindExecutable(string strFilename)
        {
            StringBuilder strResultBuffer = new StringBuilder(1024);

            long nResult = FindExecutableA(strFilename, string.Empty, strResultBuffer);

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
        public static string PrefixRegPath(string SubKey)
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
        /// Checks for default program then launches URI
        /// </summary>
        /// <param name="WebAddress">The address to launch</param>
        public static void LaunchURI(string WebAddress)
        {
            Help.ShowHelp(Form.ActiveForm, string.Copy(WebAddress));
        }

        /// <summary>
        /// Converts the string representation to its equivalent GUID
        /// </summary>
        /// <param name="s">String containing the GUID to be converted</param>
        /// <param name="guid">If conversion is sucessful, this parameter is the GUID value of the string. Otherwise, it is empty.</param>
        /// <returns>True if the conversion succeeded</returns>
        public static bool TryParseGuid(string s, out Guid guid)
        {
            guid = Guid.Empty;

            try
            {
                if (string.IsNullOrEmpty(s))
                    return false;

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
        public static void AutoResizeColumns(System.Windows.Controls.ListView listView)
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
        public static string GetNumberSuffix(int number)
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
        public static System.Windows.Controls.Image CreateBitmapSourceFromBitmap(System.Drawing.Bitmap bitmap)
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
                    DeleteObject(hBitmap);
            }
        }

        /// <summary>
        /// Checks if we have permission to delete a registry key
        /// </summary>
        /// <param name="key">Registry key</param>
        /// <returns>True if we can delete it</returns>
        public static bool CanDeleteKey(RegistryKey key)
        {
            try
            {
                if (key.SubKeyCount > 0)
                {
                    bool ret = false;

                    foreach (string subKey in key.GetSubKeyNames())
                    {
                        ret = CanDeleteKey(key.OpenSubKey(subKey));

                        if (!ret)
                            break;
                    }

                    return ret;
                }
                else
                {
                    System.Security.AccessControl.RegistrySecurity regSecurity = key.GetAccessControl();

                    foreach (System.Security.AccessControl.AuthorizationRule rule in regSecurity.GetAccessRules(true, false, typeof(System.Security.Principal.NTAccount)))
                    {
                        if ((System.Security.AccessControl.RegistryRights.Delete & ((System.Security.AccessControl.RegistryAccessRule)(rule)).RegistryRights) != System.Security.AccessControl.RegistryRights.Delete)
                        {
                            return false;
                        }
                    }

                    return true;
                }

            }
            catch (SecurityException)
            {
                return false;
            }
        }

        #region Disk Cleaner Functions
        /// <summary>
        /// Compare multiple wildcards to string
        /// </summary>
        /// <param name="WildString">String to compare</param>
        /// <param name="Mask">Wildcard masks seperated by a semicolon (;)</param>
        /// <returns>True if match found</returns>
        public static bool CompareWildcards(string WildString, string Mask, bool IgnoreCase = true)
        {
            int i = 0;

            if (String.IsNullOrEmpty(Mask))
                return false;
            if (Mask == "*")
                return true;

            while (i != Mask.Length)
            {
                if (CompareWildcard(WildString, Mask.Substring(i), IgnoreCase))
                    return true;

                while (i != Mask.Length && Mask[i] != ';')
                    i += 1;

                if (i != Mask.Length && Mask[i] == ';')
                {
                    i += 1;

                    while (i != Mask.Length && Mask[i] == ' ')
                        i += 1;
                }
            }

            return false;
        }

        /// <summary>
        /// Compares wildcard to string
        /// </summary>
        /// <param name="WildString">String to compare</param>
        /// <param name="Mask">Wildcard mask (ex: *.jpg)</param>
        /// <returns>True if match found</returns>
        public static bool CompareWildcard(string WildString, string Mask, bool IgnoreCase = true)
        {
            int i = 0, k = 0;

            while (k != WildString.Length)
            {
                switch (Mask[i])
                {
                    case '*':

                        if ((i + 1) == Mask.Length)
                            return true;

                        while (k != WildString.Length)
                        {
                            if (CompareWildcard(WildString.Substring(k + 1), Mask.Substring(i + 1), IgnoreCase))
                                return true;

                            k += 1;
                        }

                        return false;

                    case '?':

                        break;

                    default:

                        if (IgnoreCase == false && WildString[k] != Mask[i])
                            return false;

                        if (IgnoreCase && Char.ToLower(WildString[k]) != Char.ToLower(Mask[i]))
                            return false;

                        break;
                }

                i += 1;
                k += 1;
            }

            if (k == WildString.Length)
            {
                if (i == Mask.Length || Mask[i] == ';' || Mask[i] == '*')
                    return true;
            }

            return false;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.U4)]
            public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);
        const int FO_DELETE = 3;
        const int FOF_ALLOWUNDO = 0x40;
        const int FOF_NOCONFIRMATION = 0x10;    //Don't prompt the user.; 

        public static void SendFileToRecycleBin(string filePath)
        {
            SHFILEOPSTRUCT shf = new SHFILEOPSTRUCT();
            shf.wFunc = FO_DELETE;
            shf.fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION;
            shf.pFrom = filePath;
            SHFileOperation(ref shf);
        }
        #endregion

        #region Registry Optimizer Functions
        /// <summary>
        /// Gets a temporary path for a registry hive
        /// </summary>
        public static string GetTempHivePath()
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                // File cant exists, keep retrying until we get a file that doesnt exist
                if (File.Exists(tempPath))
                    return GetTempHivePath();

                return tempPath;
            }
            catch (IOException)
            {
                return GetTempHivePath();
            }
        }

        /// <summary>
        /// Converts \\Device\\HarddiskVolumeX\... to X:\...
        /// </summary>
        /// <param name="DevicePath">Device name with path</param>
        /// <returns>Drive path</returns>
        public static string ConvertDeviceToMSDOSName(string DevicePath)
        {
            string strDevicePath = string.Copy(DevicePath.ToLower());
            string strRetVal = "";

            // Convert \Device\HarddiskVolumeX\... to X:\...
            foreach (KeyValuePair<string, string> kvp in QueryDosDevice())
            {
                string strDrivePath = kvp.Key.ToLower();
                string strDeviceName = kvp.Value.ToLower();

                if (strDevicePath.StartsWith(strDeviceName))
                {
                    strRetVal = strDevicePath.Replace(strDeviceName, strDrivePath);
                    break;
                }
            }

            return strRetVal;
        }

        private static Dictionary<string, string> QueryDosDevice()
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                if (di.IsReady)
                {
                    string strDrivePath = di.Name.Substring(0, 2);
                    StringBuilder strDeviceName = new StringBuilder(260);

                    // Convert C: to \Device\HarddiskVolume1
                    if (QueryDosDevice(strDrivePath, strDeviceName, 260) > 0)
                        ret.Add(strDrivePath, strDeviceName.ToString());
                }
            }

            return ret;
        }

        /// <summary>
        /// Gets the old size of the registry hives
        /// </summary>
        /// <returns>Registry size (in bytes)</returns>
        public static long GetOldRegistrySize()
        {
            if (Little_System_Cleaner.Registry_Optimizer.Controls.Wizard.RegistryHives == null)
                return 0;

            if (Little_System_Cleaner.Registry_Optimizer.Controls.Wizard.RegistryHives.Count == 0)
                return 0;

            long size = 0;

            foreach (Little_System_Cleaner.Registry_Optimizer.Helpers.Hive h in Little_System_Cleaner.Registry_Optimizer.Controls.Wizard.RegistryHives)
            {
                size += h.OldHiveSize;
            }

            return size;
        }

        /// <summary>
        /// Gets the new size of the registry hives
        /// </summary>
        /// <returns>Registry size (in bytes)</returns>
        public static long GetNewRegistrySize()
        {
            if (Little_System_Cleaner.Registry_Optimizer.Controls.Wizard.RegistryHives == null)
                return 0;

            if (Little_System_Cleaner.Registry_Optimizer.Controls.Wizard.RegistryHives.Count == 0)
                return 0;

            long size = 0;

            foreach (Little_System_Cleaner.Registry_Optimizer.Helpers.Hive h in Little_System_Cleaner.Registry_Optimizer.Controls.Wizard.RegistryHives)
            {
                size += h.NewHiveSize;
            }

            return size;
        }
        #endregion

        #region Privacy Cleaner Functions
        #region PInvoke
        [StructLayout(LayoutKind.Sequential)]
        public struct INTERNET_CACHE_ENTRY_INFO
        {
            public UInt32 dwStructSize;
            public string lpszSourceUrlName;
            public string lpszLocalFileName;
            public UInt32 CacheEntryType;
            public UInt32 dwUseCount;
            public UInt32 dwHitRate;
            public UInt32 dwSizeLow;
            public UInt32 dwSizeHigh;
            public FILETIME LastModifiedTime;
            public FILETIME ExpireTime;
            public FILETIME LastAccessTime;
            public FILETIME LastSyncTime;
            public IntPtr lpHeaderInfo;
            public UInt32 dwHeaderInfoSize;
            public string lpszFileExtension;
            public ExemptDeltaOrReserverd dwExemptDeltaOrReserved;

        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ExemptDeltaOrReserverd
        {
            [FieldOffset(0)]
            public UInt32 dwReserved;
            [FieldOffset(0)]
            public UInt32 dwExemptDelta;
        }

        /// <summary>
        /// Used by QueryUrl method
        /// </summary>
        public enum STATURL_QUERYFLAGS : uint
        {
            /// <summary>
            /// The specified URL is in the content cache.
            /// </summary>
            STATURL_QUERYFLAG_ISCACHED = 0x00010000,
            /// <summary>
            /// Space for the URL is not allocated when querying for STATURL.
            /// </summary>
            STATURL_QUERYFLAG_NOURL = 0x00020000,
            /// <summary>
            /// Space for the Web page's title is not allocated when querying for STATURL.
            /// </summary>
            STATURL_QUERYFLAG_NOTITLE = 0x00040000,
            /// <summary>
            /// //The item is a top-level item.
            /// </summary>
            STATURL_QUERYFLAG_TOPLEVEL = 0x00080000,

        }

        /// <summary>
        /// Flag on the dwFlags parameter of the STATURL structure, used by the SetFilter method.
        /// </summary>
        public enum STATURLFLAGS : uint
        {
            /// <summary>
            /// Flag on the dwFlags parameter of the STATURL structure indicating that the item is in the cache.
            /// </summary>
            STATURLFLAG_ISCACHED = 0x00000001,
            /// <summary>
            /// Flag on the dwFlags parameter of the STATURL structure indicating that the item is a top-level item.
            /// </summary>
            STATURLFLAG_ISTOPLEVEL = 0x00000002,
        }

        /// <summary>
        /// Used bu the AddHistoryEntry method.
        /// </summary>
        public enum ADDURL_FLAG : uint
        {
            /// <summary>
            /// Write to both the visited links and the dated containers. 
            /// </summary>
            ADDURL_ADDTOHISTORYANDCACHE = 0,
            /// <summary>
            /// Write to only the visited links container.
            /// </summary>
            ADDURL_ADDTOCACHE = 1
        }

        /// <summary>
        /// The structure that contains statistics about a URL. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct STATURL
        {
            /// <summary>
            /// Struct size
            /// </summary>
            public int cbSize;
            /// <summary>
            /// URL
            /// </summary>                                                                   
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwcsUrl;
            /// <summary>
            /// Page title
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwcsTitle;
            /// <summary>
            /// Last visited date (UTC)
            /// </summary>
            public FILETIME ftLastVisited;
            /// <summary>
            /// Last updated date (UTC)
            /// </summary>
            public FILETIME ftLastUpdated;
            /// <summary>
            /// The expiry date of the Web page's content (UTC)
            /// </summary>
            public FILETIME ftExpires;
            /// <summary>
            /// Flags. STATURLFLAGS Enumaration.
            /// </summary>
            public STATURLFLAGS dwFlags;

            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public string URL
            {
                get { return pwcsUrl; }
            }
            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public string Title
            {
                get { return pwcsTitle; }
            }
            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime LastVisited
            {
                get { return DateTime.MinValue; }
            }
            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime LastUpdated
            {
                get { return DateTime.MinValue; }
            }
            /// <summary>
            /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
            /// </summary>
            public DateTime Expires
            {
                get { return DateTime.MinValue; }
            }

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UUID
        {
            public int Data1;
            public short Data2;
            public short Data3;
            public byte[] Data4;
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("AFA0DC11-C313-11D0-831A-00C04FD5AE38")]
        public interface IUrlHistoryStg2
        {
            void AddUrl(string pocsUrl, string pocsTitle, ADDURL_FLAG dwFlags);
            void DeleteUrl(string pocsUrl, int dwFlags);
            void QueryUrl([MarshalAs(UnmanagedType.LPWStr)] string pocsUrl, STATURL_QUERYFLAGS dwFlags, ref STATURL lpSTATURL);
            void BindToObject([In] string pocsUrl, [In] UUID riid, IntPtr ppvOut);
            object EnumUrls { [return: MarshalAs(UnmanagedType.IUnknown)] get; }

            void AddUrlAndNotify(string pocsUrl, string pocsTitle, int dwFlags, int fWriteHistory, object poctNotify, object punkISFolder);
            void ClearHistory();
        }

        //UrlHistory class
        [ComImport]
        [Guid("3C374A40-BAE4-11CF-BF7D-00AA006946EE")]
        public class UrlHistoryClass
        {
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "DeleteUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool DeleteUrlCacheEntry([MarshalAs(UnmanagedType.LPStr)] string lpszUrlName);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "UnlockUrlCacheEntryFileA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnlockUrlCacheEntryFile([MarshalAs(UnmanagedType.LPStr)] string lpszUrlName, uint dwReserved);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindFirstUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FindFirstUrlCacheEntry([MarshalAs(UnmanagedType.LPStr)] string lpszUrlSearchPattern, IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindNextUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        public static extern bool FindNextUrlCacheEntry(IntPtr hFind, IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true)]
        public static extern long FindCloseUrlCache(IntPtr hEnumHandle);

        #endregion

        public static List<INTERNET_CACHE_ENTRY_INFO> FindUrlCacheEntries(string urlPattern)
        {
            List<INTERNET_CACHE_ENTRY_INFO> cacheEntryList = new List<INTERNET_CACHE_ENTRY_INFO>();

            int structSize = 0;

            IntPtr bufferPtr = IntPtr.Zero;
            IntPtr cacheEnumHandle = FindFirstUrlCacheEntry(urlPattern, bufferPtr, ref structSize);

            switch (Marshal.GetLastWin32Error())
            {
                // ERROR_SUCCESS
                case 0:
                    if (cacheEnumHandle.ToInt32() > 0)
                    {
                        // Store entry
                        INTERNET_CACHE_ENTRY_INFO cacheEntry = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(INTERNET_CACHE_ENTRY_INFO));
                        cacheEntryList.Add(cacheEntry);
                    }
                    break;

                // ERROR_INSUFFICIENT_BUFFER
                case 122:
                    // Repeat call to API with size returned by first call
                    bufferPtr = Marshal.AllocHGlobal(structSize);
                    cacheEnumHandle = FindFirstUrlCacheEntry(urlPattern, bufferPtr, ref structSize);

                    if (cacheEnumHandle.ToInt32() > 0)
                    {
                        // Store entry
                        INTERNET_CACHE_ENTRY_INFO cacheEntry = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(INTERNET_CACHE_ENTRY_INFO));
                        cacheEntryList.Add(cacheEntry);
                        break;
                    }
                    else
                    {
                        // Failed to get handle, return...
                        Marshal.FreeHGlobal(bufferPtr);
                        FindCloseUrlCache(cacheEnumHandle);
                        return cacheEntryList;
                    }
                default:
                    Marshal.FreeHGlobal(bufferPtr);
                    FindCloseUrlCache(cacheEnumHandle);
                    return cacheEntryList;
            }

            do
            {
                bufferPtr = Marshal.ReAllocHGlobal(bufferPtr, new IntPtr(structSize));
                if (FindNextUrlCacheEntry(cacheEnumHandle, bufferPtr, ref structSize))
                {
                    // Store entry
                    INTERNET_CACHE_ENTRY_INFO cacheEntry = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(INTERNET_CACHE_ENTRY_INFO));
                    cacheEntryList.Add(cacheEntry);
                }
                else
                {
                    switch (Marshal.GetLastWin32Error())
                    {
                        // ERROR_INSUFFICIENT_BUFFER
                        case 122:
                            // Repeat call to API with size returned by first call
                            bufferPtr = Marshal.ReAllocHGlobal(bufferPtr, new IntPtr(structSize));

                            if (FindNextUrlCacheEntry(cacheEnumHandle, bufferPtr, ref structSize))
                            {
                                // Store entry
                                INTERNET_CACHE_ENTRY_INFO cacheEntry = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(INTERNET_CACHE_ENTRY_INFO));
                                cacheEntryList.Add(cacheEntry);
                                break;
                            }
                            else
                            {
                                Marshal.FreeHGlobal(bufferPtr);
                                FindCloseUrlCache(cacheEnumHandle);
                                return cacheEntryList;
                            }
                        // ERROR_NO_MORE_ITEMS
                        case 259:
                            Marshal.FreeHGlobal(bufferPtr);
                            FindCloseUrlCache(cacheEnumHandle);
                            return cacheEntryList;
                        default:
                            Marshal.FreeHGlobal(bufferPtr);
                            FindCloseUrlCache(cacheEnumHandle);
                            return cacheEntryList;
                    }
                }
            } while (true);

            // Wont reach here
        }

        /// <summary>
        /// Checks to see if a process is a running
        /// </summary>
        /// <param name="procName">The name of the process (ex: firefox for Firefox)</param>
        /// <returns>True if the process is running</returns>
        public static bool IsProcessRunning(string procName)
        {
            foreach (Process proc in Process.GetProcessesByName(procName))
            {
                if (!proc.HasExited)
                    return true;
            }

            return false;
        }

        public static string ExpandVars(string p)
        {
            string str = (string)p.Clone();

            if (string.IsNullOrEmpty(str))
                throw new ArgumentNullException(str);

            // Expand system variables
            str = Environment.ExpandEnvironmentVariables(str);

            // Expand program variables
            // (Needed for unspecified variables)
            str = str.Replace("%Cookies%", Environment.GetFolderPath(Environment.SpecialFolder.Cookies));
            str = str.Replace("%Favorites%", Environment.GetFolderPath(Environment.SpecialFolder.Favorites));
            str = str.Replace("%History%", Environment.GetFolderPath(Environment.SpecialFolder.History));
            str = str.Replace("%InternetCache%", Environment.GetFolderPath(Environment.SpecialFolder.InternetCache));
            str = str.Replace("%MyComputer%", Environment.GetFolderPath(Environment.SpecialFolder.MyComputer));
            str = str.Replace("%MyDocuments%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            str = str.Replace("%MyMusic%", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            str = str.Replace("%MyPictures%", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            str = str.Replace("%Recent%", Environment.GetFolderPath(Environment.SpecialFolder.Recent));
            str = str.Replace("%SendTo%", Environment.GetFolderPath(Environment.SpecialFolder.SendTo));
            str = str.Replace("%StartMenu%", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
            str = str.Replace("%Startup%", Environment.GetFolderPath(Environment.SpecialFolder.Startup));
            str = str.Replace("%Templates%", Environment.GetFolderPath(Environment.SpecialFolder.Templates));

            return str;
        }

        public static string[] GetSections(string filePath)
        {
            uint MAX_BUFFER = 32767;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("Path to INI file cannot be empty or null. Unable to get sections.");
                return new string[] { };
            }

            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
            uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, filePath);
            if (bytesReturned == 0)
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                return new string[] { };
            }

            string local = Marshal.PtrToStringAnsi(pReturnedString, (int)bytesReturned).ToString();
            Marshal.FreeCoTaskMem(pReturnedString);

            return local.Substring(0, local.Length - 1).Split('\0');
        }

        public static StringDictionary GetValues(string filePath, string sectionName)
        {
            uint MAX_BUFFER = 32767;

            StringDictionary ret = new StringDictionary();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("Path to INI file cannot be empty or null. Unable to get values.");
                return ret;
            }

            if (string.IsNullOrWhiteSpace(sectionName))
            {
                Debug.WriteLine("Section name cannot be empty or null. Unable to get values.");
                return ret;
            }

            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);

            uint bytesReturned = GetPrivateProfileSection(sectionName, pReturnedString, MAX_BUFFER, filePath);

            if ((bytesReturned == MAX_BUFFER - 2) || (bytesReturned == 0))
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                return ret;
            }

            //bytesReturned -1 to remove trailing \0

            // NOTE: Calling Marshal.PtrToStringAuto(pReturnedString) will
            //       result in only the first pair being returned
            string returnedString = Marshal.PtrToStringAuto(pReturnedString, (int)bytesReturned - 1);

            Marshal.FreeCoTaskMem(pReturnedString);

            foreach (string value in returnedString.Split('\0'))
            {
                string[] valueKey = value.Split('=');

                ret.Add(valueKey[0], valueKey[1]);
            }

            return ret;
        }

        /// <summary>
        /// Gets the file size
        /// </summary>
        /// <param name="filePath">Path to the filename</param>
        /// <returns>File Size (in bytes)</returns>
        public static long GetFileSize(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);

                return fi.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the folder size
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <param name="includeSubDirs">Include sub directories</param>
        /// <returns>Folder Size (in bytes)</returns>
        public static long GetFolderSize(string folderPath, bool includeSubDirs)
        {
            long totalSize = 0;

            try
            {
                foreach (string filePath in Directory.GetFiles(folderPath, "*", (includeSubDirs) ? (System.IO.SearchOption.AllDirectories) : (System.IO.SearchOption.TopDirectoryOnly)))
                {
                    long fileSize = GetFileSize(filePath);
                    if (fileSize != 0)
                        totalSize += fileSize;
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return totalSize;
        }

        /// <summary>
        /// Checks if file is valid for privacy cleaner
        /// </summary>
        /// <param name="fileInfo">FileInfo</param>
        /// <returns>True if file is valid</returns>
        public static bool IsFileValid(FileInfo fileInfo)
        {
            if (fileInfo == null)
                return false;

            FileAttributes fileAttribs;
            long fileLength;

            try
            {
                fileAttribs = fileInfo.Attributes;
                fileLength = fileInfo.Length;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if file is valid.");
                return false;
            }

            if ((fileAttribs & FileAttributes.System) == FileAttributes.System && (!Properties.Settings.Default.privacyCleanerIncSysFile))
                return false;

            if ((fileAttribs & FileAttributes.Hidden) == FileAttributes.Hidden && (!Properties.Settings.Default.privacyCleanerIncHiddenFile))
                return false;

            if ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly && (!Properties.Settings.Default.privacyCleanerIncReadOnlyFile))
                return false;

            if ((fileLength == 0) && (!Properties.Settings.Default.privacyCleanerInc0ByteFile))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if file path is valid
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <returns>True if file is valid</returns>
        public static bool IsFileValid(string filePath)
        {
            bool bRet = false;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("File path cannot be empty or null. Unable to check if file is valid.");
                return bRet;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                bRet = IsFileValid(fileInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if file is valid.");
                return bRet;
            }
            

            return bRet;
        }

        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="filePath">Path to file</param>
        public static void DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("File path cannot be empty or null. Unable to delete file.");
                return;
            }

            try
            {
                if (Properties.Settings.Default.privacyCleanerDeletePerm)
                    File.Delete(filePath);
                else
                    FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to delete file: " + filePath);
            }
        }

        /// <summary>
        /// Deletes a directory
        /// </summary>
        /// <param name="dirPath">Path to directory</param>
        /// <param name="recurse">Recursive delete</param>
        public static void DeleteDir(string dirPath, bool recurse)
        {
            if (string.IsNullOrWhiteSpace(dirPath))
            {
                Debug.WriteLine("Directory path cannot be empty or null. Unable to delete directory.");
                return;
            }

            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                if ((dirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    dirInfo.Attributes = dirInfo.Attributes & ~FileAttributes.ReadOnly;

                if (!recurse && dirInfo.GetFileSystemInfos().Length > 0)
                    return;

                if (Properties.Settings.Default.privacyCleanerDeletePerm)
                    Directory.Delete(dirPath, recurse);
                else
                    FileSystem.DeleteDirectory(dirPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception)
            {

            }

        }
        #endregion
    }
}

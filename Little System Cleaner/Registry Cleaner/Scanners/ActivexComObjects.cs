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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Little_System_Cleaner.Registry_Cleaner.Helpers;
using Microsoft.Win32;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class ActivexComObjects : ScannerBase
    {
        public override string ScannerName => Strings.ActivexComObjects;

        /// <summary>
        /// Scans ActiveX/COM Objects
        /// </summary>
        internal static void Scan()
        {
            try
            {
                // Scan all CLSID sub keys
                Utils.SafeOpenRegistryKey(() => ScanClsidSubKey(Registry.ClassesRoot.OpenSubKey("CLSID")));
                Utils.SafeOpenRegistryKey(() => ScanClsidSubKey(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\CLSID")));
                Utils.SafeOpenRegistryKey(() => ScanClsidSubKey(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\CLSID")));
                if (Utils.Is64BitOs)
                {
                    Utils.SafeOpenRegistryKey(() => ScanClsidSubKey(Registry.ClassesRoot.OpenSubKey("Wow6432Node\\CLSID")));
                    Utils.SafeOpenRegistryKey(() => ScanClsidSubKey(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes\\CLSID")));
                    Utils.SafeOpenRegistryKey(() => ScanClsidSubKey(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes\\CLSID")));
                }

                // Scan file extensions + progids
                Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.ClassesRoot));
                Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes")));
                Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes")));
                if (Utils.Is64BitOs)
                {
                    Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.ClassesRoot.OpenSubKey("Wow6432Node")));
                    Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes")));
                    Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes")));
                }

                // Scan appids
                Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.ClassesRoot.OpenSubKey("AppID")));
                Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\AppID")));
                Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\AppID")));
                if (Utils.Is64BitOs)
                {
                    Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.ClassesRoot.OpenSubKey("Wow6432Node\\AppID")));
                    Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\AppID")));
                    Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\AppID")));
                }

                // Scan explorer subkey
                ScanExplorer();
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }

        #region Scan functions

        /// <summary>
        /// Scans for the CLSID subkey
        /// <param name="regKey">Location of CLSID Sub Key</param>
        /// </summary>
        private static void ScanClsidSubKey(RegistryKey regKey)
        {
            string[] clsids;

            if (regKey == null)
                return;

            Wizard.Report.WriteLine("Scanning " + regKey.Name + " for invalid CLSID's");

            try
            {
                clsids = regKey.GetSubKeyNames();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to scan for invalid CLSID's");
                return;
            }

            foreach (string clsid in clsids)
            {
                RegistryKey rkClsid, regKeyDefaultIcon = null, regKeyInprocSrvr = null, regKeyInprocSrvr32 = null;

                try
                {
                    rkClsid = regKey.OpenSubKey(clsid);

                    if (rkClsid == null)
                        continue;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping...");
                    continue;
                }

                // Check for valid AppID
                try
                {
                    string appId = regKey.GetValue("AppID") as string;
                    if (!string.IsNullOrEmpty(appId))
                        if (!AppidExists(appId))
                            Wizard.StoreInvalidKey(Strings.MissingAppID, rkClsid.ToString(), "AppID");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for valid AppID");
                }
                
                // See if DefaultIcon exists
                try
                {
                    regKeyDefaultIcon = rkClsid.OpenSubKey("DefaultIcon");
                    string iconPath = regKeyDefaultIcon?.GetValue("") as string;

                    if (!string.IsNullOrEmpty(iconPath))
                        if (!ScanFunctions.IconExists(iconPath))
                            if (!Wizard.IsOnIgnoreList(iconPath))
                                Wizard.StoreInvalidKey(Strings.InvalidFile, $"{rkClsid}\\DefaultIcon");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to scan for DefaultIcon");
                }
                finally
                {
                    regKeyDefaultIcon?.Close();
                }

                // Look for InprocServer files
                try
                {
                    regKeyInprocSrvr = rkClsid.OpenSubKey("InprocServer");
                    if (regKeyInprocSrvr != null)
                    {
                        string strInprocServer = regKeyInprocSrvr.GetValue("") as string;

                        if (!string.IsNullOrEmpty(strInprocServer))
                            if (!Utils.FileExists(strInprocServer) && !Wizard.IsOnIgnoreList(strInprocServer))
                                Wizard.StoreInvalidKey(Strings.InvalidInprocServer, regKeyInprocSrvr.ToString());

                        regKeyInprocSrvr.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for InprocServer files");
                }
                finally
                {
                    regKeyInprocSrvr?.Close();
                }

                try
                {
                    regKeyInprocSrvr32 = rkClsid.OpenSubKey("InprocServer32");
                    if (regKeyInprocSrvr32 != null)
                    {
                        string strInprocServer32 = regKeyInprocSrvr32.GetValue("") as string;

                        if (!string.IsNullOrEmpty(strInprocServer32))
                            if (!Utils.FileExists(strInprocServer32) && !Wizard.IsOnIgnoreList(strInprocServer32))
                                Wizard.StoreInvalidKey(Strings.InvalidInprocServer32, regKeyInprocSrvr32.ToString());

                        regKeyInprocSrvr32.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for InprocServer32 files");
                }
                finally
                {
                    regKeyInprocSrvr32?.Close();
                }

                rkClsid.Close();
            }

            regKey.Close();
        }

        /// <summary>
        /// Looks for invalid references to AppIDs
        /// </summary>
        private static void ScanAppIds(RegistryKey regKey)
        {
            if (regKey == null)
                return;

            Wizard.Report.WriteLine("Scanning " + regKey.Name + " for invalid AppID's");

            foreach (string appId in regKey.GetSubKeyNames())
            {
                RegistryKey rkAppId = null;

                try
                {
                    rkAppId = regKey.OpenSubKey(appId);

                    // Check for reference to AppID
                    string clsid = rkAppId?.GetValue("AppID") as string;

                    if (!string.IsNullOrEmpty(clsid))
                        if (!AppidExists(clsid))
                            Wizard.StoreInvalidKey(Strings.MissingAppID, rkAppId.ToString());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid reference to AppID");
                }
                finally
                {
                    rkAppId?.Close();
                }
            }

            regKey.Close();
        }

        /// <summary>
        /// Finds invalid File extensions + ProgIDs referenced
        /// </summary>
        private static void ScanClasses(RegistryKey regKey)
        {
            if (regKey == null)
                return;

            Wizard.Report.WriteLine("Scanning " + regKey.Name + " for invalid Classes");

            string[] strClasses;

            try
            {
                strClasses = regKey.GetSubKeyNames();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid classes.");
                return;
            }

            foreach (string strSubKey in strClasses.Where(strSubKey => strSubKey != "*"))
            {
                if (strSubKey[0] == '.')
                {
                    // File Extension
                    RegistryKey rkFileExt = null;

                    try
                    {
                        rkFileExt = regKey.OpenSubKey(strSubKey);

                        // Find reference to ProgID
                        string strProgID = rkFileExt?.GetValue("") as string;

                        if (!string.IsNullOrEmpty(strProgID))
                            if (!ProgIdExists(strProgID))
                                Wizard.StoreInvalidKey(Strings.MissingProgID, rkFileExt.ToString());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check file extension.");
                    }
                    finally
                    {
                        rkFileExt?.Close();
                    }
                }
                else
                {
                    // ProgID or file class

                    // See if DefaultIcon exists
                    RegistryKey regKeyDefaultIcon = null;

                    try
                    {
                        regKeyDefaultIcon = regKey.OpenSubKey($"{strSubKey}\\DefaultIcon");

                        string iconPath = regKeyDefaultIcon?.GetValue("") as string;

                        if (!string.IsNullOrEmpty(iconPath))
                            if (!ScanFunctions.IconExists(iconPath))
                                if (!Wizard.IsOnIgnoreList(iconPath))
                                    Wizard.StoreInvalidKey(Strings.InvalidFile, regKeyDefaultIcon.Name);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if DefaultIcon exists.");
                    }
                    finally
                    {
                        regKeyDefaultIcon?.Close();
                    }

                    // Check referenced CLSID
                    RegistryKey rkCLSID = null;

                    try
                    {
                        rkCLSID = regKey.OpenSubKey($"{strSubKey}\\CLSID");

                        string guid = rkCLSID?.GetValue("") as string;

                        if (!string.IsNullOrEmpty(guid))
                            if (!ClsidExists(guid))
                                Wizard.StoreInvalidKey(Strings.MissingCLSID, $"{regKey.Name}\\{strSubKey}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if referenced CLSID exists.");
                    }
                    finally
                    {
                        rkCLSID?.Close();
                    }
                }

                // Check for unused progid/extension
                RegistryKey rk = null;

                try
                {
                    rk = regKey.OpenSubKey(strSubKey);

                    if (rk?.ValueCount <= 0 && rk.SubKeyCount <= 0)
                        Wizard.StoreInvalidKey(Strings.InvalidProgIDFileExt, rk.Name);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for unused ProgID or file extension.");
                }
                finally
                {
                    rk?.Close();
                }
            }

            regKey.Close();
        }

        /// <summary>
        /// Finds invalid windows explorer entries
        /// </summary>
        private static void ScanExplorer()
        {
            RegistryKey regKey = null;

            // Check Browser Help Objects
            try
            {
                regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\explorer\\Browser Helper Objects");

                Wizard.Report.WriteLine("Checking for invalid browser helper objects");

                if (regKey != null)
                {
                    foreach (string strGuid in regKey.GetSubKeyNames())
                    {
                        try
                        {
                            RegistryKey rkBHO = regKey.OpenSubKey(strGuid);

                            if (rkBHO != null)
                            {
                                if (!ClsidExists(strGuid))
                                    Wizard.StoreInvalidKey(Strings.MissingCLSID, rkBHO.ToString());

                                rkBHO.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for invalid BHO.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid BHOs.");
            }
            finally
            {
                regKey?.Close();
            }

            // Check IE Toolbars
            try
            {
                regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Internet Explorer\\Toolbar");

                Wizard.Report.WriteLine("Checking for invalid explorer toolbars");

                if (regKey != null)
                {
                    foreach (string strGuid in regKey.GetValueNames().Where(strGuid => !IeToolbarIsValid(strGuid)))
                    {
                        Wizard.StoreInvalidKey(Strings.InvalidToolbar, regKey.ToString(), strGuid);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid explorer toolbars.");
            }
            finally
            {
                regKey?.Close();
            }

            // Check IE Extensions
            try
            {
                regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Internet Explorer\\Extensions");

                Wizard.Report.WriteLine("Checking for invalid explorer extensions");

                if (regKey != null)
                {
                    foreach (string strGuid in regKey.GetSubKeyNames())
                    {
                        try
                        {
                            var rkExt = regKey.OpenSubKey(strGuid);

                            if (rkExt != null)
                                ValidateExplorerExt(rkExt);

                            rkExt?.Close();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid explorer extensions.");
            }
            finally
            {
                regKey?.Close();
            }

            // Check Explorer File Exts
            try
            {
                regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts");

                Wizard.Report.WriteLine("Checking for invalid explorer file extensions");

                if (regKey != null)
                {
                    foreach (string strFileExt in regKey.GetSubKeyNames())
                    {
                        try
                        {
                            var rkFileExt = regKey.OpenSubKey(strFileExt);

                            if (rkFileExt == null || strFileExt[0] != '.')
                                continue;

                            ValidateFileExt(rkFileExt);

                            rkFileExt?.Close();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid explorer file extensions.");
            }
            finally
            {
                regKey?.Close();
            }
        }

        #endregion

        #region Scan Sub-Functions

        private static void ValidateFileExt(RegistryKey regKey)
        {
            bool bProgidExists = false, bAppExists = false;
            RegistryKey rkProgids = null, rkOpenList = null;

            // Skip if UserChoice subkey exists
            if (regKey.OpenSubKey("UserChoice") != null)
                return;

            // Parse and verify OpenWithProgId List
            try
            {
                rkProgids = regKey.OpenSubKey("OpenWithProgids");

                if (rkProgids != null)
                {
                    bProgidExists = rkProgids.GetValueNames().Any(ProgIdExists);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid OpenWithProgId.");
            }
            finally
            {
                rkProgids?.Close();
            }

            // Check if files in OpenWithList exist
            try
            {
                rkOpenList = regKey.OpenSubKey("OpenWithList");

                if (rkOpenList != null)
                {
                    bAppExists =
                        rkOpenList.GetValueNames()
                            .Where(valueName => valueName != "MRUList")
                            .Select(valueName => rkOpenList.GetValue(valueName) as string)
                            .Any(AppExists);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid OpenWithList.");
            }
            finally
            {
                rkOpenList?.Close();
            }

            if (!bProgidExists && !bAppExists)
                Wizard.StoreInvalidKey(Strings.InvalidFileExt, regKey.ToString());
        }

        private static void ValidateExplorerExt(RegistryKey regKey)
        {
            // Sees if icon file exists
            try
            {
                string strHotIcon = regKey.GetValue("HotIcon") as string;
                if (!string.IsNullOrEmpty(strHotIcon))
                    if (!ScanFunctions.IconExists(strHotIcon))
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), "HotIcon");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if HotIcon exists.");
            }

            try
            {
                string strIcon = regKey.GetValue("Icon") as string;
                if (!string.IsNullOrEmpty(strIcon))
                    if (!ScanFunctions.IconExists(strIcon))
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), "Icon");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if Icon exists.");
            }

            try
            {
                // Lookup CLSID extension
                string strClsidExt = regKey.GetValue("ClsidExtension") as string;
                if (!string.IsNullOrEmpty(strClsidExt))
                    Wizard.StoreInvalidKey(Strings.MissingCLSID, regKey.ToString(), "ClsidExtension");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if ClsidExtension exists.");
            }

            try
            {
                // See if files exist
                string strExec = regKey.GetValue("Exec") as string;
                if (!string.IsNullOrEmpty(strExec))
                    if (!Utils.FileExists(strExec) && !Wizard.IsOnIgnoreList(strExec))
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), "Exec");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if Exec exists.");
            }

            try
            {
                string strScript = regKey.GetValue("Script") as string;
                if (!string.IsNullOrEmpty(strScript))
                    if (!Utils.FileExists(strScript) && !Wizard.IsOnIgnoreList(strScript))
                        Wizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), "Script");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if Script exists.");
            }
        }

        private static bool SafeInprocServerExists(RegistryKey rootKey, string subKey = "")
        {
            RegistryKey regKey = null;
            bool ret;

            subKey = subKey.Trim();

            try
            {
                regKey = !string.IsNullOrEmpty(subKey) ? rootKey.OpenSubKey(subKey) : rootKey;

                ret = InprocServerExists(regKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if InprocServer exists.");
                ret = false;
            }
            finally
            {
                regKey?.Close();
            }

            return ret;
        }

        /// <summary>
        /// Checks for inprocserver file
        /// </summary>
        /// <param name="regKey">The registry key contain Inprocserver subkey</param>
        /// <returns>False if Inprocserver is null or doesnt exist</returns>
        private static bool InprocServerExists(RegistryKey regKey)
        {
            if (regKey == null)
                return false;

            RegistryKey regKeyInprocSrvr = null, regKeyInprocSrvr32 = null;

            try
            {
                regKeyInprocSrvr = regKey.OpenSubKey("InprocServer");

                string strInprocServer = regKeyInprocSrvr?.GetValue("") as string;

                if (!string.IsNullOrEmpty(strInprocServer))
                    if (Utils.FileExists(strInprocServer) || Wizard.IsOnIgnoreList(strInprocServer))
                        return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if InprocServer exists.");
            }
            finally
            {
                regKeyInprocSrvr?.Close();
            }

            try
            {
                regKeyInprocSrvr32 = regKey.OpenSubKey("InprocServer32");

                string strInprocServer32 = regKeyInprocSrvr32?.GetValue("") as string;

                if (!string.IsNullOrEmpty(strInprocServer32))
                    if (Utils.FileExists(strInprocServer32) || Wizard.IsOnIgnoreList(strInprocServer32))
                        return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if InprocServer32 exists.");
            }
            finally
            {
                regKeyInprocSrvr32?.Close();
            }

            return false;
        }

        /// <summary>
        /// Checks if IE toolbar GUID is valid
        /// </summary>
        private static bool IeToolbarIsValid(string strGuid)
        {
            bool bRet = false;

            // Guid cannot be null/empty
            if (string.IsNullOrEmpty(strGuid))
                return true;

            if (!ClsidExists(strGuid))
                bRet = false;

            if (SafeInprocServerExists(Registry.ClassesRoot, "CLSID\\" + strGuid))
                bRet = true;

            if (SafeInprocServerExists(Registry.LocalMachine, "Software\\Classes\\CLSID\\" + strGuid))
                bRet = true;

            if (SafeInprocServerExists(Registry.CurrentUser, "Software\\Classes\\CLSID\\" + strGuid))
                bRet = true;

            if (Utils.Is64BitOs)
            {
                if (SafeInprocServerExists(Registry.ClassesRoot, "Wow6432Node\\CLSID\\" + strGuid))
                    bRet = true;

                if (SafeInprocServerExists(Registry.LocalMachine, "Software\\Wow6432Node\\Classes\\CLSID\\" + strGuid))
                    bRet = true;

                if (SafeInprocServerExists(Registry.CurrentUser, "Software\\Wow6432Node\\Classes\\CLSID\\" + strGuid))
                    bRet = true;
            }

            return bRet;
        }

        /// <summary>
        /// Sees if application exists
        /// </summary>
        /// <param name="appName">Application Name</param>
        /// <returns>True if it exists</returns>
        private static bool AppExists(string appName)
        {
            List<RegistryKey> listRegKeys = new List<RegistryKey>();

            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey("Applications")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Classes\Applications")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Classes\Applications")));

            if (Utils.Is64BitOs)
            {
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\Applications")));
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Classes\Applications")));
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Wow6432Node\Classes\Applications")));
            }

            try
            {
                foreach (RegistryKey rk in listRegKeys)
                {
                    if (rk == null)
                        continue;

                    RegistryKey subKey = null;

                    try
                    {
                        subKey = rk.OpenSubKey(appName);

                        if (subKey != null)
                            if (!Wizard.IsOnIgnoreList(appName))
                                return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for AppName");
                    }
                    finally
                    {
                        subKey?.Close();
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Sees if the specified CLSID exists
        /// </summary>
        /// <param name="clsid">The CLSID GUID</param>
        /// <returns>True if it exists</returns>
        private static bool ClsidExists(string clsid)
        {
            List<RegistryKey> listRegKeys = new List<RegistryKey>();

            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey("CLSID")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Classes\CLSID")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID")));

            if (Utils.Is64BitOs)
            {
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\CLSID")));
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Classes\CLSID")));
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Wow6432Node\Classes\CLSID")));
            }

            try
            {
                foreach (RegistryKey rk in listRegKeys)
                {
                    if (rk == null)
                        continue;

                    RegistryKey subKey = null;

                    try
                    {
                        subKey = rk.OpenSubKey(clsid);

                        if (subKey != null)
                            if (!Wizard.IsOnIgnoreList(subKey.ToString()))
                                return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for CLSID");
                    }
                    finally
                    {
                        subKey?.Close();
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks if the ProgID exists in Classes subkey
        /// </summary>
        /// <param name="progId">The ProgID</param>
        /// <returns>True if it exists</returns>
        private static bool ProgIdExists(string progId)
        {
            List<RegistryKey> listRegKeys = new List<RegistryKey>();

            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Classes")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Classes")));

            if (Utils.Is64BitOs)
            {
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey(@"Wow6432Node")));
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Classes")));
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Wow6432Node\Classes")));
            }

            try
            {
                foreach (RegistryKey rk in listRegKeys)
                {
                    if (rk == null)
                        continue;

                    RegistryKey subKey = null;

                    try
                    {
                        subKey = rk.OpenSubKey(progId);

                        if (subKey != null)
                            if (!Wizard.IsOnIgnoreList(subKey.ToString()))
                                return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for ProgID");
                    }
                    finally
                    {
                        subKey?.Close();
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks if the AppID exists
        /// </summary>
        /// <param name="appId">The AppID or GUID</param>
        /// <returns>True if it exists</returns>
        private static bool AppidExists(string appId)
        {
            List<RegistryKey> listRegKeys = new List<RegistryKey>();

            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey(@"AppID")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Classes\AppID")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppID")));

            if (Utils.Is64BitOs)
            {
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\AppID")));
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Classes\AppID")));
                Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Wow6432Node\Classes\AppID")));
            }

            try
            {
                foreach (RegistryKey rk in listRegKeys)
                {
                    if (rk == null)
                        continue;

                    RegistryKey subKey = null;

                    try
                    {
                        subKey = rk.OpenSubKey(appId);

                        if (subKey != null)
                            if (!Wizard.IsOnIgnoreList(subKey.ToString()))
                                return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for AppID");
                    }
                    finally
                    {
                        subKey?.Close();
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        #endregion
    }
}
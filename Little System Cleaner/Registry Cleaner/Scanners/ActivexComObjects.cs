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
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using System.Diagnostics;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Registry_Cleaner.Helpers;

namespace Little_System_Cleaner.Registry_Cleaner.Scanners
{
    public class ActivexComObjects : ScannerBase
    {
        public override string ScannerName
        {
            get { return Strings.ActivexComObjects; }
        }

        /// <summary>
        /// Scans ActiveX/COM Objects
        /// </summary>
        public static void Scan()
        {
            // Scan all CLSID sub keys
            Utils.SafeOpenRegistryKey(() => ScanCLSIDSubKey(Registry.ClassesRoot.OpenSubKey("CLSID")));
            Utils.SafeOpenRegistryKey(() => ScanCLSIDSubKey(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\CLSID")));
            Utils.SafeOpenRegistryKey(() => ScanCLSIDSubKey(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\CLSID")));
            if (Utils.Is64BitOS)
            {
                Utils.SafeOpenRegistryKey(() => ScanCLSIDSubKey(Registry.ClassesRoot.OpenSubKey("Wow6432Node\\CLSID")));
                Utils.SafeOpenRegistryKey(() => ScanCLSIDSubKey(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes\\CLSID")));
                Utils.SafeOpenRegistryKey(() => ScanCLSIDSubKey(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes\\CLSID")));
            }

            // Scan file extensions + progids
            Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.ClassesRoot));
            Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes")));
            Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes")));
            if (Utils.Is64BitOS)
            {
                Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.ClassesRoot.OpenSubKey("Wow6432Node")));
                Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes")));
                Utils.SafeOpenRegistryKey(() => ScanClasses(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Classes")));
            }

            // Scan appids
            Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.ClassesRoot.OpenSubKey("AppID")));
            Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\AppID")));
            Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\AppID")));
            if (Utils.Is64BitOS)
            {
                Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.ClassesRoot.OpenSubKey("Wow6432Node\\AppID")));
                Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\AppID")));
                Utils.SafeOpenRegistryKey(() => ScanAppIds(Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\AppID")));
            }

            // Scan explorer subkey
            ScanExplorer();
        }

        

        #region Scan functions

        /// <summary>
        /// Scans for the CLSID subkey
        /// <param name="regKey">Location of CLSID Sub Key</param>
        /// </summary>
        private static void ScanCLSIDSubKey(RegistryKey regKey)
        {
            string[] strCLSIDs;

            if (regKey == null)
                return;

            ScanWizard.Report.WriteLine("Scanning " + regKey.Name + " for invalid CLSID's");

            try
            {
                strCLSIDs = regKey.GetSubKeyNames();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to scan for invalid CLSID's");
                return;
            }

            foreach (string strCLSID in strCLSIDs)
            {
                RegistryKey rkCLSID, regKeyDefaultIcon = null, regKeyInprocSrvr = null, regKeyInprocSrvr32 = null;

                try
                {
                    rkCLSID = regKey.OpenSubKey(strCLSID);

                    if (rkCLSID == null)
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
                    string strAppID = regKey.GetValue("AppID") as string;
                    if (!string.IsNullOrEmpty(strAppID))
                        if (!appidExists(strAppID))
                            ScanWizard.StoreInvalidKey(Strings.MissingAppID, rkCLSID.ToString(), "AppID");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for valid AppID");
                }
                
                // See if DefaultIcon exists
                try
                {
                    regKeyDefaultIcon = rkCLSID.OpenSubKey("DefaultIcon");
                    if (regKeyDefaultIcon != null)
                    {
                        string iconPath = regKeyDefaultIcon.GetValue("") as string;

                        if (!string.IsNullOrEmpty(iconPath))
                            if (!ScanFunctions.IconExists(iconPath))
                                if (!ScanWizard.IsOnIgnoreList(iconPath))
                                    ScanWizard.StoreInvalidKey(Strings.InvalidFile, string.Format("{0}\\DefaultIcon", rkCLSID.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to scan for DefaultIcon");
                }
                finally
                {
                    if (regKeyDefaultIcon != null)
                    {
                        regKeyDefaultIcon.Close();
                    }
                }
                
                // Look for InprocServer files
                try
                {
                    regKeyInprocSrvr = rkCLSID.OpenSubKey("InprocServer");
                    if (regKeyInprocSrvr != null)
                    {
                        string strInprocServer = regKeyInprocSrvr.GetValue("") as string;

                        if (!string.IsNullOrEmpty(strInprocServer))
                            if (!Utils.FileExists(strInprocServer))
                                ScanWizard.StoreInvalidKey(Strings.InvalidInprocServer, regKeyInprocSrvr.ToString());

                        regKeyInprocSrvr.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for InprocServer files");
                }
                finally
                {
                    if (regKeyInprocSrvr != null)
                        regKeyInprocSrvr.Close();
                }

                try
                {
                    regKeyInprocSrvr32 = rkCLSID.OpenSubKey("InprocServer32");
                    if (regKeyInprocSrvr32 != null)
                    {
                        string strInprocServer32 = regKeyInprocSrvr32.GetValue("") as string;

                        if (!string.IsNullOrEmpty(strInprocServer32))
                            if (!Utils.FileExists(strInprocServer32))
                                ScanWizard.StoreInvalidKey(Strings.InvalidInprocServer32, regKeyInprocSrvr32.ToString());

                        regKeyInprocSrvr32.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for InprocServer32 files");
                }
                finally
                {
                    if (regKeyInprocSrvr32 != null)
                        regKeyInprocSrvr32.Close();
                }

                rkCLSID.Close();
            }

            regKey.Close();
            return;
        }

        /// <summary>
        /// Looks for invalid references to AppIDs
        /// </summary>
        private static void ScanAppIds(RegistryKey regKey)
        {
            if (regKey == null)
                return;

            ScanWizard.Report.WriteLine("Scanning " + regKey.Name + " for invalid AppID's");

            foreach (string strAppId in regKey.GetSubKeyNames())
            {
                RegistryKey rkAppId = null;

                try
                {
                    rkAppId = regKey.OpenSubKey(strAppId);

                    if (rkAppId != null)
                    {
                        // Check for reference to AppID
                        string strCLSID = rkAppId.GetValue("AppID") as string;

                        if (!string.IsNullOrEmpty(strCLSID))
                            if (!appidExists(strCLSID))
                                ScanWizard.StoreInvalidKey(Strings.MissingAppID, rkAppId.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check for invalid reference to AppID");
                    continue;
                }
                finally
                {
                    if (rkAppId != null)
                        rkAppId.Close();
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

            ScanWizard.Report.WriteLine("Scanning " + regKey.Name + " for invalid Classes");

            string[] strClasses;

            try
            {
                strClasses = regKey.GetSubKeyNames();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check for invalid classes.");
                return;
            }

            foreach (string strSubKey in strClasses)
            {
                // Skip any file (*)
                if (strSubKey == "*")
                    continue;

                if (strSubKey[0] == '.')
                {
                    // File Extension
                    RegistryKey rkFileExt = null;

                    try
                    {
                        rkFileExt = regKey.OpenSubKey(strSubKey);

                        if (rkFileExt != null)
                        {
                            // Find reference to ProgID
                            string strProgID = rkFileExt.GetValue("") as string;

                            if (!string.IsNullOrEmpty(strProgID))
                                if (!progIDExists(strProgID))
                                    ScanWizard.StoreInvalidKey(Strings.MissingProgID, rkFileExt.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check file extension.");
                    }
                    finally
                    {
                        if (rkFileExt != null)
                            rkFileExt.Close();
                    }
                }
                else
                {
                    // ProgID or file class

                    // See if DefaultIcon exists
                    RegistryKey regKeyDefaultIcon = null;

                    try
                    {
                        regKeyDefaultIcon = regKey.OpenSubKey(string.Format("{0}\\DefaultIcon", strSubKey));

                        if (regKeyDefaultIcon != null)
                        {
                            string iconPath = regKeyDefaultIcon.GetValue("") as string;

                            if (!string.IsNullOrEmpty(iconPath))
                                if (!ScanFunctions.IconExists(iconPath))
                                    if (!ScanWizard.IsOnIgnoreList(iconPath))
                                        ScanWizard.StoreInvalidKey(Strings.InvalidFile, regKeyDefaultIcon.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if DefaultIcon exists.");
                    }
                    finally
                    {
                        if (regKeyDefaultIcon != null)
                            regKeyDefaultIcon.Close();
                    }

                    // Check referenced CLSID
                    RegistryKey rkCLSID = null;

                    try
                    {
                        rkCLSID = regKey.OpenSubKey(string.Format("{0}\\CLSID", strSubKey));

                        if (rkCLSID != null)
                        {
                            string guid = rkCLSID.GetValue("") as string;

                            if (!string.IsNullOrEmpty(guid))
                                if (!clsidExists(guid))
                                    ScanWizard.StoreInvalidKey(Strings.MissingCLSID, string.Format("{0}\\{1}", regKey.Name, strSubKey));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if referenced CLSID exists.");
                    }
                    finally
                    {
                        if (rkCLSID != null)
                            rkCLSID.Close();
                    }
                }

                // Check for unused progid/extension
                RegistryKey rk = null;

                try
                {
                    if (rk != null)
                    {
                        if (rk.ValueCount <= 0 && rk.SubKeyCount <= 0)
                            ScanWizard.StoreInvalidKey(Strings.InvalidProgIDFileExt, rk.Name);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check for unused ProgID or file extension.");
                }
                finally
                {
                    if (rk != null)
                        rk.Close();
                }
            }

            regKey.Close();

            return;
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

                ScanWizard.Report.WriteLine("Checking for invalid browser helper objects");

                if (regKey != null)
                {
                    foreach (string strGuid in regKey.GetSubKeyNames())
                    {
                        try
                        {
                            RegistryKey rkBHO = regKey.OpenSubKey(strGuid);

                            if (rkBHO != null)
                            {
                                if (!clsidExists(strGuid))
                                    ScanWizard.StoreInvalidKey(Strings.MissingCLSID, rkBHO.ToString());

                                rkBHO.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("The following error occured: " + ex.Message + "\nSkipping check for invalid BHO.");
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check for invalid BHOs.");
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            // Check IE Toolbars
            try
            {
                regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Internet Explorer\\Toolbar");

                ScanWizard.Report.WriteLine("Checking for invalid explorer toolbars");

                if (regKey != null)
                {
                    foreach (string strGuid in regKey.GetValueNames())
                    {
                        if (!IEToolbarIsValid(strGuid))
                            ScanWizard.StoreInvalidKey(Strings.InvalidToolbar, regKey.ToString(), strGuid);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check for invalid explorer toolbars.");
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            // Check IE Extensions
            try
            {
                regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Internet Explorer\\Extensions");

                RegistryKey rkExt = null;

                ScanWizard.Report.WriteLine("Checking for invalid explorer extensions");

                if (regKey != null)
                {
                    foreach (string strGuid in regKey.GetSubKeyNames())
                    {
                        try
                        {
                            rkExt = regKey.OpenSubKey(strGuid);

                            if (rkExt != null)
                                ValidateExplorerExt(rkExt);

                            if (rkExt != null)
                                rkExt.Close();
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check for invalid explorer extensions.");
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            // Check Explorer File Exts
            try
            {
                regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts");

                RegistryKey rkFileExt = null;

                ScanWizard.Report.WriteLine("Checking for invalid explorer file extensions");

                if (regKey != null)
                {
                    foreach (string strFileExt in regKey.GetSubKeyNames())
                    {
                        try
                        {
                            rkFileExt = regKey.OpenSubKey(strFileExt);

                            if (rkFileExt == null || strFileExt[0] != '.')
                                continue;

                            ValidateFileExt(rkFileExt);

                            if (rkFileExt != null)
                                rkFileExt.Close();
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check for invalid explorer file extensions.");
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            return;
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
                    foreach (string strProgid in rkProgids.GetValueNames())
                    {
                        if (progIDExists(strProgid))
                            bProgidExists = true;
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check for invalid OpenWithProgId.");
            }
            finally
            {
                if (rkProgids != null)
                    rkProgids.Close();
            }

            // Check if files in OpenWithList exist
            try
            {
                rkOpenList = regKey.OpenSubKey("OpenWithList");

                if (rkOpenList != null)
                {
                    foreach (string strValueName in rkOpenList.GetValueNames())
                    {
                        if (strValueName == "MRUList")
                            continue;

                        string strApp = rkOpenList.GetValue(strValueName) as string;

                        if (appExists(strApp))
                            bAppExists = true;
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check for invalid OpenWithList.");
            }
            finally
            {
                if (rkOpenList != null)
                    rkOpenList.Close();
            }

            if (!bProgidExists && !bAppExists)
                ScanWizard.StoreInvalidKey(Strings.InvalidFileExt, regKey.ToString());

            return;
        }

        private static void ValidateExplorerExt(RegistryKey regKey)
        {
            // Sees if icon file exists
            try
            {
                string strHotIcon = regKey.GetValue("HotIcon") as string;
                if (!string.IsNullOrEmpty(strHotIcon))
                    if (!ScanFunctions.IconExists(strHotIcon))
                        ScanWizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), "HotIcon");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if HotIcon exists.");
            }

            try
            {
                string strIcon = regKey.GetValue("Icon") as string;
                if (!string.IsNullOrEmpty(strIcon))
                    if (!ScanFunctions.IconExists(strIcon))
                        ScanWizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), "Icon");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if Icon exists.");
            }

            try
            {
                // Lookup CLSID extension
                string strClsidExt = regKey.GetValue("ClsidExtension") as string;
                if (!string.IsNullOrEmpty(strClsidExt))
                    ScanWizard.StoreInvalidKey(Strings.MissingCLSID, regKey.ToString(), "ClsidExtension");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if ClsidExtension exists.");
            }

            try
            {
                // See if files exist
                string strExec = regKey.GetValue("Exec") as string;
                if (!string.IsNullOrEmpty(strExec))
                    if (!Utils.FileExists(strExec))
                        ScanWizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), "Exec");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if Exec exists.");
            }

            try
            {
                string strScript = regKey.GetValue("Script") as string;
                if (!string.IsNullOrEmpty(strScript))
                    if (!Utils.FileExists(strScript))
                        ScanWizard.StoreInvalidKey(Strings.InvalidFile, regKey.ToString(), "Script");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if Script exists.");
            }
        }

        private static bool SafeInprocServerExists(RegistryKey rootKey, string subKey = "")
        {
            RegistryKey regKey = null;
            bool bRet = false;

            subKey = subKey.Trim();

            try
            {
                if (!string.IsNullOrEmpty(subKey))
                    regKey = rootKey.OpenSubKey(subKey);
                else
                    regKey = rootKey;

                bRet = InprocServerExists(regKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if InprocServer exists.");
                bRet = false;
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            return bRet;
        }

        /// <summary>
        /// Checks for inprocserver file
        /// </summary>
        /// <param name="regKey">The registry key contain Inprocserver subkey</param>
        /// <returns>False if Inprocserver is null or doesnt exist</returns>
        private static bool InprocServerExists(RegistryKey regKey)
        {
            if (regKey != null)
            {
                RegistryKey regKeyInprocSrvr = null, regKeyInprocSrvr32 = null;

                try
                {
                    regKeyInprocSrvr = regKey.OpenSubKey("InprocServer");

                    if (regKeyInprocSrvr != null)
                    {
                        string strInprocServer = regKeyInprocSrvr.GetValue("") as string;

                        if (!string.IsNullOrEmpty(strInprocServer))
                            if (Utils.FileExists(strInprocServer) || ScanWizard.IsOnIgnoreList(strInprocServer))
                                return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if InprocServer exists.");
                }
                finally
                {
                    if (regKeyInprocSrvr != null)
                        regKeyInprocSrvr.Close();
                }

                try
                {
                    regKeyInprocSrvr32 = regKey.OpenSubKey("InprocServer32");

                    if (regKeyInprocSrvr32 != null)
                    {
                        string strInprocServer32 = regKeyInprocSrvr32.GetValue("") as string;

                        if (!string.IsNullOrEmpty(strInprocServer32))
                            if (Utils.FileExists(strInprocServer32) || ScanWizard.IsOnIgnoreList(strInprocServer32))
                                return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occured: " + ex.Message + "\nUnable to check if InprocServer32 exists.");
                }
                finally
                {
                    if (regKeyInprocSrvr32 != null)
                        regKeyInprocSrvr32.Close();
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if IE toolbar GUID is valid
        /// </summary>
        private static bool IEToolbarIsValid(string strGuid)
        {
            bool bRet = false;

            if (!clsidExists(strGuid))
                bRet = false;

            if (SafeInprocServerExists(Registry.ClassesRoot, "CLSID\\" + strGuid))
                bRet = true;

            if (SafeInprocServerExists(Registry.LocalMachine, "Software\\Classes\\CLSID\\" + strGuid))
                bRet = true;

            if (SafeInprocServerExists(Registry.CurrentUser, "Software\\Classes\\CLSID\\" + strGuid))
                bRet = true;

            if (Utils.Is64BitOS)
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
        private static bool appExists(string appName)
        {
            List<RegistryKey> listRegKeys = new List<RegistryKey>();

            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey("Applications")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Classes\Applications")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Classes\Applications")));

            if (Utils.Is64BitOS)
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
                            if (!ScanWizard.IsOnIgnoreList(appName.ToString()))
                                return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for AppName");
                    }
                    finally
                    {
                        if (subKey != null)
                            subKey.Close();
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
        private static bool clsidExists(string clsid)
        {
            List<RegistryKey> listRegKeys = new List<RegistryKey>();

            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey("CLSID")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Classes\CLSID")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID")));

            if (Utils.Is64BitOS)
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
                            if (!ScanWizard.IsOnIgnoreList(subKey.ToString()))
                                return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for CLSID");
                    }
                    finally
                    {
                        if (subKey != null)
                            subKey.Close();
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
        /// <param name="progID">The ProgID</param>
        /// <returns>True if it exists</returns>
        private static bool progIDExists(string progID)
        {
            List<RegistryKey> listRegKeys = new List<RegistryKey>();

            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Classes")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Classes")));

            if (Utils.Is64BitOS)
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
                        subKey = rk.OpenSubKey(progID);

                        if (subKey != null)
                            if (!ScanWizard.IsOnIgnoreList(subKey.ToString()))
                                return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for ProgID");
                    }
                    finally
                    {
                        if (subKey != null)
                            subKey.Close();
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
        /// <param name="appID">The AppID or GUID</param>
        /// <returns>True if it exists</returns>
        private static bool appidExists(string appID)
        {
            List<RegistryKey> listRegKeys = new List<RegistryKey>();

            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.ClassesRoot.OpenSubKey(@"AppID")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.LocalMachine.OpenSubKey(@"Software\Classes\AppID")));
            Utils.SafeOpenRegistryKey(() => listRegKeys.Add(Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppID")));

            if (Utils.Is64BitOS)
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
                        subKey = rk.OpenSubKey(appID);

                        if (subKey != null)
                            if (!ScanWizard.IsOnIgnoreList(subKey.ToString()))
                                return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping check for AppID");
                    }
                    finally
                    {
                        if (subKey != null)
                            subKey.Close();
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
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class ScanFunctions
    {
        /// <summary>
        /// Gets the icon path and sees if it exists
        /// </summary>
        /// <param name="IconPath">The icon path</param>
        /// <returns>True if it exists</returns>
        internal static bool IconExists(string IconPath)
        {
            string strFileName = string.Copy(IconPath.Trim().ToLower());

            // Remove quotes
            strFileName = Utils.UnqouteSpaces(strFileName);

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

                return (Utils.FileExists(strFileName) || Wizard.IsOnIgnoreList(strFileName));
            }
            else
            {
                StringBuilder sb = new StringBuilder(strFileName);
                if (PInvoke.PathParseIconLocation(sb) >= 0)
                    if (!string.IsNullOrEmpty(sb.ToString()))
                        return (Utils.FileExists(sb.ToString()) || Wizard.IsOnIgnoreList(strFileName));
            }

            return false;
        }

        /// <summary>
        /// Sees if the directory exists
        /// </summary>
        /// <remarks>Always use this to check for directories in the scanners!</remarks>
        /// <param name="dirPath">The directory</param>
        /// <returns>True if it exists or if the path should be skipped. Otherwise, false if the file path is empty or doesnt exist</returns>
        internal static bool DirExists(string dirPath)
        {
            string strDirectory = string.Copy(dirPath.Trim().ToLower());

            // Remove quotes
            strDirectory = Utils.UnqouteSpaces(strDirectory);

            // Expand enviroment variables
            strDirectory = Environment.ExpandEnvironmentVariables(strDirectory);

            // Check drive type
            Utils.VDTReturn ret = Utils.ValidDriveType(strDirectory);
            if (ret == Utils.VDTReturn.InvalidDrive)
                return false;
            else if (ret == Utils.VDTReturn.SkipCheck)
                return true;

            // Check for illegal chars
            if (Utils.FindAnyIllegalChars(strDirectory))
                return false;

            // Remove filename.ext and trailing backslash from path
            StringBuilder sb = new StringBuilder(strDirectory);
            if (PInvoke.PathRemoveFileSpec(sb))
                if (Directory.Exists(sb.ToString()))
                    return true;

            if (Directory.Exists(strDirectory))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if registry key + value name exist
        /// </summary>
        /// <param name="inPath">Registry Key</param>
        /// <param name="valueName">Value name</param>
        /// <returns>True if it exists</returns>
        internal static bool ValueNameExists(string inPath, string valueName)
        {
            string baseKey, subKey;

            if (!Utils.ParseRegKeyPath(inPath, out baseKey, out subKey))
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
        internal static bool ValueNameExists(string mainKey, string subKey, string valueName)
        {
            bool bKeyExists = false;
            RegistryKey reg = Utils.RegOpenKey(mainKey, subKey);

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
        /// Attempts to grant access to registry key so it can be removed
        /// </summary>
        /// <param name="regKey"></param>
        /// <param name="registryRights"></param>
        internal static void GrantRegistryKeyRights(RegistryKey regKey, RegistryRights registryRights)
        {
            // Just to be sure
            if (regKey == null)
                return;

            try
            {
                RegistrySecurity regSecurity = regKey.GetAccessControl();
                string user = Environment.UserDomainName + "\\" + Environment.UserName;

                RegistryAccessRule rule = new RegistryAccessRule(user, registryRights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AccessControlType.Allow);

                regSecurity.AddAccessRule(rule);
                regKey.SetAccessControl(regSecurity);
                regKey.Flush();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An error occurred trying to get permission to remove registry key ({0}). Error: {1}", regKey.ToString(), ex.Message);
            }
        }

        /// <summary>
        /// Gets the value kind and converts it accordingly
        /// </summary>
        /// <returns>Registry value formatted to a string</returns>
        internal static string RegConvertXValueToString(RegistryKey regKey, string valueName)
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

        /// <summary>
        /// Checks if we have permission to delete a registry key
        /// </summary>
        /// <param name="key">Registry key</param>
        /// <returns>True if we can delete it</returns>
        internal static bool CanDeleteKey(RegistryKey key)
        {
            try
            {
                if (key.SubKeyCount > 0)
                {
                    bool ret = false;

                    foreach (string subKey in key.GetSubKeyNames())
                    {
                        RegistryKey subRegKey = key.OpenSubKey(subKey);

                        if (subRegKey != null)
                            ret = CanDeleteKey(subRegKey);
                        else
                            ret = false;

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
            catch (SecurityException ex)
            {
                Debug.WriteLine("Unable to check if registry key ({0}) can be deleted.\nError: {1}", key.ToString(), ex.Message);
                return false;
            }
        }
    }
}

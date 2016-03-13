using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using Microsoft.Win32;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class ScanFunctions
    {
        internal enum VdtReturn
        {
            ValidDrive = 0,
            InvalidDrive = 1,
            SkipCheck = 3
        }

        /// <summary>
        ///     Sees if path has valid drive type
        /// </summary>
        /// <param name="path">Path containing drive</param>
        /// <returns>ValidDriveTypeReturn enum</returns>
        internal static VdtReturn ValidDriveType(string path)
        {
            var sb = new StringBuilder(path);
            if (!PInvoke.PathStripToRoot(sb))
                return VdtReturn.ValidDrive;

            var dt = PInvoke.GetDriveType(sb.ToString());

            if (Settings.Default.registryCleanerOptionsRemMedia)
            {
                // Just return true if its on a removable media
                if (dt == DriveType.Removable ||
                    dt == DriveType.Network ||
                    dt == DriveType.CDRom)
                    return VdtReturn.SkipCheck;
            }

            // Return false for unkown and no root dir
            if (dt == DriveType.NoRootDirectory ||
                dt == DriveType.Unknown)
                return VdtReturn.InvalidDrive;

            return VdtReturn.ValidDrive;
        }

        internal static bool FileExists(string filePath)
        {
            var fileName = Utils.SanitizeFilePath(filePath);

            if (string.IsNullOrEmpty(fileName))
                return false;

            // Check Drive Type
            var ret = ValidDriveType(fileName);
            switch (ret)
            {
                case VdtReturn.InvalidDrive:
                    return false;
                case VdtReturn.SkipCheck:
                    return true;
                case VdtReturn.ValidDrive:
                    break;
            }

            return File.Exists(fileName) || PInvoke.PathFileExists(fileName) || Utils.SearchPath(fileName);
        }

        /// <summary>
        ///     Gets the icon path and sees if it exists
        /// </summary>
        /// <param name="iconPath">The icon path</param>
        /// <returns>True if it exists</returns>
        internal static bool IconExists(string iconPath)
        {
            var strFileName = string.Copy(iconPath.Trim().ToLower());

            // Remove quotes
            strFileName = Utils.UnqouteSpaces(strFileName);

            // Remove starting @
            if (strFileName.StartsWith("@"))
                strFileName = strFileName.Substring(1);

            // Return true if %1
            if (strFileName == "%1")
                return true;

            // Get icon path
            var nSlash = strFileName.IndexOf(',');
            if (nSlash > -1)
            {
                strFileName = strFileName.Substring(0, nSlash);

                return FileExists(strFileName) || Wizard.IsOnIgnoreList(strFileName);
            }

            var sb = new StringBuilder(strFileName);
            if (PInvoke.PathParseIconLocation(sb) < 0)
                return false;

            if (!string.IsNullOrEmpty(sb.ToString()))
                return FileExists(sb.ToString()) || Wizard.IsOnIgnoreList(strFileName);

            return false;
        }

        /// <summary>
        ///     Sees if the directory exists
        /// </summary>
        /// <remarks>Always use this to check for directories in the scanners!</remarks>
        /// <param name="dirPath">The directory</param>
        /// <returns>True if it exists or if the path should be skipped. Otherwise, false if the file path is empty or doesnt exist</returns>
        internal static bool DirExists(string dirPath)
        {
            var strDirectory = string.Copy(dirPath.Trim().ToLower());

            // Remove quotes
            strDirectory = Utils.UnqouteSpaces(strDirectory);

            // Expand enviroment variables
            strDirectory = Environment.ExpandEnvironmentVariables(strDirectory);

            // Check drive type
            var ret = ValidDriveType(strDirectory);
            switch (ret)
            {
                case VdtReturn.InvalidDrive:
                    return false;
                case VdtReturn.SkipCheck:
                    return true;
                case VdtReturn.ValidDrive:
                    break;
            }

            // Check for illegal chars
            if (Utils.FindAnyIllegalChars(strDirectory))
                return false;

            // Remove filename.ext and trailing backslash from path
            var sb = new StringBuilder(strDirectory);
            if (!PInvoke.PathRemoveFileSpec(sb))
                return Directory.Exists(strDirectory);

            return Directory.Exists(sb.ToString()) || Directory.Exists(strDirectory);
        }

        /// <summary>
        ///     Checks if registry key + value name exist
        /// </summary>
        /// <param name="inPath">Registry Key</param>
        /// <param name="valueName">Value name</param>
        /// <returns>True if it exists</returns>
        internal static bool ValueNameExists(string inPath, string valueName)
        {
            string baseKey, subKey;

            return Utils.ParseRegKeyPath(inPath, out baseKey, out subKey) && ValueNameExists(baseKey, subKey, valueName);
        }

        /// <summary>
        ///     Checks if registry key + value name exist
        /// </summary>
        /// <param name="mainKey">Registry Hive</param>
        /// <param name="subKey">Registry Sub Key</param>
        /// <param name="valueName">Value Name</param>
        /// <returns>True if value name exists in registry key</returns>
        internal static bool ValueNameExists(string mainKey, string subKey, string valueName)
        {
            var bKeyExists = false;
            var reg = Utils.RegOpenKey(mainKey, subKey);

            try
            {
                if (reg?.GetValue(valueName) != null)
                    bKeyExists = true;
            }
            catch
            {
                bKeyExists = false;
            }
            finally
            {
                reg?.Close();
            }

            return bKeyExists;
        }

        /// <summary>
        ///     Attempts to grant access to registry key so it can be removed
        /// </summary>
        /// <param name="regKey"></param>
        /// <param name="registryRights"></param>
        [Obsolete("No longer used")]
        internal static void GrantRegistryKeyRights(RegistryKey regKey, RegistryRights registryRights)
        {
            // Just to be sure
            if (regKey == null)
                return;

            try
            {
                var regSecurity = regKey.GetAccessControl();
                var user = Environment.UserDomainName + "\\" + Environment.UserName;

                var rule = new RegistryAccessRule(user, registryRights,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly,
                    AccessControlType.Allow);

                regSecurity.AddAccessRule(rule);
                regKey.SetAccessControl(regSecurity);
                regKey.Flush();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An error occurred trying to get permission to remove registry key ({0}). Error: {1}",
                    regKey, ex.Message);
            }
        }

        /// <summary>
        ///     Gets the value kind and converts it accordingly
        /// </summary>
        /// <returns>Registry value formatted to a string</returns>
        internal static string RegConvertXValueToString(RegistryKey regKey, string valueName)
        {
            var strRet = "";

            if (regKey == null)
                return strRet;

            try
            {
                switch (regKey.GetValueKind(valueName))
                {
                    case RegistryValueKind.MultiString:
                    {
                        var strValue = "";
                        var strValues = (string[]) regKey.GetValue(valueName);

                        for (var i = 0; i < strValues.Length; i++)
                        {
                            if (i != 0)
                                strValue = string.Concat(strValue, ",");

                            strValue = $"{strValue} {strValues[i]}";
                        }

                        strRet = string.Copy(strValue);

                        break;
                    }
                    case RegistryValueKind.Binary:
                    {
                        var strValue = ((byte[]) regKey.GetValue(valueName)).Aggregate("",
                            (current, b) => $"{current} {b:X2}");

                        strRet = string.Copy(strValue);

                        break;
                    }
                    case RegistryValueKind.DWord:
                    case RegistryValueKind.QWord:
                    {
                        strRet = string.Format("0x{0:X} ({0:D})", regKey.GetValue(valueName));
                        break;
                    }
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.Unknown:
                    case RegistryValueKind.None:
                    {
                        strRet = $"{regKey.GetValue(valueName)}";
                        break;
                    }

                    default:
                    {
                        throw new ArgumentOutOfRangeException();
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
        ///     Checks if we have permission to delete a registry key
        /// </summary>
        /// <param name="key">Registry key</param>
        /// <returns>True if we can delete it</returns>
        internal static bool CanDeleteKey(RegistryKey key)
        {
            try
            {
                if (key.SubKeyCount > 0)
                {
                    var ret = false;

                    foreach (var subRegKey in key.GetSubKeyNames().Select(key.OpenSubKey))
                    {
                        ret = subRegKey != null && CanDeleteKey(subRegKey);

                        if (!ret)
                            break;
                    }

                    return ret;
                }

                var regSecurity = key.GetAccessControl();

                return
                    regSecurity.GetAccessRules(true, false, typeof (NTAccount))
                        .Cast<AuthorizationRule>()
                        .All(
                            rule =>
                                (RegistryRights.Delete & ((RegistryAccessRule) rule).RegistryRights) ==
                                RegistryRights.Delete);
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine("Unable to check if registry key ({0}) can be deleted.\nError: {1}", key, ex.Message);
                return false;
            }
        }
    }
}
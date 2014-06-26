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

                return (Utils.FileExists(strFileName) || ScanWizard.IsOnIgnoreList(strFileName));
            }
            else
            {
                StringBuilder sb = new StringBuilder(strFileName);
                if (PInvoke.PathParseIconLocation(sb) >= 0)
                    if (!string.IsNullOrEmpty(sb.ToString()))
                        return (Utils.FileExists(sb.ToString()) || ScanWizard.IsOnIgnoreList(strFileName));
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
            if (string.IsNullOrEmpty(dirPath))
                return false;

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
    }
}

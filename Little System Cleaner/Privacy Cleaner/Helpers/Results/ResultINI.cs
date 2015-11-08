using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{

    #region INI Info Struct

    public struct IniInfo
    {
        /// <summary>
        ///     Path of the INI File
        /// </summary>
        public string FilePath;

        /// <summary>
        ///     Section Name
        /// </summary>
        public string SectionName;

        /// <summary>
        ///     Value Name (optional)
        /// </summary>
        public string ValueName;
    }

    #endregion

    public class ResultIni : ResultNode
    {
        /// <summary>
        ///     Constructor for bad INI file
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="iniInfo">INI Info Array</param>
        public ResultIni(string desc, IniInfo[] iniInfo)
        {
            Description = desc;
            IniInfoList = iniInfo;
        }

        public override void Clean(Report report)
        {
            foreach (var iniInfo in IniInfoList)
            {
                var filePath = iniInfo.FilePath;
                var section = iniInfo.SectionName;
                var valueName = iniInfo.ValueName;

                // Delete section if value name is empty
                if (string.IsNullOrEmpty(valueName))
                {
                    if (MiscFunctions.WritePrivateProfileString(section, null, null, filePath))
                        report.WriteLine($"Erased INI File: {filePath} Section: {section}");
                    else
                    {
                        var ex = new Win32Exception(Marshal.GetLastWin32Error());
                        string message =
                            $"Unable to erase INI File: {filePath} Section: {section}\nValue: {valueName}\nError: {ex.Message}";

                        Debug.WriteLine(message);
                    }
                }
                else
                {
                    if (MiscFunctions.WritePrivateProfileString(section, valueName, null, filePath))
                        report.WriteLine($"Erased INI File: {filePath} Section: {section} Value Name: {valueName}");
                    else
                    {
                        var ex = new Win32Exception(Marshal.GetLastWin32Error());
                        string message =
                            $"Unable to erase INI File: {filePath} Section: {section} Value Name: {valueName}\nError: {ex.Message}";

                        Debug.WriteLine(message);
                    }
                }

                Settings.Default.lastScanErrorsFixed++;
            }
        }
    }
}
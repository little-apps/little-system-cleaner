using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    #region INI Info Struct
    public struct INIInfo
    {
        /// <summary>
        /// Path of the INI File
        /// </summary>
        public string filePath;
        /// <summary>
        /// Section Name
        /// </summary>
        public string sectionName;
        /// <summary>
        /// Value Name (optional)
        /// </summary>
        public string valueName;
    }
    #endregion

    public class ResultINI : ResultNode
    {
        /// <summary>
        /// Constructor for bad INI file
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="iniInfo">INI Info Array</param>
        public ResultINI(string desc, INIInfo[] iniInfo)
        {
            this.Description = desc;
            this.iniInfoList = iniInfo;
        }

        public override void Clean(Report report)
        {
            foreach (INIInfo iniInfo in this.iniInfoList)
            {
                string filePath = iniInfo.filePath;
                string section = iniInfo.sectionName;
                string valueName = iniInfo.valueName;

                // Delete section if value name is empty
                if (string.IsNullOrEmpty(valueName))
                {
                    if (PInvoke.WritePrivateProfileString(section, null, null, filePath))
                        report.WriteLine(string.Format("Erased INI File: {0} Section: {1}", filePath, section));
                    else
                    {
                        Win32Exception ex = new Win32Exception(Marshal.GetLastWin32Error());
                        string message = string.Format("Unable to erase INI File: {0} Section: {1}\nError: {3}", filePath, section, valueName, ex.Message);

                        System.Diagnostics.Debug.WriteLine(message);
                    }
                }
                else
                {
                    if (PInvoke.WritePrivateProfileString(section, valueName, null, filePath))
                        report.WriteLine(string.Format("Erased INI File: {0} Section: {1} Value Name: {2}", filePath, section, valueName));
                    else
                    {
                        Win32Exception ex = new Win32Exception(Marshal.GetLastWin32Error());
                        string message = string.Format("Unable to erase INI File: {0} Section: {1} Value Name: {2}\nError: {3}", filePath, section, valueName, ex.Message);

                        System.Diagnostics.Debug.WriteLine(message);
                    }
                }

                Properties.Settings.Default.lastScanErrorsFixed++;
            }
        }
    }
}

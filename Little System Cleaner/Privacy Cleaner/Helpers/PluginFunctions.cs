using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers
{
    public class PluginFunctions
    {
        private readonly Dictionary<RegistryKey, string[]> regKeyValueNames;
        private readonly Dictionary<RegistryKey, bool> regKeySubKeys;
        private readonly Dictionary<string, bool> dictFolders;
        private readonly List<string> filePathList;
        private readonly List<INIInfo> iniInfoList;
        private readonly Dictionary<string, string> dictXmlPaths;

        public Dictionary<RegistryKey, string[]> RegistryValueNames 
        {
            get
            {
                return regKeyValueNames;
            }
        }

        public Dictionary<RegistryKey, bool> RegistrySubKeys
        {
            get
            {
                return regKeySubKeys;
            }
        }
        public Dictionary<string, bool> Folders
        {
            get
            {
                return dictFolders;
            }
        }
        public List<string> FilePaths
        {
            get
            {
                return filePathList;
            }
        }
        public List<INIInfo> INIList
        {
            get
            {
                return iniInfoList;
            }
        }
        public Dictionary<string, string> XmlPaths
        {
            get
            {
                return dictXmlPaths;
            }
        }

        public PluginFunctions()
        {
            regKeyValueNames = new Dictionary<RegistryKey, string[]>();
            regKeySubKeys = new Dictionary<RegistryKey, bool>();
            dictFolders = new Dictionary<string, bool>();
            filePathList = new List<string>();
            iniInfoList = new List<INIInfo>();
            dictXmlPaths = new Dictionary<string, string>();
        }

        public void DeleteKey(RegistryKey regKey, bool recurse)
        {
            if (regKey == null)
                return;

            Wizard.CurrentFile = regKey.Name;

            RegistrySubKeys.Add(regKey, recurse);
        }

        public void DeleteValue(RegistryKey regKey, string searchText)
        {
            List<string> valueNames = new List<string>();

            if (regKey == null)
                return;

            Wizard.CurrentFile = regKey.Name;

            // Get value names that match regex
            foreach (string valueName in regKey.GetValueNames())
            {
                if (Regex.IsMatch(valueName, searchText))
                    valueNames.Add(valueName);
            }

            if (!RegistryValueNames.ContainsKey(regKey))
                // Create new entry if regkey doesnt exist
                RegistryValueNames.Add(regKey, valueNames.ToArray());
            else
            {
                // Append value names to existing entry
                valueNames.AddRange(RegistryValueNames[regKey]);

                RegistryValueNames[regKey] = valueNames.ToArray();
            }
        }

        public void DeleteFile(string filePath)
        {
            Wizard.CurrentFile = filePath;

            if (!filePathList.Contains(filePath) && File.Exists(filePath))
                FilePaths.Add(filePath);
        }

        public void DeleteFolder(string folderPath, bool recurse)
        {
            Wizard.CurrentFile = folderPath;

            if (!Folders.ContainsKey(folderPath) && Directory.Exists(folderPath))
                Folders.Add(folderPath, recurse);
        }

        public void DeleteFileList(string searchPath, string searchText, SearchOption includeSubFolders)
        {
            // Skip if search path doesnt exist
            if (!Directory.Exists(searchPath))
                return;

            foreach (string filePath in Directory.GetFiles(searchPath, searchText, includeSubFolders))
            {
                string fileName = Path.GetFileName(filePath);

                Wizard.CurrentFile = filePath;

                FilePaths.Add(filePath);
            }
        }

        public void DeleteFolderList(string searchPath, string searchText, SearchOption includeSubFolders)
        {
            // Skip if search path doesnt exist
            if (!Directory.Exists(searchPath))
                return;

            foreach (string folderPath in Directory.GetDirectories(searchPath, searchText, includeSubFolders))
            {
                string folderName = Path.GetDirectoryName(folderPath);

                Wizard.CurrentFile = folderPath;

                if (!Folders.ContainsKey("folderPath"))
                    Folders.Add(folderPath, false);
            }
        }

        public void DeleteFoundRegKeys(RegistryKey regKey, bool includeSubKeys, XmlReader xmlChildren)
        {
            if (regKey == null)
                return;

            Dictionary<string, bool> regexSubKeys = new Dictionary<string, bool>();
            List<string> regexValueNames = new List<string>();
            Dictionary<RegistryKey, string[]> valueNames = new Dictionary<RegistryKey, string[]>();
            Dictionary<RegistryKey, bool> subKeys = new Dictionary<RegistryKey, bool>();

            while (xmlChildren.Read())
            {
                if (xmlChildren.Name == "IfSubKey")
                {
                    string searchText = xmlChildren.GetAttribute("SearchText");
                    bool recurse = ((xmlChildren.GetAttribute("Recursive") == "Y") ? (true) : (false));

                    regexSubKeys.Add(searchText, recurse);
                }
                else if (xmlChildren.Name == "IfValueName")
                {
                    string searchText = xmlChildren.GetAttribute("SearchText");
                    regexValueNames.Add(searchText);
                }
            }


            valueNames = RecurseRegKeyValueNames(regKey, regexValueNames, includeSubKeys);
            subKeys = RecurseRegKeySubKeys(regKey, regexSubKeys, includeSubKeys);

            if (valueNames.Count > 0)
            {
                foreach (KeyValuePair<RegistryKey, string[]> kvp in valueNames)
                    RegistryValueNames.Add(kvp.Key, kvp.Value);
            }

            if (subKeys.Count > 0)
            {
                foreach (KeyValuePair<RegistryKey, bool> kvp in subKeys)
                    RegistrySubKeys.Add(kvp.Key, kvp.Value);
            }
        }

        public void DeleteFoundPaths(string searchPath, string searchText, SearchOption includeSubFolders, XmlReader xmlChildren)
        {
            List<string> regexFiles = new List<string>();
            Dictionary<string, bool> regexFolders = new Dictionary<string, bool>();

            while (xmlChildren.Read())
            {
                if (xmlChildren.Name == "IfFile")
                {
                    string fileName = xmlChildren.GetAttribute("SearchText");
                    if (!string.IsNullOrEmpty(fileName))
                        regexFiles.Add(fileName);
                }
                else if (xmlChildren.Name == "IfFolder")
                {
                    string folderName = xmlChildren.GetAttribute("SearchText");
                    bool recurse = ((xmlChildren.GetAttribute("Recursive") == "Y") ? (true) : (false));

                    if (!string.IsNullOrEmpty(folderName))
                        regexFolders.Add(folderName, recurse);
                }
            }

            // Skip if search path doesnt exist or the lists are empty
            if (!Directory.Exists(searchPath) || (regexFiles.Count == 0 && regexFolders.Count == 0))
                return;

            try
            {
                foreach (string folderPath in Directory.GetDirectories(searchPath, searchText, includeSubFolders))
                {
                    Wizard.CurrentFile = folderPath;
                    string folderName = folderPath.Substring(Path.GetDirectoryName(folderPath).Length + 1);

                    // Iterate through the files and folders in the current folder
                    foreach (KeyValuePair<string, bool> kvp in regexFolders)
                    {
                        if (Regex.IsMatch(folderName, kvp.Key))
                        {
                            Folders.Add(folderPath, kvp.Value);
                        }
                    }

                    foreach (string filePath in Directory.GetFiles(folderPath))
                    {
                        if (string.IsNullOrEmpty(filePath))
                            continue;

                        // Get filename from file path
                        string fileName = Path.GetFileName(filePath);

                        foreach (string regex in regexFiles)
                        {
                            if (string.IsNullOrEmpty(regex))
                                continue;

                            if (Regex.IsMatch(fileName, regex))
                            {
                                FilePaths.Add(filePath);
                                break;
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {

            }
        }

        public void DeleteINIValue(string filePath, string searchSectionText, string searchValueNameText)
        {
            if (!File.Exists(filePath))
                return;

            foreach (string sectionName in Utils.GetSections(filePath))
            {
                if (string.IsNullOrEmpty(sectionName))
                    continue;

                if (Regex.IsMatch(sectionName, searchSectionText))
                {
                    foreach (KeyValuePair<string, string> kvp in Utils.GetValues(filePath, sectionName))
                    {
                        if (Regex.IsMatch(kvp.Key, searchValueNameText))
                        {
                            INIList.Add(new INIInfo() { filePath = filePath, sectionName = sectionName, valueName = kvp.Key });
                        }
                    }
                }
            }
        }

        public void DeleteINISection(string filePath, string searchSectionText)
        {
            if (!File.Exists(filePath))
                return;

            foreach (string sectionName in Utils.GetSections(filePath))
            {
                if (string.IsNullOrEmpty(sectionName))
                    continue;

                if (Regex.IsMatch(sectionName, searchSectionText))
                {
                    INIList.Add(new INIInfo() { filePath = filePath, sectionName = sectionName });
                }
            }
        }

        public void DeleteXml(string filePath, string xPath)
        {
            if (!File.Exists(filePath))
                return;

            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(xPath))
                XmlPaths.Add(filePath, xPath);
        }

        Dictionary<RegistryKey, string[]> RecurseRegKeyValueNames(RegistryKey regKey, List<string> regexValueNames, bool recurse)
        {
            Dictionary<RegistryKey, string[]> ret = new Dictionary<RegistryKey, string[]>();
            List<string> valueNames = new List<string>();

            if (regKey == null || regexValueNames.Count == 0)
                return ret;

            foreach (string valueName in regKey.GetValueNames())
            {
                foreach (string regex in regexValueNames)
                {
                    if (Regex.IsMatch(valueName, regex) && (!valueNames.Contains(valueName)))
                    {
                        valueNames.Add(valueName);
                        break;
                    }
                }
            }

            if (recurse)
            {
                foreach (string subKey in regKey.GetSubKeyNames())
                {
                    RegistryKey subRegKey = regKey.OpenSubKey(subKey);

                    foreach (KeyValuePair<RegistryKey, string[]> kvp in RecurseRegKeyValueNames(subRegKey, regexValueNames, recurse))
                        ret.Add(kvp.Key, kvp.Value);

                }
            }

            if (valueNames.Count > 0)
                ret.Add(regKey, valueNames.ToArray());

            return ret;
        }

        Dictionary<RegistryKey, bool> RecurseRegKeySubKeys(RegistryKey regKey, Dictionary<string, bool> regexSubKeys, bool recurse)
        {
            Dictionary<RegistryKey, bool> ret = new Dictionary<RegistryKey, bool>();

            if (regKey == null || regexSubKeys.Count == 0)
                return ret;

            foreach (string subKeyName in regKey.GetSubKeyNames())
            {
                foreach (KeyValuePair<string, bool> kvp in regexSubKeys)
                {
                    if (Regex.IsMatch(subKeyName, kvp.Key))
                    {
                        RegistryKey subKey = regKey.OpenSubKey(subKeyName, true);

                        if (subKey != null)
                        {
                            ret.Add(subKey, kvp.Value);
                            break;
                        }
                    }
                }
            }

            if (recurse)
            {
                foreach (string subKey in regKey.GetSubKeyNames())
                {
                    RegistryKey subRegKey = regKey.OpenSubKey(subKey, true);

                    foreach (KeyValuePair<RegistryKey, bool> kvp in RecurseRegKeySubKeys(subRegKey, regexSubKeys, recurse))
                        ret.Add(kvp.Key, kvp.Value);
                }
            }

            return ret;
        }
    }
}

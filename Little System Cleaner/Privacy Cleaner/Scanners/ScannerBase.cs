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
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using System.Windows.Controls;
using CommonTools;
using System.Windows;
using System.Windows.Media;
using CommonTools.WpfAnimatedGif;

namespace Little_System_Cleaner.Privacy_Cleaner.Scanners
{
    public abstract class ScannerBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly ObservableCollection<ScannerBase> _children = new ObservableCollection<ScannerBase>();
        public ObservableCollection<ScannerBase> Children
        {
            get { return _children; }
        }

        private bool? _bIsChecked = true;

        #region IsChecked Methods
        public void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _bIsChecked)
                return;

            _bIsChecked = value;

            if (updateChildren && _bIsChecked.HasValue)
            {
                foreach (ScannerBase c in this.Children)
                    c.SetIsChecked(_bIsChecked, true, false);
            }

            if (updateParent && Parent != null)
                Parent.VerifyCheckState();

            this.OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool? current = this.Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            this.SetIsChecked(state, false, true);
        }
        #endregion

        public bool? IsChecked
        {
            get { return _bIsChecked; }
            set { this.SetIsChecked(value, true, true); }
        }

        public ScannerBase Parent { get; set; }
        public string Description { get; set; }

        public ImageSource bMapImg { get; private set; }
        public System.Drawing.Bitmap Icon
        {
            set
            {
                IntPtr hBitmap = value.GetHbitmap();

                bMapImg = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                this.OnPropertyChanged("bMapImg");
            }
        }

        public virtual void Scan(ScannerBase child)
        {
            return;
        }

        public virtual void Scan()
        {
            return;
        }

        public virtual bool IsRunning()
        {
            return false;
        }

        public string Errors { get; set; }

        /// <summary>
        /// Returns process name for scanner
        /// </summary>
        public virtual string ProcessName
        {
            get { return string.Empty; }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set 
            {
                _name = value;
                this.Results = new RootNode(this.Section);
            }
        }

        public string ToolTipText
        {
            get { return Description; }
        }

        public string PluginPath
        {
            get;
            set;
        }

        public string Section
        {
            get
            {
                if (this.Parent == null)
                {
                    return this.Name;
                }
                else
                {
                    string parentName = this.Parent.Name;
                    return parentName + " - " + this.Name;
                }
            }
        }

        public string Status
        {
            get;
            set;
        }

        public Image Image
        {
            get;
            set;
        }

        public ScannerBase() { }

        public void LoadGif()
        {
            this.Image = new System.Windows.Controls.Image();

            BitmapSource gif = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.ajax_loader.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            ImageBehavior.SetAnimatedSource(this.Image, gif);
        }

        public void UnloadGif()
        {
            this.Image = null;

            this.Image = new System.Windows.Controls.Image();
            this.Image.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.finished_scanning.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }

        public ResultNode Results;

        #region Plugin Scanner

        public bool PluginIsValid(string xmlFilePath, out string name, out string description)
        {
            bool bRet = false;
            name = description = string.Empty;

            if (!File.Exists(xmlFilePath))
                return false;

            using (XmlTextReader xmlReader = new XmlTextReader(xmlFilePath))
            {
                // Read Information node and add it to node list
                if (xmlReader.ReadToFollowing("Information"))
                {
                    if (xmlReader.ReadToFollowing("Name"))
                        name = xmlReader.ReadElementContentAsString();
                    if (xmlReader.ReadToFollowing("Description"))
                        description = xmlReader.ReadElementContentAsString();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
                        return false;
                }

                // See if scanner is valid
                if (xmlReader.ReadToFollowing("IsValid"))
                {
                    while (xmlReader.Read()) 
                    {
                        if (xmlReader.NodeType != XmlNodeType.Element)
                            continue;

                        // Parse registry key and see if it exists
                        if (xmlReader.Name == "KeyExist")
                        {
                            string regKeyPath = xmlReader.ReadElementContentAsString();
                            bRet = Utils.RegKeyExists(regKeyPath);

                            if (!bRet)
                                return bRet;
                        }
                        else if (xmlReader.Name == "ValueExist")
                        {
                            string regKeyPath = xmlReader.GetAttribute("RegKey");
                            string valueNameRegEx = xmlReader.GetAttribute("ValueName");

                            using (RegistryKey rk = Utils.RegOpenKey(regKeyPath))
                            {
                                if (rk == null)
                                    continue;

                                foreach (string valueName in rk.GetValueNames())
                                {
                                    if (Regex.IsMatch(valueName, valueNameRegEx))
                                    {
                                        bRet = true;
                                        break;
                                    }
                                }
                            }

                            if (!bRet)
                                return bRet;
                        }
                        // See if file exists
                        else if (xmlReader.Name == "FileExist")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.ReadElementContentAsString());
                            bRet = File.Exists(filePath);

                            if (!bRet)
                                return bRet;
                        }
                        // See if folder exists
                        else if (xmlReader.Name == "FolderExist")
                        {
                            string folderPath = Utils.ExpandVars(xmlReader.ReadElementContentAsString());
                            bRet = Directory.Exists(folderPath);

                            if (!bRet)
                                return bRet;
                        }
                    }
                }

                return bRet;
            }
        }

        /// <summary>
        /// Goes through the nodes and parses the .xml file
        /// </summary>
        public void ScanPlugins()
        {
            foreach (ScannerBase n in this.Children)
            {
                if (!string.IsNullOrEmpty(n.Name) && !string.IsNullOrEmpty(n.PluginPath))
                    ScanPlugin(n.Name, n.PluginPath);
            }
        }

        /// <summary>
        /// Parses the .xml file and performs the actions specified
        /// </summary>
        /// <param name="pluginFile">Path to .xml file</param>
        public void ScanPlugin(string name, string pluginFile)
        {
            Dictionary<RegistryKey, string[]> resultRegKeyValueNames = new Dictionary<RegistryKey, string[]>();
            Dictionary<RegistryKey, bool> resultRegKeySubKeys = new Dictionary<RegistryKey, bool>();
            Dictionary<string, bool> dictFolders = new Dictionary<string, bool>();
            List<string> filePathList = new List<string>();
            List<INIInfo> iniInfoList = new List<INIInfo>();
            Dictionary<string, string> dictXmlPaths = new Dictionary<string,string>();

            using (XmlTextReader xmlReader = new XmlTextReader(pluginFile))
            {
                while (xmlReader.ReadToFollowing("IsRunning"))
                {
                    string procName = xmlReader.ReadElementContentAsString();

                    if (RunningMsg.DisplayRunningMsg(name, procName).GetValueOrDefault() == false)
                        return;
                }

                if (xmlReader.ReadToFollowing("Action"))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType != XmlNodeType.Element)
                            continue;

                        if (xmlReader.Name == "DeleteKey")
                        {
                            string regPath = xmlReader.ReadElementContentAsString();
                            RegistryKey regKey = Utils.RegOpenKey(regPath);
                            bool recurse = ((xmlReader.GetAttribute("Recursive") == "Y") ? (true) : (false));

                            if (regKey == null)
                                continue;

                            Wizard.CurrentFile = regKey.Name;

                            resultRegKeySubKeys.Add(regKey, recurse);
                        }

                        if (xmlReader.Name == "DeleteValue")
                        {
                            string regPath = xmlReader.GetAttribute("RegKey");
                            string valueNameRegEx = xmlReader.GetAttribute("ValueName");
                            List<string> valueNames = new List<string>();

                            RegistryKey regKey = Utils.RegOpenKey(regPath);

                            if (regKey == null)
                                continue;

                            Wizard.CurrentFile = regKey.Name;

                            // Get value names that match regex
                            foreach (string valueName in regKey.GetValueNames())
                            {
                                if (Regex.IsMatch(valueName, valueNameRegEx))
                                    valueNames.Add(valueName);
                            }

                            if (!resultRegKeyValueNames.ContainsKey(regKey))
                                // Create new entry if regkey doesnt exist
                                resultRegKeyValueNames.Add(regKey, valueNames.ToArray());
                            else
                            {
                                // Append value names to existing entry
                                valueNames.AddRange(resultRegKeyValueNames[regKey]);

                                resultRegKeyValueNames[regKey] = valueNames.ToArray();
                            }
                        }

                        if (xmlReader.Name == "DeleteFile")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.ReadElementContentAsString());

                            Wizard.CurrentFile = filePath;

                            if (!filePathList.Contains(filePath) && File.Exists(filePath)) 
                                filePathList.Add(filePath);
                        }

                        if (xmlReader.Name == "DeleteFolder")
                        {
                            string folderPath = Utils.ExpandVars(xmlReader.ReadElementContentAsString());
                            bool recurse = ((xmlReader.GetAttribute("Recursive") == "Y") ? (true) : (false));

                            Wizard.CurrentFile = folderPath;

                            if (!dictFolders.ContainsKey(folderPath) && Directory.Exists(folderPath))
                                dictFolders.Add(folderPath, recurse);
                        }

                        if (xmlReader.Name == "DeleteFileList")
                        {
                            string searchPath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string searchText = xmlReader.GetAttribute("SearchText");
                            SearchOption includeSubFolders = ((xmlReader.GetAttribute("IncludeSubFolders") == "Y") ? (SearchOption.AllDirectories) : (SearchOption.TopDirectoryOnly));

                            // Skip if search path doesnt exist
                            if (!Directory.Exists(searchPath))
                                continue;

                            foreach (string filePath in Directory.GetFiles(searchPath, searchText, includeSubFolders))
                            {
                                string fileName = Path.GetFileName(filePath);

                                Wizard.CurrentFile = filePath;

                                filePathList.Add(filePath);
                            }
                        }

                        if (xmlReader.Name == "DeleteFolderList")
                        {
                            string searchPath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string searchText = xmlReader.GetAttribute("SearchText");
                            SearchOption includeSubFolders = ((xmlReader.GetAttribute("IncludeSubFolders") == "Y") ? (SearchOption.AllDirectories) : (SearchOption.TopDirectoryOnly));

                            // Skip if search path doesnt exist
                            if (!Directory.Exists(searchPath))
                                continue;

                            foreach (string folderPath in Directory.GetDirectories(searchPath, searchText, includeSubFolders))
                            {
                                string folderName = Path.GetDirectoryName(folderPath);

                                Wizard.CurrentFile = folderPath;

                                if (!dictFolders.ContainsKey("folderPath"))
                                    dictFolders.Add(folderPath, false);
                            }
                        }

                        if (xmlReader.Name == "FindRegKey")
                        {
                            string regKey = xmlReader.GetAttribute("RegKey");
                            bool includeSubKeys = ((xmlReader.GetAttribute("IncludeSubKeys") == "Y") ? (true):(false));

                            RegistryKey rk = Utils.RegOpenKey(regKey);
                            if (rk == null)
                                continue;

                            Dictionary<string, bool> regexSubKeys = new Dictionary<string,bool>();
                            List<string> regexValueNames = new List<string>();

                            while (xmlReader.Read())
                            {
                                if (xmlReader.Name == "IfSubKey")
                                {
                                    string searchText = xmlReader.GetAttribute("SearchText");
                                    bool recurse = ((xmlReader.GetAttribute("Recursive") == "Y") ? (true) : (false));

                                    regexSubKeys.Add(searchText, recurse);
                                }
                                else if (xmlReader.Name == "IfValueName")
                                {
                                    string searchText = xmlReader.GetAttribute("SearchText");
                                    regexValueNames.Add(searchText);
                                }
                            }

                            Dictionary<RegistryKey, string[]> valueNames = new Dictionary<RegistryKey, string[]>();
                            Dictionary<RegistryKey, bool> subKeys = new Dictionary<RegistryKey, bool>();

                            valueNames = RecurseRegKeyValueNames(rk, regexValueNames, includeSubKeys);
                            subKeys = RecurseRegKeySubKeys(rk, regexSubKeys, includeSubKeys);

                            if (valueNames.Count > 0)
                            {
                                foreach (KeyValuePair<RegistryKey, string[]> kvp in valueNames)
                                    resultRegKeyValueNames.Add(kvp.Key, kvp.Value);
                            }

                            if (subKeys.Count > 0)
                            {
                                foreach (KeyValuePair<RegistryKey, bool> kvp in subKeys)
                                    resultRegKeySubKeys.Add(kvp.Key, kvp.Value);
                            }
                        }

                        if (xmlReader.Name == "FindPath")
                        {
                            string searchPath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string searchText = xmlReader.GetAttribute("SearchText");
                            SearchOption includeSubFolders = ((xmlReader.GetAttribute("IncludeSubFolders") == "Y") ? (SearchOption.AllDirectories) : (SearchOption.TopDirectoryOnly));

                            List<string> regexFiles = new List<string>();
                            Dictionary<string, bool> regexFolders = new Dictionary<string, bool>();

                            while (xmlReader.Read())
                            {
                                if (xmlReader.Name == "IfFile")
                                {
                                    string fileName = xmlReader.GetAttribute("SearchText");
                                    if (!string.IsNullOrEmpty(fileName))
                                        regexFiles.Add(fileName);
                                }
                                else if (xmlReader.Name == "IfFolder")
                                {
                                    string folderName = xmlReader.GetAttribute("SearchText");
                                    bool recurse = ((xmlReader.GetAttribute("Recursive") == "Y") ? (true) : (false));

                                    if (!string.IsNullOrEmpty(folderName))
                                        regexFolders.Add(folderName, recurse);
                                }
                            }

                            // Skip if search path doesnt exist or the lists are empty
                            if (!Directory.Exists(searchPath) || (regexFiles.Count == 0 && regexFolders.Count == 0))
                                continue;

                            try
                            {
                                foreach (string folderPath in Directory.GetDirectories(searchPath, searchText, includeSubFolders))
                                {
                                    Wizard.CurrentFile = folderPath;
                                    string folderName = folderPath.Substring(Path.GetDirectoryName(folderPath).Length +1);

                                    // Iterate through the files and folders in the current folder
                                    foreach (KeyValuePair<string, bool> kvp in regexFolders)
                                    {
                                        if (Regex.IsMatch(folderName, kvp.Key))
                                        {
                                            dictFolders.Add(folderPath, kvp.Value);
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
                                                filePathList.Add(filePath);
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

                        if (xmlReader.Name == "RemoveINIValue")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string sectionRegEx = xmlReader.GetAttribute("Section");
                            string valueRegEx = xmlReader.GetAttribute("Name");

                            if (!File.Exists(filePath))
                                continue;

                            foreach (string sectionName in Utils.GetSections(filePath))
                            {
                                if (string.IsNullOrEmpty(sectionName))
                                    continue;

                                if (Regex.IsMatch(sectionName, sectionRegEx))
                                {
                                    foreach (KeyValuePair<string, string> kvp in Utils.GetValues(filePath, sectionName))
                                    {
                                        if (Regex.IsMatch(kvp.Key, valueRegEx))
                                        {
                                            iniInfoList.Add(new INIInfo() { filePath = filePath, sectionName = sectionName, valueName = kvp.Key });
                                        }
                                    }
                                }
                            }
                        }

                        if (xmlReader.Name == "RemoveINISection")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string sectionRegEx = xmlReader.GetAttribute("Section");

                            if (!File.Exists(filePath))
                                continue;

                            foreach (string sectionName in Utils.GetSections(filePath))
                            {
                                if (string.IsNullOrEmpty(sectionName))
                                    continue;

                                if (Regex.IsMatch(sectionName, sectionRegEx))
                                {
                                    iniInfoList.Add(new INIInfo() { filePath = filePath, sectionName = sectionName });
                                }
                            }
                        }

                        if (xmlReader.Name == "RemoveXML")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string xPath = xmlReader.GetAttribute("XPath");

                            if (!File.Exists(filePath))
                                continue;

                            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(xPath))
                                dictXmlPaths.Add(filePath, xPath);
                        }
                    }
                }
            }

            if (resultRegKeySubKeys.Count > 0)
                Wizard.StoreBadRegKeySubKeys(name, resultRegKeySubKeys);

            if (resultRegKeyValueNames.Count > 0)
                Wizard.StoreBadRegKeyValueNames(name, resultRegKeyValueNames);

            if (filePathList.Count > 0)
                Wizard.StoreBadFileList(name, filePathList.ToArray());

            if (dictFolders.Count > 0)
                Wizard.StoreBadFolderList(name, dictFolders);

            if (iniInfoList.Count > 0)
                Wizard.StoreINIKeys(name, iniInfoList.ToArray());

            if (dictXmlPaths.Count > 0)
                Wizard.StoreXML(name, dictXmlPaths);

            return;
        }

        Dictionary<RegistryKey, string[]> RecurseRegKeyValueNames(RegistryKey regKey, List<string> regexValueNames, bool recurse)
        {
            Dictionary<RegistryKey, string[]> ret = new Dictionary<RegistryKey,string[]>();
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
        #endregion
    }
}

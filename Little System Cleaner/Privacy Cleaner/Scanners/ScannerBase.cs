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
using System.Diagnostics;

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

        /// <summary>
        /// Ensures that plugin is valid before being added
        /// </summary>
        /// <param name="xmlFilePath">Path to plugin XML file</param>
        /// <param name="name">Outputs name of plugin</param>
        /// <param name="description">Outputs description of plugin</param>
        /// <returns>True if plugin is valid</returns>
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

                            if (!string.IsNullOrWhiteSpace(regKeyPath))
                                bRet = Utils.RegKeyExists(regKeyPath);
                            else
                                bRet = false;

                            if (!bRet)
                                return bRet;
                        }
                        else if (xmlReader.Name == "ValueExist")
                        {
                            string regKeyPath = xmlReader.GetAttribute("RegKey");
                            string valueNameRegEx = xmlReader.GetAttribute("ValueName");

                            if (string.IsNullOrWhiteSpace(regKeyPath) || string.IsNullOrWhiteSpace(valueNameRegEx))
                                bRet = false;
                            else
                            {
                                using (RegistryKey rk = Utils.RegOpenKey(regKeyPath))
                                {
                                    if (rk == null)
                                        continue;

                                    string[] valueNames = null;

                                    try
                                    {
                                        valueNames = rk.GetValueNames();
                                    }
                                    catch (System.Security.SecurityException ex)
                                    {
                                        Debug.WriteLine("The following exception occurred: " + ex.Message + "\nUnable to get registry key (" + regKeyPath + ") value names.");
                                        bRet = false;
                                    }
                                    catch (UnauthorizedAccessException ex)
                                    {
                                        Debug.WriteLine("The following exception occurred: " + ex.Message + "\nUnable to get registry key (" + regKeyPath + ") value names.");
                                        bRet = false;
                                    }

                                    if (valueNames != null)
                                    {
                                        foreach (string valueName in valueNames)
                                        {
                                            if (!string.IsNullOrWhiteSpace(valueName))
                                            {
                                                if (Regex.IsMatch(valueName, valueNameRegEx))
                                                {
                                                    bRet = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (!bRet)
                                return bRet;
                        }
                        // See if file exists
                        else if (xmlReader.Name == "FileExist")
                        {
                            string filePath = xmlReader.ReadElementContentAsString();

                            if (string.IsNullOrWhiteSpace(filePath))
                                bRet = false;
                            else
                            {
                                filePath = Utils.ExpandVars(filePath);
                                bRet = File.Exists(filePath);
                            }
                            
                            if (!bRet)
                                return bRet;
                        }
                        // See if folder exists
                        else if (xmlReader.Name == "FolderExist")
                        {
                            string folderPath = xmlReader.ReadElementContentAsString();

                            if (string.IsNullOrWhiteSpace(folderPath))
                                bRet = false;
                            else
                            {
                                folderPath = Utils.ExpandVars(folderPath);
                                bRet = Directory.Exists(folderPath);
                            }

                            if (!bRet)
                                return bRet;
                        }
                    }
                }

                // Ensure IsRunning commands are valid before being added
                while (xmlReader.ReadToFollowing("IsRunning"))
                {
                    if (string.IsNullOrWhiteSpace(xmlReader.ReadElementContentAsString()))
                    {
                        bRet = false;
                        break;
                    }
                }

                // Ensure Action commands are valid before being added
                if (xmlReader.ReadToFollowing("Action"))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType != XmlNodeType.Element)
                            continue;

                        if (xmlReader.Name == "DeleteKey")
                        {
                            string regPath = xmlReader.ReadElementContentAsString();

                            if (string.IsNullOrWhiteSpace(regPath))
                            {
                                bRet = false;
                                break;
                            }
                        }

                        if (xmlReader.Name == "DeleteValue")
                        {
                            string regPath = xmlReader.GetAttribute("RegKey");
                            string valueNameRegEx = xmlReader.GetAttribute("ValueName");

                            if (string.IsNullOrWhiteSpace(regPath) || string.IsNullOrWhiteSpace(valueNameRegEx))
                            {
                                bRet = false;
                                break;
                            }
                        }

                        if (xmlReader.Name == "DeleteFile")
                        {
                            string filePath = xmlReader.ReadElementContentAsString();

                            if (string.IsNullOrWhiteSpace(filePath))
                            {
                                bRet = false;
                                break;
                            }
                        }

                        if (xmlReader.Name == "DeleteFolder")
                        {
                            string folderPath = xmlReader.ReadElementContentAsString();

                            if (string.IsNullOrWhiteSpace(folderPath))
                            {
                                bRet = false;
                                break;
                            }
                        }

                        if (xmlReader.Name == "DeleteFileList")
                        {
                            string searchPath = xmlReader.GetAttribute("Path");
                            string searchText = xmlReader.GetAttribute("SearchText");
                            if (string.IsNullOrWhiteSpace(searchPath) || string.IsNullOrWhiteSpace(searchText))
                            {
                                bRet = false;
                                break;
                            }
                        }

                        if (xmlReader.Name == "DeleteFolderList")
                        {
                            string searchPath = xmlReader.GetAttribute("Path");
                            string searchText = xmlReader.GetAttribute("SearchText");

                            if (string.IsNullOrWhiteSpace(searchPath) || string.IsNullOrWhiteSpace(searchText))
                            {
                                bRet = false;
                                break;
                            }
                        }

                        if (xmlReader.Name == "FindRegKey")
                        {
                            string regKey = xmlReader.GetAttribute("RegKey");

                            if (string.IsNullOrWhiteSpace(regKey))
                            {
                                bRet = false;
                                break;
                            }

                            // Must have child nodes
                            using (XmlReader children = xmlReader.ReadSubtree()) {
                                if (children.IsEmptyElement)
                                {
                                    bRet = false;
                                    break;
                                }

                                bool hasChildren = false;
                                while (children.Read()) {
                                    if ((children.Name == "IfSubKey" || children.Name == "IfValueName") && !string.IsNullOrWhiteSpace(xmlReader.GetAttribute("SearchText")))
                                    {
                                        hasChildren = true;
                                        break;
                                    }
                                }

                                if (!hasChildren)
                                {
                                    bRet = false;
                                    break;
                                }
                            }
                        }

                        if (xmlReader.Name == "FindPath")
                        {
                            string searchPath = xmlReader.GetAttribute("Path");
                            string searchText = xmlReader.GetAttribute("SearchText");

                            if (string.IsNullOrWhiteSpace(searchPath) || string.IsNullOrWhiteSpace(searchText))
                            {
                                bRet = false;
                                break;
                            }

                            // Must have child nodes
                            using (XmlReader children = xmlReader.ReadSubtree())
                            {
                                if (children.IsEmptyElement)
                                {
                                    bRet = false;
                                    break;
                                }

                                bool hasChildren = false;
                                while (children.Read())
                                {
                                    if ((children.Name == "IfFile" || children.Name == "IfFile") && !string.IsNullOrWhiteSpace(xmlReader.GetAttribute("SearchText")))
                                    {
                                        hasChildren = true;
                                        break;
                                    }
                                }

                                if (!hasChildren)
                                {
                                    bRet = false;
                                    break;
                                }
                            }
                        }

                        if (xmlReader.Name == "RemoveINIValue")
                        {
                            string filePath = xmlReader.GetAttribute("Path");
                            string sectionRegEx = xmlReader.GetAttribute("Section");
                            string valueRegEx = xmlReader.GetAttribute("Name");

                            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(sectionRegEx) || string.IsNullOrWhiteSpace(valueRegEx))
                            {
                                bRet = false;
                                break;
                            }
                        }

                        if (xmlReader.Name == "RemoveINISection")
                        {
                            string filePath = xmlReader.GetAttribute("Path");
                            string sectionRegEx = xmlReader.GetAttribute("Section");

                            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(sectionRegEx))
                            {
                                bRet = false;
                                break;
                            }
                        }

                        if (xmlReader.Name == "RemoveXML")
                        {
                            string filePath = xmlReader.GetAttribute("Path");
                            string xPath = xmlReader.GetAttribute("XPath");

                            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(xPath))
                            {
                                bRet = false;
                                break;
                            }
                        }
                    }
                }
            }

            return bRet;
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
            PluginFunctions pluginFunctions = new PluginFunctions();

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

                            pluginFunctions.DeleteKey(regKey, recurse);
                        }

                        if (xmlReader.Name == "DeleteValue")
                        {
                            string regPath = xmlReader.GetAttribute("RegKey");
                            string valueNameRegEx = xmlReader.GetAttribute("ValueName");

                            RegistryKey regKey = Utils.RegOpenKey(regPath);

                            pluginFunctions.DeleteValue(regKey, valueNameRegEx);
                        }

                        if (xmlReader.Name == "DeleteFile")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.ReadElementContentAsString());

                            pluginFunctions.DeleteFile(filePath);
                        }

                        if (xmlReader.Name == "DeleteFolder")
                        {
                            string folderPath = Utils.ExpandVars(xmlReader.ReadElementContentAsString());
                            bool recurse = ((xmlReader.GetAttribute("Recursive") == "Y") ? (true) : (false));

                            pluginFunctions.DeleteFolder(folderPath, recurse);
                        }

                        if (xmlReader.Name == "DeleteFileList")
                        {
                            string searchPath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string searchText = xmlReader.GetAttribute("SearchText");
                            SearchOption includeSubFolders = ((xmlReader.GetAttribute("IncludeSubFolders") == "Y") ? (SearchOption.AllDirectories) : (SearchOption.TopDirectoryOnly));

                            pluginFunctions.DeleteFileList(searchPath, searchText, includeSubFolders);
                        }

                        if (xmlReader.Name == "DeleteFolderList")
                        {
                            string searchPath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string searchText = xmlReader.GetAttribute("SearchText");
                            SearchOption includeSubFolders = ((xmlReader.GetAttribute("IncludeSubFolders") == "Y") ? (SearchOption.AllDirectories) : (SearchOption.TopDirectoryOnly));

                            pluginFunctions.DeleteFolderList(searchPath, searchText, includeSubFolders);
                        }

                        if (xmlReader.Name == "FindRegKey")
                        {
                            string regKey = xmlReader.GetAttribute("RegKey");
                            bool includeSubKeys = ((xmlReader.GetAttribute("IncludeSubKeys") == "Y") ? (true):(false));

                            RegistryKey rk = Utils.RegOpenKey(regKey);
                            XmlReader xmlChildren = xmlReader.ReadSubtree();

                            pluginFunctions.DeleteFoundRegKeys(rk, includeSubKeys, xmlChildren);
                        }

                        if (xmlReader.Name == "FindPath")
                        {
                            string searchPath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string searchText = xmlReader.GetAttribute("SearchText");
                            SearchOption includeSubFolders = ((xmlReader.GetAttribute("IncludeSubFolders") == "Y") ? (SearchOption.AllDirectories) : (SearchOption.TopDirectoryOnly));

                            XmlReader xmlChildren = xmlReader.ReadSubtree();

                            pluginFunctions.DeleteFoundPaths(searchPath, searchText, includeSubFolders, xmlChildren);
                        }

                        if (xmlReader.Name == "RemoveINIValue")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string sectionRegEx = xmlReader.GetAttribute("Section");
                            string valueRegEx = xmlReader.GetAttribute("Name");

                            pluginFunctions.DeleteINIValue(filePath, sectionRegEx, valueRegEx);
                        }

                        if (xmlReader.Name == "RemoveINISection")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string sectionRegEx = xmlReader.GetAttribute("Section");

                            pluginFunctions.DeleteINISection(filePath, sectionRegEx);
                        }

                        if (xmlReader.Name == "RemoveXML")
                        {
                            string filePath = Utils.ExpandVars(xmlReader.GetAttribute("Path"));
                            string xPath = xmlReader.GetAttribute("XPath");

                            pluginFunctions.DeleteXml(filePath, xPath);
                        }
                    }
                }
            }

            if (pluginFunctions.RegistrySubKeys.Count > 0)
                Wizard.StoreBadRegKeySubKeys(name, pluginFunctions.RegistrySubKeys);

            if (pluginFunctions.RegistryValueNames.Count > 0)
                Wizard.StoreBadRegKeyValueNames(name, pluginFunctions.RegistryValueNames);

            if (pluginFunctions.FilePaths.Count > 0)
                Wizard.StoreBadFileList(name, pluginFunctions.FilePaths.ToArray());

            if (pluginFunctions.Folders.Count > 0)
                Wizard.StoreBadFolderList(name, pluginFunctions.Folders);

            if (pluginFunctions.INIList.Count > 0)
                Wizard.StoreINIKeys(name, pluginFunctions.INIList.ToArray());

            if (pluginFunctions.XmlPaths.Count > 0)
                Wizard.StoreXML(name, pluginFunctions.XmlPaths);

            return;
        }

        
        #endregion
    }
}

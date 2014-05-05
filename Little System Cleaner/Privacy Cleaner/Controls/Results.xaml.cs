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

using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Results : UserControl
    {
        Wizard scanBase;

        public Results(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            this._tree.Model = ResultModel.CreateResultModel();
            this._tree.ExpandAll();
        }

        private void ShowDetails()
        {
            if (this._tree.SelectedNode == null)
                return;

            ResultNode resultNode = this._tree.SelectedNode.Tag as ResultNode;

            if (resultNode is RootNode)
                return;

            if (resultNode is ResultDelegate)
                return;

            this.scanBase.ShowDetails(resultNode);
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this._tree.SelectedNode == null)
            {
                MessageBox.Show(App.Current.MainWindow, "Nothing is selected", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                this.ShowDetails();
            }
        }

        private void buttonClean_Click(object sender, RoutedEventArgs e)
        {
            long seqNum = 0;

            if (MessageBox.Show(App.Current.MainWindow, "Are you sure?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Report report = Report.CreateReport(Properties.Settings.Default.privacyCleanerLog);

#if (!DEBUG)
            // Create system restore point
            if (Properties.Settings.Default.bAutoSysRestore)
                SysRestore.StartRestore("Before Little Privacy Cleaner Fix", out seqNum);
#endif

            foreach (ResultNode parent in (this._tree.Model as ResultModel).Root.Children)
            {
                foreach (ResultNode n in parent.Children)
                {
                    if (n.IsChecked.GetValueOrDefault() != true)
                        continue;

                    report.WriteLine(string.Format("Section: {0}", parent.Section));

                    if (n is ResultFiles)
                    {
                        foreach (string filePath in n.FilePaths)
                        {
                            try
                            {
                                if (File.Exists(filePath))
                                {
                                    Utils.DeleteFile(filePath);
                                    report.WriteLine(string.Format("Deleted File: {0}", filePath));
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                System.Diagnostics.Debug.WriteLine("error accessing file");
                            }
                        }
                    }
                    else if (n is ResultFolders)
                    {
                        foreach (KeyValuePair<string, bool> kvp in n.FolderPaths)
                        {
                            try
                            {
                                string folderPath = kvp.Key;
                                bool recurse = kvp.Value;

                                if (Directory.Exists(folderPath))
                                {
                                    Utils.DeleteDir(folderPath, recurse);
                                    report.WriteLine(string.Format("Deleted Folder: {0}", folderPath));
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                System.Diagnostics.Debug.WriteLine("error accessing file");
                            }
                        }
                    }
                    else if (n is ResultDelegate)
                    {
                        if (n.CleanDelegate != null)
                        {
                            n.CleanDelegate();
                            report.WriteLine(n.Description);
                        }
                    }
                    else if (n is ResultRegKeys)
                    {
                        foreach (KeyValuePair<RegistryKey, bool> kvp in n.RegKeySubKeys)
                        {
                            RegistryKey regKey = kvp.Key;
                            bool recurse = kvp.Value;

                            RegistryKey reg;
                            string rootKey, subkey;

                            if (!Utils.ParseRegKeyPath(regKey.Name, out rootKey, out subkey))
                                continue;

                            try
                            {
                                if (rootKey.ToUpper().CompareTo("HKEY_CLASSES_ROOT") == 0)
                                {
                                    reg = Registry.ClassesRoot;
                                }
                                else if (rootKey.ToUpper().CompareTo("HKEY_CURRENT_USER") == 0)
                                {
                                    reg = Registry.CurrentUser;
                                }
                                else if (rootKey.ToUpper().CompareTo("HKEY_LOCAL_MACHINE") == 0)
                                {
                                    reg = Registry.LocalMachine;
                                }
                                else if (rootKey.ToUpper().CompareTo("HKEY_USERS") == 0)
                                {
                                    reg = Registry.Users;
                                }
                                else if (rootKey.ToUpper().CompareTo("HKEY_CURRENT_CONFIG") == 0)
                                {
                                    reg = Registry.CurrentConfig;
                                }
                                else
                                    continue; // break here
                            }
                            catch (Exception)
                            {
                                continue;
                            }

                            if (reg != null)
                            {
                                if (recurse)
                                    reg.DeleteSubKeyTree(subkey);
                                else
                                    reg.DeleteSubKey(subkey);

                                reg.Flush();
                                reg.Close();

                                report.WriteLine(string.Format("Removed Registry Key: {0}", regKey.Name));
                            }
                        }

                        foreach (KeyValuePair<RegistryKey, string[]> kvp in n.RegKeyValueNames)
                        {
                            RegistryKey regKey = kvp.Key;
                            string[] valueNames = kvp.Value;

                            if (regKey == null)
                            {
                                // Registry key is closed
#if (DEBUG)
                                throw new ObjectDisposedException("regKey", "Registry Key is closed");
#else
                                continue;
#endif
                            }

                            if (valueNames == null || valueNames.Length == 0)
                                continue;

                            foreach (string valueName in valueNames)
                            {
                                if (regKey.GetValue(valueName) != null)
                                {
                                    regKey.DeleteValue(valueName);
                                    report.WriteLine(string.Format("Removed Registry Key: {0} Value Name: {0}", regKey.Name, valueName));
                                }
                            }
                        }
                    }
                    else if (n is ResultINI)
                    {
                        foreach (INIInfo iniInfo in n.iniInfoList)
                        {
                            string filePath = iniInfo.filePath;
                            string section = iniInfo.sectionName;
                            string valueName = iniInfo.valueName;

                            // Delete section if value name is empty
                            if (string.IsNullOrEmpty(valueName))
                            {
                                PInvoke.WritePrivateProfileString(section, null, null, filePath);
                                report.WriteLine(string.Format("Erased INI File: {0} Section: {1}", filePath, section));
                            }
                            else
                            {
                                PInvoke.WritePrivateProfileString(section, valueName, null, filePath);
                                report.WriteLine(string.Format("Erased INI File: {0} Section: {1} Value Name: {2}", filePath, section, valueName));
                            }
                        }
                    }
                    else if (n is ResultXML)
                    {
                        foreach (KeyValuePair<string, string> kvp in n.XMLPaths)
                        {
                            string filePath = kvp.Key;
                            string xPath = kvp.Value;

                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.Load(filePath);
                            foreach (XmlNode xmlNode in xmlDoc.SelectNodes(xPath))
                            {
                                XmlNode parentNode = xmlNode.ParentNode;
                                if (parentNode != null)
                                    parentNode.RemoveChild(xmlNode);
                                else
                                    xmlDoc.RemoveChild(xmlNode);
                            }
                            xmlDoc.Save(filePath);
                            report.WriteLine("Removed XML File: {0} Matching XPath: {0}", filePath, xPath);
                        }
                    }
                }
            }

            report.WriteLine("Successfully Cleaned Disk @ " + DateTime.Now.ToLongTimeString());
            report.DisplayLogFile(Properties.Settings.Default.privacyCleanerDisplayLog);

#if (!DEBUG)
            // End restore point
            SysRestore.EndRestore(seqNum);
#endif

            MessageBox.Show(App.Current.MainWindow, "Successfully Cleaned Disk", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(App.Current.MainWindow, "Would you like to cancel?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.scanBase.MoveFirst();
            }
        }
    }
}

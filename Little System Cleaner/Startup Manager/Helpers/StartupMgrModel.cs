using CommonTools.TreeListView.Tree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Startup_Manager.Helpers
{
    public class StartupMgrModel : ITreeModel
    {
        public StartupEntry Root { get; private set; }

        public StartupMgrModel()
        {
            Root = new StartupEntry();
        }

        public static StartupMgrModel CreateStarupMgrModel()
        {
            StartupMgrModel treeModel = new StartupMgrModel();

            // Adds registry keys to model

            // all user keys
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)));

            if (Utils.Is64BitOS)
            {
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true)));
            }

            // current user keys
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
            Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)));

            if (Utils.Is64BitOS)
            {
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true)));
            }

            // Adds startup folders
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP));
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(Utils.CSIDL_COMMON_STARTUP));

            return treeModel;
        }

        /// <summary>
        /// Loads registry sub key into tree view
        /// </summary>
        /// <remarks>Do NOT close the registry key after use!</remarks>
        private static void LoadRegistryAutoRun(StartupMgrModel treeModel, RegistryKey regKey)
        {
            if (regKey == null)
                return;

            if (regKey.ValueCount <= 0)
                return;

            string[] strValueNames = null;
            StartupEntry nodeRoot = new StartupEntry() { SectionName = regKey.Name };

            if (regKey.Name.Contains(Registry.CurrentUser.ToString()))
                nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.current_user);
            else
                nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.all_users);

            try
            {
                strValueNames = regKey.GetValueNames();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to get value names for " + regKey.ToString());
                return;
            }

            foreach (string strItem in strValueNames)
            {
                try
                {
                    string strFilePath = regKey.GetValue(strItem) as string;

                    if (!string.IsNullOrEmpty(strFilePath))
                    {
                        // Get file arguments
                        string strFile = "", strArgs = "";

                        if (Utils.FileExists(strFilePath))
                            strFile = strFilePath;
                        else
                        {
                            if (!Utils.ExtractArguments(strFilePath, out strFile, out strArgs))
                                if (!Utils.ExtractArguments2(strFilePath, out strFile, out strArgs))
                                    // If command line cannot be extracted, set file path to command line
                                    strFile = strFilePath;
                        }

                        StartupEntry node = new StartupEntry() { Parent = nodeRoot, SectionName = strItem, Path = strFile, Args = strArgs, RegKey = regKey };

                        Icon ico = Utils.ExtractIcon(strFile);
                        if (ico != null)
                            node.bMapImg = Utils.CreateBitmapSourceFromBitmap(ico.ToBitmap().Clone() as Bitmap);
                        else
                            node.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.appinfo.ToBitmap());

                        nodeRoot.Children.Add(node);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping trying to get value for " + strItem + " in " + regKey.ToString() + "...");
                    continue;
                }
            }

            if (nodeRoot.Children.Count > 0)
                treeModel.Root.Children.Add(nodeRoot);
        }

        /// <summary>
        /// Loads startup folder into tree view
        /// </summary>
        private static void AddStartupFolder(StartupMgrModel treeModel, string strFolder)
        {
            if (string.IsNullOrEmpty(strFolder) || !Directory.Exists(strFolder))
                return;

            string[] strShortcuts = null;
            StartupEntry nodeRoot = new StartupEntry() { SectionName = strFolder };

            if (Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP) == strFolder)
                nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.current_user);
            else
                nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.all_users);

            try
            {
                strShortcuts = Directory.GetFiles(strFolder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to get files in " + strFolder);
                return;
            }

            foreach (string strShortcut in strShortcuts)
            {
                try
                {
                    string strShortcutName = Path.GetFileName(strShortcut);
                    string strFilePath, strFileArgs;

                    if (Path.GetExtension(strShortcut) == ".lnk")
                    {
                        if (!Utils.ResolveShortcut(strShortcut, out strFilePath, out strFileArgs))
                            continue;

                        StartupEntry node = new StartupEntry() { Parent = nodeRoot, SectionName = strShortcutName, Path = strFilePath, Args = strFileArgs };

                        Icon ico = Utils.ExtractIcon(strFilePath);
                        if (ico != null)
                            node.bMapImg = Utils.CreateBitmapSourceFromBitmap(ico.ToBitmap().Clone() as Bitmap);
                        else
                            node.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.appinfo.ToBitmap());

                        nodeRoot.Children.Add(node);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping trying to resolve shortcut for " + strShortcut);
                }
                    
            }

            if (nodeRoot.Children.Count > 0)
                treeModel.Root.Children.Add(nodeRoot);

        }

        public System.Collections.IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;
            return (parent as StartupEntry).Children;
        }

        public bool HasChildren(object parent)
        {
            return (parent as StartupEntry).Children.Count > 0;
        }
    }
}

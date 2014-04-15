using CommonTools.TreeListView.Tree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

            // Adds registry keys
            try
            {
                // all user keys
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true));
                LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true));

                if (Utils.Is64BitOS)
                {
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true));
                    LoadRegistryAutoRun(treeModel, Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true));
                }

                // current user keys
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true));
                LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true));

                if (Utils.Is64BitOS)
                {
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true));
                    LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true));
                }
            }
            catch (System.Security.SecurityException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            // Adds startup folders
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP));
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(Utils.CSIDL_COMMON_STARTUP));

            return treeModel;
        }

        /// <summary>
        /// Loads registry sub key into tree view
        /// </summary>
        private static void LoadRegistryAutoRun(StartupMgrModel treeModel, RegistryKey regKey)
        {
            if (regKey == null)
                return;

            if (regKey.ValueCount <= 0)
                return;

            StartupEntry nodeRoot = new StartupEntry() { SectionName = regKey.Name };

            if (regKey.Name.Contains(Registry.CurrentUser.ToString()))
                nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.User);
            else
                nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.Users);

            foreach (string strItem in regKey.GetValueNames())
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
                        node.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.app);

                    nodeRoot.Children.Add(node);
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
            try
            {
                if (string.IsNullOrEmpty(strFolder) || !Directory.Exists(strFolder))
                    return;

                StartupEntry nodeRoot = new StartupEntry() { SectionName = strFolder };

                if (Utils.GetSpecialFolderPath(Utils.CSIDL_STARTUP) == strFolder)
                    nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.User);
                else
                    nodeRoot.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.Users);

                foreach (string strShortcut in Directory.GetFiles(strFolder))
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
                            node.bMapImg = Utils.CreateBitmapSourceFromBitmap(Properties.Resources.app);

                        nodeRoot.Children.Add(node);
                    }
                }

                if (nodeRoot.Children.Count > 0)
                    treeModel.Root.Children.Add(nodeRoot);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

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

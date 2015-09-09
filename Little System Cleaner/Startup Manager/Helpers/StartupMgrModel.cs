using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Microsoft.Win32;

namespace Little_System_Cleaner.Startup_Manager.Helpers
{
    public class StartupMgrModel : ITreeModel
    {
        public StartupEntry Root { get; }

        public StartupMgrModel()
        {
            Root = new StartupEntry();
        }

        internal static StartupMgrModel CreateStarupMgrModel()
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

            if (Utils.Is64BitOs)
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

            if (Utils.Is64BitOs)
            {
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
                Utils.SafeOpenRegistryKey(() => LoadRegistryAutoRun(treeModel, Registry.CurrentUser.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true)));
            }

            // Adds startup folders
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(PInvoke.CSIDL_STARTUP));
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(PInvoke.CSIDL_COMMON_STARTUP));

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

            string[] strValueNames;
            StartupEntry nodeRoot = new StartupEntry
            {
                SectionName = regKey.Name,
                bMapImg =
                    Utils.CreateBitmapSourceFromBitmap(regKey.Name.Contains(Registry.CurrentUser.ToString())
                        ? Resources.current_user
                        : Resources.all_users)
            };


            try
            {
                strValueNames = regKey.GetValueNames();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to get value names for " + regKey);
                return;
            }

            foreach (string strItem in strValueNames)
            {
                try
                {
                    string strFilePath = regKey.GetValue(strItem) as string;

                    if (string.IsNullOrEmpty(strFilePath))
                        continue;

                    // Get file arguments
                    string strFile, strArgs = "";

                    if (Utils.FileExists(strFilePath))
                        strFile = strFilePath;
                    else
                    {
                        if (!Utils.ExtractArguments(strFilePath, out strFile, out strArgs))
                            if (!Utils.ExtractArguments2(strFilePath, out strFile, out strArgs))
                                // If command line cannot be extracted, set file path to command line
                                strFile = strFilePath;
                    }

                    StartupEntry node = new StartupEntry { Parent = nodeRoot, SectionName = strItem, Path = strFile, Args = strArgs, RegKey = regKey };

                    Icon ico = Utils.ExtractIcon(strFile);
                    node.bMapImg = ico != null ? Utils.CreateBitmapSourceFromBitmap(ico.ToBitmap().Clone() as Bitmap) : Utils.CreateBitmapSourceFromBitmap(Resources.appinfo.ToBitmap());

                    nodeRoot.Children.Add(node);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping trying to get value for " + strItem + " in " + regKey + "...");
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
            Debug.WriteLine(strFolder);
            if (string.IsNullOrEmpty(strFolder) || !Directory.Exists(strFolder))
                return;

            string[] strShortcuts;
            StartupEntry nodeRoot = new StartupEntry
            {
                SectionName = strFolder,
                bMapImg =
                    Utils.CreateBitmapSourceFromBitmap(Utils.GetSpecialFolderPath(PInvoke.CSIDL_STARTUP) == strFolder
                        ? Resources.current_user
                        : Resources.all_users)
            };


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

                    if (Path.GetExtension(strShortcut) != ".lnk")
                        continue;

                    string filePath, fileArgs;

                    if (!Utils.ResolveShortcut(strShortcut, out filePath, out fileArgs))
                        continue;

                    StartupEntry node = new StartupEntry { Parent = nodeRoot, SectionName = strShortcutName, Path = filePath, Args = fileArgs };

                    Icon ico = Utils.ExtractIcon(filePath);
                    node.bMapImg = ico != null ? Utils.CreateBitmapSourceFromBitmap(ico.ToBitmap().Clone() as Bitmap) : Utils.CreateBitmapSourceFromBitmap(Resources.appinfo.ToBitmap());

                    nodeRoot.Children.Add(node);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message + "\nSkipping trying to resolve shortcut for " + strShortcut);
                }
                    
            }

            if (nodeRoot.Children.Count > 0)
                treeModel.Root.Children.Add(nodeRoot);

        }

        public IEnumerable GetChildren(object parent)
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

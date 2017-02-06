using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using CommonTools.TreeListView.Tree;
using Microsoft.Win32;
using Shared;
using Startup_Manager.Properties;

namespace Startup_Manager.Helpers
{
    public class StartupMgrModel : ITreeModel
    {
        public StartupMgrModel()
        {
            Root = new StartupEntry();
        }

        public StartupEntry Root { get; }

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;
            return (parent as StartupEntry)?.Children;
        }

        public bool HasChildren(object parent)
        {
            var startupEntry = parent as StartupEntry;
            return startupEntry != null && startupEntry.Children.Count > 0;
        }

        internal static StartupMgrModel CreateStarupMgrModel()
        {
            var treeModel = new StartupMgrModel();

            // Adds registry keys to model

            // all user keys
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.LocalMachine.OpenSubKey(
                            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.LocalMachine.OpenSubKey(
                            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices",
                            true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.LocalMachine.OpenSubKey(
                            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)));

            if (Utils.Is64BitOs)
            {
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.LocalMachine.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run",
                                true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.LocalMachine.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.LocalMachine.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.LocalMachine.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.LocalMachine.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.LocalMachine.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true)));
            }

            // current user keys
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.CurrentUser.OpenSubKey(
                            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.CurrentUser.OpenSubKey(
                            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServices",
                            true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup",
                            true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
            Utils.SafeOpenRegistryKey(
                () =>
                    LoadRegistryAutoRun(treeModel,
                        Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)));

            if (Utils.Is64BitOs)
            {
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.CurrentUser.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run",
                                true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.CurrentUser.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce", true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.CurrentUser.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunServices", true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.CurrentUser.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce\\Setup", true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.CurrentUser.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true)));
                Utils.SafeOpenRegistryKey(
                    () =>
                        LoadRegistryAutoRun(treeModel,
                            Registry.CurrentUser.OpenSubKey(
                                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", true)));
            }

            // Adds startup folders
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(PInvoke.CsidlStartup));
            AddStartupFolder(treeModel, Utils.GetSpecialFolderPath(PInvoke.CsidlCommonStartup));

            return treeModel;
        }

        /// <summary>
        ///     Loads registry sub key into tree view
        /// </summary>
        /// <remarks>Do NOT close the registry key after use!</remarks>
        private static void LoadRegistryAutoRun(StartupMgrModel treeModel, RegistryKey regKey)
        {
            if (regKey == null)
                return;

            if (regKey.ValueCount <= 0)
                return;

            string[] valueNames;
            var bitmap = regKey.Name.Contains(Registry.CurrentUser.ToString())
                ? Resources.current_user
                : Resources.all_users;

            var nodeRoot = new StartupEntry
            {
                SectionName = regKey.Name,
                BitmapImg = bitmap.CreateBitmapSourceFromBitmap()
            };

            try
            {
                valueNames = regKey.GetValueNames();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to get value names for " +
                                regKey);
                return;
            }

            foreach (var item in valueNames)
            {
                try
                {
                    var filePath = regKey.GetValue(item) as string;

                    if (string.IsNullOrEmpty(filePath))
                        continue;

                    // Get file arguments
                    string file, args = "";

                    if (Utils.FileExists(filePath))
                        file = filePath;
                    else
                    {
                        if (!Utils.ExtractArguments(filePath, out file, out args))
                            if (!Utils.ExtractArguments2(filePath, out file, out args))
                                // If command line cannot be extracted, set file path to command line
                                file = filePath;
                    }

                    var node = new StartupEntry
                    {
                        Parent = nodeRoot,
                        SectionName = item,
                        Path = file,
                        Args = args,
                        RegKey = regKey
                    };

                    var ico = Utils.ExtractIcon(file);
                    node.BitmapImg = ico != null
                        ? (ico.ToBitmap().Clone() as Bitmap).CreateBitmapSourceFromBitmap()
                        : Resources.appinfo.ToBitmap().CreateBitmapSourceFromBitmap();

                    nodeRoot.Children.Add(node);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message +
                                    "\nSkipping trying to get value for " + item + " in " + regKey + "...");
                }
            }

            if (nodeRoot.Children.Count > 0)
                treeModel.Root.Children.Add(nodeRoot);
        }

        /// <summary>
        ///     Loads startup folder into tree view
        /// </summary>
        private static void AddStartupFolder(StartupMgrModel treeModel, string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return;

            string[] shortcutList;
            var bitmap = Utils.GetSpecialFolderPath(PInvoke.CsidlStartup) == folder
                ? Resources.current_user
                : Resources.all_users;
            var nodeRoot = new StartupEntry
            {
                SectionName = folder,
                BitmapImg = bitmap.CreateBitmapSourceFromBitmap()
            };

            try
            {
                shortcutList = Directory.GetFiles(folder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to get files in " + folder);
                return;
            }

            foreach (var shortcut in shortcutList)
            {
                try
                {
                    var shortcutName = Path.GetFileName(shortcut);

                    if (Path.GetExtension(shortcut) != ".lnk")
                        continue;

                    string filePath, fileArgs;

                    if (!Utils.ResolveShortcut(shortcut, out filePath, out fileArgs))
                        continue;

                    var node = new StartupEntry
                    {
                        Parent = nodeRoot,
                        SectionName = shortcutName,
                        Path = filePath,
                        Args = fileArgs
                    };

                    var ico = Utils.ExtractIcon(filePath);
                    node.BitmapImg = ico != null
                        ? (ico.ToBitmap().Clone() as Bitmap).CreateBitmapSourceFromBitmap()
                        : Resources.appinfo.ToBitmap().CreateBitmapSourceFromBitmap();

                    nodeRoot.Children.Add(node);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("The following error occurred: " + ex.Message +
                                    "\nSkipping trying to resolve shortcut for " + shortcut);
                }
            }

            if (nodeRoot.Children.Count > 0)
                treeModel.Root.Children.Add(nodeRoot);
        }
    }
}
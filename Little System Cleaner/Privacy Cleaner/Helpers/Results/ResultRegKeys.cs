using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultRegKeys : ResultNode
    {
        /// <summary>
        ///     Constructor for registry key node
        /// </summary>
        /// <param name="desc">Description of node</param>
        /// <param name="regKeys">Registry key with a list of value names</param>
        public ResultRegKeys(string desc, Dictionary<RegistryKey, string[]> regKeys)
        {
            Description = desc;
            RegKeyValueNames = regKeys;
        }

        /// <summary>
        ///     Constructor for registry key node
        /// </summary>
        /// <param name="desc">Description of node</param>
        /// <param name="regKeys">Registry sub keys with a bool to specify if the tree should be removed</param>
        public ResultRegKeys(string desc, Dictionary<RegistryKey, bool> regKeys)
        {
            Description = desc;
            RegKeySubKeys = regKeys;
        }

        public override void Clean(Report report)
        {
            if (RegKeySubKeys != null)
            {
                foreach (var kvp in RegKeySubKeys)
                {
                    var regKey = kvp.Key;
                    var recurse = kvp.Value;

                    RegistryKey reg;
                    string rootKey, subkey;

                    if (regKey == null)
                    {
                        // Registry key is closed
#if (DEBUG)
                        throw new ObjectDisposedException("regKey", "Registry Key is closed");
#else
                        continue;
#endif
                    }

                    if (!Utils.ParseRegKeyPath(regKey.Name, out rootKey, out subkey))
                        continue;

                    try
                    {
                        switch (rootKey.ToUpper())
                        {
                            case "HKEY_CLASSES_ROOT":
                                reg = Registry.ClassesRoot;
                                break;

                            case "HKEY_CURRENT_USER":
                                reg = Registry.CurrentUser;
                                break;

                            case "HKEY_LOCAL_MACHINE":
                                reg = Registry.LocalMachine;
                                break;

                            case "HKEY_USERS":
                                reg = Registry.Users;
                                break;

                            case "HKEY_CURRENT_CONFIG":
                                reg = Registry.CurrentConfig;
                                break;

                            default:
                                continue; // break here
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (reg == null)
                        continue;

                    try
                    {
                        if (recurse)
                            reg.DeleteSubKeyTree(subkey);
                        else
                            reg.DeleteSubKey(subkey);

                        report.WriteLine("Removed Registry Key: {0}", regKey.Name);
                        Settings.Default.lastScanErrorsFixed++;
                    }
                    catch (Exception ex)
                    {
                        string message =
                            $"The following registry key could not be removed: {rootKey + "\\" + subkey}\nError: {ex.Message}";
                        Debug.WriteLine(message);
                    }
                    finally
                    {
                        reg.Flush();
                        reg.Close();
                        //regKey.Close();
                    }
                }
            }

            if (RegKeyValueNames == null)
                return;

            foreach (var kvp in RegKeyValueNames)
            {
                var regKey = kvp.Key;
                var valueNames = kvp.Value;

                if (regKey == null)
                {
                    // Registry key is closed
#if (DEBUG)
                    throw new ObjectDisposedException("regKey", "Registry Key is closed");
#else
                    continue;
#endif
                }

                if ((valueNames == null) || valueNames.Length == 0)
                    continue;

                foreach (var valueName in valueNames)
                {
                    try
                    {
                        if (regKey.GetValue(valueName) == null)
                            continue;

                        regKey.DeleteValue(valueName);

                        report.WriteLine("Removed Registry Key: {0} Value Name: {1}", regKey.Name, valueName);
                        Settings.Default.lastScanErrorsFixed++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "The following exception occurred: {0}\nUnable to remove value name ({1}) from registry key ({2})",
                            ex.Message, regKey.Name, valueName);
                    }
                }

                regKey.Close();
            }
        }
    }
}
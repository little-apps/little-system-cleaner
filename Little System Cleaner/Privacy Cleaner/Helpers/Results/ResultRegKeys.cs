using Little_System_Cleaner.Misc;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultRegKeys : ResultNode
    {
        /// <summary>
        /// Constructor for registry key node
        /// </summary>
        /// <param name="desc">Description of node</param>
        /// <param name="regKeys">Registry key with a list of value names</param>
        public ResultRegKeys(string desc, Dictionary<RegistryKey, string[]> regKeys)
        {
            this.Description = desc;
            this.RegKeyValueNames = regKeys;
        }

        /// <summary>
        /// Constructor for registry key node
        /// </summary>
        /// <param name="desc">Description of node</param>
        /// <param name="regKeys">Registry sub keys with a bool to specify if the tree should be removed</param>
        public ResultRegKeys(string desc, Dictionary<RegistryKey, bool> regKeys)
        {
            this.Description = desc;
            this.RegKeySubKeys = regKeys;
        }

        public override void Clean(Report report)
        {
            if (this.RegKeySubKeys != null)
            {
                foreach (KeyValuePair<RegistryKey, bool> kvp in this.RegKeySubKeys)
                {
                    RegistryKey regKey = kvp.Key;
                    bool recurse = kvp.Value;

                    RegistryKey reg;
                    string rootKey, subkey;

                    if (regKey == null)
                    {
                        // Registry key is closed
#if (DEBUG)
                        throw new ObjectDisposedException("regKey", "Registry Key is closed");
#endif
                        continue;
                    }

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
                        try
                        {
                            if (recurse)
                                reg.DeleteSubKeyTree(subkey);
                            else
                                reg.DeleteSubKey(subkey);

                            report.WriteLine(string.Format("Removed Registry Key: {0}", regKey.Name));
                            Properties.Settings.Default.lastScanErrorsFixed++;
                        }
                        catch (Exception ex)
                        {
                            string message = string.Format("The following registry key could not be removed: {0}\nError: {1}", rootKey + "\\" + subkey, ex.Message);
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
            }

            if (this.RegKeyValueNames != null)
            {
                foreach (KeyValuePair<RegistryKey, string[]> kvp in this.RegKeyValueNames)
                {
                    RegistryKey regKey = kvp.Key;
                    string[] valueNames = kvp.Value;

                    if (regKey == null)
                    {
                        // Registry key is closed
#if (DEBUG)
                        throw new ObjectDisposedException("regKey", "Registry Key is closed");
#endif
                        continue;
                    }

                    if ((valueNames == null) || valueNames.Length == 0)
                        continue;

                    foreach (string valueName in valueNames)
                    {
                        try
                        {
                            if (regKey.GetValue(valueName) != null)
                            {
                                regKey.DeleteValue(valueName);

                                report.WriteLine(string.Format("Removed Registry Key: {0} Value Name: {0}", regKey.Name, valueName));
                                Properties.Settings.Default.lastScanErrorsFixed++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("The following exception occurred: {0}\nUnable to remove value name ({1}) from registry key ({2})", ex.Message, regKey.Name, valueName);
                        }
                    }

                    regKey.Close();
                }
            }
        }
    }
}

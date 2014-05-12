using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
            foreach (KeyValuePair<RegistryKey, bool> kvp in this.RegKeySubKeys)
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

            foreach (KeyValuePair<RegistryKey, string[]> kvp in this.RegKeyValueNames)
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
    }
}

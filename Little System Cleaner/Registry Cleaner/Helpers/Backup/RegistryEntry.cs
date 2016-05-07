using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers.Backup
{
    public class RegistryEntry : IEquatable<RegistryEntry>
    {
        [NonSerialized]
        private RegistryKey _regKey;

        private string _rootKey;

        public RegistryEntry()
        {
        }

        public RegistryEntry(string regPath)
        {
            Values = new List<RegistryValue>();

            var slashIndex = regPath.IndexOf('\\');

            if (slashIndex >= 0)
            {
                _rootKey = regPath.Substring(0, regPath.IndexOf('\\'));
                SubKey = regPath.Substring(regPath.IndexOf('\\') + 1).Trim().Trim('\\');
            }
            else
            {
                _rootKey = regPath;
            }
        }

        /// <summary>
        ///     Gets the root key (HKEY_LOCAL_MACHINE, HKEY_CURRENT_USER, etc)
        /// </summary>
        [XmlAttribute("RootKey")]
        public string RootKey
        {
            get { return _rootKey.ToUpper(); }
            set { _rootKey = value; }
        }

        /// <summary>
        ///     Gets the subkey
        /// </summary>
        /// <remarks>This can be null/empty if no subkey exists</remarks>
        [XmlAttribute("SubKey")]
        public string SubKey { get; set; }

        /// <summary>
        ///     Gets a list contains values of subkey
        /// </summary>
        [XmlArray("Values"), XmlArrayItem("Value")]
        public List<RegistryValue> Values { get; set; }

        /// <summary>
        ///     Gets the path for the registry key
        /// </summary>
        [XmlIgnore]
        public string RegistryKeyPath
        {
            get
            {
                if (string.IsNullOrEmpty(SubKey))
                    return RootKey;
                return RootKey + "\\" + SubKey;
            }
        }

        /// <summary>
        ///     Gets RegistryKey for registry key path
        /// </summary>
        /// <remarks>This will ONLY open a registry key. To create it, use CreateSubKey()</remarks>
        [XmlIgnore]
        public RegistryKey RegistryKey
        {
            get
            {
                if (string.IsNullOrEmpty(RootKey))
                    return null;

                if (_regKey == null)
                {
                    try
                    {
                        if (string.Compare(RootKey, "HKEY_CLASSES_ROOT", StringComparison.Ordinal) == 0)
                            _regKey = Registry.ClassesRoot;
                        else if (string.Compare(RootKey, "HKEY_CURRENT_USER", StringComparison.Ordinal) == 0)
                            _regKey = Registry.CurrentUser;
                        else if (string.Compare(RootKey, "HKEY_LOCAL_MACHINE", StringComparison.Ordinal) == 0)
                            _regKey = Registry.LocalMachine;
                        else if (string.Compare(RootKey, "HKEY_USERS", StringComparison.Ordinal) == 0)
                            _regKey = Registry.Users;
                        else if (string.Compare(RootKey, "HKEY_CURRENT_CONFIG", StringComparison.Ordinal) == 0)
                            _regKey = Registry.CurrentConfig;

                        if (!string.IsNullOrEmpty(SubKey))
                            _regKey = _regKey?.OpenSubKey(SubKey);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to open registry key using registry path ({0}).\nError: {1}",
                            RegistryKeyPath, ex.Message);
                        _regKey = null;
                    }
                }

                return _regKey;
            }
        }

        /// <summary>
        ///     Adds subkey to registry key
        /// </summary>
        /// <returns>Registry key, or null if unable to be created</returns>
        public RegistryKey CreateSubKey()
        {
            RegistryKey regKey = null;

            if (string.Compare(RootKey, "HKEY_CLASSES_ROOT", StringComparison.Ordinal) == 0)
                regKey = Registry.ClassesRoot;
            else if (string.Compare(RootKey, "HKEY_CURRENT_USER", StringComparison.Ordinal) == 0)
                regKey = Registry.CurrentUser;
            else if (string.Compare(RootKey, "HKEY_LOCAL_MACHINE", StringComparison.Ordinal) == 0)
                regKey = Registry.LocalMachine;
            else if (string.Compare(RootKey, "HKEY_USERS", StringComparison.Ordinal) == 0)
                regKey = Registry.Users;
            else if (string.Compare(RootKey, "HKEY_CURRENT_CONFIG", StringComparison.Ordinal) == 0)
                regKey = Registry.CurrentConfig;

            if (string.IsNullOrEmpty(SubKey))
                return regKey;

            if (SubKey.IndexOf('\\') > 0)
            {
                foreach (var subKey in SubKey.Split('\\'))
                {
                    try
                    {
                        regKey = regKey?.CreateSubKey(subKey);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Unable to create sub key ({0}) for registry path ({1}).\nError: {2}", subKey,
                            RegistryKeyPath, ex.Message);
                        regKey = null;
                    }

                    if (regKey == null)
                        break;
                }
            }
            else
            {
                try
                {
                    regKey = regKey?.CreateSubKey(SubKey);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to create sub key ({0}) for registry path ({1}).\nError: {2}", SubKey,
                        RegistryKeyPath, ex.Message);
                    regKey = null;
                }
            }

            return regKey;
        }

        /// <summary>
        ///     Gets all values from registry key
        /// </summary>
        /// <returns>True if values added</returns>
        public bool AddValues()
        {
            if (RegistryKey == null)
            {
                string message = $"Failed to open registry key ({RegistryKeyPath}) to get all values";
                Debug.WriteLine(message);
                return false;
            }

            if (RegistryKey.ValueCount <= 0)
                return true;

            string[] valueNames;

            try
            {
                valueNames = RegistryKey.GetValueNames();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to get value names from registry key ({0}).\nError: {1}", RegistryKeyPath,
                    ex.Message);
                return false;
            }

            // Add values
            foreach (var valueName in valueNames)
            {
                AddValue(valueName);
            }

            return true;
        }

        /// <summary>
        ///     Adds value from registry key
        /// </summary>
        /// <param name="valueName">Value name</param>
        /// <returns>True if value added</returns>
        public bool AddValue(string valueName)
        {
            RegistryValueKind regValueKind;
            object value;

            if (RegistryKey == null)
            {
                Debug.WriteLine("Failed to open registry key ({0}) to get a value ({1})", RegistryKeyPath, valueName);
                return false;
            }

            try
            {
                regValueKind = RegistryKey.GetValueKind(valueName);
                value = RegistryKey.GetValue(valueName);

                if (value == null)
                    throw new Exception("Value doesn't seem to exist or there is insufficient permissions to access it.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to get value ({0} from registry key ({1}).\nError: {2}", valueName,
                    RegistryKeyPath, ex.Message);
                return false;
            }

            var regValue = new RegistryValue(valueName, regValueKind, value);

            if (!Values.Contains(regValue))
                Values.Add(regValue);

            return true;
        }

        #region IEquatable Methods

        public bool Equals(RegistryEntry regEntry)
        {
            return regEntry.RegistryKeyPath == RegistryKeyPath;
        }

        public override bool Equals(object obj)
        {
            var a = obj as RegistryEntry;
            return a != null && Equals(a);
        }

        public override int GetHashCode()
        {
            return RegistryKeyPath.GetHashCode();
        }

        public static bool operator ==(RegistryEntry regEntry1, RegistryEntry regEntry2)
        {
            if ((object)regEntry1 == null || (object)regEntry2 == null)
                return Equals(regEntry1, regEntry2);

            return regEntry1.Equals(regEntry2);
        }

        public static bool operator !=(RegistryEntry regEntry1, RegistryEntry regEntry2)
        {
            if (regEntry1 == null || regEntry2 == null)
                return !Equals(regEntry1, regEntry2);

            return !regEntry1.Equals(regEntry2);
        }

        #endregion IEquatable Methods
    }
}
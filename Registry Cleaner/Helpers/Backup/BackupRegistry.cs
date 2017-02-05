﻿using Registry_Cleaner.Helpers.BadRegistryKeys;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using Shared;

namespace Registry_Cleaner.Helpers.Backup
{
    public class BackupRegistry : IDisposable
    {
        private bool _disposed;

        public BackupRegistry(string file)
        {
            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException(nameof(file));

            FilePath = file;
            RegistryEntries = new RegistryEntries();
        }

        public string FilePath { get; }

        public Stream Stream { get; private set; } = Stream.Null;

        public RegistryEntries RegistryEntries { get; private set; }

        public DateTime Created => RegistryEntries?.CreatedDateTime ?? DateTime.MinValue;

        public bool Open(bool openExisting)
        {
            try
            {
                if (openExisting)
                {
                    // Open for reading
                    Stream = File.OpenRead(FilePath);
                }
                else
                {
                    Stream = File.OpenWrite(FilePath);

                    if (Stream.Length > 0)
                    {
                        var fileStream = (FileStream)Stream;
                        fileStream?.SetLength(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to open file ({0}).\nError: {1}", FilePath, ex.Message);

                Stream = Stream.Null;
                return false;
            }

            return true;
        }

        public void Serialize()
        {
            if (Stream == Stream.Null)
            {
                Console.WriteLine("Unable to serialize file as the file stream isn't open.");
                return;
            }

            var serializer = new XmlSerializer(RegistryEntries.GetType());
            serializer.Serialize(Stream, RegistryEntries);
        }

        public bool Deserialize(out string errorMsg)
        {
            if (Stream == Stream.Null)
            {
                errorMsg = "Unable to deserialize from file as the file stream isn't open.";
                return false;
            }

            if (Stream.Length == 0)
            {
                errorMsg = "File seems to be blank.";
                return false;
            }

            if (Stream.Position > 0)
                Stream.Position = 0;

            var serializer = new XmlSerializer(RegistryEntries.GetType());

            try
            {
                using (var reader = new XmlTextReader(Stream))
                {
                    if (serializer.CanDeserialize(reader))
                    {
                        RegistryEntries = (RegistryEntries)serializer.Deserialize(reader);
                    }
                    else
                    {
                        throw new Exception("Backup file is in the wrong format.");
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = $"The following error occurred trying to restore the registry: {ex.Message}";

                return false;
            }

            errorMsg = "";
            return true;
        }

        public bool Restore()
        {
            // Just in case
            if (RegistryEntries.Count == 0)
                return false;

            foreach (var regEntry in RegistryEntries.RegEntries)
            {
                var regKey = regEntry.CreateSubKey();

                if (regKey == null)
                {
                    string message = $"Unable to create registry sub key ({regEntry.RegistryKeyPath})";

                    MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Debug.WriteLine(message);

                    continue;
                }

                if (regEntry.Values.Count <= 0)
                    continue;

                foreach (var regValue in regEntry.Values)
                {
                    var valueName = regValue.Name;
                    var value = regValue.Value;
                    var type = regValue.Type;

                    try
                    {
                        regKey.SetValue(valueName, value, type);
                    }
                    catch (Exception ex)
                    {
                        string message =
                            $"Unable to restore value name ({valueName}) for registry key ({regKey}).\nError: {ex.Message}";

                        MessageBox.Show(Application.Current.MainWindow, message, Utils.ProductName, MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Debug.WriteLine(message);
                    }
                }
            }

            return true;
        }

        public bool Store(BadRegistryKey brk)
        {
            var regEntry = new RegistryEntry(brk.RegKeyPath);

            if (!string.IsNullOrEmpty(brk.ValueName))
            {
                if (RegistryEntries.Contains(regEntry))
                {
                    regEntry = RegistryEntries[RegistryEntries.IndexOf(regEntry)];

                    return regEntry.AddValue(brk.ValueName);
                }
                var ret = regEntry.AddValue(brk.ValueName);

                RegistryEntries.Add(regEntry);
                return ret;
            }
            // Recurse subkeys
            RecurseSubKeys(brk.RegKeyPath);

            return true;
        }

        private void RecurseSubKeys(string regPath)
        {
            var regEntry = new RegistryEntry(regPath);

            if (regEntry.RegistryKey == null)
                return;

            var alreadyAdded = RegistryEntries.Contains(regEntry);

            if (alreadyAdded)
                regEntry = RegistryEntries[RegistryEntries.IndexOf(regEntry)];

            // Add all values
            regEntry.AddValues();

            if (!alreadyAdded)
                RegistryEntries.Add(regEntry);

            foreach (var subKeyPath in regEntry.RegistryKey.GetSubKeyNames().Select(subKey => regPath + "\\" + subKey))
            {
                RecurseSubKeys(subKeyPath);
            }
        }

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources.
                if (Stream != Stream.Null)
                {
                    Stream.Close();
                    Stream = Stream.Null;
                }

                if (RegistryEntries.Count > 0)
                    RegistryEntries.Clear();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Members
    }
}
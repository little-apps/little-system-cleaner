using Little_System_Cleaner.Registry_Cleaner.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Little_System_Cleaner.Properties
{
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings
    {
        [UserScopedSetting]
        [DebuggerNonUserCode]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public ObservableCollection<ExcludeItem> ArrayExcludeList
        {
            get
            {
                if ((ObservableCollection<ExcludeItem>)this["ArrayExcludeList"] == null)
                    this["ArrayExcludeList"] = new ObservableCollection<ExcludeItem>();

                return (ObservableCollection<ExcludeItem>)this["ArrayExcludeList"];
            }
            set { this["ArrayExcludeList"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public string ProgramSettingsDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["ProgramSettingsDir"] as string))
                    this["ProgramSettingsDir"] =
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles)}\\Little System Cleaner";

                if (!Directory.Exists((string)this["ProgramSettingsDir"]))
                    Directory.CreateDirectory((string)this["ProgramSettingsDir"]);

                return (string)this["ProgramSettingsDir"];
            }
            set { this["ProgramSettingsDir"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public string OptionsBackupDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["OptionsBackupDir"] as string))
                    this["OptionsBackupDir"] = $"{ProgramSettingsDir}\\Backups";

                if (!Directory.Exists((string)this["OptionsBackupDir"]))
                    Directory.CreateDirectory((string)this["OptionsBackupDir"]);

                return (string)this["OptionsBackupDir"];
            }
            set { this["OptionsBackupDir"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public string OptionsLogDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["OptionsLogDir"] as string))
                    this["OptionsLogDir"] = $"{ProgramSettingsDir}\\Logs";

                if (!Directory.Exists((string)this["OptionsLogDir"]))
                    Directory.CreateDirectory((string)this["OptionsLogDir"]);

                return (string)this["OptionsLogDir"];
            }
            set { this["OptionsLogDir"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public string BuildTime
            =>
                new DateTime(2000, 1, 1).AddDays(Assembly.GetExecutingAssembly().GetName().Version.Build)
                    .ToString("MM/dd/yyyy");
    }
}
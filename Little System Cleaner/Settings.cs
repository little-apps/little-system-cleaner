using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Little_System_Cleaner.Registry_Cleaner.Helpers;

namespace Little_System_Cleaner.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {
        [UserScopedSetting]
        [DebuggerNonUserCode]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        public ObservableCollection<ExcludeItem> ArrayExcludeList
        {
            get
            {
                if (((ObservableCollection<ExcludeItem>)(this["arrayExcludeList"])) == null)
                    ((this["arrayExcludeList"])) = new ObservableCollection<ExcludeItem>();

                return ((ObservableCollection<ExcludeItem>)(this["arrayExcludeList"]));
            }
            set
            {
                this["arrayExcludeList"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public string ProgramSettingsDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["strProgramSettingsDir"] as string))
                    this["strProgramSettingsDir"] = $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles)}\\Little System Cleaner";

                if (!Directory.Exists((string) this["strProgramSettingsDir"]))
                    Directory.CreateDirectory((string) this["strProgramSettingsDir"]);

                return this["strProgramSettingsDir"] as string;
            }
            set { this["strProgramSettingsDir"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public string OptionsBackupDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["optionsBackupDir"] as string))
                    this["optionsBackupDir"] = $"{ProgramSettingsDir}\\Backups";

                if (!Directory.Exists((string) this["optionsBackupDir"]))
                    Directory.CreateDirectory((string) this["optionsBackupDir"]);

                return (string) this["optionsBackupDir"]; ;
            }
            set { this["optionsBackupDir"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public string OptionsLogDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["optionsLogDir"] as string))
                    this["optionsLogDir"] = $"{ProgramSettingsDir}\\Logs";

                if (!Directory.Exists((string) this["optionsLogDir"]))
                    Directory.CreateDirectory((string) this["optionsLogDir"]);

                return this["optionsLogDir"] as string;
            }
            set { this["optionsLogDir"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public string BuildTime => new DateTime(2000, 1, 1).AddDays(Assembly.GetExecutingAssembly().GetName().Version.Build).ToString("MM/dd/yyyy");
    }
}

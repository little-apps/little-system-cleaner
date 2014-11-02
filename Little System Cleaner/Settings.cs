using Little_System_Cleaner.Registry_Cleaner.Helpers;
using System.Collections.ObjectModel;
namespace Little_System_Cleaner.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {
        
        public Settings() {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }
        
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // Add code to handle the SettingChangingEvent event here.
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Add code to handle the SettingsSaving event here.
        }

        [global::System.Configuration.UserScopedSettingAttribute]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        [global::System.Configuration.SettingsSerializeAs(System.Configuration.SettingsSerializeAs.Binary)]
        public ObservableCollection<ExcludeItem> arrayExcludeList
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

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public string strProgramSettingsDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["strProgramSettingsDir"] as string))
                    this["strProgramSettingsDir"] = string.Format("{0}\\Little System Cleaner", global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.CommonProgramFiles));

                if (!global::System.IO.Directory.Exists(this["strProgramSettingsDir"] as string))
                    global::System.IO.Directory.CreateDirectory(this["strProgramSettingsDir"] as string);

                return this["strProgramSettingsDir"] as string;
            }
            set { this["strProgramSettingsDir"] = value; }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public string optionsBackupDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["optionsBackupDir"] as string))
                    this["optionsBackupDir"] = string.Format("{0}\\Backups", strProgramSettingsDir);

                if (!global::System.IO.Directory.Exists(this["optionsBackupDir"] as string))
                    global::System.IO.Directory.CreateDirectory(this["optionsBackupDir"] as string);

                return this["optionsBackupDir"] as string; ;
            }
            set { this["optionsBackupDir"] = value; }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public string optionsLogDir
        {
            get
            {
                if (string.IsNullOrEmpty(this["optionsLogDir"] as string))
                    this["optionsLogDir"] = string.Format("{0}\\Logs", strProgramSettingsDir);

                if (!global::System.IO.Directory.Exists(this["optionsLogDir"] as string))
                    global::System.IO.Directory.CreateDirectory(this["optionsLogDir"] as string);

                return this["optionsLogDir"] as string;
            }
            set { this["optionsLogDir"] = value; }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public string strBuildTime
        {
            get
            {
                return new global::System.DateTime(2000, 1, 1).AddDays(global::System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build).ToString("MM/dd/yyyy");
            }
        }

    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Startup_Manager.Helpers
{
    public class StartupEntry : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly ObservableCollection<StartupEntry> _children = new ObservableCollection<StartupEntry>();
        public ObservableCollection<StartupEntry> Children
        {
            get { return _children; }
        }

        public RegistryKey RegKey { get; set; }

        public StartupEntry Parent { get; set; }

        public bool IsLeaf
        {
            get { return (Children.Count == 0); }
        }

        public string SectionName { get; set; }
        public string Path { get; set; }
        public string Args { get; set; }

        public System.Windows.Controls.Image bMapImg { get; set; }

        public StartupEntry()
        {
        }
    }
}

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
        private string _cmd = null;

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

        public string Command
        {
            get
            {
                if (_cmd == null)
                {
                    if (!this.IsLeaf)
                    {
                        this._cmd = string.Empty;
                        return this._cmd;
                    }

                    if (string.IsNullOrWhiteSpace(this.Path) && string.IsNullOrWhiteSpace(this.Args))
                    {
                        this._cmd = string.Empty;
                        return this._cmd;
                    }

                    string cmd = this.Path.Trim();
                    string args = this.Args.Trim();

                    if (!string.IsNullOrEmpty(args))
                        cmd = cmd + " " + args;

                    this._cmd = cmd;
                }

                return this._cmd;
            }
        }

        public System.Windows.Controls.Image bMapImg { get; set; }

        public StartupEntry()
        {
        }
    }
}

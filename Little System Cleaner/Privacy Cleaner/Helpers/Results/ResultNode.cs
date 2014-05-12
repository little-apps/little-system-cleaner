using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public abstract class ResultNode : INotifyPropertyChanged, ICloneable
    {
        #region INotifyPropertyChanged & ICloneable Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion

        #region Properties
        private readonly ObservableCollection<ResultNode> _children = new ObservableCollection<ResultNode>();
        public ObservableCollection<ResultNode> Children
        {
            get { return _children; }
        }

        private bool? _bIsChecked = true;

        #region IsChecked Methods
        public void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _bIsChecked)
                return;

            _bIsChecked = value;

            if (updateChildren && _bIsChecked.HasValue)
                this.Children.ToList().ForEach(c => c.SetIsChecked(_bIsChecked, true, false));

            if (updateParent && Parent != null)
                Parent.VerifyCheckState();

            this.OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool? current = this.Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            this.SetIsChecked(state, false, true);
        }
        #endregion

        public bool? IsChecked
        {
            get { return _bIsChecked; }
            set { this.SetIsChecked(value, true, true); }
        }

        public ResultNode Parent { get; set; }

        /// <summary>
        /// Gets/Sets the section name
        /// </summary>
        public string Section
        {
            get;
            set;
        }
        /// <summary>
        /// Gets/Sets a list of procs to check before cleaning
        /// </summary>
        public string[] ProcNames
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the file path string array
        /// </summary>
        public string[] FilePaths
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets information for INI
        /// </summary>
        public INIInfo[] iniInfoList
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets information for XML
        /// </summary>
        public Dictionary<string, string> XMLPaths
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the folder path string array
        /// </summary>
        public Dictionary<string, bool> FolderPaths
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the file size as a string (ex: 10 MB)
        /// </summary>
        public string Size
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the delegate
        /// </summary>
        public CleanDelegate CleanDelegate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the description of the delegate
        /// </summary>
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the registry key and value names
        /// </summary>
        public Dictionary<RegistryKey, string[]> RegKeyValueNames
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the registry key and whether to recurse through them
        /// </summary>
        public Dictionary<RegistryKey, bool> RegKeySubKeys
        {
            get;
            set;
        }

        public string Header
        {
            get
            {
                if (!string.IsNullOrEmpty(this.Section))
                    return this.Section;
                else if (!string.IsNullOrEmpty(this.Description))
                    return this.Description;
                else
                    return string.Empty;
            }
        }
        #endregion

        public abstract void Clean(Report report);

        public override string ToString()
        {
            if (Parent != null)
                return string.Copy(Description);
            else if (!string.IsNullOrEmpty(Section))
                return string.Copy(Section);
            else
                return string.Empty;
        }
    }
}

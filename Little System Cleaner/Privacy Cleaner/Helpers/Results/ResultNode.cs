using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Little_System_Cleaner.Misc;
using Microsoft.Win32;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public abstract class ResultNode : INotifyPropertyChanged, ICloneable
    {
        public abstract void Clean(Report report);

        public override string ToString()
        {
            if (Parent != null)
                return string.Copy(Description);

            return !string.IsNullOrEmpty(Section) ? string.Copy(Section) : string.Empty;
        }

        #region INotifyPropertyChanged & ICloneable Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        #region Properties

        public ObservableCollection<ResultNode> Children { get; } = new ObservableCollection<ResultNode>();

        private bool? _bIsChecked = true;

        #region IsChecked Methods

        public void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _bIsChecked)
                return;

            _bIsChecked = value;

            if (updateChildren && _bIsChecked.HasValue)
                Children.ToList().ForEach(c => c.SetIsChecked(_bIsChecked, true, false));

            if (updateParent)
                Parent?.VerifyCheckState();

            OnPropertyChanged("IsChecked");
        }

        private void VerifyCheckState()
        {
            bool? state = null;
            for (var i = 0; i < Children.Count; ++i)
            {
                var current = Children[i].IsChecked;
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
            SetIsChecked(state, false, true);
        }

        #endregion

        public bool? IsChecked
        {
            get { return _bIsChecked; }
            set { SetIsChecked(value, true, true); }
        }

        public ResultNode Parent { get; set; }

        /// <summary>
        ///     Gets/Sets the section name
        /// </summary>
        public string Section { get; set; }

        /// <summary>
        ///     Gets/Sets a list of procs to check before cleaning
        /// </summary>
        public string[] ProcNames { get; set; }

        /// <summary>
        ///     Gets/Sets the file path string array
        /// </summary>
        public string[] FilePaths { get; set; }

        /// <summary>
        ///     Gets/Sets information for INI
        /// </summary>
        public IniInfo[] IniInfoList { get; set; }

        /// <summary>
        ///     Gets/Sets information for XML
        /// </summary>
        public Dictionary<string, List<string>> XmlPaths { get; set; }

        /// <summary>
        ///     Gets/Sets the folder path string array
        /// </summary>
        public Dictionary<string, bool> FolderPaths { get; set; }

        /// <summary>
        ///     Gets/Sets the file size as a string (ex: 10 MB)
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        ///     Gets/Sets the delegate
        /// </summary>
        public CleanDelegate CleanDelegate { get; set; }

        /// <summary>
        ///     Gets/Sets the description of the delegate
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Gets/Sets the registry key and value names
        /// </summary>
        public Dictionary<RegistryKey, string[]> RegKeyValueNames { get; set; }

        /// <summary>
        ///     Gets/Sets the registry key and whether to recurse through them
        /// </summary>
        public Dictionary<RegistryKey, bool> RegKeySubKeys { get; set; }

        public string Header
        {
            get
            {
                if (!string.IsNullOrEmpty(Section))
                    return Section;
                if (!string.IsNullOrEmpty(Description))
                    return Description;
                return string.Empty;
            }
        }

        #endregion
    }
}
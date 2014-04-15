/*
    Little System Cleaner
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers
{
    public delegate void CleanDelegate();

    #region INI Info Struct
    public struct INIInfo
    {
        /// <summary>
        /// Path of the INI File
        /// </summary>
        public string filePath;
        /// <summary>
        /// Section Name
        /// </summary>
        public string sectionName;
        /// <summary>
        /// Value Name (optional)
        /// </summary>
        public string valueName;
    }

    public struct XMLInfo
    {
        public string filePath;
        public string searchElement;
        public string searchElementText;
        public string searchAttribute;
        public string searchAttributeText;
    }
    #endregion

    public class ResultNode : INotifyPropertyChanged, ICloneable
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

        public override string ToString()
        {
            if (Parent != null)
                return string.Copy(Description);
            else
                return string.Copy(Section);
        }
    }

    public class RootNode : ResultNode
    {
        /// <summary>
        /// Constructor for root node
        /// </summary>
        /// <param name="sectionName">Section Name</param>
        public RootNode(string sectionName)
        {
            this.Section = sectionName;
        }
    }

    public class ResultFolders : ResultNode
    {
        /// <summary>
        /// Constructor for folder path string array
        /// </summary>
        /// <param name="desc">Description of registry keys</param>
        /// <param name="folderPaths">Folder path list</param>
        public ResultFolders(string desc, Dictionary<string, bool> folderPaths)
        {
            this.Description = desc;
            this.FolderPaths = folderPaths;
        }
    }

    public class ResultFiles : ResultNode
    {
        /// <summary>
        /// Constructor for bad file path (leaf node)
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="fileSize">The size of the file (in bytes)</param>
        public ResultFiles(string desc, string[] filePaths, long fileSize)
        {
            this.Description = desc;
            this.FilePaths = filePaths;
            this.Size = Utils.ConvertSizeToString(fileSize);
        }
    }

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
    }

    public class ResultDelegate : ResultNode
    {
        /// <summary>
        /// Constructor for cleaning delegate
        /// </summary>
        /// <param name="cleanDelegate">Delegate pointer</param>
        /// <param name="desc">Description of delegate</param>
        /// <param name="size">Size of file or files in bytes (optional)</param>
        public ResultDelegate(CleanDelegate cleanDelegate, string desc, long size)
        {
            this.CleanDelegate = cleanDelegate;
            this.Description = desc;
            if (size > 0)
                this.Size = Utils.ConvertSizeToString(size);
        }
    }

    public class ResultINI : ResultNode
    {
        /// <summary>
        /// Constructor for bad INI file
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="iniInfo">INI Info Array</param>
        public ResultINI(string desc, INIInfo[] iniInfo)
        {
            this.Description = desc;
            this.iniInfoList = iniInfo;
        }
    }

    public class ResultXML : ResultNode
    {
        /// <summary>
        /// Constructor for XML file
        /// </summary>
        /// <param name="desc">Description</param>
        /// <param name="xmlInfo">XML Info Array</param>
        public ResultXML(string desc, Dictionary<string, string> xmlPaths)
        {
            this.Description = desc;
            this.XMLPaths = xmlPaths;
        }
    }

    #region Result Array
    public class ResultArray : ObservableCollection<ResultNode>
    {
        public ResultArray()
            : base()
        {

        }

        public ResultNode this[int index]
        {
            get { return (ResultNode)base[index]; }
            set { base[index] = value; }
        }

        public void Add(ResultNode resultNode)
        {
            if (resultNode == null)
                throw new ArgumentNullException("resultNode");

            base.Add(resultNode);

            return;
        }

        public int IndexOf(ResultNode resultNode)
        {
            return (base.IndexOf(resultNode));
        }

        public void Insert(int index, ResultNode resultNode)
        {
            if (resultNode == null)
                throw new ArgumentNullException("resultNode");

            base.Insert(index, resultNode);
        }

        public void Remove(ResultNode resultNode)
        {
            if (resultNode == null)
                throw new ArgumentNullException("resultNode");

            base.Remove(resultNode);
        }

        public bool Contains(ResultNode resultNode)
        {
            return (base.Contains(resultNode));
        }

        public int Problems(string section)
        {
            foreach (ResultNode n in this) {
                if (n.Section == section)
                    return n.Children.Count;
            }

            return 0;
        }
    }
    #endregion

    #region Result Model
    public class ResultModel : ITreeModel
    {
        public ResultNode Root { get; private set; }

        public static ResultModel CreateResultModel()
        {
            ResultModel model = new ResultModel();

            foreach (ResultNode n in Wizard.ResultArray)
            {
                model.Root.Children.Add(n);
            }

            return model;
        }

        public ResultModel()
        {
            Root = new ResultNode();
        }

        public System.Collections.IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;

            return (parent as ResultNode).Children;
        }

        public bool HasChildren(object parent)
        {
            return (parent as ResultNode).Children.Count > 0;
        }
    }
    #endregion
}

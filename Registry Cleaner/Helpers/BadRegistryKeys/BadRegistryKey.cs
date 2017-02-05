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

using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Registry_Cleaner.Properties;
using Shared;
using Image = System.Windows.Controls.Image;

namespace Registry_Cleaner.Helpers.BadRegistryKeys
{
    public class BadRegistryKey : INotifyPropertyChanged, ICloneable
    {
        /// <summary>
        ///     Constructor for new bad registry key
        /// </summary>
        /// <param name="sectionName">Section name</param>
        /// <param name="problem">Reason registry key is invalid</param>
        /// <param name="baseKey">Registry hive</param>
        /// <param name="subKey">Path to registry key (excluding registry hive)</param>
        /// <param name="valueName">Value Name (can be null)</param>
        /// <param name="severity">The severity (between 1-5) of the problem</param>
        public BadRegistryKey(string sectionName, string problem, string baseKey, string subKey, string valueName,
            int severity)
        {
            SectionName = sectionName;
            Problem = problem;
            BaseRegKey = baseKey;
            SubRegKey = subKey;
            _nSeverity = severity;

            if (string.IsNullOrEmpty(valueName))
                return;

            ValueName = valueName;

            // Open registry key
            var regKey = Utils.RegOpenKey(baseKey, subKey);

            // Convert value to string
            if (regKey != null)
                Data = ScanFunctions.RegConvertXValueToString(regKey, valueName);
        }

        /// <summary>
        ///     Constructor for root node
        /// </summary>
        public BadRegistryKey(BitmapImage icon, string sectionName)
        {
            BitmapImg = new Image { Source = icon };

            Problem = sectionName;

            SectionName = "";
            ValueName = "";
            BaseRegKey = "";
            SubRegKey = "";
            _nSeverity = 0;
        }

        public void Init()
        {
            foreach (var childBadRegKey in Children)
            {
                childBadRegKey._parent = this;
                childBadRegKey.Init();
            }
        }

        public bool Delete()
        {
            var ret = false;
            RegistryKey regKey = null;

            try
            {
                if (!string.IsNullOrEmpty(ValueName))
                {
                    regKey = Utils.RegOpenKey(RegKeyPath, false, true);
                    var valueName = ValueName.ToUpper() == "(DEFAULT)" ? string.Empty : ValueName;

                    if (regKey != null)
                    {
                        regKey.DeleteValue(valueName, true);

                        ret = true;
                    }
                }
                else
                {
                    regKey = Utils.RegOpenKey(BaseRegKey, false, true);

                    if (regKey != null)
                    {
                        regKey.DeleteSubKeyTree(SubRegKey);
                        regKey.Flush();

                        ret = true;
                    }
                }
            }
            catch (Exception ex)
            {
                var message = !string.IsNullOrEmpty(ValueName)
                    ? $"Unable to delete value name ({ValueName}) from registry key ({RegKeyPath}).\nError: {ex.Message}"
                    : $"An error occurred deleting registry key ({RegKeyPath}).\nError: {ex.Message}";

                Debug.WriteLine(message);

                ret = false;
            }
            finally
            {
                regKey?.Close();
            }

            return ret;
        }

        public override string ToString()
        {
            return string.Copy(RegKeyPath);
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

        #endregion INotifyPropertyChanged & ICloneable Members

        #region Properties

        public ObservableCollection<BadRegistryKey> Children { get; } = new ObservableCollection<BadRegistryKey>();

        private BadRegistryKey _parent;
        private bool? _isChecked = true;
        private readonly int _nSeverity;
        public readonly string BaseRegKey;
        public readonly string SubRegKey;

        public Image BitmapImg { get; set; }

        /// <summary>
        ///     Gets/Sets whether the item is checked
        /// </summary>
        public bool? IsChecked
        {
            get { return _isChecked; }
            set { SetIsChecked(value, true, true); }
        }

        /// <summary>
        ///     Gets the section name
        /// </summary>
        public string SectionName { get; set; }

        /// <summary>
        ///     Get the problem
        /// </summary>
        public string Problem { get; }

        /// <summary>
        ///     Gets the value name
        /// </summary>
        public string ValueName { get; } = "";

        /// <summary>
        ///     Gets the data in the bad registry key
        /// </summary>
        public string Data { get; } = "";

        /// <summary>
        ///     Gets the registry path
        /// </summary>
        public string RegKeyPath
        {
            get
            {
                if (!string.IsNullOrEmpty(BaseRegKey) && !string.IsNullOrEmpty(SubRegKey))
                    return $"{BaseRegKey}\\{SubRegKey}";
                return !string.IsNullOrEmpty(BaseRegKey) ? BaseRegKey : "";
            }
        }

        /// <summary>
        ///     Gets the image showing the severity
        /// </summary>
        public Image SeverityImg
        {
            get
            {
                var img = new Image();
                Bitmap bmp;

                switch (_nSeverity)
                {
                    case 1:
                        bmp = Resources._1;
                        break;

                    case 2:
                        bmp = Resources._2;
                        break;

                    case 3:
                        bmp = Resources._3;
                        break;

                    case 4:
                        bmp = Resources._4;
                        break;

                    case 5:
                        bmp = Resources._5;
                        break;

                    default:
                        return img;
                }

                var hBitmap = bmp.GetHbitmap();

                img.Source = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                return img;
            }
        }

        public object Tag { get; set; }

        #region IsChecked Methods

        private void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _isChecked)
                return;

            _isChecked = value;

            if (updateChildren && _isChecked.HasValue)
                Children.ToList().ForEach(c => c.SetIsChecked(_isChecked, true, false));

            if (updateParent)
                _parent?.VerifyCheckState();

            OnPropertyChanged(nameof(IsChecked));
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

        #endregion IsChecked Methods

        #endregion Properties
    }
}
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

using System;
using System.Linq;
using System.Windows;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start
    {
        readonly Wizard _scanBase;

        public SectionModel Model { get; set; } = null;

        public Start(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;

            Tree.Model = SectionModel.CreateSectionModel();
            Tree.ExpandAll();

            RadioButtonPerm.IsChecked = Settings.Default.privacyCleanerDeletePerm;
            RadioButtonMove.IsChecked = Settings.Default.privacyCleanerDeleteRecBin;
            CheckBoxReadOnly.IsChecked = Settings.Default.privacyCleanerIncReadOnlyFile;
            CheckBoxHidden.IsChecked = Settings.Default.privacyCleanerIncHiddenFile;
            CheckBoxSystem.IsChecked = Settings.Default.privacyCleanerIncSysFile;
            CheckBoxZeroByte.IsChecked = Settings.Default.privacyCleanerInc0ByteFile;
            CheckBoxLogScan.IsChecked = Settings.Default.privacyCleanerLog;
            CheckBoxDisplayLog.IsChecked = Settings.Default.privacyCleanerDisplayLog;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Wizard.SqLiteLoaded = Utils.IsAssemblyLoaded("System.Data.SQLite", new Version(1, 0, 66), true);

            if (!Wizard.SqLiteLoaded)
                MessageBox.Show(Application.Current.MainWindow, "It appears that System.Data.SQLite.dll is not loaded, because of this, some privacy information will not be able to be cleaned.\n\nPlease ensure that the file is located in the same folder as Little System Cleaner and that the version is at least 1.0.66.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);   
        }

        private void UpdateSettings(object sender, RoutedEventArgs e)
        {
            UpdateSettings();
        }

        private void UpdateSettings()
        {
            Settings.Default.privacyCleanerDeletePerm = RadioButtonPerm.IsChecked.GetValueOrDefault();
            Settings.Default.privacyCleanerDeleteRecBin = RadioButtonMove.IsChecked.GetValueOrDefault();
            Settings.Default.privacyCleanerIncReadOnlyFile = CheckBoxReadOnly.IsChecked.GetValueOrDefault();
            Settings.Default.privacyCleanerIncHiddenFile = CheckBoxHidden.IsChecked.GetValueOrDefault();
            Settings.Default.privacyCleanerIncSysFile = CheckBoxSystem.IsChecked.GetValueOrDefault();
            Settings.Default.privacyCleanerInc0ByteFile = CheckBoxZeroByte.IsChecked.GetValueOrDefault();
            Settings.Default.privacyCleanerLog = CheckBoxLogScan.IsChecked.GetValueOrDefault();
            Settings.Default.privacyCleanerDisplayLog = CheckBoxDisplayLog.IsChecked.GetValueOrDefault();
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            _scanBase.Model = Tree.Model as SectionModel;

            if (!_scanBase.Model.RootChildren.Any(n => n.IsChecked == null || n.IsChecked == true))
            {
                MessageBox.Show(Application.Current.MainWindow, "At least one item must be selected in order for privacy issues to be found.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _scanBase.MoveNext();
        }

        
    }
}

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Little_System_Cleaner.Privacy_Cleaner.Helpers;
using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using Little_System_Cleaner.Misc;

namespace Little_System_Cleaner.Privacy_Cleaner.Controls
{
    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : UserControl
    {
        Wizard scanBase;
        SectionModel _model = null;

        public SectionModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
            }
        }

        public Start(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            this._tree.Model = SectionModel.CreateSectionModel();
            this._tree.ExpandAll();

            this.radioButtonPerm.IsChecked = Properties.Settings.Default.privacyCleanerDeletePerm;
            this.radioButtonMove.IsChecked = Properties.Settings.Default.privacyCleanerDeleteRecBin;
            this.checkBoxReadOnly.IsChecked = Properties.Settings.Default.privacyCleanerIncReadOnlyFile;
            this.checkBoxHidden.IsChecked = Properties.Settings.Default.privacyCleanerIncHiddenFile;
            this.checkBoxSystem.IsChecked = Properties.Settings.Default.privacyCleanerIncSysFile;
            this.checkBoxZeroByte.IsChecked = Properties.Settings.Default.privacyCleanerInc0ByteFile;
            this.checkBoxLogScan.IsChecked = Properties.Settings.Default.privacyCleanerLog;
            this.checkBoxDisplayLog.IsChecked = Properties.Settings.Default.privacyCleanerDisplayLog;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Wizard.SQLiteLoaded = Utils.IsAssemblyLoaded("System.Data.SQLite", new Version(1, 0, 66), true);

            if (!Wizard.SQLiteLoaded)
                MessageBox.Show(App.Current.MainWindow, "It appears that System.Data.SQLite.dll is not loaded, because of this, some privacy information will not be able to be cleaned.\n\nPlease ensure that the file is located in the same folder as Little System Cleaner and that the version is at least 1.0.66.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);   
        }

        private void UpdateSettings(object sender, RoutedEventArgs e)
        {
            this.UpdateSettings();
        }

        private void UpdateSettings()
        {
            Properties.Settings.Default.privacyCleanerDeletePerm = this.radioButtonPerm.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.privacyCleanerDeleteRecBin = this.radioButtonMove.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.privacyCleanerIncReadOnlyFile = this.checkBoxReadOnly.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.privacyCleanerIncHiddenFile = this.checkBoxHidden.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.privacyCleanerIncSysFile = this.checkBoxSystem.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.privacyCleanerInc0ByteFile = this.checkBoxZeroByte.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.privacyCleanerLog = this.checkBoxLogScan.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.privacyCleanerDisplayLog = this.checkBoxDisplayLog.IsChecked.GetValueOrDefault();
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            this.scanBase.Model = this._tree.Model as SectionModel;

            this.scanBase.MoveNext();
        }

        
    }
}

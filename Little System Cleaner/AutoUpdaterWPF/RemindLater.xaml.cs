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
using System.Windows.Shapes;

namespace Little_System_Cleaner.AutoUpdaterWPF
{
    /// <summary>
    /// Interaction logic for RemindLater.xaml
    /// </summary>
    internal partial class RemindLater : Window
    {
        public RemindLaterFormat RemindLaterFormat { get; private set; }

        public int RemindLaterAt { get; private set; }

        public RemindLater()
        {
            InitializeComponent();
        }

        private void RemindLaterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            comboBoxRemindLater.SelectedIndex = 0;
            radioButtonYes.IsChecked = true;
        }

        private void ButtonOkClick(object sender, EventArgs e)
        {
            if (radioButtonYes.IsChecked.GetValueOrDefault())
            {
                switch (comboBoxRemindLater.SelectedIndex)
                {
                    case 0:
                        RemindLaterFormat = RemindLaterFormat.Minutes;
                        RemindLaterAt = 30;
                        break;
                    case 1:
                        RemindLaterFormat = RemindLaterFormat.Hours;
                        RemindLaterAt = 12;
                        break;
                    case 2:
                        RemindLaterFormat = RemindLaterFormat.Days;
                        RemindLaterAt = 1;
                        break;
                    case 3:
                        RemindLaterFormat = RemindLaterFormat.Days;
                        RemindLaterAt = 2;
                        break;
                    case 4:
                        RemindLaterFormat = RemindLaterFormat.Days;
                        RemindLaterAt = 4;
                        break;
                    case 5:
                        RemindLaterFormat = RemindLaterFormat.Days;
                        RemindLaterAt = 8;
                        break;
                    case 6:
                        RemindLaterFormat = RemindLaterFormat.Days;
                        RemindLaterAt = 10;
                        break;
                }
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }
        }

        private void radioButtonYes_Checked(object sender, RoutedEventArgs e)
        {
            comboBoxRemindLater.IsEnabled = radioButtonYes.IsChecked.GetValueOrDefault();
        }

        
    }
}

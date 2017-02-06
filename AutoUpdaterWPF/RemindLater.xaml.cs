using System;
using System.Windows;

namespace AutoUpdaterWPF
{
    /// <summary>
    ///     Interaction logic for RemindLater.xaml
    /// </summary>
    internal partial class RemindLater
    {
        public RemindLater()
        {
            InitializeComponent();
        }

        public RemindLaterFormat RemindLaterFormat { get; private set; }

        public int RemindLaterAt { get; private set; }

        private void RemindLaterWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBoxRemindLater.SelectedIndex = 0;
            RadioButtonYes.IsChecked = true;
        }

        private void ButtonOkClick(object sender, EventArgs e)
        {
            if (RadioButtonYes.IsChecked.GetValueOrDefault())
            {
                switch (ComboBoxRemindLater.SelectedIndex)
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
            ComboBoxRemindLater.IsEnabled = RadioButtonYes.IsChecked.GetValueOrDefault();
        }
    }
}
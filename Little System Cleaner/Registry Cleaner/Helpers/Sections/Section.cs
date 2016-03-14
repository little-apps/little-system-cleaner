using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers.Sections
{
    public class Section : INotifyPropertyChanged
    {
        private bool? _isChecked = true;

        public ObservableCollection<Section> Children { get; } = new ObservableCollection<Section>();

        public bool? IsChecked
        {
            get { return _isChecked; }
            set { SetIsChecked(value, true, true); }
        }

        public Section Parent { get; set; }

        public string SectionName { get; set; }
        public string Description { get; set; }

        public BitmapImage BitmapImage { get; private set; }

        public Icon Icon
        {
            set
            {
                using (var ms = new MemoryStream())
                {
                    value.Save(ms);

                    BitmapImage = new BitmapImage();
                    BitmapImage.BeginInit();
                    BitmapImage.StreamSource = new MemoryStream(ms.ToArray());
                    BitmapImage.EndInit();
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        #region IsChecked Methods

        private void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _isChecked)
                return;

            _isChecked = value;

            if (updateChildren && _isChecked.HasValue)
                Children.ToList().ForEach(c => c.SetIsChecked(_isChecked, true, false));

            if (updateParent)
                Parent?.VerifyCheckState();

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

        #endregion
    }
}
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
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public ObservableCollection<Section> Children { get; } = new ObservableCollection<Section>();

        private bool? _bIsChecked = true;

        #region IsChecked Methods
        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
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

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < Children.Count; ++i)
            {
                bool? current = Children[i].IsChecked;
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

        public Section Parent { get; set; }

        public string SectionName { get; set; }
        public string Description { get; set; }

        public BitmapImage bMapImg { get; private set; }

        public Icon Icon
        {
            set
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    value.Save(ms);

                    bMapImg = new BitmapImage();
                    bMapImg.BeginInit();
                    bMapImg.StreamSource = new MemoryStream(ms.ToArray());
                    bMapImg.EndInit();
                }
            }
        }
    }
}

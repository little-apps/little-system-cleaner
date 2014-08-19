using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class Section : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        private readonly ObservableCollection<Section> _children = new ObservableCollection<Section>();
        public ObservableCollection<Section> Children
        {
            get { return _children; }
        }

        private bool? _bIsChecked = true;

        #region IsChecked Methods
        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
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
                    bMapImg.StreamSource = new System.IO.MemoryStream(ms.ToArray());
                    bMapImg.EndInit();
                }
            }
        }

        public Section()
        {
        }
    }
}

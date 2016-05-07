using System.ComponentModel;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class ScannerListViewItem : INotifyPropertyChanged
    {
        private string _animatedImage;
        private string _image;

        public ScannerListViewItem(string section)
        {
            Section = section;
            Status = "Queued";
            Errors = "0 Errors";
        }

        public string Section { get; }

        public string Status { get; set; }

        public string Errors { get; set; }

        public string Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

        public string AnimatedImage
        {
            get { return _animatedImage; }
            set
            {
                _animatedImage = value;
                OnPropertyChanged(nameof(AnimatedImage));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void LoadGif()
        {
            AnimatedImage = "/Little_System_Cleaner;component/Resources/ajax-loader.gif";
        }

        public void UnloadGif()
        {
            AnimatedImage = null;
            Image = @"/Little_System_Cleaner;component/Resources/registry cleaner/finished-scanning.png";
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
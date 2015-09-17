using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class lviScanner : INotifyPropertyChanged
    {
        private string _image;
        private string _animatedImage;

        public string Section { get; }

        public string Status { get; set; }

        public string Errors { get; set; }

        public string Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }

        public string AnimatedImage
        {
            get { return _animatedImage; }
            set
            {
                _animatedImage = value;
                OnPropertyChanged("AnimatedImage");
            }
        }

        public lviScanner(string section)
        {
            Section = section;
            Status = "Queued";
            Errors = "0 Errors";
        }

        public void LoadGif()
        {
            /*Image = new Image();

            BitmapSource gif = Imaging.CreateBitmapSourceFromHBitmap(Resources.ajax_loader.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            ImageBehavior.SetAnimatedSource(Image, gif);*/

            //Image = @"pack://application:,,,\Resources\ajax-loader.gif";
            AnimatedImage = "/Little_System_Cleaner;component/Resources/ajax-loader.gif";
        }

        public void UnloadGif()
        {
            /*Image = null;

            Image = new Image
            {
                Source =
                    Imaging.CreateBitmapSourceFromHBitmap(
                        Resources.finished_scanning.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions())
            };*/

            AnimatedImage = "";
            Image = @"/Little_System_Cleaner;component/Resources/registry cleaner/finished-scanning.png";
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

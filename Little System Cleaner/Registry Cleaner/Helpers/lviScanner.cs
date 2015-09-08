using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using CommonTools.WpfAnimatedGif;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class lviScanner
    {
        public string Section { get; }

        public string Status { get; set; }

        public string Errors { get; set; }

        public Image Image { get; private set; }
        public Uri bMapImg { get; private set; }

        public lviScanner(string section, Uri bMapImg)
        {
            this.bMapImg = bMapImg;

            Section = section;
            Status = "Queued";
            Errors = "0 Errors";
        }

        public void LoadGif()
        {
            Image = new Image();

            BitmapSource gif = Imaging.CreateBitmapSourceFromHBitmap(Resources.ajax_loader.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            ImageBehavior.SetAnimatedSource(Image, gif);
        }

        public void UnloadGif()
        {
            Image = null;

            Image = new Image
            {
                Source =
                    Imaging.CreateBitmapSourceFromHBitmap(
                        Resources.finished_scanning.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions())
            };
        }
    }
}

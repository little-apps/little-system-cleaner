using CommonTools.WpfAnimatedGif;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class lviScanner
    {
        public string Section { get; set; }
        public string Status { get; set; }
        public string Errors { get; set; }

        public System.Windows.Controls.Image Image { get; private set; }
        public Uri bMapImg { get; private set; }

        public lviScanner(string section)
        {
            Section = section;
            Status = "Queued";
            Errors = "0 Errors";
        }

        public void LoadGif()
        {
            this.Image = new System.Windows.Controls.Image();

            BitmapSource gif = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.ajax_loader.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            ImageBehavior.SetAnimatedSource(this.Image, gif);
        }

        public void UnloadGif()
        {
            this.Image = null;

            this.Image = new System.Windows.Controls.Image();
            this.Image.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.Repair.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }
    }
}

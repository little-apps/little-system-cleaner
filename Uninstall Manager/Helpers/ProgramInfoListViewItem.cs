using Microsoft.Win32;
using Shared;
using Shared.Uninstall_Manager;
using System.Windows.Controls;
using Uninstall_Manager.Properties;

namespace Uninstall_Manager.Helpers
{
    public class ProgramInfoListViewItem : ProgramInfo
    {
        public Image BitmapImg
            =>
                Uninstallable
                    ? Resources.uninstall.CreateBitmapSourceFromBitmap()
                    : Resources.cancel.CreateBitmapSourceFromBitmap();

        public string Program
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayName))
                    return DisplayName;

                return !string.IsNullOrEmpty(QuietDisplayName) ? QuietDisplayName : Key;
            }
        }

        public string Size => SizeBytes > 0 ? Utils.ConvertSizeToString(SizeBytes) : string.Empty;

        public long SizeBytes
        {
            get
            {
                if (InstallSize > 0)
                    return (uint)InstallSize;

                if (EstimatedSize.GetValueOrDefault(0) <= 0)
                    return 0;

                if (EstimatedSize != null)
                    return EstimatedSize.Value * 1024;

                return 0;
            }
        }

        public ProgramInfoListViewItem(RegistryKey regKey) : base(regKey)
        {
        }
    }
}
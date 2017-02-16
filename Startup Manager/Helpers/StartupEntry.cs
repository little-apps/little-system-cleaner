using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Startup_Manager.Helpers
{
    public class StartupEntry
    {
        private string _cmd;

        public ObservableCollection<StartupEntry> Children { get; } = new ObservableCollection<StartupEntry>();

        public RegistryKey RegKey { get; set; }

        public StartupEntry Parent { get; set; }

        public bool IsLeaf => Children.Count == 0;

        public string SectionName { get; set; }
        public string Path { get; set; }
        public string Args { get; set; }

        public string Command
        {
            get
            {
                if (_cmd != null)
                    return _cmd;

                if (!IsLeaf)
                {
                    _cmd = string.Empty;
                    return _cmd;
                }

                if (string.IsNullOrWhiteSpace(Path) && string.IsNullOrWhiteSpace(Args))
                {
                    _cmd = string.Empty;
                    return _cmd;
                }

                var cmd = Path.Trim();
                var args = Args.Trim();

                if (!string.IsNullOrEmpty(args))
                    cmd = cmd + " " + args;

                _cmd = cmd;

                return _cmd;
            }
        }

        public Image BitmapImg { get; set; }
    }
}
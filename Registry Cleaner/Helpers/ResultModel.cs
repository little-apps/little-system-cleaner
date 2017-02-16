using CommonTools.TreeListView.Tree;
using Registry_Cleaner.Controls;
using Registry_Cleaner.Helpers.BadRegistryKeys;
using Shared;
using System.Collections;
using System.Linq;

namespace Registry_Cleaner.Helpers
{
    public class ResultModel : ITreeModel
    {
        public ResultModel()
        {
            Root = new BadRegistryKey(null, "");
        }

        public BadRegistryKey Root { get; }

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;

            return (parent as BadRegistryKey)?.Children;
        }

        public bool HasChildren(object parent)
        {
            var badRegistryKey = parent as BadRegistryKey;
            return badRegistryKey != null && badRegistryKey.Children.Count > 0;
        }

        internal static ResultModel CreateResultModel()
        {
            var model = new ResultModel();

            foreach (var scanner in Scan.EnabledScanners)
            {
                var rootBadRegKey = new BadRegistryKey(scanner.BitmapImg, scanner.ScannerName);

                rootBadRegKey.Children.AddRange(
                    Wizard.BadRegKeyArray.Cast<BadRegistryKey>()
                        .Where(childBadRegKey => scanner.ScannerName == childBadRegKey.SectionName));

                rootBadRegKey.Init();

                if (rootBadRegKey.Children.Count > 0)
                    model.Root.Children.Add(rootBadRegKey);
            }

            return model;
        }
    }
}
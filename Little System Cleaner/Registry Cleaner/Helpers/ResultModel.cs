using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Registry_Cleaner.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class ResultModel : ITreeModel
    {
        public BadRegistryKey Root { get; private set; }

        internal static ResultModel CreateResultModel()
        {
            ResultModel model = new ResultModel();

            foreach (var scanner in Scan.EnabledScanners)
            {
                BadRegistryKey rootBadRegKey = new BadRegistryKey(scanner.bMapImg, scanner.ScannerName);

                foreach (BadRegistryKey childBadRegKey in Wizard.badRegKeyArray)
                {
                    if (scanner.ScannerName == childBadRegKey.SectionName)
                        rootBadRegKey.Children.Add(childBadRegKey);
                }

                rootBadRegKey.Init();

                if (rootBadRegKey.Children.Count > 0)
                    model.Root.Children.Add(rootBadRegKey);
            }

            return model;
        }

        public ResultModel()
        {
            Root = new BadRegistryKey(null, "");
        }

        public System.Collections.IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;

            return (parent as BadRegistryKey).Children;
        }

        public bool HasChildren(object parent)
        {
            return (parent as BadRegistryKey).Children.Count > 0;
        }
    }
}

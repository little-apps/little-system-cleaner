using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Duplicate_Finder.Controls;
using System.Collections.Generic;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class ResultModel : ITreeModel
    {
        public Result Root { get; }

        internal static ResultModel CreateResultModel(Wizard scanBase)
        {
            ResultModel model = new ResultModel();

            if (scanBase.FilesGroupedByFilename.Count > 0)
            {
                foreach (KeyValuePair<string, List<FileEntry>> kvp in scanBase.FilesGroupedByFilename)
                {
                    Result res = new Result();

                    foreach (FileEntry fileEntry in kvp.Value)
                    {
                        res.Children.Add(new Result(fileEntry, res));
                    }

                    model.Root.Children.Add(res);
                }
            }

            if (scanBase.FilesGroupedByHash.Count > 0)
            {
                foreach (KeyValuePair<string, List<FileEntry>> kvp in scanBase.FilesGroupedByHash)
                {
                    Result res = new Result();

                    foreach (FileEntry fileEntry in kvp.Value)
                    {
                        res.Children.Add(new Result(fileEntry, res));
                    }

                    model.Root.Children.Add(res);
                }
            }

            //model.Root.Children.Add(root);

            return model;
        }

        public ResultModel()
        {
            Root = new Result();
        }

        public System.Collections.IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;

            var result = parent as Result;
            return result?.Children;
        }

        public bool HasChildren(object parent)
        {
            var result = parent as Result;

            return result != null && result.Children.Count > 0;
        }
    }
}

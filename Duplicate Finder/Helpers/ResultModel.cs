using System.Collections;
using System.Linq;
using CommonTools.TreeListView.Tree;
using Duplicate_Finder.Controls;
using Shared;

namespace Duplicate_Finder.Helpers
{
    public class ResultModel : ITreeModel
    {
        public ResultModel()
        {
            Root = new Result();
        }

        public Result Root { get; }

        public IEnumerable GetChildren(object parent)
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

        internal static ResultModel CreateResultModel(Wizard scanBase)
        {
            var model = new ResultModel();

            if (scanBase.FilesGroupedByFilename.Count > 0)
            {
                foreach (var kvp in scanBase.FilesGroupedByFilename)
                {
                    var res = new Result();

                    res.Children.AddRange(kvp.Value.Select(fileEntry => new Result(fileEntry, res)));

                    model.Root.Children.Add(res);
                }
            }

            if (scanBase.FilesGroupedByHash.Count <= 0)
                return model;

            foreach (var kvp in scanBase.FilesGroupedByHash)
            {
                var res = new Result();

                res.Children.AddRange(kvp.Value.Select(fileEntry => new Result(fileEntry, res)));

                model.Root.Children.Add(res);
            }

            //model.Root.Children.Add(root);

            return model;
        }
    }
}
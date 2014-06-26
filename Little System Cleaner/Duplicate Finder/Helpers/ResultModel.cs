using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Duplicate_Finder.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class ResultModel : ITreeModel
    {
        public Result Root { get; private set; }

        internal static ResultModel CreateResultModel(Wizard scanBase)
        {
            Result root = new Result();
            ResultModel model = new ResultModel();

            if (scanBase.FilesGroupedByFilename.Count > 0)
            {
                foreach (KeyValuePair<string, List<FileEntry>> kvp in scanBase.FilesGroupedByFilename)
                {
                    string fileName = kvp.Key;

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
                    string fileName = kvp.Key;

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
            return (parent as Result).Children;
        }

        public bool HasChildren(object parent)
        {
            return (parent as Result).Children.Count > 0;
        }
    }
}

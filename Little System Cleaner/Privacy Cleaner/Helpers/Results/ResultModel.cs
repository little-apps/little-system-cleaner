using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using System.Collections;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultModel : ITreeModel
    {
        public ResultModel()
        {
            Root = new RootNode();
        }

        public ResultNode Root { get; }

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null)
                parent = Root;

            return (parent as ResultNode)?.Children;
        }

        public bool HasChildren(object parent)
        {
            var resultNode = parent as ResultNode;
            return resultNode != null && resultNode.Children.Count > 0;
        }

        internal static ResultModel CreateResultModel()
        {
            var model = new ResultModel();

            foreach (var n in Wizard.ResultArray)
            {
                model.Root.Children.Add(n);
            }

            return model;
        }
    }
}
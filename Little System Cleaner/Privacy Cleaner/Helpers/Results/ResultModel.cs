using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Privacy_Cleaner.Controls;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultModel : ITreeModel
    {
        public ResultNode Root { get; }

        internal static ResultModel CreateResultModel()
        {
            ResultModel model = new ResultModel();

            foreach (ResultNode n in Wizard.ResultArray)
            {
                model.Root.Children.Add(n);
            }

            return model;
        }

        public ResultModel()
        {
            Root = new RootNode();
        }

        public System.Collections.IEnumerable GetChildren(object parent)
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
    }
}

using CommonTools.TreeListView.Tree;
using Little_System_Cleaner.Privacy_Cleaner.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultModel : ITreeModel
    {
        public ResultNode Root { get; private set; }

        public static ResultModel CreateResultModel()
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

            return (parent as ResultNode).Children;
        }

        public bool HasChildren(object parent)
        {
            return (parent as ResultNode).Children.Count > 0;
        }
    }
}

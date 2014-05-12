using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultArray : ObservableCollection<ResultNode>
    {
        public ResultArray()
            : base()
        {

        }

        public ResultNode this[int index]
        {
            get { return (ResultNode)base[index]; }
            set { base[index] = value; }
        }

        public void Add(ResultNode resultNode)
        {
            if (resultNode == null)
                throw new ArgumentNullException("resultNode");

            base.Add(resultNode);

            return;
        }

        public int IndexOf(ResultNode resultNode)
        {
            return (base.IndexOf(resultNode));
        }

        public void Insert(int index, ResultNode resultNode)
        {
            if (resultNode == null)
                throw new ArgumentNullException("resultNode");

            base.Insert(index, resultNode);
        }

        public void Remove(ResultNode resultNode)
        {
            if (resultNode == null)
                throw new ArgumentNullException("resultNode");

            base.Remove(resultNode);
        }

        public bool Contains(ResultNode resultNode)
        {
            return (base.Contains(resultNode));
        }

        public int Problems(string section)
        {
            foreach (ResultNode n in this)
            {
                if (n.Section == section)
                    return n.Children.Count;
            }

            return 0;
        }
    }
}

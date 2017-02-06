using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;

namespace Uninstall_Manager.Helpers
{
    public class ProgramInfoSorter : IComparer<ProgramInfoListViewItem>, IComparer
    {
        private readonly GridViewColumn _column;
        private readonly ListSortDirection _direction;

        public ProgramInfoSorter(GridViewColumn column, ListSortDirection direction)
        {
            _column = column;
            _direction = direction;
        }

        public int Compare(object x, object y)
        {
            return Compare(x as ProgramInfoListViewItem, y as ProgramInfoListViewItem);
        }

        public int Compare(ProgramInfoListViewItem x, ProgramInfoListViewItem y)
        {
            try
            {
                var priority = 0;

                switch ((string)_column.Header)
                {
                    case "Program":
                        priority = string.CompareOrdinal(x.Program, y.Program);
                        break;

                    case "Publisher":
                        priority = string.CompareOrdinal(x.Publisher, y.Publisher);
                        break;

                    case "Size":
                        priority = x.SizeBytes.CompareTo(y.SizeBytes);
                        break;
                }

                return _direction.Equals(ListSortDirection.Ascending) ? priority : -priority;
            }
            catch
            {
                return 0;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    [Serializable()]
    public class ExcludeArray : ObservableCollection<ExcludeItem>
    {
        public ExcludeArray()
        {
        }

        public new bool Contains(ExcludeItem excludeItem)
        {
            foreach (ExcludeItem item in this.Items)
            {
                if (item.ToString() == excludeItem.ToString())
                    return true;
            }

            return false;
        }


    }
}

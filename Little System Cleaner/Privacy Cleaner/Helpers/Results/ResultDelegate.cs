using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public delegate void CleanDelegate();

    public class ResultDelegate : ResultNode
    {
        /// <summary>
        /// Constructor for cleaning delegate
        /// </summary>
        /// <param name="cleanDelegate">Delegate pointer</param>
        /// <param name="desc">Description of delegate</param>
        /// <param name="size">Size of file or files in bytes (optional)</param>
        public ResultDelegate(CleanDelegate cleanDelegate, string desc, long size)
        {
            this.CleanDelegate = cleanDelegate;
            this.Description = desc;
            if (size > 0)
                this.Size = Utils.ConvertSizeToString(size);
        }

        public override void Clean(Report report)
        {
            if (this.CleanDelegate != null)
            {
                this.CleanDelegate();
                report.WriteLine(this.Description);
            }
        }
    }
}

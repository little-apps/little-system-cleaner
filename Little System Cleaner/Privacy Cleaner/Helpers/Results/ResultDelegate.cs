using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public delegate void CleanDelegate();

    public class ResultDelegate : ResultNode
    {
        /// <summary>
        ///     Constructor for cleaning delegate
        /// </summary>
        /// <param name="cleanDelegate">Delegate pointer</param>
        /// <param name="desc">Description of delegate</param>
        /// <param name="size">Size of file or files in bytes (optional)</param>
        public ResultDelegate(CleanDelegate cleanDelegate, string desc, long size)
        {
            CleanDelegate = cleanDelegate;
            Description = desc;
            if (size > 0)
                Size = Utils.ConvertSizeToString(size);
        }

        public override void Clean(Report report)
        {
            if (CleanDelegate == null)
                return;

            CleanDelegate();
            report.WriteLine(Description);
            Settings.Default.lastScanErrorsFixed++;
        }
    }
}
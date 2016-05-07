using Little_System_Cleaner.Misc;
using System.IO;

namespace Little_System_Cleaner.Disk_Cleaner.Helpers
{
    public class ProblemFile
    {
        public ProblemFile(FileInfo fi)
        {
            Checked = true;
            FileInfo = fi;
        }

        public bool? Checked { get; set; }

        public string Name => FileInfo.Name;

        public string Location => FileInfo.DirectoryName;

        public string Size => Utils.ConvertSizeToString(FileInfo.Length);

        public FileInfo FileInfo { get; }
    }
}
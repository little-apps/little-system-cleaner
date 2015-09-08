using Little_System_Cleaner.Misc;
using System;
using System.IO;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class RestoreFile
    {
        public FileInfo FileInfo { get; }

        public string File { get; }

        public string Date { get; }

        public string Size { get; }

        public RestoreFile(FileInfo fileInfo, DateTime fileDateTime)
        {
            FileInfo = fileInfo;
            File = fileInfo.Name;
            Date = fileDateTime.ToString();
            Size = Utils.ConvertSizeToString((uint)fileInfo.Length);
        }
    }
}

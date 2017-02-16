using Shared;
using System;
using System.Globalization;
using System.IO;

namespace Registry_Cleaner.Helpers
{
    public class RestoreFile
    {
        public RestoreFile(FileInfo fileInfo, DateTime fileDateTime)
        {
            FileInfo = fileInfo;
            File = fileInfo.Name;
            Date = fileDateTime.ToString(CultureInfo.InvariantCulture);
            Size = Utils.ConvertSizeToString((uint)fileInfo.Length);
        }

        public FileInfo FileInfo { get; }

        public string File { get; }

        public string Date { get; }

        public string Size { get; }
    }
}
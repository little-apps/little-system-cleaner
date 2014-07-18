using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Little_System_Cleaner.Registry_Cleaner.Helpers.Backup;

namespace Little_System_Cleaner.Registry_Cleaner.Helpers
{
    public class RestoreFile
    {
        private FileInfo _fileInfo;
        private string _file, _date, _size;

        public FileInfo FileInfo
        {
            get { return _fileInfo; }
        }

        public string File
        {
            get { return _file; }
        }
        public string Date
        {
            get { return _date; }
        }
        public string Size
        {
            get { return _size; }
        }

        public RestoreFile(FileInfo fileInfo, DateTime fileDateTime)
        {
            _fileInfo = fileInfo;
            _file = fileInfo.Name;
            _date = fileDateTime.ToString();
            _size = Utils.ConvertSizeToString((uint)fileInfo.Length);
        }
    }
}

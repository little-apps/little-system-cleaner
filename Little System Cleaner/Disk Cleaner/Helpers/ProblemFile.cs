using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Disk_Cleaner.Helpers
{
    public class ProblemFile
    {
        private FileInfo _fileInfo;

        public bool? Checked
        {
            get;
            set;
        }

        public string Name
        {
            get
            {
                return this.FileInfo.Name;
            }
        }

        public string Location
        {
            get
            {
                return this.FileInfo.DirectoryName;
            }
        }

        public string Size
        {
            get
            {
                return Utils.ConvertSizeToString(this.FileInfo.Length);
            }
        }

        public FileInfo FileInfo
        {
            get
            {
                return this._fileInfo;
            }
        }

        public ProblemFile(FileInfo fi)
        {
            this.Checked = true;
            this._fileInfo = fi;
        }
    }
}

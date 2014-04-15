using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Disk_Cleaner.Helpers
{
    public class lviDrive
    {
        public bool? Checked
        {
            get;
            set;
        }

        public string Drive
        {
            get;
            set;
        }

        public string DriveFormat
        {
            get;
            set;
        }

        public string DriveCapacity
        {
            get;
            set;
        }

        public string DriveFreeSpace
        {
            get;
            set;
        }

        public object Tag
        {
            get;
            set;
        }

        public lviDrive(bool isChecked, string driveName, string driveFormat, string driveCapacity, string driveFreeSpace, DriveInfo di)
        {
            this.Checked = isChecked;
            this.Drive = driveName;
            this.DriveFormat = driveFormat;
            this.DriveCapacity = driveCapacity;
            this.DriveFreeSpace = driveFreeSpace;
            this.Tag = di;
        }
    }
}

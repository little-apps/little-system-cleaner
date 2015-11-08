using System.IO;

namespace Little_System_Cleaner.Disk_Cleaner.Helpers
{
    public class LviDrive
    {
        public LviDrive(bool isChecked, string driveName, string driveFormat, string driveCapacity,
            string driveFreeSpace, DriveInfo di)
        {
            Checked = isChecked;
            Drive = driveName;
            DriveFormat = driveFormat;
            DriveCapacity = driveCapacity;
            DriveFreeSpace = driveFreeSpace;
            Tag = di;
        }

        public bool? Checked { get; set; }

        public string Drive { get; set; }

        public string DriveFormat { get; set; }

        public string DriveCapacity { get; set; }

        public string DriveFreeSpace { get; set; }

        public object Tag { get; set; }
    }
}
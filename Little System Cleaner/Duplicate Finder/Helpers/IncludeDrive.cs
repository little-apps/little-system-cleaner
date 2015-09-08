using System.ComponentModel;
using System.IO;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class IncludeDrive : INotifyPropertyChanged
    {        
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        public bool? IsChecked { get; set; } = true;

        public string Name { get; }

        public IncludeDrive(DriveInfo drive)
        {
            Name = drive.ToString();
        }

        public bool Equals(IncludeDrive other)
        {
            return (other != null && Name == other.Name);
        }
        public override bool Equals(object obj)
        {
            var a = obj as IncludeDrive;

            return a != null && Equals(a);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}

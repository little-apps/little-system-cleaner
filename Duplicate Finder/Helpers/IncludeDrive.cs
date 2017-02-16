using Duplicate_Finder.Annotations;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Duplicate_Finder.Helpers
{
    [XmlInclude(typeof(IncludeDrive))]
    public class IncludeDrive : INotifyPropertyChanged
    {
        public IncludeDrive()
        {
        }

        public IncludeDrive(DriveInfo drive)
        {
            Name = drive.ToString();
        }

        private bool? _isChecked = true;
        private string _name;

        public bool? IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public override bool Equals(object obj)
        {
            return obj is IncludeDrive ? Equals((IncludeDrive)obj) : base.Equals(obj);
        }

        public bool Equals(IncludeDrive other)
        {
            return other != null && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
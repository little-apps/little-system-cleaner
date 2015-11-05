using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class HashAlgorithm : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public enum Algorithms { CRC32, MD5, SHA1, SHA256, SHA512 };

        public Algorithms Algorithm
        {
            get;
            set;
        }

        public HashAlgorithm(Algorithms algorithm)
        {
            switch (algorithm)
            {
                case Algorithms.CRC32:
                    {
                        Name = "CRC32 (Fastest)";
                        break;
                    }
                case Algorithms.MD5:
                    {
                        Name = "MD5";
                        break;
                    }
                case Algorithms.SHA1:
                    {
                        Name = "SHA-1";
                        break;
                    }
                case Algorithms.SHA256:
                    {
                        Name = "SHA-256";
                        break;
                    }
                case Algorithms.SHA512:
                    {
                        Name = "SHA-512 (Slowest)";
                        break;
                    }
            }

            Algorithm = algorithm;
        }

        internal static ObservableCollection<HashAlgorithm> CreateList()
        {
            var algorithms = new ObservableCollection<HashAlgorithm>
            {
                new HashAlgorithm(Algorithms.CRC32),
                new HashAlgorithm(Algorithms.MD5),
                new HashAlgorithm(Algorithms.SHA1),
                new HashAlgorithm(Algorithms.SHA256),
                new HashAlgorithm(Algorithms.SHA512)
            };

            return algorithms;
        }
    }
}

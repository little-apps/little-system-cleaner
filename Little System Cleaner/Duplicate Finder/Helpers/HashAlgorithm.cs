using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class HashAlgorithm : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
        private string _name;

        public string Name
        {
            get { return this._name; }
            set
            {
                this._name = value;
                this.OnPropertyChanged("Name");
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
                        this.Name = "CRC32 (Fastest)";
                        break;
                    }
                case Algorithms.MD5:
                    {
                        this.Name = "MD5";
                        break;
                    }
                case Algorithms.SHA1:
                    {
                        this.Name = "SHA-1";
                        break;
                    }
                case Algorithms.SHA256:
                    {
                        this.Name = "SHA-256";
                        break;
                    }
                case Algorithms.SHA512:
                    {
                        this.Name = "SHA-512 (Slowest)";
                        break;
                    }
            }

            this.Algorithm = algorithm;
        }

        internal static ObservableCollection<HashAlgorithm> CreateList()
        {
            ObservableCollection<HashAlgorithm> algorithms = new ObservableCollection<HashAlgorithm>()
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;
using Little_System_Cleaner.Annotations;
using Little_System_Cleaner.Misc;
using WpfAnimatedGif;

namespace Little_System_Cleaner.ProcessInfo
{
    /// <summary>
    /// Interaction logic for LoadProgram.xaml
    /// </summary>
    public partial class ProcessInfo : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private string _status;
        private string _startDateTime;
        private string _endDateTime;
        private Process _process = new Process();
        private IntPtr _mainWindowHandle = IntPtr.Zero;
        private readonly Timer _timer = new Timer();
        private static readonly Dictionary<string, string> _props = new Dictionary<string,string>();

        private bool _moreDetailsExpanded;
        private bool _modulesExpanded;
        private bool _threadsExpanded;
        
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string StartTime
        {
            get { return _startDateTime; }
            set
            {
                _startDateTime = value;
                OnPropertyChanged(nameof(StartTime));
            }
        }

        public string EndTime
        {
            get { return _endDateTime; }
            set
            {
                _endDateTime = value;
                OnPropertyChanged(nameof(EndTime));
            }
        }

        public string ErrorStream
        {
            get
            {
                if (_process == null)
                    return "Process instance is null";

                try
                {
                    var str = _process.StandardError.ReadToEnd();

                    return !string.IsNullOrEmpty(str) ? str : "No error data received";
                }
                catch
                {
                    return "No error data received";
                }
                
            }
        } 

        public string OutputStream
        {
            get
            {
                if (_process == null)
                    return "Process instance is null";

                try
                {
                    var str = _process.StandardOutput.ReadToEnd();

                    return !string.IsNullOrEmpty(str) ? str : "No output data received";
                }
                catch
                {
                    return "No output data received";
                }
                
            }
        }

        public ObservableCollection<ModuleInfo> ProcModules
        {
            get
            {
                try
                {
                    return ToObservableCollection(_process.Modules
                        .Cast<ProcessModule>()
                        .Select(procModule => new ModuleInfo(procModule)));
                }
                catch
                {
                    return new ObservableCollection<ModuleInfo>();
                }
            }
        }

        public ObservableCollection<ThreadInfo> ProcThreads
        {
            get
            {
                try
                {
                    return ToObservableCollection(_process.Threads
                        .Cast<ProcessThread>()
                        .Select(procThread => new ThreadInfo(procThread)));
                }
                catch
                {
                    return new ObservableCollection<ThreadInfo>();
                }
            }
        } 

        public string ProcName => TryCatch(() => _process?.ProcessName, nameof(ProcName));
        public string ProcMachineName => TryCatch(() => _process?.MachineName, nameof(ProcMachineName));
        public string ProcId => TryCatch(() => _process?.Id.ToString(), nameof(ProcId));
        public string ProcHandle => TryCatch(() => _process?.Handle.ToString(), nameof(ProcHandle));
        public string ProcMainModuleName => TryCatch(() => _process.MainModule.ModuleName, nameof(ProcMainModuleName));
        public string ProcBaseAddress => TryCatch(() => _process?.MainModule.BaseAddress.ToString("X8"), nameof(ProcBaseAddress), true);
        public string ProcWindowHandle => TryCatch(() => _process?.MainWindowHandle.ToString(), nameof(ProcWindowHandle), true);
        public string ProcWindowTitle => TryCatch(() => _process?.MainWindowTitle, nameof(ProcWindowTitle), true);
        public string ProcNonPagedSysMemory => TryCatch(() => Utils.ConvertSizeToString(_process.NonpagedSystemMemorySize64), nameof(ProcNonPagedSysMemory), true);
        public string ProcPrivateMemory => TryCatch(() => Utils.ConvertSizeToString(_process.PrivateMemorySize64), nameof(ProcPrivateMemory), true);
        public string ProcPagedMemory => TryCatch(() => Utils.ConvertSizeToString(_process.PagedMemorySize64), nameof(ProcPagedMemory), true);
        public string ProcPagedSysMemory => TryCatch(() => Utils.ConvertSizeToString(_process.PagedSystemMemorySize64), nameof(ProcPagedSysMemory), true);
        public string ProcPagedPeakMemory => TryCatch(() => Utils.ConvertSizeToString(_process.PeakPagedMemorySize64), nameof(ProcPagedPeakMemory), true);
        public string ProcPagedVirtualMemory => TryCatch(() => Utils.ConvertSizeToString(_process.PeakVirtualMemorySize64), nameof(ProcPagedVirtualMemory), true);
        public string ProcVirtMemory => TryCatch(() => Utils.ConvertSizeToString(_process.VirtualMemorySize64), nameof(ProcVirtMemory), true);
        public string ProcWorkingSetPeak => TryCatch(() => Utils.ConvertSizeToString(_process.PeakWorkingSet64), nameof(ProcWorkingSetPeak), true);
        public string ProcPriority => TryCatch(() => _process?.PriorityClass.ToString(), nameof(ProcPriority), true);
        public string ProcPriorityBoostEnabled => TryCatch(() => _process?.PriorityBoostEnabled.ToString(), nameof(ProcPriorityBoostEnabled), true);
        public string ProcHandlesCount => TryCatch(() => _process?.HandleCount.ToString(), nameof(ProcHandlesCount), true);
        public string ProcIsResponding => TryCatch(() => _process?.Responding.ToString(), nameof(ProcIsResponding), true);

        public bool MoreDetailsExpanded
        {
            get { return _moreDetailsExpanded; }
            set
            {
                _moreDetailsExpanded = value;

                if (_moreDetailsExpanded && ProcessExitedWithoutInfo)
                    MessageBox.Show(this, "The process exited before any information could be retrieved.\nLimited information will be available.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                OnPropertyChanged(nameof(MoreDetailsExpanded));
            }
        }

        public bool ModulesExpanded
        {
            get { return _modulesExpanded; }
            set
            {
                _modulesExpanded = value;

                if (_modulesExpanded && ProcessExitedWithoutInfo)
                    MessageBox.Show(this, "The process exited before any information could be retrieved.\nLimited information will be available.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                OnPropertyChanged(nameof(ModulesExpanded));
            }
        }

        public bool ThreadsExpanded
        {
            get { return _threadsExpanded; }
            set
            {
                _threadsExpanded = value;

                if (_threadsExpanded && ProcessExitedWithoutInfo)
                    MessageBox.Show(this, "The process exited before any information could be retrieved.\nLimited information will be available.", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                OnPropertyChanged(nameof(ThreadsExpanded));
            }
        }

        public bool ProcessExitedWithoutInfo => (string.IsNullOrEmpty(ProcName) && _process.HasExited);

        public ProcessInfo(string fileName, string args = "")
        {
            var procStartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = args
            };
            Init(procStartInfo);
        }

        public ProcessInfo(ProcessStartInfo procStartInfo)
        {
            Init(procStartInfo);
        }

        private void Init(ProcessStartInfo processStartInfo)
        {
            _process.StartInfo = processStartInfo;
            _timer.Elapsed += TimerOnElapsed;

            _timer.Start();
            _process.Start();
   
            //TimerOnElapsed(this, (ElapsedEventArgs)EventArgs.Empty);

            Status = $"Process started with ID #{_process.Id}...";

            StartTime = _process.StartTime.ToLongTimeString();

            _process.EnableRaisingEvents = true;

            _process.OutputDataReceived += (o, args) => OnPropertyChanged(nameof(OutputStream));
            _process.ErrorDataReceived += (o, args) => OnPropertyChanged(nameof(ErrorStream));
            _process.Exited += (o, args) =>
            {
                //Dispatcher.Invoke(new Action(() => Image.SetValue(ImageBehavior.AnimatedSourceProperty, null)));
                Status = $"Process exited with exit code {_process.ExitCode}";
                
                EndTime = _process.ExitTime.ToLongTimeString();
            };

            this.HideIcon();

            InitializeComponent();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_process.HasExited)
                // Process exited so no need to keep updating
                _timer.Stop();

            // Updates all properties
            OnPropertyChanged(string.Empty);
        }

        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure you want to kill the process?", Utils.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _process.Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred trying to kill the process: {ex.Message}", Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadProgram_OnClosing(object sender, CancelEventArgs e)
        {
            if (
                MessageBox.Show(this, "Are you sure you want to close this window?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _timer.Stop();
        }

        public ObservableCollection<T> ToObservableCollection<T>(IEnumerable<T> enumeration)
        {
            return new ObservableCollection<T>(enumeration);
        }

        public static string TryCatch(Func<string> action, string propName, bool valueChanges = false, bool returnErrorMessage = false, string defaultValue = "")
        {
            try
            {
                var ret = action();

                if (!_props.ContainsKey(propName))
                    _props.Add(propName, ret);
                else if (valueChanges)
                    _props[propName] = ret;

                return _props[propName];
            }
            catch (Exception e)
            {
                var origValue = (_props.ContainsKey(propName) ? _props[propName] : defaultValue);

                return returnErrorMessage ? e.Message : origValue;
            }
        }
        
    }
}

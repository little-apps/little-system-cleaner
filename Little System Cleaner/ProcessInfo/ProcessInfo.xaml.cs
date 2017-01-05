using Little_System_Cleaner.Annotations;
using Little_System_Cleaner.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;

namespace Little_System_Cleaner.ProcessInfo
{
    /// <summary>
    ///     Interaction logic for LoadProgram.xaml
    /// </summary>
    public sealed partial class ProcessInfo : INotifyPropertyChanged
    {
        #region Fields
        private static readonly Dictionary<string, string> Props = new Dictionary<string, string>();
        private readonly Timer _timer = new Timer();
        private string _endDateTime;
        private bool _modulesExpanded;

        private bool _moreDetailsExpanded;
        private readonly Process _process = new Process();
        private string _startDateTime;

        private string _status;
        private bool _threadsExpanded;
        #endregion
        
        #region Properties
        /// <summary>
        /// Status of process
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// Time process was started
        /// </summary>
        public string StartTime
        {
            get { return _startDateTime; }
            set
            {
                _startDateTime = value;
                OnPropertyChanged(nameof(StartTime));
            }
        }

        /// <summary>
        /// Time process was ended (if it has)
        /// </summary>
        public string EndTime
        {
            get { return _endDateTime; }
            set
            {
                _endDateTime = value;
                OnPropertyChanged(nameof(EndTime));
            }
        }

        /// <summary>
        /// Data from error stream
        /// </summary>
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

        /// <summary>
        /// Data from output stream
        /// </summary>
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

        /// <summary>
        /// List of <see cref="ModuleInfo"/> for process
        /// </summary>
        public ObservableCollection<ModuleInfo> ProcModules
        {
            get
            {
                try
                {
                    return
                        _process.Modules.Cast<ProcessModule>()
                            .Select(procModule => new ModuleInfo(procModule))
                            .ToObservableCollection();
                }
                catch
                {
                    return new ObservableCollection<ModuleInfo>();
                }
            }
        }

        /// <summary>
        /// List of <see cref="ThreadInfo"/> for process
        /// </summary>
        public ObservableCollection<ThreadInfo> ProcThreads
        {
            get
            {
                try
                {
                    return
                        _process.Threads.Cast<ProcessThread>()
                            .Select(procThread => new ThreadInfo(procThread))
                            .ToObservableCollection();
                }
                catch
                {
                    return new ObservableCollection<ThreadInfo>();
                }
            }
        }

        /// <summary>
        /// Name of process
        /// </summary>
        public string ProcName => TryGetProperty(() => _process?.ProcessName, nameof(ProcName));
        
        /// <summary>
        /// Machine name that process is on
        /// </summary>
        public string ProcMachineName => TryGetProperty(() => _process?.MachineName, nameof(ProcMachineName));
        
        /// <summary>
        /// Process ID
        /// </summary>
        public string ProcId => TryGetProperty(() => _process?.Id.ToString(), nameof(ProcId));
        
        /// <summary>
        /// Process handle
        /// </summary>
        public string ProcHandle => TryGetProperty(() => _process?.Handle.ToString(), nameof(ProcHandle));
       
        /// <summary>
        /// Main module name of process
        /// </summary>
        public string ProcMainModuleName => TryGetProperty(() => _process.MainModule.ModuleName, nameof(ProcMainModuleName));

        /// <summary>
        /// Base address (in hex format) of main module
        /// </summary>
        public string ProcBaseAddress
            => TryGetProperty(() => _process?.MainModule.BaseAddress.ToString("X8"), nameof(ProcBaseAddress), true);

        /// <summary>
        /// Handle of main window for process
        /// </summary>
        public string ProcWindowHandle
            => TryGetProperty(() => _process?.MainWindowHandle.ToString(), nameof(ProcWindowHandle), true);

        /// <summary>
        /// Title of main window for processs
        /// </summary>
        public string ProcWindowTitle => TryGetProperty(() => _process?.MainWindowTitle, nameof(ProcWindowTitle), true);

        /// <summary>
        /// Size of non paged system memory for process
        /// </summary>
        public string ProcNonPagedSysMemory
            =>
                TryGetProperty(() => Utils.ConvertSizeToString(_process.NonpagedSystemMemorySize64),
                    nameof(ProcNonPagedSysMemory), true);

        /// <summary>
        /// Size of private memory for process
        /// </summary>
        public string ProcPrivateMemory
            => TryGetProperty(() => Utils.ConvertSizeToString(_process.PrivateMemorySize64), nameof(ProcPrivateMemory), true);

        /// <summary>
        /// Size of paged memory for process
        /// </summary>
        public string ProcPagedMemory
            => TryGetProperty(() => Utils.ConvertSizeToString(_process.PagedMemorySize64), nameof(ProcPagedMemory), true);

        /// <summary>
        /// Size of paged system memory for process
        /// </summary>
        public string ProcPagedSysMemory
            =>
                TryGetProperty(() => Utils.ConvertSizeToString(_process.PagedSystemMemorySize64), nameof(ProcPagedSysMemory),
                    true);

        /// <summary>
        /// Size of paged peak memory for process
        /// </summary>
        public string ProcPagedPeakMemory
            =>
                TryGetProperty(() => Utils.ConvertSizeToString(_process.PeakPagedMemorySize64), nameof(ProcPagedPeakMemory),
                    true);

        /// <summary>
        /// Size of paged virtual memory for process
        /// </summary>
        public string ProcPagedVirtualMemory
            =>
                TryGetProperty(() => Utils.ConvertSizeToString(_process.PeakVirtualMemorySize64),
                    nameof(ProcPagedVirtualMemory), true);

        /// <summary>
        /// Size of virtual memory for process
        /// </summary>
        public string ProcVirtMemory
            => TryGetProperty(() => Utils.ConvertSizeToString(_process.VirtualMemorySize64), nameof(ProcVirtMemory), true);

        /// <summary>
        /// Size of working set peak memory for process
        /// </summary>
        public string ProcWorkingSetPeak
            => TryGetProperty(() => Utils.ConvertSizeToString(_process.PeakWorkingSet64), nameof(ProcWorkingSetPeak), true);

        /// <summary>
        /// Priority of process
        /// </summary>
        public string ProcPriority => TryGetProperty(() => _process?.PriorityClass.ToString(), nameof(ProcPriority), true);

        /// <summary>
        /// Whether priority boost is enabled for process
        /// </summary>
        public string ProcPriorityBoostEnabled
            => TryGetProperty(() => _process?.PriorityBoostEnabled.ToString(), nameof(ProcPriorityBoostEnabled), true);

        /// <summary>
        /// Number of handles open with process
        /// </summary>
        public string ProcHandlesCount
            => TryGetProperty(() => _process?.HandleCount.ToString(), nameof(ProcHandlesCount), true);

        /// <summary>
        /// Whether process is responding
        /// </summary>
        public string ProcIsResponding
            => TryGetProperty(() => _process?.Responding.ToString(), nameof(ProcIsResponding), true);

        /// <summary>
        /// If more details accordion is expanded
        /// </summary>
        public bool MoreDetailsExpanded
        {
            get { return _moreDetailsExpanded; }
            set
            {
                _moreDetailsExpanded = value;

                if (_moreDetailsExpanded && ProcessExitedWithoutInfo)
                    MessageBox.Show(this,
                        "The process exited before any information could be retrieved.\nLimited information will be available.",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                OnPropertyChanged(nameof(MoreDetailsExpanded));
            }
        }

        /// <summary>
        /// If modules accordion is expanded
        /// </summary>
        public bool ModulesExpanded
        {
            get { return _modulesExpanded; }
            set
            {
                _modulesExpanded = value;

                if (_modulesExpanded && ProcessExitedWithoutInfo)
                    MessageBox.Show(this,
                        "The process exited before any information could be retrieved.\nLimited information will be available.",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                OnPropertyChanged(nameof(ModulesExpanded));
            }
        }

        /// <summary>
        /// If threads accordion is expanded
        /// </summary>
        public bool ThreadsExpanded
        {
            get { return _threadsExpanded; }
            set
            {
                _threadsExpanded = value;

                if (_threadsExpanded && ProcessExitedWithoutInfo)
                    MessageBox.Show(this,
                        "The process exited before any information could be retrieved.\nLimited information will be available.",
                        Utils.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

                OnPropertyChanged(nameof(ThreadsExpanded));
            }
        }

        /// <summary>
        /// True if process exited without any further information
        /// </summary>
        public bool ProcessExitedWithoutInfo => string.IsNullOrEmpty(ProcName) && _process.HasExited;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor that takes in file and arguments to start process
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <param name="args">Arguments (default is empty string)</param>
        public ProcessInfo(string fileName, string args = "")
        {
            var procStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args
            };
            Init(procStartInfo);
        }

        /// <summary>
        /// Constructor that takes in ProcessStartInfo to start process
        /// </summary>
        /// <param name="procStartInfo">ProcessStartInfo instance</param>
        public ProcessInfo(ProcessStartInfo procStartInfo)
        {
            Init(procStartInfo);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Starts process and timer and then fills window with process information
        /// </summary>
        /// <param name="processStartInfo"><see cref="ProcessStartInfo"/> instance</param>
        private void Init(ProcessStartInfo processStartInfo)
        {
            _process.StartInfo = processStartInfo;
            _timer.Elapsed += TimerOnElapsed;

            _timer.Start();
            _process.Start();

            Status = $"Process started with ID #{_process.Id}...";

            StartTime = _process.StartTime.ToLongTimeString();

            _process.EnableRaisingEvents = true;

            _process.OutputDataReceived += (o, args) => OnPropertyChanged(nameof(OutputStream));
            _process.ErrorDataReceived += (o, args) => OnPropertyChanged(nameof(ErrorStream));
            _process.Exited += (o, args) =>
            {
                Status = $"Process exited with exit code {_process.ExitCode}";

                EndTime = _process.ExitTime.ToLongTimeString();
            };

            this.HideIcon();

            InitializeComponent();
        }

        /// <summary>
        /// Updates all properties when timer is fired
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="elapsedEventArgs"></param>
        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_process.HasExited)
                // Process exited so no need to keep updating
                _timer.Stop();

            // Updates all properties
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Kills process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show(this, "Are you sure you want to kill the process?", Utils.ProductName,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _process.Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred trying to kill the process: {ex.Message}", Utils.ProductName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Closes window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Prompts user if they want to close window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Tries to get property value by calling function
        /// </summary>
        /// <param name="action">Function to encapsulate with try-catch block</param>
        /// <param name="propName">Name of property</param>
        /// <param name="valueChanges">Whether the property value changes after (default is false)</param>
        /// <param name="returnErrorMessage">If true, the property value will be exception message if one occurs (default is false)</param>
        /// <param name="defaultValue">Default value for property (default is empty string)</param>
        /// <returns></returns>
        public static string TryGetProperty(Func<string> action, string propName, bool valueChanges = false,
            bool returnErrorMessage = false, string defaultValue = "")
        {
            try
            {
                var ret = action();

                if (!Props.ContainsKey(propName))
                    Props.Add(propName, ret);
                else if (valueChanges)
                    Props[propName] = ret;

                return Props[propName];
            }
            catch (Exception e)
            {
                var origValue = Props.ContainsKey(propName) ? Props[propName] : defaultValue;

                return returnErrorMessage ? e.Message : origValue;
            }
        }
        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged Members
    }
}
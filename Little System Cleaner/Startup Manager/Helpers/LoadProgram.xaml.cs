using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using Little_System_Cleaner.Annotations;
using Little_System_Cleaner.Misc;
using WpfAnimatedGif;

namespace Little_System_Cleaner.Startup_Manager.Helpers
{
    /// <summary>
    /// Interaction logic for LoadProgram.xaml
    /// </summary>
    public partial class LoadProgram : INotifyPropertyChanged
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
        private Process _process;
        private IntPtr _mainWindowHandle = IntPtr.Zero;
        private readonly Timer _timer = new Timer();
        private readonly List<string> _stringList = new List<string>();

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

        public string Output => _stringList.Count > 0 ? string.Join("\r\n", _stringList) : "No error/output data received";

        public LoadProgram(string fileName, string args = "")
        {
            var procStartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = args
            };

            Init(procStartInfo);
        }

        public LoadProgram(ProcessStartInfo procStartInfo)
        {
            Init(procStartInfo);
        }

        private void Init(ProcessStartInfo processStartInfo)
        {
            InitializeComponent();

            Status = "Please wait for the program to load...";

            StartTime = "N/A";
            EndTime = "N/A";

            _process = new Process {StartInfo = processStartInfo};
            _timer.Elapsed += TimerOnElapsed;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_process.HasExited)
                return;

            try
            {
                if (_process.MainWindowHandle != _mainWindowHandle)
                {
                    if (_mainWindowHandle == IntPtr.Zero)
                    {
                        _mainWindowHandle = _process.MainWindowHandle;
                        AppendLine($"Process opened main window with handle #{_mainWindowHandle.ToInt64()}");
                    }
                    else
                    {
                        _mainWindowHandle = _process.MainWindowHandle;
                        AppendLine($"Process changed main window to handle #{_mainWindowHandle.ToInt64()}");
                    }

                }
            }
            catch
            {
                // ignored
            }
        }

        private void LoadProgram_OnLoaded(object sender, RoutedEventArgs e)
        {
            
            _timer.Start();
            _process.Start();

            Status = $"Process started with ID #{_process.Id}...";

            StartTime = _process.StartTime.ToLongTimeString();

            _process.EnableRaisingEvents = true;

            _process.OutputDataReceived += (o, args) => AppendLine(args.Data);
            _process.ErrorDataReceived += (o, args) => AppendLine(args.Data);
            _process.Exited += (o, args) =>
            {
                Dispatcher.Invoke(new Action(() => Image.SetValue(ImageBehavior.AnimatedSourceProperty, null)));
                Status = $"Process exited with exit code {_process.ExitCode}";
                EndTime = _process.ExitTime.ToLongTimeString();
            };
        }

        private void AppendLine(string line)
        {
            _stringList.Add(line);

            OnPropertyChanged(nameof(Output));
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
            this.Close();
        }
    }
}

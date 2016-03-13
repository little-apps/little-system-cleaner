using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Little_System_Cleaner.Registry_Optimizer.Helpers;
using Microsoft.Win32;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    ///     Interaction logic for LoadHives.xaml
    /// </summary>
    public partial class LoadHives
    {
        private readonly Wizard _scanBase;

        public LoadHives(Wizard sb)
        {
            InitializeComponent();

            _scanBase = sb;

            Task.Run(new Action(InitHives));
        }

        private void InitHives()
        {
            RegistryKey regKeyHives;
            var i = 0;
            Wizard.RegistryHives = new ObservableCollection<Hive>();

            using (regKeyHives = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\hivelist"))
            {
                if (regKeyHives == null)
                    throw new ApplicationException("Unable to open hive list... this can be a problem!");

                foreach (var valueName in regKeyHives.GetValueNames())
                {
                    Dispatcher.Invoke(new Action(() => Message.Text = $"Loading {++i}/{regKeyHives.ValueCount} Hives"));

                    // Don't touch these hives because they are critical for Windows
                    if (valueName.Contains("BCD") || valueName.Contains("HARDWARE"))
                        continue;

                    var hivePath = regKeyHives.GetValue(valueName) as string;

                    if (string.IsNullOrEmpty(hivePath))
                        continue;

                    if (hivePath[hivePath.Length - 1] == 0)
                        hivePath = hivePath.Substring(0, hivePath.Length - 1);

                    if (string.IsNullOrEmpty(valueName) || string.IsNullOrEmpty(hivePath))
                        continue;

                    var h = new Hive(valueName, hivePath);

                    if (h.IsValid)
                        Wizard.RegistryHives.Add(h);
                }
            }

            _scanBase.HivesLoaded = true;

            _scanBase.MoveNext();
        }
    }
}
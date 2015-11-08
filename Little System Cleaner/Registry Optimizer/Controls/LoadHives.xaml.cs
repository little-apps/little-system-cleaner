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
            RegistryKey rkHives;
            var i = 0;
            Wizard.RegistryHives = new ObservableCollection<Hive>();

            using (rkHives = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\hivelist"))
            {
                if (rkHives == null)
                    throw new ApplicationException("Unable to open hive list... this can be a problem!");

                foreach (var strValueName in rkHives.GetValueNames())
                {
                    Dispatcher.Invoke(new Action(() => Message.Text = $"Loading {++i}/{rkHives.ValueCount} Hives"));

                    // Don't touch these hives because they are critical for Windows
                    if (strValueName.Contains("BCD") || strValueName.Contains("HARDWARE"))
                        continue;

                    var strHivePath = rkHives.GetValue(strValueName) as string;

                    if (string.IsNullOrEmpty(strHivePath))
                        continue;

                    if (strHivePath[strHivePath.Length - 1] == 0)
                        strHivePath = strHivePath.Substring(0, strHivePath.Length - 1);

                    if (string.IsNullOrEmpty(strValueName) || string.IsNullOrEmpty(strHivePath))
                        continue;

                    var h = new Hive(strValueName, strHivePath);

                    if (h.IsValid)
                        Wizard.RegistryHives.Add(h);
                }
            }

            _scanBase.HivesLoaded = true;

            _scanBase.MoveNext();
        }
    }
}
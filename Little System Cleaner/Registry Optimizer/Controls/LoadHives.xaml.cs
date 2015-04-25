using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace Little_System_Cleaner.Registry_Optimizer.Controls
{
    /// <summary>
    /// Interaction logic for LoadHives.xaml
    /// </summary>
    public partial class LoadHives : UserControl
    {
        Wizard scanBase;

        public LoadHives(Wizard sb)
        {
            InitializeComponent();

            this.scanBase = sb;

            Thread t = new Thread(new ThreadStart(InitHives));
            t.Start();
        }

        private void InitHives()
        {
            RegistryKey rkHives = null;
            int i = 0;
            Little_System_Cleaner.Registry_Optimizer.Controls.Wizard.RegistryHives = new System.Collections.ObjectModel.ObservableCollection<Little_System_Cleaner.Registry_Optimizer.Helpers.Hive>();

            using (rkHives = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\hivelist"))
            {
                if (rkHives == null)
                    throw new ApplicationException("Unable to open hive list... this can be a problem!");

                foreach (string strValueName in rkHives.GetValueNames())
                {
                    this.Dispatcher.Invoke(new Action(() => this.label1.Text = string.Format("Loading {0}/{1} Hives", ++i, rkHives.ValueCount)));

                    // Don't touch these hives because they are critical for Windows
                    if (strValueName.Contains("BCD") || strValueName.Contains("HARDWARE"))
                        continue;

                    string strHivePath = rkHives.GetValue(strValueName) as string;

                    if (string.IsNullOrEmpty(strHivePath))
                        continue;

                    if (strHivePath[strHivePath.Length - 1] == 0)
                        strHivePath = strHivePath.Substring(0, strHivePath.Length - 1);

                    if (!string.IsNullOrEmpty(strValueName) && !string.IsNullOrEmpty(strHivePath)) 
                    {
                        Little_System_Cleaner.Registry_Optimizer.Helpers.Hive h = new Little_System_Cleaner.Registry_Optimizer.Helpers.Hive(strValueName, strHivePath);

                        if (h.IsValid)
                            Little_System_Cleaner.Registry_Optimizer.Controls.Wizard.RegistryHives.Add(h);
                    }
                        
                }
            }

            this.scanBase.HivesLoaded = true;

            this.scanBase.MoveNext();
        }
    }
}

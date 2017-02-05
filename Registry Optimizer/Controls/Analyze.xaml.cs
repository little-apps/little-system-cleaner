﻿/*
    Little System Cleaner
    Copyright (C) 2008 Little Apps (http://www.little-apps.com/)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using Shared;

namespace Registry_Optimizer.Controls
{
    /// <summary>
    ///     Interaction logic for Analyze.xaml
    /// </summary>
    public partial class Analyze
    {
        public Analyze()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set taskbar progress bar
            Utils.TaskbarProgressState = TaskbarItemProgressState.Normal;
            Utils.TaskbarProgressValue = 0;

            // Set progress bar
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = Wizard.RegistryHives.Count;
            ProgressBar.Value = 0;

            //_taskScanMain = new Task(AnalyzeHives, _cancellationTokenSource.Token);
            //_taskScanMain.Start();
            AnalyzeHives();
        }

        private async void AnalyzeHives()
        {
            var dtStart = DateTime.Now;

            Thread.BeginCriticalRegion();

            foreach (var h in Wizard.RegistryHives)
            {
                IncrementProgressBar(h.RegistryHive);

                // Analyze Hive
                await Task.Run(() => h.AnalyzeHive(this));
            }

            Thread.EndCriticalRegion();

            var timeSpan = DateTime.Now.Subtract(dtStart);

            Utils.Watcher.EventPeriod("Registry Optimizer", "Analyze", (int)timeSpan.TotalSeconds,
                true);

            Utils.TaskbarProgressState = TaskbarItemProgressState.None;
            Close();
        }

        private void IncrementProgressBar(string currentHive)
        {
            ProgressBar.Value++;
            TextBlockStatus.Text = $"Analyzing: {currentHive}";
        }

        private void progressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(ProgressBar.Maximum) > 0)
                Utils.TaskbarProgressValue = e.NewValue / ProgressBar.Maximum;
        }
    }
}
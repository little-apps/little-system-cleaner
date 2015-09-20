using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.ProcessInfo
{
    public class ThreadInfo
    {
        private readonly ProcessThread _processThread;

        public string ID => ProcessInfo.TryCatch(() => _processThread.Id.ToString(), nameof(ID), true);
        public string StartAddress => ProcessInfo.TryCatch(() => _processThread.StartAddress.ToString("X8"), nameof(StartAddress));
        public string Priority => ProcessInfo.TryCatch(() => _processThread.PriorityLevel.ToString(), nameof(Priority));
        public string State => ProcessInfo.TryCatch(() => _processThread.ThreadState.ToString(), nameof(State));
        public string StartTime => ProcessInfo.TryCatch(() => _processThread.StartTime.ToString(), nameof(StartTime));

        public ThreadInfo(ProcessThread thread)
        {
            _processThread = thread;
        }
    }
}

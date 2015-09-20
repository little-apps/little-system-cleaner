using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.LoadProgram
{
    public class ThreadInfo
    {
        private readonly ProcessThread _processThread;

        public string ID => LoadProgram.TryCatch(() => _processThread.Id.ToString(), nameof(ID), true);
        public string StartAddress => LoadProgram.TryCatch(() => _processThread.StartAddress.ToString("X8"), nameof(StartAddress));
        public string Priority => LoadProgram.TryCatch(() => _processThread.PriorityLevel.ToString(), nameof(Priority));
        public string State => LoadProgram.TryCatch(() => _processThread.ThreadState.ToString(), nameof(State));
        public string StartTime => LoadProgram.TryCatch(() => _processThread.StartTime.ToString(), nameof(StartTime));

        public ThreadInfo(ProcessThread thread)
        {
            _processThread = thread;
        }
    }
}

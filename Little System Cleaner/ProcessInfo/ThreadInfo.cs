using System.Diagnostics;

namespace Little_System_Cleaner.ProcessInfo
{
    public class ThreadInfo
    {
        private readonly ProcessThread _processThread;

        public ThreadInfo(ProcessThread thread)
        {
            _processThread = thread;
        }

        public string ID => ProcessInfo.TryCatch(() => _processThread.Id.ToString(), nameof(ID), true);

        public string StartAddress
            => ProcessInfo.TryCatch(() => _processThread.StartAddress.ToString("X8"), nameof(StartAddress), true);

        public string Priority
            => ProcessInfo.TryCatch(() => _processThread.PriorityLevel.ToString(), nameof(Priority), true);

        public string State => ProcessInfo.TryCatch(() => _processThread.ThreadState.ToString(), nameof(State), true);
        public string StartTime => ProcessInfo.TryCatch(() => _processThread.StartTime.ToString(), nameof(StartTime));
    }
}
using System.Diagnostics;
using System.Globalization;

namespace Shared.ProcessInfo
{
    /// <summary>
    /// Wrapper class for ProcessThread
    /// </summary>
    public class ThreadInfo
    {
        private readonly ProcessThread _processThread;

        /// <summary>
        /// Constructor for ThreadInfo
        /// </summary>
        /// <param name="thread">ProcessThread</param>
        public ThreadInfo(ProcessThread thread)
        {
            _processThread = thread;
        }

        /// <summary>
        /// Gets the ProcessThread ID
        /// </summary>
        public string Id => ProcessInfo.TryGetProperty(() => _processThread.Id.ToString(), nameof(Id), true);

        /// <summary>
        /// Gets the ProcessThread start address (in hex format)
        /// </summary>
        public string StartAddress
            => ProcessInfo.TryGetProperty(() => _processThread.StartAddress.ToString("X8"), nameof(StartAddress), true);

        /// <summary>
        /// Gets priority of ProcessThread
        /// </summary>
        public string Priority
            => ProcessInfo.TryGetProperty(() => _processThread.PriorityLevel.ToString(), nameof(Priority), true);

        /// <summary>
        /// Gets state of ProcessThread
        /// </summary>
        public string State
            => ProcessInfo.TryGetProperty(() => _processThread.ThreadState.ToString(), nameof(State), true);

        /// <summary>
        /// Gets the time ProcessThread was started
        /// </summary>
        public string StartTime
            =>
                ProcessInfo.TryGetProperty(() => _processThread.StartTime.ToString(CultureInfo.CurrentCulture),
                    nameof(StartTime));
    }
}
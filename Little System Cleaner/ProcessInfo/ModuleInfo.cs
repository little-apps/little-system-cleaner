using System.Diagnostics;

namespace Little_System_Cleaner.ProcessInfo
{
    /// <summary>
    /// Wrapper class for ProcessModule
    /// </summary>
    public class ModuleInfo
    {
        private readonly ProcessModule _module;

        /// <summary>
        /// Constructor for ModuleInfo
        /// </summary>
        /// <param name="module">ProcessModule</param>
        public ModuleInfo(ProcessModule module)
        {
            _module = module;
        }

        /// <summary>
        /// Gets the module name of ProcessModule
        /// </summary>
        public string ModuleName => ProcessInfo.TryGetProperty(() => _module.ModuleName, nameof(ModuleName), true);

        /// <summary>
        /// Gets the file version of ProcessModule
        /// </summary>
        public string FileVersion
            => ProcessInfo.TryGetProperty(() => _module.FileVersionInfo.FileVersion, nameof(FileVersion));

        /// <summary>
        /// Gets the base address (in hex format) of ProcessModule
        /// </summary>
        public string BaseAddress
            => ProcessInfo.TryGetProperty(() => _module.BaseAddress.ToString("X8"), nameof(BaseAddress));

        /// <summary>
        /// Gets the entry point address (in hex format) of ProcessModule
        /// </summary>
        public string EntryPointAddress
            => ProcessInfo.TryGetProperty(() => _module.EntryPointAddress.ToString("X8"), nameof(EntryPointAddress));

        /// <summary>
        /// Gets the file path of ProcessModule
        /// </summary>
        public string FilePath => ProcessInfo.TryGetProperty(() => _module.FileName, nameof(FilePath));
    }
}
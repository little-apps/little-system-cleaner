using System.Diagnostics;

namespace Little_System_Cleaner.ProcessInfo
{
    public class ModuleInfo
    {
        private readonly ProcessModule _module;

        public ModuleInfo(ProcessModule module)
        {
            _module = module;
        }

        public string ModuleName => ProcessInfo.TryCatch(() => _module.ModuleName, nameof(ModuleName), true);

        public string FileVersion
            => ProcessInfo.TryCatch(() => _module.FileVersionInfo.FileVersion, nameof(FileVersion));

        public string BaseAddress => ProcessInfo.TryCatch(() => _module.BaseAddress.ToString("X8"), nameof(BaseAddress))
            ;

        public string EntryPointAddress
            => ProcessInfo.TryCatch(() => _module.EntryPointAddress.ToString("X8"), nameof(EntryPointAddress));

        public string FilePath => ProcessInfo.TryCatch(() => _module.FileName, nameof(FilePath));
    }
}
using System.Diagnostics;

namespace Little_System_Cleaner.LoadProgram
{
    public class ModuleInfo
    {
        private readonly ProcessModule _module;

        public string ModuleName => LoadProgram.TryCatch(() => _module.ModuleName, nameof(ModuleName), true);
        public string FileVersion => LoadProgram.TryCatch(() => _module.FileVersionInfo.FileVersion, nameof(FileVersion));
        public string BaseAddress => LoadProgram.TryCatch(() => _module.BaseAddress.ToString("X8"), nameof(BaseAddress));
        public string EntryPointAddress => LoadProgram.TryCatch(() => _module.EntryPointAddress.ToString("X8"), nameof(EntryPointAddress));
        public string FilePath => LoadProgram.TryCatch(() => _module.FileName, nameof(FilePath));

        public ModuleInfo(ProcessModule module)
        {
            _module = module;
        }
    }
}

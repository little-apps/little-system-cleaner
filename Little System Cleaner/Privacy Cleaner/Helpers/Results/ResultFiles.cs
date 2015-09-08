using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultFiles : ResultNode
    {
        /// <summary>
        /// Constructor for bad file path (leaf node)
        /// </summary>
        /// <param name="desc">Description of problem</param>
        /// <param name="filePaths">The file paths</param>
        /// <param name="fileSize">The size of the files (in bytes)</param>
        public ResultFiles(string desc, string[] filePaths, long fileSize)
        {
            Description = desc;
            FilePaths = filePaths;
            Size = Utils.ConvertSizeToString(fileSize);
        }

        public override void Clean(Report report)
        {
            foreach (string filePath in FilePaths.Where(File.Exists))
            {
                try
                {
                    MiscFunctions.DeleteFile(filePath);
                    report.WriteLine($"Deleted File: {filePath}");
                    Settings.Default.lastScanErrorsFixed++;
                }
                catch (Exception ex)
                {
                    string message = $"The following file could not be removed: {filePath}\nError: {ex.Message}";
                    Debug.WriteLine(message);
                }
            }
        }
    }
}

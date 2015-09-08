using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Little_System_Cleaner.Misc;
using Little_System_Cleaner.Properties;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultFolders : ResultNode
    {
        /// <summary>
        /// Constructor for folder path string array
        /// </summary>
        /// <param name="desc">Description of registry keys</param>
        /// <param name="folderPaths">Folder path list</param>
        public ResultFolders(string desc, Dictionary<string, bool> folderPaths)
        {
            Description = desc;
            FolderPaths = folderPaths;
        }

        public override void Clean(Report report)
        {
            foreach (KeyValuePair<string, bool> kvp in FolderPaths.Where(kvp => Directory.Exists(kvp.Key)))
            {
                try
                {
                    string folderPath = kvp.Key;
                    bool recurse = kvp.Value;

                    MiscFunctions.DeleteDir(folderPath, recurse);
                    report.WriteLine($"Deleted Folder: {folderPath}");
                    Settings.Default.lastScanErrorsFixed++;
                }
                catch (Exception ex)
                {
                    string message = $"The following folder could not be removed: {kvp.Key}\nError: {ex.Message}";
                    Debug.WriteLine(message);
                }
            }
        }
    }
}

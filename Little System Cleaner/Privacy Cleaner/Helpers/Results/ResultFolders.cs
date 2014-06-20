using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
            this.Description = desc;
            this.FolderPaths = folderPaths;
        }

        public override void Clean(Report report)
        {
            foreach (KeyValuePair<string, bool> kvp in this.FolderPaths)
            {
                try
                {
                    string folderPath = kvp.Key;
                    bool recurse = kvp.Value;

                    if (Directory.Exists(folderPath))
                    {
                        MiscFunctions.DeleteDir(folderPath, recurse);
                        report.WriteLine(string.Format("Deleted Folder: {0}", folderPath));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    System.Diagnostics.Debug.WriteLine("error accessing file");
                }
            }
        }
    }
}

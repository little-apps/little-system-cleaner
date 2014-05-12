using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers.Results
{
    public class ResultFiles : ResultNode
    {
        /// <summary>
        /// Constructor for bad file path (leaf node)
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="fileSize">The size of the file (in bytes)</param>
        public ResultFiles(string desc, string[] filePaths, long fileSize)
        {
            this.Description = desc;
            this.FilePaths = filePaths;
            this.Size = Utils.ConvertSizeToString(fileSize);
        }

        public override void Clean(Report report)
        {
            foreach (string filePath in this.FilePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        Utils.DeleteFile(filePath);
                        report.WriteLine(string.Format("Deleted File: {0}", filePath));
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

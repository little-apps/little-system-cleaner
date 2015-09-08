using Little_System_Cleaner.Privacy_Cleaner.Scanners;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Little_System_Cleaner.Privacy_Cleaner.Helpers
{
    internal static class MiscFunctions
    {
        #region Functions
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindFirstUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr FindFirstUrlCacheEntry([MarshalAs(UnmanagedType.LPStr)] string lpszUrlSearchPattern, IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "FindNextUrlCacheEntryA", CallingConvention = CallingConvention.StdCall)]
        internal static extern bool FindNextUrlCacheEntry(IntPtr hFind, IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true)]
        internal static extern long FindCloseUrlCache(IntPtr hEnumHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
        #endregion

        internal static List<InternetExplorer.INTERNET_CACHE_ENTRY_INFO> FindUrlCacheEntries(string urlPattern)
        {
            List<InternetExplorer.INTERNET_CACHE_ENTRY_INFO> cacheEntryList = new List<InternetExplorer.INTERNET_CACHE_ENTRY_INFO>();

            int structSize = 0;

            IntPtr bufferPtr = IntPtr.Zero;
            IntPtr cacheEnumHandle = FindFirstUrlCacheEntry(urlPattern, bufferPtr, ref structSize);

            InternetExplorer.INTERNET_CACHE_ENTRY_INFO? cacheEntry;

            switch (Marshal.GetLastWin32Error())
            {
                // ERROR_SUCCESS
                case 0:
                    {
                        if (cacheEnumHandle.ToInt32() > 0)
                        {
                            // Store entry
                            if ((cacheEntry = GetCacheEntry(bufferPtr)).HasValue)
                                cacheEntryList.Add(cacheEntry.Value);
                        }

                        break;
                    }
                    

                // ERROR_INSUFFICIENT_BUFFER
                case 122:
                    {
                        // Repeat call to API with size returned by first call
                        bufferPtr = Marshal.AllocHGlobal(structSize);
                        cacheEnumHandle = FindFirstUrlCacheEntry(urlPattern, bufferPtr, ref structSize);

                        if (cacheEnumHandle.ToInt32() > 0)
                        {
                            // Store entry
                            if ((cacheEntry = GetCacheEntry(bufferPtr)).HasValue)
                                cacheEntryList.Add(cacheEntry.Value);

                            break;
                        }
                        else
                        {
                            // Failed to get handle, return...
                            Marshal.FreeHGlobal(bufferPtr);
                            FindCloseUrlCache(cacheEnumHandle);

                            return cacheEntryList;
                        }
                    }

                default:
                    {
                        Marshal.FreeHGlobal(bufferPtr);
                        FindCloseUrlCache(cacheEnumHandle);

                        return cacheEntryList;
                    }
            }

            do
            {
                bufferPtr = Marshal.ReAllocHGlobal(bufferPtr, new IntPtr(structSize));

                if (FindNextUrlCacheEntry(cacheEnumHandle, bufferPtr, ref structSize))
                {
                    // Store entry
                    if ((cacheEntry = GetCacheEntry(bufferPtr)).HasValue)
                        cacheEntryList.Add(cacheEntry.Value);
                }
                else
                {
                    switch (Marshal.GetLastWin32Error())
                    {
                        // ERROR_INSUFFICIENT_BUFFER
                        case 122:
                            {
                                // Repeat call to API with size returned by first call
                                bufferPtr = Marshal.ReAllocHGlobal(bufferPtr, new IntPtr(structSize));

                                if (FindNextUrlCacheEntry(cacheEnumHandle, bufferPtr, ref structSize))
                                {
                                    // Store entry
                                    if ((cacheEntry = GetCacheEntry(bufferPtr)).HasValue)
                                        cacheEntryList.Add(cacheEntry.Value);

                                    break;
                                }
                                else
                                {
                                    Marshal.FreeHGlobal(bufferPtr);
                                    FindCloseUrlCache(cacheEnumHandle);

                                    return cacheEntryList;
                                }
                            }
                            
                        // ERROR_NO_MORE_ITEMS
                        case 259:
                            {
                                Marshal.FreeHGlobal(bufferPtr);
                                FindCloseUrlCache(cacheEnumHandle);

                                return cacheEntryList;
                            }

                        default:
                            {
                                Marshal.FreeHGlobal(bufferPtr);
                                FindCloseUrlCache(cacheEnumHandle);

                                return cacheEntryList;
                            }
                    }
                }
            } while (true);

            // Wont reach here
        }

        /// <summary>
        /// Gets INTERNET_CACHE_ENTRY_INFO from buffer
        /// </summary>
        /// <param name="bufferPtr">Pointer to buffer</param>
        /// <returns>INTERNET_CACHE_ENTRY_INFO struct or null on error</returns>
        private static InternetExplorer.INTERNET_CACHE_ENTRY_INFO? GetCacheEntry(IntPtr bufferPtr)
        {
            InternetExplorer.INTERNET_CACHE_ENTRY_INFO? cacheEntry;

            try
            {
                cacheEntry = (InternetExplorer.INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(bufferPtr, typeof(InternetExplorer.INTERNET_CACHE_ENTRY_INFO));
            }
            catch (Exception)
            {
                return null;
            }

            return cacheEntry;
        } 

        /// <summary>
        /// Checks to see if a process is a running
        /// </summary>
        /// <param name="procName">The name of the process (ex: firefox for Firefox)</param>
        /// <returns>True if the process is running</returns>
        internal static bool IsProcessRunning(string procName)
        {
            return Process.GetProcessesByName(procName).Any(proc => !proc.HasExited);
        }

        internal static string ExpandVars(string p)
        {
            string str = (string)p.Clone();

            if (string.IsNullOrEmpty(str))
                throw new ArgumentNullException(str);

            // Expand system variables
            str = Environment.ExpandEnvironmentVariables(str);

            // Expand program variables
            // (Needed for unspecified variables)
            str = str.Replace("%Cookies%", Environment.GetFolderPath(Environment.SpecialFolder.Cookies));
            str = str.Replace("%Favorites%", Environment.GetFolderPath(Environment.SpecialFolder.Favorites));
            str = str.Replace("%History%", Environment.GetFolderPath(Environment.SpecialFolder.History));
            str = str.Replace("%InternetCache%", Environment.GetFolderPath(Environment.SpecialFolder.InternetCache));
            str = str.Replace("%MyComputer%", Environment.GetFolderPath(Environment.SpecialFolder.MyComputer));
            str = str.Replace("%MyDocuments%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            str = str.Replace("%MyMusic%", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            str = str.Replace("%MyPictures%", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            str = str.Replace("%Recent%", Environment.GetFolderPath(Environment.SpecialFolder.Recent));
            str = str.Replace("%SendTo%", Environment.GetFolderPath(Environment.SpecialFolder.SendTo));
            str = str.Replace("%StartMenu%", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
            str = str.Replace("%Startup%", Environment.GetFolderPath(Environment.SpecialFolder.Startup));
            str = str.Replace("%Templates%", Environment.GetFolderPath(Environment.SpecialFolder.Templates));

            return str;
        }

        internal static string[] GetSections(string filePath)
        {
            uint MAX_BUFFER = 32767;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("Path to INI file cannot be empty or null. Unable to get sections.");
                return new string[] { };
            }

            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
            uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, filePath);
            if (bytesReturned == 0)
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                return new string[] { };
            }

            string local = Marshal.PtrToStringAnsi(pReturnedString, (int)bytesReturned);
            Marshal.FreeCoTaskMem(pReturnedString);

            return local.Substring(0, local.Length - 1).Split('\0');
        }

        internal static StringDictionary GetValues(string filePath, string sectionName)
        {
            uint MAX_BUFFER = 32767;

            StringDictionary ret = new StringDictionary();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("Path to INI file cannot be empty or null. Unable to get values.");
                return ret;
            }

            if (string.IsNullOrWhiteSpace(sectionName))
            {
                Debug.WriteLine("Section name cannot be empty or null. Unable to get values.");
                return ret;
            }

            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);

            uint bytesReturned = GetPrivateProfileSection(sectionName, pReturnedString, MAX_BUFFER, filePath);

            if ((bytesReturned == MAX_BUFFER - 2) || (bytesReturned == 0))
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                return ret;
            }

            //bytesReturned -1 to remove trailing \0

            // NOTE: Calling Marshal.PtrToStringAuto(pReturnedString) will
            //       result in only the first pair being returned
            string returnedString = Marshal.PtrToStringAuto(pReturnedString, (int)bytesReturned - 1);

            Marshal.FreeCoTaskMem(pReturnedString);

            foreach (string value in returnedString.Split('\0'))
            {
                string[] valueKey = value.Split('=');

                ret.Add(valueKey[0], valueKey[1]);
            }

            return ret;
        }

        /// <summary>
        /// Gets the file size
        /// </summary>
        /// <param name="filePath">Path to the filename</param>
        /// <returns>File Size (in bytes)</returns>
        internal static long GetFileSize(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);

                return fi.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the folder size
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <param name="includeSubDirs">Include sub directories</param>
        /// <returns>Folder Size (in bytes)</returns>
        internal static long GetFolderSize(string folderPath, bool includeSubDirs)
        {
            long totalSize = 0;

            try
            {
                totalSize += Directory.GetFiles(folderPath, "*", (includeSubDirs) ? (System.IO.SearchOption.AllDirectories) : (System.IO.SearchOption.TopDirectoryOnly)).Select(GetFileSize).Where(fileSize => fileSize != 0).Sum();
            }
            catch (Exception)
            {
                return 0;
            }

            return totalSize;
        }

        /// <summary>
        /// Checks if file is valid for privacy cleaner
        /// </summary>
        /// <param name="fileInfo">FileInfo</param>
        /// <returns>True if file is valid</returns>
        internal static bool IsFileValid(FileInfo fileInfo)
        {
            if (fileInfo == null)
                return false;

            FileAttributes fileAttribs;
            long fileLength;

            try
            {
                fileAttribs = fileInfo.Attributes;
                fileLength = fileInfo.Length;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if file is valid.");
                return false;
            }

            if ((fileAttribs & FileAttributes.System) == FileAttributes.System && (!Properties.Settings.Default.privacyCleanerIncSysFile))
                return false;

            if ((fileAttribs & FileAttributes.Hidden) == FileAttributes.Hidden && (!Properties.Settings.Default.privacyCleanerIncHiddenFile))
                return false;

            if ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly && (!Properties.Settings.Default.privacyCleanerIncReadOnlyFile))
                return false;

            if ((fileLength == 0) && (!Properties.Settings.Default.privacyCleanerInc0ByteFile))
                return false;

            return true;
        }

        /// <summary>
        /// Checks if file path is valid
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <returns>True if file is valid</returns>
        internal static bool IsFileValid(string filePath)
        {
            bool bRet = false;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("File path cannot be empty or null. Unable to check if file is valid.");
                return false;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                bRet = IsFileValid(fileInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to check if file is valid.");
                return bRet;
            }


            return bRet;
        }

        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="filePath">Path to file</param>
        internal static void DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.WriteLine("File path cannot be empty or null. Unable to delete file.");
                return;
            }

            try
            {
                if (Properties.Settings.Default.privacyCleanerDeletePerm)
                    File.Delete(filePath);
                else
                    FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to delete file: " + filePath);
            }
        }

        /// <summary>
        /// Deletes a directory
        /// </summary>
        /// <param name="dirPath">Path to directory</param>
        /// <param name="recurse">Recursive delete</param>
        internal static void DeleteDir(string dirPath, bool recurse)
        {
            if (string.IsNullOrWhiteSpace(dirPath))
            {
                Debug.WriteLine("Directory path cannot be empty or null. Unable to delete directory.");
                return;
            }

            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                if ((dirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    dirInfo.Attributes = dirInfo.Attributes & ~FileAttributes.ReadOnly;

                if (!recurse && dirInfo.GetFileSystemInfos().Length > 0)
                    return;

                if (Properties.Settings.Default.privacyCleanerDeletePerm)
                    Directory.Delete(dirPath, recurse);
                else
                    FileSystem.DeleteDirectory(dirPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following error occurred: " + ex.Message + "\nUnable to delete folder: " + dirPath);
            }

        }
    }
}

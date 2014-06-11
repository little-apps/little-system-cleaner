using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class FileEntry
    {
        private readonly FileInfo _fileInfo;

        private string _filePath;
        private long _fileSize;
        private string _fileChecksum;
        private bool _hasAudioTags;

        public string FileName
        {
            get
            {
                if (this._fileInfo != null)
                    return this._fileInfo.Name;
                else
                    return Path.GetFileName(this._filePath);
            }
        }

        public string FilePath
        {
            get 
            {
                if (this._fileInfo != null)
                    return this._fileInfo.ToString();
                else
                    return this._filePath;
            }
            set
            {
                this._filePath = value;
            }
        }

        public long FileSize
        {
            get 
            {
                if (this._fileInfo != null)
                    return this._fileInfo.Length;
                else
                    return this._fileSize;
            }
            set
            {
                this._fileSize = value;
            }
        }

        public bool HasAudioTags
        {
            get { return this._hasAudioTags; }
        }

        public bool IsDeleteable
        {
            get
            {
                FileSecurity accessControlList;
                bool deleteAllow = false;
                bool deleteDeny = false;

                try 
                {
                    accessControlList = File.GetAccessControl(this.FilePath);
                }
                catch (Exception)
                {
                    return false;
                }

                if (accessControlList == null)
                    return false;
                
                AuthorizationRuleCollection accessRules = accessControlList.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                if (accessRules == null)
                    return false;

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if ((rule.FileSystemRights & FileSystemRights.Delete) != FileSystemRights.Delete)
                        continue;

                    if (rule.AccessControlType == AccessControlType.Allow)
                        deleteAllow = true;
                    else if (rule.AccessControlType == AccessControlType.Deny)
                        deleteDeny = true;
                }

                return deleteAllow && !deleteDeny;
            }
        }

        public string Checksum
        {
            get { return this._fileChecksum; }
        }

        public FileEntry()
        {
            this._fileInfo = null;
        }

        public FileEntry(FileInfo fi)
        {
            CommonTools.TagLib.File file;

            this._fileInfo = fi;

            // Get audio tags
            try
            {
                file = CommonTools.TagLib.File.Create(this.FilePath);
            }
            catch (CommonTools.TagLib.UnsupportedFormatException)
            {
                file = null;
            }

            if (file != null)
            {
                //file.Tag.Album;
            }
        }

        public void GetChecksum(HashAlgorithm.Algorithms algorithm, bool includeFilename = false)
        {
            string checksum = string.Empty;

            if (algorithm == HashAlgorithm.Algorithms.CRC32)
            {
                checksum = this.CalculateCRC32(includeFilename);
            }
            else if (algorithm == HashAlgorithm.Algorithms.MD5)
            {
                checksum = this.CalculateMD5(includeFilename);
            }
            else if (algorithm == HashAlgorithm.Algorithms.SHA1)
            {
                checksum = this.CalculateSHA1(includeFilename);
            }
            else if (algorithm == HashAlgorithm.Algorithms.SHA256)
            {
                checksum = this.CalculateSHA256(includeFilename);
            }
            else if (algorithm == HashAlgorithm.Algorithms.SHA512)
            {
                checksum = this.CalculateSHA512(includeFilename);
            }

            if (!string.IsNullOrEmpty(checksum))
                this._fileChecksum = checksum;
        }

        private MemoryStream GetFileStream(bool includeFilename)
        {
            MemoryStream memStream = new MemoryStream();
            FileStream fileStream = null;

            try
            {
                if (this._fileInfo != null)
                    fileStream = this._fileInfo.OpenRead();
                else
                    fileStream = File.OpenRead(this._filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An error occurred ({0}) trying to open a FileStream for a file ({1})", ex.Message, this._filePath);
            }

            if (fileStream == null)
            {
                memStream.Close();
                return null;
            }

            if (includeFilename)
            {
                byte[] fileNameNoExt = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(this.FilePath).ToLower());

                memStream.Write(fileNameNoExt, 0, fileNameNoExt.Length);
            }

            fileStream.CopyTo(memStream);

            fileStream.Close();

            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }

        private string CalculateCRC32(bool includeFilename)
        {
            uint polynomial = 0xedb88320u;
            uint seed = 0xffffffffu;
            uint i;
            uint crc;

            // Initialize table
            uint[] table = new uint[256];
            for (i = 0; i < 256; i++)
            {
                uint entry = (uint)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry = entry >> 1;
                }

                table[i] = entry;
            }

            crc = seed;

            using (MemoryStream memStream = this.GetFileStream(includeFilename))
            {
                if (memStream == null)
                    return string.Empty;

                foreach (byte b in memStream.GetBuffer())
                {
                    crc = (crc >> 8) ^ table[b ^ crc & 0xff];
                }
            }

            return (~(crc)).ToString();
        }

        private string CalculateMD5(bool includeFilename)
        {
            string hash = string.Empty;

            using (var md5 = MD5.Create())
            {
                using (MemoryStream memStream = this.GetFileStream(includeFilename))
                {
                    if (memStream == null)
                        return hash;

                    byte[] hashBytes = md5.ComputeHash(memStream);

                    // Convert to hex
                    foreach (byte b in hashBytes)
                        hash += b.ToString("x2");
                }
            }

            return hash;
        }

        private string CalculateSHA1(bool includeFilename)
        {
            string hash = string.Empty;

            using (var sha1 = new SHA1Managed())
            {
                using (MemoryStream memStream = this.GetFileStream(includeFilename))
                {
                    if (memStream == null)
                        return hash;

                    byte[] hashBytes = sha1.ComputeHash(memStream);

                    // Convert to hex
                    foreach (byte b in hashBytes)
                        hash += b.ToString("x2");
                }
            }

            return hash;
        }

        private string CalculateSHA256(bool includeFilename)
        {
            string hash = string.Empty;

            using (var sha256 = new SHA256Managed())
            {
                using (MemoryStream memStream = this.GetFileStream(includeFilename))
                {
                    if (memStream == null)
                        return hash;

                    byte[] hashBytes = sha256.ComputeHash(memStream);

                    // Convert to hex
                    foreach (byte b in hashBytes)
                        hash += b.ToString("x2");
                }
            }

            return hash;
        }

        private string CalculateSHA512(bool includeFilename)
        {
            string hash = string.Empty;

            using (var sha512 = new SHA512Managed())
            {
                using (MemoryStream memStream = this.GetFileStream(includeFilename))
                {
                    if (memStream == null)
                        return hash;

                    byte[] hashBytes = sha512.ComputeHash(memStream);

                    // Convert to hex
                    foreach (byte b in hashBytes)
                        hash += b.ToString("x2");
                }
            }

            return hash;
        }

    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using CommonTools.TagLib;
using CommonTools.TagLib.Mpeg;
using Little_System_Cleaner.Duplicate_Finder.Controls;
using File = CommonTools.TagLib.File;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class FileEntry
    {
        private readonly FileInfo _fileInfo;

        private string _filePath;
        private long _fileSize;

        public string FileName => _fileInfo?.Name ?? Path.GetFileName(_filePath);

        public string FilePath
        {
            get
            {
                return _fileInfo?.ToString() ?? _filePath;
            }
            set
            {
                _filePath = value;
            }
        }

        public long FileSize
        {
            get
            {
                return _fileInfo?.Length ?? _fileSize;
            }
            set
            {
                _fileSize = value;
            }
        }

        public bool HasAudioTags { get; private set; }

        public bool IsDeleteable
        {
            get
            {
                FileSecurity accessControlList;
                bool deleteAllow = false;
                bool deleteDeny = false;

                try 
                {
                    accessControlList = System.IO.File.GetAccessControl(FilePath);
                }
                catch (Exception)
                {
                    return false;
                }

                AuthorizationRuleCollection accessRules = accessControlList?.GetAccessRules(true, true, typeof(SecurityIdentifier));
                if (accessRules == null)
                    return false;

                foreach (FileSystemAccessRule rule in accessRules.Cast<FileSystemAccessRule>().Where(rule => (rule.FileSystemRights & FileSystemRights.Delete) == FileSystemRights.Delete))
                {
                    switch (rule.AccessControlType)
                    {
                        case AccessControlType.Allow:
                            deleteAllow = true;
                            break;
                        case AccessControlType.Deny:
                            deleteDeny = true;
                            break;
                    }
                }

                return deleteAllow && !deleteDeny;
            }
        }

        public string Checksum { get; private set; }

        public string Artist { get; }
        public string Title { get; }
        public uint Year { get; }
        public string Genre { get; }
        public string Album { get; }
        public TimeSpan Duration { get; }
        public uint TrackNo { get; }
        public int Bitrate { get; }
        public string TagsChecksum { get; private set; }

        public FileEntry()
        {
            _fileInfo = null;
        }

        public FileEntry(FileInfo fi, bool compareMusicTags)
        {
            _fileInfo = fi;

            if (!compareMusicTags)
                return;

            File file = GetTags();

            if ((HasAudioTags) && file.Tag.IsEmpty)
                HasAudioTags = false;

            if (!HasAudioTags)
                return;

            if (file.Tag.AlbumArtists.Length == 0 && file.Tag.Performers.Length > 0)
                Artist = string.Join(",", file.Tag.Performers);
            else if (file.Tag.Performers.Length == 0 && file.Tag.AlbumArtists.Length > 0)
                Artist = string.Join(",", file.Tag.AlbumArtists);
            else
                Artist = string.Empty;

            if (!string.IsNullOrEmpty(file.Tag.Title))
                Title = file.Tag.Title;

            if (file.Tag.Year > 0)
                Year = file.Tag.Year;

            if (file.Tag.Genres.Length > 0)
                Genre = string.Join(",", file.Tag.Genres);

            if (!string.IsNullOrEmpty(file.Tag.Album))
                Album = file.Tag.Album;

            if (file.Properties.Duration != TimeSpan.Zero)
                Duration = file.Properties.Duration;

            if (file.Tag.Track > 0)
                TrackNo = file.Tag.Track;

            if (file.Properties.AudioBitrate > 0)
                Bitrate = file.Properties.AudioBitrate;

            if (string.IsNullOrEmpty(Artist) &&
                string.IsNullOrEmpty(Title) &&
                Year <= 0 &&
                string.IsNullOrEmpty(Genre) &&
                string.IsNullOrEmpty(Album) &&
                Duration.TotalSeconds == 0 &&
                TrackNo <= 0 &&
                Bitrate <= 0)
                HasAudioTags = false;
        }

        /// <summary>
        /// Gets audio tags
        /// </summary>
        /// <remarks>The direct constructor calls are nessecary as using System.Activator or System.Reflection causes Visual Studio debugger to ignore the try-catch block</remarks>
        /// <returns></returns>
        private File GetTags()
        {
            File file = null;
            string ext = Path.GetExtension(FilePath);

            if (string.IsNullOrEmpty(ext))
                return null;

            ext = ext.Substring(1).ToLower();

            if (!Scan.ValidAudioFiles.Contains(ext))
                return null;

            try
            {
                File.LocalFileAbstraction abstraction = new File.LocalFileAbstraction(FilePath);
                ReadStyle propertiesStyle = ReadStyle.Average;

                switch (ext)
                {
                    case "aac":
                    {
                        file = new CommonTools.TagLib.Aac.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "aif":
                    {
                        file = new CommonTools.TagLib.Aiff.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "ape":
                    {
                        file = new CommonTools.TagLib.Ape.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "wma":
                    {
                        file = new CommonTools.TagLib.Asf.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "aa":
                    case "aax":
                    {
                        file = new CommonTools.TagLib.Audible.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "flac":
                    {
                        file = new CommonTools.TagLib.Flac.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "mka":
                    {
                        file = new CommonTools.TagLib.Matroska.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "mpc":
                    case "mp+":
                    case "mpp":
                    {
                        file = new CommonTools.TagLib.MusePack.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "mp4":
                    case "m4a":
                    {
                        file = new CommonTools.TagLib.Mpeg4.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "ogg":
                    case "oga":
                    {
                        file = new CommonTools.TagLib.Ogg.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "wav":
                    {
                        file = new CommonTools.TagLib.Riff.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "wv":
                    {
                        file = new CommonTools.TagLib.WavPack.File(abstraction, propertiesStyle);
                        break;
                    }
                    case "mp3":
                    case "m2a":
                    case "mp2":
                    case "mp1":
                    {
                        file = new AudioFile(abstraction, propertiesStyle);
                        break;
                    }
                }

                if (file != null)
                    HasAudioTags = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following exception occurred: " + ex.Message);
            }

            return file;
        }

        public void GetTagsChecksum(UserOptions options)
        {
            if (!HasAudioTags)
                return;

            string md5String = string.Empty;

            using (MemoryStream memStream = new MemoryStream())
            {
                if (options.MusicTagAlbum.GetValueOrDefault() && !string.IsNullOrEmpty(Album))
                {
                    byte[] bufferHash = GetMD5Sum(Album);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagArtist.GetValueOrDefault() && !string.IsNullOrEmpty(Artist))
                {
                    byte[] bufferHash = GetMD5Sum(Artist);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagBitRate.GetValueOrDefault() && Bitrate > 0)
                {
                    string bitRate = Convert.ToString(Bitrate);
                    byte[] bufferHash = GetMD5Sum(bitRate);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagDuration.GetValueOrDefault() && Duration != TimeSpan.Zero)
                {
                    string duration = Duration.ToString();
                    byte[] bufferHash = GetMD5Sum(duration);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagGenre.GetValueOrDefault() && !string.IsNullOrEmpty(Genre))
                {
                    byte[] bufferHash = GetMD5Sum(Genre);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagTitle.GetValueOrDefault() && !string.IsNullOrEmpty(Title))
                {
                    byte[] bufferHash = GetMD5Sum(Title);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagTrackNo.GetValueOrDefault() && TrackNo > 0)
                {
                    string trackNo = Convert.ToString(TrackNo);
                    byte[] bufferHash = GetMD5Sum(trackNo);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagYear.GetValueOrDefault() && Year > 0)
                {
                    string year = Convert.ToString(Year);
                    byte[] bufferHash = GetMD5Sum(year);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                memStream.Seek(0, SeekOrigin.Begin);

                if (memStream.Length == 0)
                {
                    HasAudioTags = false;
                    TagsChecksum = string.Empty;

                    return;
                }

                byte[] md5Bytes = GetMD5Sum(memStream.ToArray());
                md5String = md5Bytes.Aggregate(md5String, (current, b) => current + b.ToString("x2"));
            }

            TagsChecksum = md5String;
        }

        private static byte[] GetMD5Sum(byte[] value)
        {
            byte[] hash = { };

            if (value.Length > 0)
            {
                using (var md5 = MD5.Create())
                {
                    hash = md5.ComputeHash(value);
                }
            }

            return hash;
        }

        private static byte[] GetMD5Sum(string value)
        {
            return GetMD5Sum(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Gets checksum of filename (if it includeFilename is true) and file contents
        /// </summary>
        /// <param name="algorithm">Hash algorithm to use (this is not the same as System.Security.Cryptography.HashAlgorithm)</param>
        /// <param name="includeFilename">If true, includes filename when calculating hash</param>
        public void GetChecksum(HashAlgorithm.Algorithms algorithm, bool includeFilename = false)
        {
            string checksum = string.Empty;

            switch (algorithm)
            {
                case HashAlgorithm.Algorithms.CRC32:
                    checksum = CalculateCRC32(includeFilename);
                    break;
                case HashAlgorithm.Algorithms.MD5:
                    checksum = CalculateHash(includeFilename, MD5.Create());
                    break;
                case HashAlgorithm.Algorithms.SHA1:
                    checksum = CalculateHash(includeFilename, SHA1.Create());
                    break;
                case HashAlgorithm.Algorithms.SHA256:
                    checksum = CalculateHash(includeFilename, SHA256.Create());
                    break;
                case HashAlgorithm.Algorithms.SHA512:
                    checksum = CalculateHash(includeFilename, SHA256.Create());
                    break;
            }

            if (!string.IsNullOrEmpty(checksum))
                Checksum = checksum;
        }

        /// <summary>
        /// Gets FileStream for file
        /// </summary>
        /// <returns>FileStream or null if it couldn't be opened</returns>
        private FileStream GetFileStream()
        {
            FileStream fileStream = null;

            try
            {
                if (_fileInfo != null)
                    fileStream = _fileInfo.OpenRead();
                else if (!string.IsNullOrEmpty(_filePath))
                    fileStream = System.IO.File.OpenRead(_filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An error occurred ({0}) trying to open a FileStream for a file ({1})", ex.Message, _filePath);
            }

            fileStream?.Seek(0, SeekOrigin.Begin);

            return fileStream;
        }

        /// <summary>
        /// Calculate hash using CRC32
        /// </summary>
        /// <param name="includeFilename">If true, the filename is including when computing the hash</param>
        /// <returns>A string representation of the computed hash</returns>
        private string CalculateCRC32(bool includeFilename)
        {
            uint polynomial = 0xedb88320u;
            uint seed = 0xffffffffu;
            uint i;

            // Initialize table
            uint[] table = new uint[256];
            for (i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry = entry >> 1;
                }

                table[i] = entry;
            }

            var crc = seed;

            if (includeFilename)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FilePath);

                if (fileNameWithoutExtension != null)
                {
                    byte[] fileNameNoExt = Encoding.UTF8.GetBytes(fileNameWithoutExtension.ToLower());

                    if (fileNameNoExt.Length > 0)
                    {
                        crc = fileNameNoExt.Aggregate(crc, (current, b) => (current >> 8) ^ table[b ^ current & 0xff]);
                    }
                }
            }

            try
            {
                using (FileStream fileStream = GetFileStream())
                {
                    if (fileStream == null)
                        return string.Empty;

                    for (i = 0; i < fileStream.Length; i++)
                    {
                        byte b = (byte)fileStream.ReadByte();
                        crc = (crc >> 8) ^ table[b ^ crc & 0xff];
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine("The following error occurred trying to compute hash of file contents:" + ex.Message);

                return string.Empty;
            }
            

            return (~(crc)).ToString();
        }

        /// <summary>
        /// Calculate hash using SHA512
        /// </summary>
        /// <param name="includeFilename">If true, the filename is including when computing the hash</param>
        /// <returns>A string representation of the computed hash</returns>
        private string CalculateHash(bool includeFilename, System.Security.Cryptography.HashAlgorithm algo)
        {
            string hash = string.Empty;

            using (MemoryStream memStream = new MemoryStream())
            {
                if (includeFilename)
                    AddFilenameHash(memStream, algo);

                try
                {
                    using (FileStream fileStream = GetFileStream())
                    {
                        if (fileStream == null)
                            return hash;

                        byte[] hashFile = algo.ComputeHash(fileStream);

                        memStream.Write(hashFile, 0, hashFile.Length);
                    }
                }
                catch (IOException ex)
                {
                    Debug.WriteLine("The following error occurred trying to compute hash of file contents:" + ex.Message);

                    return hash;
                }

                memStream.Seek(0, SeekOrigin.Begin);

                return CalculateHash(algo, memStream);
            }
        }

        /// <summary>
        /// Calculates hash and returns it in string format
        /// </summary>
        /// <param name="algo">System.Security.Cryptography.HashAlgorithm to use</param>
        /// <param name="stream">Stream to read from</param>
        /// <returns>Hash in string format</returns>
        private static string CalculateHash(System.Security.Cryptography.HashAlgorithm algo, Stream stream)
        {
            string hash = string.Empty;

            var hashBytes = algo.ComputeHash(stream);

            return hashBytes.Aggregate(hash, (current, b) => current + b.ToString("x2"));
        }

        /// <summary>
        /// Calculates the hash of the filename using the specified HashAlgorithm and adds it to the MemoryStream
        /// </summary>
        /// <param name="memStream">MemoryStream containing hash bytes</param>
        /// <param name="algo">HashAlgorithm to compute hash (MD5, SHA1, SHA256, etc)</param>
        private void AddFilenameHash(MemoryStream memStream, System.Security.Cryptography.HashAlgorithm algo)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FilePath);

            if (fileNameWithoutExtension == null)
                return;

            byte[] fileNameNoExt = Encoding.UTF8.GetBytes(fileNameWithoutExtension.ToLower());

            if (fileNameNoExt.Length <= 0)
                return;

            byte[] hashFileName = algo.ComputeHash(fileNameNoExt);

            if (hashFileName.Length > 0)
                memStream.Write(hashFileName, 0, hashFileName.Length);
        }
    }
}

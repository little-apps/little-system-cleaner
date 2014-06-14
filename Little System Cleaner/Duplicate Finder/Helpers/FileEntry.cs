using Little_System_Cleaner.Duplicate_Finder.Controls;
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
        private bool _hasAudioTags = false;

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

        public string Artist { get; private set; }
        public string Title { get; private set; }
        public uint Year { get; private set; }
        public string Genre { get; private set; }
        public string Album { get; private set; }
        public TimeSpan Duration { get; private set; }
        public uint TrackNo { get; private set; }
        public int Bitrate { get; private set; }
        public string TagsChecksum { get; private set; }

        public FileEntry()
        {
            this._fileInfo = null;
        }

        public FileEntry(FileInfo fi, bool compareMusicTags)
        {
            this._fileInfo = fi;

            if (compareMusicTags)
            {
                CommonTools.TagLib.File file = this.GetTags();

                if ((this.HasAudioTags) && file.Tag.IsEmpty)
                    this._hasAudioTags = false;

                if (this.HasAudioTags)
                {
                    if (file.Tag.AlbumArtists.Length == 0 && file.Tag.Performers.Length > 0)
                        this.Artist = string.Join(",", file.Tag.Performers);
                    else if (file.Tag.Performers.Length == 0 && file.Tag.AlbumArtists.Length > 0)
                        this.Artist = string.Join(",", file.Tag.AlbumArtists);
                    else
                        this.Artist = string.Empty;

                    if (!string.IsNullOrEmpty(file.Tag.Title))
                        this.Title = file.Tag.Title;

                    if (file.Tag.Year > 0)
                        this.Year = file.Tag.Year;

                    if (file.Tag.Genres.Length > 0)
                        this.Genre = string.Join(",", file.Tag.Genres);

                    if (!string.IsNullOrEmpty(file.Tag.Album))
                        this.Album = file.Tag.Album;

                    if (file.Properties.Duration != TimeSpan.Zero)
                        this.Duration = file.Properties.Duration;

                    if (file.Tag.Track > 0)
                        this.TrackNo = file.Tag.Track;

                    if (file.Properties.AudioBitrate > 0)
                        this.Bitrate = file.Properties.AudioBitrate;

                    if (string.IsNullOrEmpty(this.Artist) &&
                        string.IsNullOrEmpty(this.Title) &&
                        this.Year <= 0 &&
                        string.IsNullOrEmpty(this.Genre) &&
                        string.IsNullOrEmpty(this.Album) &&
                        this.Duration.TotalSeconds == 0 &&
                        this.TrackNo <= 0 &&
                        this.Bitrate <= 0)
                        this._hasAudioTags = false;
                }
            }

        }

        /// <summary>
        /// Gets audio tags
        /// </summary>
        /// <remarks>The direct constructor calls are nessecary as using System.Activator or System.Reflection causes Visual Studio debugger to ignore the try-catch block</remarks>
        /// <returns></returns>
        private CommonTools.TagLib.File GetTags()
        {
            CommonTools.TagLib.File file = null;
            string ext = Path.GetExtension(this.FilePath);

            if (!string.IsNullOrEmpty(ext))
            {
                ext = ext.Substring(1).ToLower();

                if (Scan.validAudioFiles.Contains(ext))
                {
                    try
                    {
                        CommonTools.TagLib.File.LocalFileAbstraction abstraction = new CommonTools.TagLib.File.LocalFileAbstraction(this.FilePath);
                        CommonTools.TagLib.ReadStyle propertiesStyle = CommonTools.TagLib.ReadStyle.Average;

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
                                    file = new CommonTools.TagLib.Mpeg.AudioFile(abstraction, propertiesStyle);
                                    break;
                                }
                        }

                        if (file != null)
                            this._hasAudioTags = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("The following exception occurred: " + ex.Message);
                    }
                }
            }

            return file;
        }

        public void GetTagsChecksum(UserOptions options)
        {
            if (!this.HasAudioTags)
                return;

            string md5string = string.Empty;

            using (MemoryStream memStream = new MemoryStream())
            {
                if (options.MusicTagAlbum.GetValueOrDefault() && !string.IsNullOrEmpty(this.Album))
                {
                    byte[] bufferHash = this.GetMD5Sum(this.Album);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagArtist.GetValueOrDefault() && !string.IsNullOrEmpty(this.Artist))
                {
                    byte[] bufferHash = this.GetMD5Sum(this.Artist);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }
                if (options.MusicTagBitRate.GetValueOrDefault() && this.Bitrate > 0)
                {
                    string bitRate = Convert.ToString(this.Bitrate);
                    byte[] bufferHash = this.GetMD5Sum(bitRate);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }
                if (options.MusicTagDuration.GetValueOrDefault() && this.Duration != TimeSpan.Zero)
                {
                    string duration = this.Duration.ToString();
                    byte[] bufferHash = this.GetMD5Sum(duration);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }
                if (options.MusicTagGenre.GetValueOrDefault() && !string.IsNullOrEmpty(this.Genre))
                {
                    byte[] bufferHash = this.GetMD5Sum(this.Genre);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }
                if (options.MusicTagTitle.GetValueOrDefault() && !string.IsNullOrEmpty(this.Title))
                {
                    byte[] bufferHash = this.GetMD5Sum(this.Title);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }
                if (options.MusicTagTrackNo.GetValueOrDefault() && this.TrackNo > 0)
                {
                    string trackNo = Convert.ToString(this.TrackNo);
                    byte[] bufferHash = this.GetMD5Sum(trackNo);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }
                if (options.MusicTagYear.GetValueOrDefault() && this.Year > 0)
                {
                    string year = Convert.ToString(this.Year);
                    byte[] bufferHash = this.GetMD5Sum(year);
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                memStream.Seek(0, SeekOrigin.Begin);

                if (memStream.Length == 0)
                {
                    this._hasAudioTags = false;
                    this.TagsChecksum = string.Empty;
                    return;
                }

                byte[] md5Bytes = this.GetMD5Sum(memStream.ToArray());
                
                foreach (byte b in md5Bytes)
                {
                    md5string += b.ToString("x2");
                }
            }

            this.TagsChecksum = md5string;
        }

        private byte[] GetMD5Sum(byte[] value)
        {
            byte[] hash = new byte[] { };

            if (value.Length > 0)
            {
                using (var md5 = MD5.Create())
                {
                    hash = md5.ComputeHash(value);
                }
            }

            return hash;
        }

        private byte[] GetMD5Sum(string value)
        {
            return this.GetMD5Sum(Encoding.UTF8.GetBytes(value));
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using CommonTools.TagLib;
using CommonTools.TagLib.Mpeg;
using Little_System_Cleaner.Duplicate_Finder.Controls;
using Little_System_Cleaner.Misc;
using File = CommonTools.TagLib.File;

namespace Little_System_Cleaner.Duplicate_Finder.Helpers
{
    public class FileEntry
    {
        private readonly FileInfo _fileInfo;

        private string _filePath;
        private long _fileSize;

        public FileEntry()
        {
            _fileInfo = null;
        }

        public FileEntry(FileInfo fi, UserOptions.ScanMethods scanMethod)
        {
            _fileInfo = fi;

            switch (scanMethod)
            {
                case UserOptions.ScanMethods.MusicTags:
                    var file = GetTags();

                    if (HasAudioTags && file.Tag.IsEmpty)
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
                    break;
                case UserOptions.ScanMethods.Images:
                    IsImage();
                    break;
            }
        }

        public string FileName => _fileInfo?.Name ?? Path.GetFileName(_filePath);

        public string FilePath
        {
            get { return _fileInfo?.ToString() ?? _filePath; }
            set { _filePath = value; }
        }

        public long FileSize
        {
            get { return _fileInfo?.Length ?? _fileSize; }
            set { _fileSize = value; }
        }

        public Size ThumbnailSize { get; } = new Size(16, 16);
        
        public bool HasAudioTags { get; private set; }



        public bool IsDeleteable
        {
            get
            {
                FileSecurity accessControlList;
                var deleteAllow = false;
                var deleteDeny = false;

                try
                {
                    accessControlList = System.IO.File.GetAccessControl(FilePath);
                }
                catch (Exception)
                {
                    return false;
                }

                var accessRules = accessControlList?.GetAccessRules(true, true, typeof (SecurityIdentifier));
                if (accessRules == null)
                    return false;

                foreach (
                    var rule in
                        accessRules.Cast<FileSystemAccessRule>()
                            .Where(rule => (rule.FileSystemRights & FileSystemRights.Delete) == FileSystemRights.Delete)
                    )
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

        private static readonly Dictionary<string, byte[]> ImageFileFormats = new Dictionary<string, byte[]>
        {
            {"jpg", new byte[] {0xFF, 0xD8}},
            {"bmp", new byte[] {0x42, 0x4D}},
            {"gif", new byte[] {0x47, 0x49, 0x46}},
            {"png", new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}}
        };

        private static readonly int ImageFileFormatsMaxLen = ImageFileFormats.Values.Max(bytes => bytes.Length);

        private bool? _isImage;

        public bool IsImage()
        {
            if (_isImage != null)
                return _isImage.GetValueOrDefault();

            FileStream fileStream = null;

            try
            {
                fileStream = _fileInfo.OpenRead();

                var startBytes = new byte[ImageFileFormatsMaxLen];

                fileStream.Read(startBytes, 0, startBytes.Length);

                _isImage =
                    ImageFileFormats.Where(fileFormat => fileStream.Length >= fileFormat.Value.Length)
                        .Any(
                            fileFormat =>
                                startBytes.Take(fileFormat.Value.Length).SequenceEqual(fileFormat.Value));
            }
            catch
            {
                // Unable to determine if image or not
                _isImage = null;
            }
            finally
            {
                fileStream?.Close();
            }

            return _isImage.GetValueOrDefault();
        }

        private Bitmap GetImage()
        {
            var ext = Path.GetExtension(FilePath);

            if (string.IsNullOrEmpty(ext))
                return null;

            ext = ext.Substring(1).ToLower();

            if (!Scan.ValidImageFiles.Contains(ext))
                return null;
            
            Image img = null;

            try
            {
                img = (Image) Image.FromFile(FilePath).Clone();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("The following exception occurred: " + ex.Message);
            }
            
            // Image is valid if img is not null

            return img != null ? ResizeImage(img, ThumbnailSize.Width, ThumbnailSize.Height) : null;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        
        /// <summary>
        ///     Compares current file entry with other file entry image
        /// </summary>
        /// <param name="otherFileEntry">Other file entry</param>
        /// <returns>Decimal value between 0 and 1 of percentage of same pixels (with 0 being no pixels)</returns>
        public decimal CompareImages(FileEntry otherFileEntry)
        {
            if (this == otherFileEntry)
                return decimal.MinusOne;

            if (Pixels.Count == 0)
            {
                var imageFirst = GetImage();
                
                if (imageFirst == null)
                    return decimal.Zero;

                Pixels.AddRange(GetPixels(imageFirst));
            }

            if (otherFileEntry.Pixels.Count == 0)
            {
                var imageSecond = otherFileEntry.GetImage();

                if (imageSecond == null)
                    return decimal.Zero;

                otherFileEntry.Pixels.AddRange(GetPixels(imageSecond));
            }
            
            if (Pixels.Count != otherFileEntry.Pixels.Count)
                return 0;

            var sameColor =
                Pixels.Zip(otherFileEntry.Pixels, (colorFirst, colorSecond) => colorFirst == colorSecond)
                    .Count(x => x);

            return sameColor/(decimal) Pixels.Count;
        }

        public List<Color> Pixels { get; private set; } = new List<Color>();

        private static IEnumerable<Color> GetPixels(Bitmap bitmap)
        {

            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var pixelCount = bitmap.Width * bitmap.Height;
            var depth = Image.GetPixelFormatSize(bitmap.PixelFormat);

            // create byte array to copy pixel values
            var pixelsFirst = new byte[pixelCount * (depth / 8)];

            // Copy data from pointer to array
            Marshal.Copy(bitmapData.Scan0, pixelsFirst, 0, pixelsFirst.Length);

            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var color = Color.Empty;

                    // Get color components count
                    var cCount = depth / 8;

                    // Get start index of the specified pixel
                    var i = (y * bitmap.Width + x) * cCount;

                    if (i > pixelsFirst.Length - cCount)
                        throw new IndexOutOfRangeException();

                    switch (depth)
                    {
                        case 32:
                            {
                                var b = pixelsFirst[i];
                                var g = pixelsFirst[i + 1];
                                var r = pixelsFirst[i + 2];
                                var a = pixelsFirst[i + 3]; // a
                                color = Color.FromArgb(a, r, g, b);
                                break;
                            }
                        case 24:
                            {
                                var b = pixelsFirst[i];
                                var g = pixelsFirst[i + 1];
                                var r = pixelsFirst[i + 2];
                                color = Color.FromArgb(r, g, b);
                            }
                            break;
                        case 8:
                            {
                                var c = pixelsFirst[i];
                                color = Color.FromArgb(c, c, c);
                                break;
                            }
                    }

                    yield return color;
                }
            }

            bitmap.UnlockBits(bitmapData);
        } 

        /// <summary>
        ///     Gets audio tags
        /// </summary>
        /// <remarks>
        ///     The direct constructor calls are nessecary as using System.Activator or System.Reflection causes Visual Studio
        ///     debugger to ignore the try-catch block
        /// </remarks>
        /// <returns></returns>
        private File GetTags()
        {
            File file = null;
            var ext = Path.GetExtension(FilePath);

            if (string.IsNullOrEmpty(ext))
                return null;

            ext = ext.Substring(1).ToLower();

            if (!Scan.ValidAudioFiles.Contains(ext))
                return null;

            try
            {
                var abstraction = new File.LocalFileAbstraction(FilePath);
                var propertiesStyle = ReadStyle.Average;

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

            string hashString;
            var hashAlgorithm = GetHashAlgorithm(options.HashAlgorithm.Algorithm);

            using (var memStream = new MemoryStream())
            {
                if (options.MusicTagAlbum.GetValueOrDefault() && !string.IsNullOrEmpty(Album))
                {
                    var bufferHash = CalculateHashBytes(hashAlgorithm, Encoding.UTF8.GetBytes(Album));
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagArtist.GetValueOrDefault() && !string.IsNullOrEmpty(Artist))
                {
                    var bufferHash = CalculateHashBytes(hashAlgorithm, Encoding.UTF8.GetBytes(Artist));
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagBitRate.GetValueOrDefault() && Bitrate > 0)
                {
                    var bitRate = Convert.ToString(Bitrate);
                    var bufferHash = CalculateHashBytes(hashAlgorithm, Encoding.UTF8.GetBytes(bitRate));
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagDuration.GetValueOrDefault() && Duration != TimeSpan.Zero)
                {
                    var duration = Duration.ToString();
                    var bufferHash = CalculateHashBytes(hashAlgorithm, Encoding.UTF8.GetBytes(duration));
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagGenre.GetValueOrDefault() && !string.IsNullOrEmpty(Genre))
                {
                    var bufferHash = CalculateHashBytes(hashAlgorithm, Encoding.UTF8.GetBytes(Genre));
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagTitle.GetValueOrDefault() && !string.IsNullOrEmpty(Title))
                {
                    var bufferHash = CalculateHashBytes(hashAlgorithm, Encoding.UTF8.GetBytes(Title));
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagTrackNo.GetValueOrDefault() && TrackNo > 0)
                {
                    var trackNo = Convert.ToString(TrackNo);
                    var bufferHash = CalculateHashBytes(hashAlgorithm, Encoding.UTF8.GetBytes(trackNo));
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                if (options.MusicTagYear.GetValueOrDefault() && Year > 0)
                {
                    var year = Convert.ToString(Year);
                    var bufferHash = CalculateHashBytes(hashAlgorithm, Encoding.UTF8.GetBytes(year));
                    memStream.Write(bufferHash, 0, bufferHash.Length);
                }

                memStream.Seek(0, SeekOrigin.Begin);

                if (memStream.Length == 0)
                {
                    HasAudioTags = false;
                    TagsChecksum = string.Empty;

                    return;
                }

                hashString = CalculateHashString(hashAlgorithm, memStream);
            }

            TagsChecksum = hashString;
        }

        private System.Security.Cryptography.HashAlgorithm GetHashAlgorithm(HashAlgorithm.Algorithms algorithm)
        {
            System.Security.Cryptography.HashAlgorithm hashAlgorithm;

            switch (algorithm)
            {
                case HashAlgorithm.Algorithms.CRC32:
                    hashAlgorithm = new CRC32();
                    break;
                case HashAlgorithm.Algorithms.MD5:
                    hashAlgorithm = MD5.Create();
                    break;
                case HashAlgorithm.Algorithms.SHA1:
                    hashAlgorithm = SHA1.Create();
                    break;
                case HashAlgorithm.Algorithms.SHA256:
                    hashAlgorithm = SHA256.Create();
                    break;
                case HashAlgorithm.Algorithms.SHA512:
                    hashAlgorithm = SHA512.Create();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }

            return hashAlgorithm;
        }

        /// <summary>
        ///     Gets checksum of filename (if it includeFilename is true) and file contents
        /// </summary>
        /// <param name="algorithm">Hash algorithm to use (this is not the same as System.Security.Cryptography.HashAlgorithm)</param>
        /// <param name="includeFilename">If true, includes filename when calculating hash</param>
        public void GetChecksum(HashAlgorithm.Algorithms algorithm, bool includeFilename = false)
        {
            var hashAlgorithm = GetHashAlgorithm(algorithm);
            var checksum = CalculateHash(includeFilename, hashAlgorithm);

            if (!string.IsNullOrEmpty(checksum))
                Checksum = checksum;
        }

        /// <summary>
        ///     Gets FileStream for file
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
                Debug.WriteLine("An error occurred ({0}) trying to open a FileStream for a file ({1})", ex.Message,
                    _filePath);
            }

            fileStream?.Seek(0, SeekOrigin.Begin);

            return fileStream;
        }

        /// <summary>
        ///     Calculate hash using SHA512
        /// </summary>
        /// <param name="includeFilename">If true, the filename is including when computing the hash</param>
        /// <returns>A string representation of the computed hash</returns>
        private string CalculateHash(bool includeFilename, System.Security.Cryptography.HashAlgorithm algo)
        {
            var hash = string.Empty;

            using (var memStream = new MemoryStream())
            {
                if (includeFilename)
                    AddFilenameHash(memStream, algo);

                try
                {
                    using (var fileStream = GetFileStream())
                    {
                        if (fileStream == null)
                            return hash;

                        var hashFile = algo.ComputeHash(fileStream);

                        memStream.Write(hashFile, 0, hashFile.Length);
                    }
                }
                catch (IOException ex)
                {
                    Debug.WriteLine("The following error occurred trying to compute hash of file contents:" + ex.Message);

                    return hash;
                }

                memStream.Seek(0, SeekOrigin.Begin);

                return CalculateHashString(algo, memStream);
            }
        }

        /// <summary>
        ///     Calculates hash and returns as byte array
        /// </summary>
        /// <param name="algo">System.Security.Cryptography.HashAlgorithm to use</param>
        /// <param name="bytes">Bytes to generate hash from</param>
        /// <returns>Hash in byte array</returns>
        private static byte[] CalculateHashBytes(System.Security.Cryptography.HashAlgorithm algo, byte[] bytes)
        {
            return algo.ComputeHash(bytes);
        }

        /// <summary>
        ///     Calculates hash and returns it in string format
        /// </summary>
        /// <param name="algo">System.Security.Cryptography.HashAlgorithm to use</param>
        /// <param name="stream">Stream to read from</param>
        /// <returns>Hash in string format</returns>
        private static byte[] CalculateHashBytes(System.Security.Cryptography.HashAlgorithm algo, Stream stream)
        {
            return algo.ComputeHash(stream);
        }

        /// <summary>
        ///     Calculates hash and returns it in string format
        /// </summary>
        /// <param name="algo">System.Security.Cryptography.HashAlgorithm to use</param>
        /// <param name="stream">Stream to read from</param>
        /// <returns>Hash in string format</returns>
        private static string CalculateHashString(System.Security.Cryptography.HashAlgorithm algo, Stream stream)
        {
            var hash = string.Empty;

            var hashBytes = CalculateHashBytes(algo, stream);

            return hashBytes.Aggregate(hash, (current, b) => current + b.ToString("x2"));
        }

        /// <summary>
        ///     Calculates the hash of the filename using the specified HashAlgorithm and adds it to the MemoryStream
        /// </summary>
        /// <param name="memStream">MemoryStream containing hash bytes</param>
        /// <param name="algo">HashAlgorithm to compute hash (MD5, SHA1, SHA256, etc)</param>
        private void AddFilenameHash(Stream memStream, System.Security.Cryptography.HashAlgorithm algo)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FilePath);

            if (fileNameWithoutExtension == null)
                return;

            var fileNameNoExt = Encoding.UTF8.GetBytes(fileNameWithoutExtension.ToLower());

            if (fileNameNoExt.Length <= 0)
                return;

            var hashFileName = algo.ComputeHash(fileNameNoExt);

            if (hashFileName.Length > 0)
                memStream.Write(hashFileName, 0, hashFileName.Length);
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }
    }
}
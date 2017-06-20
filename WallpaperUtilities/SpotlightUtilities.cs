using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using static System.Console;
using static System.Drawing.Image;
using static System.IO.Directory;
using static System.IO.File;
using static System.IO.Path;

namespace WallpaperUtilities
{
    /// <summary>
    /// </summary>
    public class SpotlightUtilities
    {
        /// <summary>
        /// </summary>
        private const string HorizontalCode = "Desktop", VerticalCode = "Mobile", TempCode = "CopyAssets";


        /// <summary>
        /// </summary>
        private const int BytesPerKb = 1024;

        /// <summary>
        /// </summary>
        private static readonly Random Rng = new Random();

        /// <summary>
        /// </summary>
        private static readonly string SpotlightLocal;

        /// <summary>
        /// </summary>
        public static readonly string DefaultSavePath;

        /// <summary>
        /// </summary>
        private readonly Bitmap _currentWallpaper;

        private readonly bool _saveOnlyDesktopImages;
        private readonly bool _saveOnlyMobileImages;

        /// <summary>
        /// </summary>
        private readonly string _spotlightBase,
            _spotlight,
            _spotlightAssets,
            _spotlightHorizontal,
            _spotlightVertical,
            _currentWallpaperPath;

        /// <summary>
        /// </summary>
        private readonly int _thresholdBytes;

        /// <summary>
        /// </summary>
        private readonly int _thresholdKb;

        static SpotlightUtilities()
        {
            SpotlightLocal =
                $"{Environment.GetEnvironmentVariable("LOCALAPPDATA")}\\Packages\\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\\LocalState\\Assets";
            DefaultSavePath = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Pictures";
        }

        /// <summary>
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="threshold"></param>
        /// <param name="saveOnlyDesktopImages"></param>
        /// <param name="saveOnlyMobileImages"></param>
        private SpotlightUtilities(string savePath = null, int threshold = 100, bool saveOnlyDesktopImages = false,
            bool saveOnlyMobileImages = false)
        {
            _currentWallpaperPath = Wallpaper.GetCurrentDesktopWallpaperPath();
            using (var stream = Open(_currentWallpaperPath, FileMode.Open))
            {
                _currentWallpaper = (Bitmap)FromStream(stream);
            }
            if (string.IsNullOrWhiteSpace(savePath))
                savePath = DefaultSavePath;
            _spotlightBase = $"{savePath}\\Spotlight";
            _spotlight = $"{_spotlightBase}\\{DateTime.Today.Date:dd-MM-yyyy}";
            _spotlightAssets = $"{_spotlight}\\{TempCode}";
            _spotlightHorizontal = $"{_spotlight}\\{HorizontalCode}";
            _spotlightVertical = $"{_spotlight}\\{VerticalCode}";
            _thresholdKb = threshold;
            _saveOnlyDesktopImages = saveOnlyDesktopImages;
            _saveOnlyMobileImages = saveOnlyMobileImages;
            _thresholdBytes = _thresholdKb * BytesPerKb;
        }

        /// <summary>
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <param name="onlyName"></param>
        /// <returns></returns>
        private static List<string> GetFileList(string rootFolder, bool onlyName = false)
        {
            if (Directory.Exists(rootFolder))
            {
                var rawFileList = EnumerateDirectories(rootFolder) // Daily image directories under base folder
                    .SelectMany(EnumerateDirectories) // Size variant folders under daily image folders
                    .SelectMany(EnumerateFiles); // Image files
                return (onlyName ? rawFileList.Select(file => new FileInfo(file).Name) : rawFileList).ToList();
            }
            return new List<string>();
        }

        /// <exception cref="IOException">
        ///     The directory specified by <paramref name="path" /> is a file.-or-The network name is not
        ///     known.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive). </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path" /> is a zero-length string, contains only white space, or
        ///     contains one or more invalid characters. You can query for invalid characters by using the
        ///     <see cref="M:System.IO.Path.GetInvalidPathChars" /> method.-or-<paramref name="path" /> is prefixed with, or
        ///     contains, only a colon character (:).
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path" /> is null. </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        ///     For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than
        ///     260 characters.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path" /> contains a colon character (:) that is not part of a
        ///     drive label ("C:\").
        /// </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        /// <exception cref="FileNotFoundException">The file does not exist.-or- The Length property is called for a directory. </exception>
        public static void SaveSpotlightImages(string savePath = null, int threshold = 100,
            bool saveOnlyDesktopImages = false, bool saveOnlyMobileImages = false)
        {
            var utilities = new SpotlightUtilities(savePath, threshold, saveOnlyDesktopImages, saveOnlyMobileImages);
            CreateDirectory(utilities._spotlight);
            CreateDirectory(utilities._spotlightAssets);
            if (utilities._saveOnlyMobileImages)
                WriteLine("Only saving mobile images, not creating folder for desktop images.");
            else
                CreateDirectory(utilities._spotlightHorizontal);

            if (utilities._saveOnlyDesktopImages)
                WriteLine("Only saving desktop images, not creating folder for mobile images.");
            else
                CreateDirectory(utilities._spotlightVertical);

            //Clean up temporary files if present
            foreach (var file in EnumerateFiles(utilities._spotlightAssets))
                File.Delete(file);

            var existingFileNames = GetFileList(utilities._spotlightBase, true);
            WriteLine($"Existing files:\n{string.Join("\n", existingFileNames)}\n");
            WriteLine($"There were {existingFileNames.Count} files already present.\n");
          
            var newFiles = 0;
            foreach (var file in EnumerateFiles(SpotlightLocal))
            {
                var fileInfo = new FileInfo(file);

                WriteLine($"Processing file {fileInfo.Name}");

                if (fileInfo.Length < utilities._thresholdBytes)
                {
                    WriteLine(
                        $"File {fileInfo.Name} of size {fileInfo.Length / BytesPerKb} KB < {utilities._thresholdKb} KB skipped.\n");
                    continue;
                }

                if (existingFileNames.Any(savedFileName => savedFileName.StartsWith(fileInfo.Name)))
                {
                    WriteLine($"Existing File {fileInfo.Name} skipped.\n");
                    continue;
                }
                ++newFiles;
                var newFile = Combine(utilities._spotlightAssets, $"{fileInfo.Name}.jpg");
                Copy(file, newFile);
            }
            WriteLine($"New Files = {newFiles}\n");
            WriteLine("Files copied to save folder.\n");
            if (newFiles == 0)
            {
                WriteLine("No new files, cleaning up.");
                Delete(utilities._spotlight, true);
            }
            else
            {
                foreach (var newFile in EnumerateFiles(utilities._spotlightAssets))
                {
                    var fileInfo = new FileInfo(newFile);
                    Image image;
                    using (var stream = Open(newFile, FileMode.Open))
                    {
                        image = FromStream(stream);
                    }
                    var isMobileImage = image.Width <= image.Height;
                    if (isMobileImage && !utilities._saveOnlyDesktopImages)
                    {
                        WriteLine("Saving Mobile Image.");
                        Copy(newFile, Combine(utilities._spotlightVertical, fileInfo.Name));
                    }
                    else if (!isMobileImage && !utilities._saveOnlyMobileImages)
                    {
                        WriteLine("Saving Desktop Image.");
                        Copy(newFile, Combine(utilities._spotlightHorizontal, fileInfo.Name));
                    }
                }

                WriteLine("Files segregated based on dimensions.\n");

                Delete(utilities._spotlightAssets, true);

                WriteLine("Temporary files deleted.\n");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="style"></param>
        /// <param name="temporary"></param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is less than 0.-or-<paramref name="index" /> is
        ///     equal to or greater than <see cref="P:System.Collections.Generic.List`1.Count" />.
        /// </exception>
        public static void SetRandomSpotlightWallpaper(string loadPath = null,
            Wallpaper.Style style = Wallpaper.Style.Stretched, bool temporary = false)
        {
            SetSpotlightWallpaper(spotlightImages => spotlightImages[Rng.Next(spotlightImages.Count)], loadPath, style,
                temporary);
        }

        /// <summary>
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="style"></param>
        /// <param name="temporary"></param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="keySelector" /> is null.</exception>
        public static void SetLatestSpotlightWallpaper(string loadPath = null,
            Wallpaper.Style style = Wallpaper.Style.Stretched, bool temporary = false)
        {
            SetSpotlightWallpaper(spotlightImages => spotlightImages.OrderBy(File.GetLastWriteTime).Last(), loadPath,
                style, temporary);
        }

        /// <summary>
        /// </summary>
        /// <param name="determiner"></param>
        /// <param name="loadPath"></param>
        /// <param name="style"></param>
        /// <param name="temporary"></param>
        private static void SetSpotlightWallpaper(Func<List<string>, string> determiner,
            string loadPath = null, Wallpaper.Style style = Wallpaper.Style.Stretched, bool temporary = false)
        {
            var utilities = new SpotlightUtilities();
            var deletePath = false;
            if (string.IsNullOrWhiteSpace(loadPath))
                if (temporary && (!Directory.Exists(utilities._spotlightBase) ||
                                  GetFileSystemEntries(utilities._spotlightBase).Length == 0))
                {
                    loadPath = $"{GetTempPath()}SpotlightTemp";

                    WriteLine($"Cleaning and using temporary folder: {loadPath}");

                    if (Directory.Exists(loadPath))
                        Delete(loadPath, true);

                    utilities = new SpotlightUtilities(loadPath);
                    SaveSpotlightImages(loadPath);
                    loadPath = utilities._spotlightBase;

                    deletePath = true;
                }
                else
                {
                    loadPath = utilities._spotlightBase;
                }

            WriteLine($"Original wallpaper was at {utilities._currentWallpaperPath}, loading images from {loadPath}");

            var spotlightImages = GetFileList(loadPath);

            spotlightImages = spotlightImages
                .Where(image => !ImageUtilities.ImagesEqual(image, utilities._currentWallpaper)).ToList();

            var imagePath = determiner(spotlightImages);

            Wallpaper.SetDesktopWallpaper(imagePath, style);

            WriteLine($"Wallpaper set to {imagePath}.");
            WriteLine("Cleaning up temporary directories.");

            if (!deletePath) return;
            try
            {
                WriteLine("Deleting temporary files.\n");
                Delete(loadPath, true);
            }
            catch (IOException)
            {
                //silence exception - being unable to delete the file set as wallpaper is expected.    
            }
        }

        /// <summary>
        /// </summary>
        private static class ImageUtilities
        {
            /// <summary>
            /// </summary>
            /// <param name="firstImagePath"></param>
            /// <param name="secondImagePath"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException">
            ///     <paramref name="path" /> is a zero-length string, contains only white space, or
            ///     contains one or more invalid characters as defined by <see cref="F:System.IO.Path.InvalidPathChars" />.
            /// </exception>
            /// <exception cref="ArgumentNullException"><paramref name="path" /> is null. </exception>
            /// <exception cref="PathTooLongException">
            ///     The specified path, file name, or both exceed the system-defined maximum length.
            ///     For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than
            ///     260 characters.
            /// </exception>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode" /> specified an invalid value. </exception>
            /// <exception cref="NotSupportedException"><paramref name="path" /> is in an invalid format. </exception>
            /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive). </exception>
            /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
            /// <exception cref="UnauthorizedAccessException">
            ///     <paramref name="path" /> specified a file that is read-only.-or- This
            ///     operation is not supported on the current platform.-or- <paramref name="path" /> specified a directory.-or- The
            ///     caller does not have the required permission. -or-<paramref name="mode" /> is
            ///     <see cref="F:System.IO.FileMode.Create" /> and the specified file is a hidden file.
            /// </exception>
            /// <exception cref="FileNotFoundException">The file specified in <paramref name="path" /> was not found. </exception>
            internal static bool ImagesEqual(string firstImagePath, string secondImagePath)
            {
                Bitmap secondImage;
                using (var stream = Open(secondImagePath, FileMode.Open))
                {
                    secondImage = (Bitmap)FromStream(stream);
                }
                return ImagesEqual(firstImagePath, secondImage);
            }

            /// <summary>
            /// </summary>
            /// <param name="firstImagePath"></param>
            /// <param name="secondImage"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentException">
            ///     <paramref name="path" /> is a zero-length string, contains only white space, or
            ///     contains one or more invalid characters as defined by <see cref="F:System.IO.Path.InvalidPathChars" />.
            /// </exception>
            /// <exception cref="ArgumentNullException"><paramref name="path" /> is null. </exception>
            /// <exception cref="PathTooLongException">
            ///     The specified path, file name, or both exceed the system-defined maximum length.
            ///     For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than
            ///     260 characters.
            /// </exception>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode" /> specified an invalid value. </exception>
            /// <exception cref="NotSupportedException"><paramref name="path" /> is in an invalid format. </exception>
            /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive). </exception>
            /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
            /// <exception cref="UnauthorizedAccessException">
            ///     <paramref name="path" /> specified a file that is read-only.-or- This
            ///     operation is not supported on the current platform.-or- <paramref name="path" /> specified a directory.-or- The
            ///     caller does not have the required permission. -or-<paramref name="mode" /> is
            ///     <see cref="F:System.IO.FileMode.Create" /> and the specified file is a hidden file.
            /// </exception>
            /// <exception cref="FileNotFoundException">The file specified in <paramref name="path" /> was not found. </exception>
            internal static bool ImagesEqual(string firstImagePath, Bitmap secondImage)
            {
                Bitmap firstImage;
                using (var stream = Open(firstImagePath, FileMode.Open))
                {
                    firstImage = (Bitmap)FromStream(stream);
                }
                return ImagesEqual(firstImage, secondImage);
            }

            /// <summary>
            /// </summary>
            /// <param name="firstImage"></param>
            /// <param name="secondImage"></param>
            /// <returns></returns>
            /// <exception cref="Exception">The operation failed.</exception>
            /// <exception cref="ArgumentOutOfRangeException">
            ///     <paramref name="x" /> is less than 0, or greater than or equal to
            ///     <see cref="P:System.Drawing.Image.Width" />. -or-<paramref name="y" /> is less than 0, or greater than or equal to
            ///     <see cref="P:System.Drawing.Image.Height" />.
            /// </exception>
            private static bool ImagesEqual(Bitmap firstImage, Bitmap secondImage)
            {
                //Test to see if we have the same size of image
                if (firstImage.Size != secondImage.Size)
                    return false;
                for (var x = 0; x < firstImage.Width; x++)
                    for (var y = 0; y < firstImage.Height; y++)
                        if (firstImage.GetPixel(x, y) != secondImage.GetPixel(x, y)) return false;
                return true;
            }
        }
    }
}
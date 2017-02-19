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
    public class SpotlightUtilities
    {
        private readonly int _thresholdKb, _thresholdBytes;
        private static readonly Random Rng = new Random();
        private readonly string _spotlightBase, _spotlight, _spotlightAssets, _spotlightHorizontal, _spotlightVertical, _currentWallpaperPath;
        private readonly Bitmap _currentWallpaper;
        public static readonly string SpotlightLocal, DefaultSavePath;

        static SpotlightUtilities()
        {
            SpotlightLocal = $"{Environment.GetEnvironmentVariable("LOCALAPPDATA")}\\Packages\\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\\LocalState\\Assets";
            DefaultSavePath = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Pictures";
        }

        private const string HorizontalCode = "Desktop", VerticalCode = "Mobile", TempCode = "CopyAssets";
        private SpotlightUtilities(string savePath = null, int threshold = 100)
        {
            _currentWallpaperPath = Wallpaper.GetCurrentDesktopWallpaperPath();
            using (var stream = Open(_currentWallpaperPath, FileMode.Open))
            {
                _currentWallpaper = (Bitmap)FromStream(stream);
            }
            if (string.IsNullOrWhiteSpace(savePath))
            {
                savePath = DefaultSavePath;
            }
            _spotlightBase = $"{savePath}\\Spotlight";
            _spotlight = $"{_spotlightBase}\\{DateTime.Today.Date.ToString("dd-MM-yyyy")}";
            _spotlightAssets = $"{_spotlight}\\{TempCode}";
            _spotlightHorizontal = $"{_spotlight}\\{HorizontalCode}";
            _spotlightVertical = $"{_spotlight}\\{VerticalCode}";
            _thresholdKb = threshold;
            _thresholdBytes = _thresholdKb * BytesPerKb;
        }

        private static List<string> GetFileList(string rootFolder, bool onlyName = false, bool onlyLandscape = true)
        {
            var savedFiles = new List<string>();
            if (Directory.Exists(rootFolder))
            {
                savedFiles.AddRange(from folder in EnumerateDirectories(rootFolder) //get daily images folders
                                    from subfolder in (onlyLandscape ?
                                        new List<string> { $"{folder}\\{HorizontalCode}" } //get desktop folder
                                        : EnumerateDirectories(folder)) //get desktop and mobile folders
                                    from file in EnumerateFiles(subfolder) //get all image file names in these folders
                                    select onlyName ? new FileInfo(file).Name : file);
            }
            return new HashSet<string>(savedFiles).ToList();//eliminate duplicates
        }
        public static void SaveSpotlightImages(string savePath = null, int threshold = 100)
        {
            var utilities = new SpotlightUtilities(savePath, threshold);
            var savedFiles = GetFileList(utilities._spotlightBase, true, false);
            CreateDirectory(utilities._spotlight);
            CreateDirectory(utilities._spotlightAssets);
            CreateDirectory(utilities._spotlightHorizontal);
            CreateDirectory(utilities._spotlightVertical);

            WriteLine($"Existing files:\n{string.Join("\n", savedFiles)}\n\n");

            //Clean up temporary files if present
            if (EnumerateFiles(utilities._spotlightAssets).Any())
            {
                Delete(utilities._spotlightAssets, true);
            }
            var newFiles = 0;
            foreach (var file in EnumerateFiles(SpotlightLocal))
            {
                var fileInfo = new FileInfo(file);

                WriteLine($"Processing file {fileInfo.Name}");

                if (fileInfo.Length < utilities._thresholdBytes)
                {
                    WriteLine($"File {fileInfo.Name} of size {fileInfo.Length / BytesPerKb} KB < {utilities._thresholdKb} KB skipped.\n");
                    continue;
                }

                if (savedFiles.Any(savedFile => new FileInfo(savedFile).Name.Contains(fileInfo.Name)))
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
                Delete(utilities._spotlightAssets, true);
                Delete(utilities._spotlightHorizontal, true);
                Delete(utilities._spotlightVertical, true);
            }

            foreach (var newFile in EnumerateFiles(utilities._spotlightAssets))
            {
                var fileInfo = new FileInfo(newFile);
                Image image;
                using (var stream = Open(newFile, FileMode.Open))
                {
                    image = FromStream(stream);
                }
                Copy(newFile,
                        image.Width >= image.Height
                            ? Combine(utilities._spotlightHorizontal, fileInfo.Name)
                            : Combine(utilities._spotlightVertical, fileInfo.Name));
            }

            WriteLine("Files segregated based on dimensions.\n");

            Delete(utilities._spotlightAssets, true);

            WriteLine("Temporary files deleted.\n");
        }


        private const int BytesPerKb = 1024;

        public static void SetRandomSpotlightWallpaper(string loadPath = null, Wallpaper.Style style = Wallpaper.Style.Stretched, bool temporary = false)
        {
            SetSpotlightWallpaper(spotlightImages => spotlightImages[Rng.Next(spotlightImages.Count)], loadPath, style, temporary);
        }

        public static void SetLatestSpotlightWallpaper(string loadPath = null, Wallpaper.Style style = Wallpaper.Style.Stretched, bool temporary = false)
        {
            SetSpotlightWallpaper(spotlightImages => spotlightImages.OrderBy(File.GetLastWriteTime).Last(), loadPath, style, temporary);
        }

        private static void SetSpotlightWallpaper(Func<List<string>, string> determiner,
            string loadPath = null, Wallpaper.Style style = Wallpaper.Style.Stretched, bool temporary = false)
        {
            var utilities = new SpotlightUtilities();
            var deletePath = false;
            if (string.IsNullOrWhiteSpace(loadPath))
            {
                if (temporary && (!Directory.Exists(utilities._spotlightBase) || (GetFileSystemEntries(utilities._spotlightBase).Length == 0)))
                {
                    loadPath = $"{GetTempPath()}SpotlightTemp";

                    WriteLine($"Cleaning and using temporary folder: {loadPath}");

                    if (Directory.Exists(loadPath))
                    {
                        Delete(loadPath, true);
                    }

                    utilities = new SpotlightUtilities(loadPath);
                    SaveSpotlightImages(loadPath);
                    loadPath = utilities._spotlightBase;

                    deletePath = true;
                }
                else
                {
                    loadPath = utilities._spotlightBase;
                }
            }

            WriteLine($"Original wallpaper was at {utilities._currentWallpaperPath}, loading images from {loadPath}");

            var spotlightImages = GetFileList(loadPath);

            spotlightImages = spotlightImages.Where(image => !ImageUtilities.ImagesEqual(image, utilities._currentWallpaper)).ToList();

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

        private static class ImageUtilities
        {
            internal static bool ImagesEqual(string firstImagePath, string secondImagePath)
            {
                Bitmap secondImage;
                using (var stream = Open(secondImagePath, FileMode.Open))
                {
                    secondImage = (Bitmap)FromStream(stream);
                }
                return ImagesEqual(firstImagePath, secondImage);
            }

            internal static bool ImagesEqual(string firstImagePath, Bitmap secondImage)
            {
                Bitmap firstImage;
                using (var stream = Open(firstImagePath, FileMode.Open))
                {
                    firstImage = (Bitmap)FromStream(stream);
                }
                return ImagesEqual(firstImage, secondImage);
            }

            private static bool ImagesEqual(Bitmap firstImage, Bitmap secondImage)
            {
                //Test to see if we have the same size of image
                if (firstImage.Size != secondImage.Size)
                {
                    return false;
                }
                for (var x = 0; x < firstImage.Width; x++)
                {
                    for (var y = 0; y < firstImage.Height; y++)
                    {
                        if (firstImage.GetPixel(x, y) != secondImage.GetPixel(x, y)) return false;
                    }
                }
                return true;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace WallpaperUtilities
{
    public class SpotlightUtilities
    {
        private readonly int _threshold;
        private static readonly Random Rng = new Random();
        private readonly string _spotlightBase, _spotlight, _spotlightAssets, _spotlightLocal, _spotlightHorizontal, _spotlightVertical, _currentWallpaperPath;
        private readonly Bitmap _currentWallpaper;

        private const string HorizontalCode = "Desktop", VerticalCode = "Mobile", TempCode = "CopyAssets";
        private SpotlightUtilities(string savePath = null, int threshold = 100)
        {
            _currentWallpaperPath = Wallpaper.GetCurrentDesktopWallpaperPath();
            using (var stream = File.Open(_currentWallpaperPath, FileMode.Open))
            {
                _currentWallpaper = (Bitmap) Image.FromStream(stream);
            }
            if (string.IsNullOrWhiteSpace(savePath))
            {
                savePath = $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\Pictures";
            }
            _spotlightBase = $"{savePath}\\Spotlight";
            _spotlight = $"{_spotlightBase}\\{DateTime.Today.Date.ToString("dd-MM-yyyy")}";
            _spotlightAssets = $"{_spotlight}\\{TempCode}";
            _spotlightHorizontal = $"{_spotlight}\\{HorizontalCode}";
            _spotlightVertical = $"{_spotlight}\\{VerticalCode}";
            _threshold = threshold;
            _spotlightLocal = $"{Environment.GetEnvironmentVariable("LOCALAPPDATA")}\\Packages\\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\\LocalState\\Assets";
       }

        private static List<string> GetFileList(string rootFolder, bool onlyName = false, bool onlyLandscape = true)
        {
            var savedFiles = new List<string>();
            if (Directory.Exists(rootFolder))
            {
                savedFiles.AddRange(from folder in Directory.EnumerateDirectories(rootFolder) //get daily images folders
                                    from subfolder in (onlyLandscape ?
                                        new List<string> { $"{folder}\\{HorizontalCode}"} //get desktop folder
                                        : Directory.EnumerateDirectories(folder)) //get desktop and mobile folders
                                    from file in Directory.EnumerateFiles(subfolder) //get all image file names in these folders
                                    select onlyName ? new FileInfo(file).Name : file);
            }
            return new HashSet<string>(savedFiles).ToList();//eliminate duplicates
        }
        public static void SaveSpotlightImages(string savePath = null, int threshold = 100)
        {
            try
            { 
                var utilities = new SpotlightUtilities(savePath, threshold);
                var savedFiles = GetFileList(utilities._spotlightBase, true, false);
                Directory.CreateDirectory(utilities._spotlight);
                Directory.CreateDirectory(utilities._spotlightAssets);
                Directory.CreateDirectory(utilities._spotlightHorizontal);
                Directory.CreateDirectory(utilities._spotlightVertical);

                Console.WriteLine($"Existing files:\n{string.Join("\n", savedFiles)}\n\n");
                //Console.WriteLine($"New files: {string.Join("\n", savedFiles)}");

                //Clean up temporary files if present
                if (Directory.EnumerateFiles(utilities._spotlightAssets).Any())
                {
                    Directory.Delete(utilities._spotlightAssets, true);
                }

                foreach (var file in Directory.EnumerateFiles(utilities._spotlightLocal))
                {
                    var fileInfo = new FileInfo(file);

                    Console.WriteLine($"Processing file {fileInfo.Name}");

                    if (fileInfo.Length < utilities._threshold * 1024 /* threshold KB in bytes*/)
                    {
                        Console.WriteLine($"File {fileInfo.Name} of size {fileInfo.Length/1024} KB < {utilities._threshold} KB skipped.\n");
                        continue;
                    }
                    
                    if (savedFiles.Any(savedFile => new FileInfo(savedFile).Name.Contains(fileInfo.Name)))
                    {
                        Console.WriteLine($"Existing File {fileInfo.Name} skipped.\n");
                        continue;
                    }
                    var newFile = $"{utilities._spotlightAssets}\\{fileInfo.Name}.jpg";
                    File.Copy(file, newFile);
                }

                Console.WriteLine("Files copied to save folder.\n");

                foreach (var newFile in Directory.EnumerateFiles(utilities._spotlightAssets))
                {
                    var fileInfo = new FileInfo(newFile);
                    Image image;
                    using (var stream = File.Open(newFile, FileMode.Open))
                    {
                        image = Image.FromStream(stream);
                    }
                    File.Copy(newFile,
                            image.Width >= image.Height
                                ? $"{utilities._spotlightHorizontal}\\{fileInfo.Name}"
                                : $"{utilities._spotlightVertical}\\{fileInfo.Name}");
                }

                Console.WriteLine("Files segregated based on dimensions.\n");
                
                Directory.Delete(utilities._spotlightAssets, true);

                Console.WriteLine("Temporary files deleted.\n");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public static void SetRandomSpotlightWallpaper(string loadPath = null, Wallpaper.Style style = Wallpaper.Style.Stretched)
        {
            SetSpotlightWallpaper(spotlightImages => spotlightImages[Rng.Next(spotlightImages.Count)], loadPath, style);
        }

        public static void SetLatestSpotlightWallpaper(string loadPath = null, Wallpaper.Style style = Wallpaper.Style.Stretched)
        {
            SetSpotlightWallpaper(spotlightImages => spotlightImages.OrderBy(File.GetLastWriteTime).Last(), loadPath, style);
        }

        private static void SetSpotlightWallpaper(Func<List<string>, string> determiner, 
            string loadPath = null, Wallpaper.Style style = Wallpaper.Style.Stretched)
        {
            try
            {
               SpotlightUtilities utilities;
                var deletePath = false;
                if (string.IsNullOrWhiteSpace(loadPath))
                {
                    loadPath = $"{Path.GetTempPath()}SpotlightTemp";

                    Console.WriteLine($"Cleaning and using temporary folder: {loadPath}");

                    if (Directory.Exists(loadPath))
                    {
                        Directory.Delete(loadPath, true);
                    }

                    utilities = new SpotlightUtilities(loadPath);
                    SaveSpotlightImages(loadPath);
                    loadPath = utilities._spotlightBase;
                    
                    deletePath = true;
                }
                else
                {
                    utilities = new SpotlightUtilities();
                }

                Console.WriteLine($"Original wallpaper was at {utilities._currentWallpaperPath}");
                
                var spotlightImages = GetFileList(loadPath);
                
                spotlightImages = spotlightImages.Where(image => !ImageUtilities.ImagesEqual(image, utilities._currentWallpaper)).ToList();
                
                var imagePath = determiner(spotlightImages);

                Wallpaper.SetDesktopWallpaper(imagePath, style);

                Console.WriteLine($"Wallpaper set to {imagePath}.");
                Console.WriteLine("Cleaning up temporary directories.");

                if (!deletePath) return;
                try
                {
                    Console.WriteLine("Deleting temporary files.\n");
                    Directory.Delete(loadPath, true);
                }
                catch (IOException)
                {
                    //silence exception - being unable to delete the file set as wallpaper is expected.    
                }
            }
            catch (IOException ioException)
            {
                Console.WriteLine(ioException.Message);
            }
        }

        private static class ImageUtilities
        {
            internal static bool ImagesEqual(string firstImagePath, string secondImagePath)
            {
                Bitmap secondImage;
                using (var stream = File.Open(secondImagePath, FileMode.Open))
                {
                    secondImage = (Bitmap) Image.FromStream(stream);
                }
                return ImagesEqual(firstImagePath, secondImage);
            }

            internal static bool ImagesEqual(string firstImagePath, Bitmap secondImage)
            {
                Bitmap firstImage;
                using (var stream = File.Open(firstImagePath, FileMode.Open))
                {
                    firstImage = (Bitmap) Image.FromStream(stream);
                }
                return ImagesEqual(firstImage, secondImage);
            }

            private static bool ImagesEqual(Bitmap firstImage, Bitmap secondImage)
            {
                var result = true;

                //Test to see if we have the same size of image
                if (firstImage.Size != secondImage.Size)
                {
                    result = false;
                }
                else
                {
                    for (var x = 0; x < firstImage.Width && result; x++)
                    {
                        for (var y = 0; y < firstImage.Height; y++)
                        {
                            if (firstImage.GetPixel(x, y) == secondImage.GetPixel(x, y)) continue;
                            result = false;
                            break;
                        }
                    }
                }
                return result;
            }
        }
    }
}

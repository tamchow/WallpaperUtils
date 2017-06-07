using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace WallpaperUtilities
{
    /// <summary>
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var firstArg = args[0].ToLower();
                var secondArg = (args.Length > 1) ? args[1] : null;
                switch (firstArg)
                {
                    case "-cw":
                    case "--change-wallpaper":
                        Wallpaper.Main(args.Skip(1).ToArray());
                        break;
                    case "-sw":
                    case "--save-wallpaper":
                        {
                            if (secondArg == null)
                            {
                                throw new ArgumentException("No source specified");
                            }
                            var savePath = (args.Length < 3) ? SpotlightUtilities.DefaultSavePath : args[2];
                            switch (secondArg)
                            {
                                case "-l":
                                case "--lockscreen":
                                    Wallpaper.SaveLockscreenWallpaper(savePath);
                                    break;
                                case "-d":
                                case "--desktop":
                                    Wallpaper.SaveDesktopWallpaper(savePath);
                                    break;
                                default:
                                    throw new ArgumentException("Unrecognized save source");
                            }
                        }
                        break;
                    case "-s":
                    case "--save":
                    case "-si":
                    case "--save-images":
                        SpotlightUtilities.SaveSpotlightImages(secondArg,
                            saveOnlyDesktopImages: args.Any(x => x == "--no-mobile" || x == "-nm"),
                            saveOnlyMobileImages: args.Any(x => x == "--no-desktop" || x == "-nd"));
                        break;
                    case "-srw":
                    case "--set-random-wallpaper":
                    case "-slw":
                    case "--set-latest-wallpaper":
                    {
                        Func<string, Func<string, bool>> modifier = prefix => x => x.StartsWith(prefix.ToLowerInvariant());
                            Func<string, Func<string, string>> removePrefix = prefix => str => str.Remove(
                                str.IndexOf(prefix, StringComparison.Ordinal), prefix.Length);
                            var styleId = (int)Wallpaper.Style.Stretched;
                            var useTemporaryPath = true;
                            var styleModifier = modifier("styleID:");
                            var temporaryModifier = modifier("temp:");
                            var styleFormatter = removePrefix("styleID:");
                            var temporaryFormatter = removePrefix("temp:");
                            if (args.Any(styleModifier))
                            {
                                styleId = int.Parse(styleFormatter(args.First(styleModifier)));
                                secondArg = null;
                            }
                            if (args.Any(temporaryModifier))
                            {
                                useTemporaryPath = bool.Parse(temporaryFormatter(args.First(temporaryModifier)));
                                secondArg = null;
                            }
                            var style = (Wallpaper.Style)styleId;
                            switch (firstArg)
                            {
                                case "-srw":
                                case "--set-random-wallpaper":
                                    SpotlightUtilities.SetRandomSpotlightWallpaper(secondArg, style, useTemporaryPath);
                                    break;
                                case "-slw":
                                case "--set-latest-wallpaper":
                                    SpotlightUtilities.SetLatestSpotlightWallpaper(secondArg, style, useTemporaryPath);
                                    break;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unrecognized option {firstArg}");
                }
            }
            else
            {
                throw new ArgumentException("args must have length > 0");
            }
        }
    }
}
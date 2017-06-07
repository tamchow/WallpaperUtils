using System;
using System.Collections.Generic;
using System.Linq;

namespace WallpaperUtilities
{
    /// <summary>
    /// </summary>
    public static class Program
    {
        private static readonly IEnumerable<string>
            NoMobileFlag = new HashSet<string> {"--no-mobile", "-nm"},
            NoDesktopFlag = new HashSet<string> {"--no-desktop", "-nd"},
            Flags = NoMobileFlag.Concat(NoDesktopFlag);

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var saveOnlyDesktopImages = args.Any(x => NoMobileFlag.Contains(x.ToLower().Trim()));
                var saveOnlyMobileImages = args.Any(x => NoDesktopFlag.Contains(x.ToLower().Trim()));
                args = args.Where(x => !Flags.Contains(x.ToLower().Trim())).ToArray();
                var firstArg = args[0].ToLower().Trim();
                var secondArg = args.Length > 1 ? args[1].Trim() : null;
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
                            throw new ArgumentException("No source specified");
                        var savePath = args.Length < 3 ? SpotlightUtilities.DefaultSavePath : args[2];
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
                    {
                        SpotlightUtilities.SaveSpotlightImages(secondArg,
                            saveOnlyDesktopImages: saveOnlyDesktopImages,
                            saveOnlyMobileImages: saveOnlyMobileImages);
                    }
                        break;
                    case "-srw":
                    case "--set-random-wallpaper":
                    case "-slw":
                    case "--set-latest-wallpaper":
                    {
                        Func<string, Func<string, bool>> modifier =
                            prefix => x => x.StartsWith(prefix.ToLowerInvariant());
                        Func<string, Func<string, string>> removePrefix = prefix => str => str.Remove(
                            str.IndexOf(prefix, StringComparison.Ordinal), prefix.Length);
                        var styleId = (int) Wallpaper.Style.Stretched;
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
                        var style = (Wallpaper.Style) styleId;
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
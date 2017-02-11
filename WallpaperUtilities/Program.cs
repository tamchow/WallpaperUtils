using System;
using System.Linq;

namespace WallpaperUtilities
{
    public static class Program
    {
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
                    case "-s":
                    case "--save":
                        SpotlightUtilities.SaveSpotlightImages(secondArg);
                        break;
                    case "-srw":
                    case "--set-random-wallpaper":
                    case "-slw":
                    case "--set-latest-wallpaper":
                        var styleId = (int) Wallpaper.Style.Stretched;
                        if (args.Length >= 3)
                        {
                            int.TryParse(args[2], out styleId);
                        }
                        var style = (Wallpaper.Style) styleId;
                        switch (firstArg)
                        {
                            case "-srw":
                            case "--set-random-wallpaper":
                                SpotlightUtilities.SetRandomSpotlightWallpaper(secondArg, style);
                                break;
                            case "-slw":
                            case "--set-latest-wallpaper":
                                SpotlightUtilities.SetLatestSpotlightWallpaper(secondArg, style);
                                break;
                        }
                        break;
                    default:
                        throw new ArgumentException("Unrecognized option");
                }
            }
            else
            {
                throw new ArgumentException("args must have length > 0");
            }
        }
    }
}
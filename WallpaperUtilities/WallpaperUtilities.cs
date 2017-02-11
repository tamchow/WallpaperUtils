using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WallpaperUtilities
{
    public static class Wallpaper
    {
        const int SpiSetdeskwallpaper = 20;
        const int SpifUpdateinifile = 0x01;
        const int SpifSendwininichange = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public enum Style
        {
            Stretched,
            Fill,
            Fit,
            Span,
            Tile,
            Center,
            NoChange
        }

        public  static string GetCurrentDesktopWallpaperPath()
        {
            var pathWallpaper = "";
            using (var regKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
            {
                if (regKey != null)
                {
                    pathWallpaper = regKey.GetValue("WallPaper").ToString();
                }
            }
            return pathWallpaper;
        }

        public static string GetCurrentLockScreenWallpaperPath()
        {
            var pathWallpaper = "";
            using (var regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Lock Screen\Creative", false))
            {
                if (regKey != null)
                {
                    pathWallpaper = regKey.GetValue("LandscapeAssetPath").ToString();
                }
            }
            return pathWallpaper;
        }
        public static void SetLockScreenWallpaper(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SetDesktopWallpaperAsLockScreenWallpaper();
            }
            else
            {
                //This doesn't work - alternative involves group policy (only for Enterprise+ SKUs) or UWP, which I cannot work with.
                using (var regKey = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lock Screen\Creative", true))
                {
                    if (regKey != null)
                    {
                        regKey.SetValue("LandscapeAssetPath", path);
                        regKey.SetValue("PortraitAssetPath", path);
                        regKey.SetValue("HotspotImageFolderPath", path.Substring(0, path.LastIndexOfAny(new[] {'/','\\'})));
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Cannot set lock screen wallpaper - required registry key does not exist.");
                    }
                }
                //GPO version
                using (var regKey = Registry.LocalMachine.CreateSubKey(
                    @"Software\Policies\Microsoft\Windows\Personalization"))
                {
                    if (regKey != null)
                    {
                        regKey.SetValue("LockScreenImage", path);
                        regKey.SetValue("LockScreenOverlaysDisabled", false);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Cannot set lock screen wallpaper - required registry key does not exist.");
                    }
                }
            }
        }
        public static void SetLockScreenWallpaperAsDesktopWallpaper()
        {
            SetDesktopWallpaper(GetCurrentLockScreenWallpaperPath());
        }
        public static void SetDesktopWallpaperAsLockScreenWallpaper()
        {
            SetLockScreenWallpaper(GetCurrentDesktopWallpaperPath());
        }
        private sealed class StyleData
        {
            public StyleData(int wallpaperStyle, bool tileWallpaper = false)
            {
                _wallpaperStyle = wallpaperStyle;
                _tileWallpaper = tileWallpaper;
            }

            readonly int _wallpaperStyle;
            readonly bool _tileWallpaper;
            public string WallpaperStyle() => _wallpaperStyle.ToString();
            public string TileWallpaper() => (_tileWallpaper ? 1 : 0).ToString();
        }

        static readonly Dictionary<Style, StyleData> Styles = new Dictionary<Style, StyleData>()
        {
            { Style.Stretched, new StyleData(2) },
            { Style.Fill, new StyleData(10) },
            { Style.Tile, new StyleData(0, true) },
            { Style.Span, new StyleData(22) },
            { Style.Center, new StyleData(1) },
            { Style.Fit, new StyleData(6) }
        };
        public static void SetDesktopWallpaper(string path, Style style = Style.Stretched)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SetLockScreenWallpaperAsDesktopWallpaper();
            }
            else
            {
                SetDesktopWallpaper(new Uri(path), style);
            }
        }
        public static void SetDesktopWallpaper(Uri uri, Style style)
        {
            if (style == Style.NoChange) return;
            var inputStream = new System.Net.WebClient().OpenRead(uri.ToString());
            if (inputStream == null || inputStream == Stream.Null)
            {
                throw new InvalidOperationException($"Source image URI {uri} is invalid");
            }

            var img = Image.FromStream(inputStream);
            var tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.bmp");
            img.Save(tempPath, ImageFormat.Bmp);

            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                if (key != null)
                {
                    var styleData = Styles[style];
                    key.SetValue(@"WallpaperStyle", styleData.WallpaperStyle());
                    key.SetValue(@"TileWallpaper", styleData.TileWallpaper());
                }
                else
                {
                    throw new InvalidOperationException("Registry key  not found");
                }
            }

            SystemParametersInfo(SpiSetdeskwallpaper,
                0,
                tempPath,
                SpifUpdateinifile | SpifSendwininichange);
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0 || !(args.Length > 0 && new List<string> {"-d", "--desktop", "-l", "--lockscreen"}.Contains(args[0])))
            {
                var nArgs = new List<string> {"--desktop", args.Length > 0 ? args[0] : ""};
                if (args.Length > 1)
                {
                    nArgs.Add(args[1]);
                }
                args = nArgs.ToArray();
            }
            else if (args.Length == 1)
            {
                var nArgs = new List<string> {args[0], ""};
                args = nArgs.ToArray();
            }
            if (args.Length >= 2)
            {
                var firstArg = args[0];
                switch (firstArg)
                {
                    case "-d":
                    case "--desktop":
                        SetDesktopWallpaper(args[1], (args.Length > 2) ? (Style)int.Parse(args[2]) : Style.Stretched);
                        Console.WriteLine($"Desktop Wallpaper was changed to {args[1]}");
                        break;
                    case "-l":
                    case "--lockscreen":
                        SetLockScreenWallpaper(args[1]);
                        Console.WriteLine($"Lock screen Wallpaper was changed to {args[1]}");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid arguments");
            }
        }
    }
}

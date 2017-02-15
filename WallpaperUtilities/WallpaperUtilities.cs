#define INTERMEDIATE_FOLDER
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace WallpaperUtilities
{
    public static class Wallpaper
    {
        private const int WaitMilliseconds = 10;
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
                if (regKey == null) return pathWallpaper;
                pathWallpaper = regKey.GetValue("LandscapeAssetPath").ToString();
                if (!string.IsNullOrWhiteSpace(pathWallpaper)) return pathWallpaper;
                Process.Start("lsbg:");
                var destinationFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                Func<string, bool> filter = file =>
                {
                    var extension = Path.GetExtension(file);
                    return extension != null &&
                           extension.Equals(CompletedExtension, StringComparison.InvariantCultureIgnoreCase);
                };
                while (!Directory.EnumerateFiles(destinationFolder).Any(filter))
                {
                    Thread.Sleep(WaitMilliseconds);
                }
                var sourceName = Directory.EnumerateFiles(destinationFolder).Where(filter).First();
                var newFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(sourceName)}";
                pathWallpaper = Path.Combine(destinationFolder, newFileName);
                File.Move(sourceName, pathWallpaper);
                return pathWallpaper;
            }
        }
        /// <summary>
        /// Sets the lockscreen wallpaper to the image file denoted by the parameter path.
        /// </summary>
        /// <param name="path">the path to the image file to set as the lockscreen wallpaper</param>
        public static void SetLockScreenWallpaper(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SetDesktopWallpaperAsLockScreenWallpaper();
            }
            else
            {
#if NORMAL
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
#endif
#if GPO
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
                Process.Start("gpupdate /force");
#endif
#if MANUAL
    //Manual mode - Launch settings and ask user to change the lockscreen image
                Console.WriteLine("Manual mode - please follow the instructions:");
                Console.WriteLine("Opening Settings app to lockscreen page...");
                Console.WriteLine("Please set lockscreen type to \"Picture\".");
                Console.WriteLine("Choose \"Browse\" and enter the following path in the \"File Name:\" field:");
                Console.WriteLine(path);
                Process.Start("ms-settings:lockscreen");
#endif
#if NETWORK
    //UWP version - Sends the image data over a TCP socket (only way to send that much data into a UWP app from a Windows app)
                var tcpServerListener = new TcpListener(IPAddress.Loopback, 4444);
                tcpServerListener.Start(); //start server
                Console.WriteLine("Server Started.");
                Process.Start("lsbg:4444");
                Console.WriteLine("Started helper app.");
                //block tcplistener to accept incoming connection
                Socket serverSocket;
                do
                {
                    serverSocket = tcpServerListener.AcceptSocket();
                } 
                while(!serverSocket.Connected);
                Console.WriteLine("Client connected.");
                //open network stream on accepted socket
                using (var serverSockStream = new NetworkStream(serverSocket))
                using (var serverStreamWriter = new StreamWriter(serverSockStream))
                using (var fileReader = new FileStream(path, FileMode.Open))
                {
                    await fileReader.CopyToAsync(serverStreamWriter.BaseStream);
                }
                Console.WriteLine("Data copied to network stream.");a
#endif
#if INTERMEDIATE_FOLDER
                var intermediateFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var intermediateName = $"{Guid.NewGuid()}_{new FileInfo(path).Name}";
                var intermediateFile = Path.Combine(intermediateFolder, intermediateName);
                File.Copy(path, intermediateFile, true);
                //block until file has been copied
                while (!Directory.EnumerateFiles(intermediateFolder).Contains(intermediateFile))
                {
                    Thread.Sleep(WaitMilliseconds);
                }
                Console.WriteLine($"File {new FileInfo(path).Name} has been written to the intermediate folder as {intermediateName}.");
                Console.WriteLine("Starting helper app...");
                Process.Start($"lsbg:{intermediateName}");
                Console.WriteLine("Helper app started...");
                var completedActionIndicator = $"{intermediateFile}{CompletedExtension}";
                while (!Directory.EnumerateFiles(intermediateFolder).Contains(completedActionIndicator))
                {
                    Thread.Sleep(WaitMilliseconds);
                }
                File.Delete(completedActionIndicator);
                Console.WriteLine("Lockscreen background has been set, intermediate file deleted.");
#endif
            }
        }

        private const string CompletedExtension = ".done";
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
                var appDataDirectory =
                    Directory.CreateDirectory(Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? Path.GetTempPath(), "WallpaperUtilities"));
                if(style == Style.NoChange) return;

                var newPath = Path.Combine(appDataDirectory.FullName, $"wallpaper{Path.GetExtension(path)}");
                File.Copy(path, newPath, true);

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
                    newPath,
                    SpifUpdateinifile | SpifSendwininichange);
            }
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
                        Console.WriteLine($"Desktop Wallpaper was changed. {args[1]}");
                        break;
                    case "-l":
                    case "--lockscreen":
                        SetLockScreenWallpaper(args[1]);
                        Console.WriteLine($"Lock screen Wallpaper was changed. {args[1]}");
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

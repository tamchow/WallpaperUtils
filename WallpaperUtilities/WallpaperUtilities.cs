#define INTERMEDIATE_FOLDER
//#define INDEPENDENT
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using static System.Console;
using static System.Diagnostics.Process;
using static System.Environment;
using static System.Guid;
using static System.IO.Directory;
using static System.IO.File;
using static System.IO.Path;
using static System.String;
using static System.StringComparison;
using static System.Threading.Thread;

namespace WallpaperUtilities
{
    /// <summary>
    /// </summary>
    public static class Wallpaper
    {
        /// <summary>
        /// </summary>
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

        /// <summary>
        /// </summary>
        private const int WaitMilliseconds = 10, DelayMilliseconds = 2000;

        /// <summary>
        /// </summary>
        private const int SpiSetdeskwallpaper = 20;

        /// <summary>
        /// </summary>
        private const int SpifUpdateinifile = 0x01;

        /// <summary>
        /// </summary>
        private const int SpifSendwininichange = 0x02;

        private const string CompletedExtension = ".done";

        /// <summary>
        /// </summary>
        private static readonly Dictionary<Style, StyleData> Styles = new Dictionary<Style, StyleData>
        {
            {Style.Stretched, new StyleData(2)},
            {Style.Fill, new StyleData(10)},
            {Style.Tile, new StyleData(0, true)},
            {Style.Span, new StyleData(22)},
            {Style.Center, new StyleData(1)},
            {Style.Fit, new StyleData(6)}
        };

        /// <summary>
        /// </summary>
        /// <param name="uAction"></param>
        /// <param name="uParam"></param>
        /// <param name="lpvParam"></param>
        /// <param name="fuWinIni"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="SecurityException">
        ///     The user does not have the permissions required to access the registry key in the
        ///     specified mode.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is null. </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="T:Microsoft.Win32.RegistryKey" /> is closed (closed keys
        ///     cannot be accessed).
        /// </exception>
        /// <exception cref="IOException">
        ///     The <see cref="T:Microsoft.Win32.RegistryKey" /> that contains the specified value has
        ///     been marked for deletion.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The user does not have the necessary registry rights.</exception>
        public static string GetCurrentDesktopWallpaperPath()
        {
            var pathWallpaper = "";
            using (var regKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
            {
                if (regKey != null)
                    pathWallpaper = regKey.GetValue("WallPaper").ToString();
            }
            return pathWallpaper;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IOException">The destination file already exists.-or-<paramref name="sourceFileName" /> was not found. </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceFileName" /> or <paramref name="destFileName" /> is null. </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="sourceFileName" /> or <paramref name="destFileName" /> is a
        ///     zero-length string, contains only white space, or contains invalid characters as defined in
        ///     <see cref="F:System.IO.Path.InvalidPathChars" />.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The path specified in <paramref name="sourceFileName" /> or
        ///     <paramref name="destFileName" /> is invalid, (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        ///     For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than
        ///     260 characters.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="sourceFileName" /> or <paramref name="destFileName" /> is in an
        ///     invalid format.
        /// </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The time-out value is negative and is not equal to
        ///     <see cref="F:System.Threading.Timeout.Infinite" />.
        /// </exception>
        /// <exception cref="Win32Exception">An error occurred when opening the associated file. </exception>
        /// <exception cref="FileNotFoundException">The PATH environment variable has a string containing quotes.</exception>
        /// <exception cref="ObjectDisposedException">The process object has already been disposed. </exception>
        public static Tuple<string, bool> GetCurrentLockScreenWallpaperPath()
        {
            var pathWallpaper = "";
            using (
                var regKey =
                    Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Lock Screen\Creative",
                        false))
            {
                if (regKey != null)
                {
                    pathWallpaper = regKey.GetValue("LandscapeAssetPath").ToString();
                    if (!IsNullOrWhiteSpace(pathWallpaper)) return new Tuple<string, bool>(pathWallpaper, false);
                }
            }
            Start("lsbg:");
            var destinationFolder = GetFolderPath(SpecialFolder.MyPictures);
            Func<string, bool> filter = file =>
            {
                var extension = GetExtension(file);
                return extension != null &&
                       extension.Equals(CompletedExtension, InvariantCultureIgnoreCase);
            };
            while (!EnumerateFiles(destinationFolder).Any(filter))
                Sleep(WaitMilliseconds);
            var sourceName = EnumerateFiles(destinationFolder).Where(filter).First();
            var newFileName = $"{NewGuid()}_{GetFileNameWithoutExtension(sourceName)}";
            pathWallpaper = Combine(destinationFolder, newFileName);
            File.Move(sourceName, newFileName);
#if INDEPENDENT
            ManualCopyFile(sourceName, pathWallpaper);
            File.Delete(sourceName);
#endif
            return new Tuple<string, bool>(pathWallpaper, true);
        }

        private static void ManualCopyFile(string sourceName, string destinationName)
        {
            destinationName = HandleFileNaming(sourceName, destinationName);
            Sleep(DelayMilliseconds);
            //Reimplementing move here to avoid ridiculous race condition errors.
            //I hate this ridiculous IPC mechanism!
            using (var readStream = OpenRead(sourceName))
            {
                using (var writeStream = OpenWrite(destinationName))
                {
                    var buffer = new byte[new FileInfo(sourceName).Length];
                    readStream.Read(buffer, 0, buffer.Length);
                    writeStream.Write(buffer, 0, buffer.Length);
                }
            }
            //End typically useless reimplementation
        }

        /// <summary>
        ///     Sets the lockscreen wallpaper to the image file denoted by the
        ///     parameter path.
        /// </summary>
        /// <param name="path">
        ///     the path to the image file to set as the
        ///     lockscreen wallpaper
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="fileName" />
        ///     is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The file name is empty, contains
        ///     only white spaces, or contains invalid characters.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file
        ///     name, or both exceed the system-defined maximum length. For example,
        ///     on Windows-based platforms, paths must be less than 248 characters,
        ///     and file names must be less than 260 characters.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="fileName" />
        ///     contains a colon (:) in the middle of the string.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     The caller does not have the
        ///     required permission.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     Access to
        ///     <paramref name="fileName" /> is denied.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     <paramref name="path" /> is invalid, such as referring to an
        ///     unmapped drive.
        /// </exception>
        /// <exception cref="IOException">
        ///     <paramref name="path" /> is a file
        ///     name.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The time-out value is
        ///     negative and is not equal to
        ///     <see cref="F:System.Threading.Timeout.Infinite" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The process object has
        ///     already been disposed.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     The PATH environment
        ///     variable has a string containing quotes.
        /// </exception>
        public static void SetLockScreenWallpaper(string path)
        {
            if (IsNullOrWhiteSpace(path))
            {
                SetDesktopWallpaperAsLockScreenWallpaper();
            }
            else
            {
#if NORMAL //This doesn't work - alternative involves group policy (only for Enterprise+ SKUs) or UWP, which I cannot work with.
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
#if GPO //GPO version
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
#if MANUAL //Manual mode - Launch settings and ask user to change the lockscreen image
                Console.WriteLine("Manual mode - please follow the instructions:");
                Console.WriteLine("Opening Settings app to lockscreen page...");
                Console.WriteLine("Please set lockscreen type to \"Picture\".");
                Console.WriteLine("Choose \"Browse\" and enter the following path in the \"File Name:\" field:");
                Console.WriteLine(path);
                Process.Start("ms-settings:lockscreen");
#endif
#if NETWORK //UWP version - Sends the image data over a TCP socket (only way to send that much data into a UWP app from a Windows app)
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
                var intermediateFolder = GetFolderPath(SpecialFolder.MyPictures);
                var intermediateName = $"{NewGuid()}_{new FileInfo(path).Name}";
                var intermediateFile = Combine(intermediateFolder, intermediateName);
                Copy(path, intermediateFile, true);
                //block until file has been copied
                while (!EnumerateFiles(intermediateFolder).Contains(intermediateFile))
                    Sleep(WaitMilliseconds);
                WriteLine(
                    $"File {new FileInfo(path).Name} has been written to the intermediate folder as {intermediateName}.");
                WriteLine("Starting helper app...");
                try
                {
                    Start($"lsbg:{intermediateName}");
                }
                catch (Win32Exception)
                {
                }
                WriteLine("Helper app started...");
                var completedActionIndicator = $"{intermediateFile}{CompletedExtension}";
                while (!EnumerateFiles(intermediateFolder).Contains(completedActionIndicator))
                    Sleep(WaitMilliseconds);
                File.Delete(completedActionIndicator);
                WriteLine("Lockscreen background has been set, intermediate file deleted.");
#endif
            }
        }

        /// <summary>
        /// </summary>
        /// <exception cref="FileNotFoundException">The PATH environment variable has a string containing quotes.</exception>
        /// <exception cref="Win32Exception">An error occurred when opening the associated file. </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The path specified in <paramref name="sourceFileName" /> or
        ///     <paramref name="destFileName" /> is invalid, (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        /// <exception cref="IOException">The destination file already exists.-or-<paramref name="sourceFileName" /> was not found. </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The time-out value is negative and is not equal to
        ///     <see cref="F:System.Threading.Timeout.Infinite" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The process object has already been disposed. </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        ///     For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than
        ///     260 characters.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="sourceFileName" /> or <paramref name="destFileName" /> is in an
        ///     invalid format.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="sourceFileName" /> or <paramref name="destFileName" /> is a
        ///     zero-length string, contains only white space, or contains invalid characters as defined in
        ///     <see cref="F:System.IO.Path.InvalidPathChars" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceFileName" /> or <paramref name="destFileName" /> is null. </exception>
        /// <exception cref="InvalidOperationException">Condition.</exception>
        public static void SetLockScreenWallpaperAsDesktopWallpaper()
        {
            var lockscreenWallpaperData = GetCurrentLockScreenWallpaperPath();
            SetDesktopWallpaper(lockscreenWallpaperData.Item1, cleanup: lockscreenWallpaperData.Item2);
        }

        /// <summary>
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">
        ///     Access to
        ///     <paramref name="fileName" /> is denied.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     The user does not have the permissions required to access the registry key in the
        ///     specified mode.
        /// </exception>
        /// <exception cref="IOException">
        ///     The <see cref="T:Microsoft.Win32.RegistryKey" /> that contains the specified value has
        ///     been marked for deletion.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="T:Microsoft.Win32.RegistryKey" /> is closed (closed keys
        ///     cannot be accessed).
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is null. </exception>
        /// <exception cref="FileNotFoundException">
        ///     The PATH environment
        ///     variable has a string containing quotes.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     <paramref name="path" /> is invalid, such as referring to an
        ///     unmapped drive.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file
        ///     name, or both exceed the system-defined maximum length. For example,
        ///     on Windows-based platforms, paths must be less than 248 characters,
        ///     and file names must be less than 260 characters.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="fileName" />
        ///     contains a colon (:) in the middle of the string.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The time-out value is
        ///     negative and is not equal to
        ///     <see cref="F:System.Threading.Timeout.Infinite" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The file name is empty, contains
        ///     only white spaces, or contains invalid characters.
        /// </exception>
        public static void SetDesktopWallpaperAsLockScreenWallpaper()
        {
            SetLockScreenWallpaper(GetCurrentDesktopWallpaperPath());
        }

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="style"></param>
        /// <param name="cleanup"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The path encapsulated in the <see cref="T:System.IO.DirectoryInfo" />
        ///     object is invalid (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="PathTooLongException">The fully qualified path and file name is 260 or more characters.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path" /> contains one or more of the invalid characters defined in
        ///     <see cref="M:System.IO.Path.GetInvalidPathChars" />.
        /// </exception>
        /// <exception cref="IOException">
        ///     The directory specified by <paramref name="path" /> is a file.-or-The network name is not
        ///     known.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission. </exception>
        /// <exception cref="ArgumentNullException"><paramref name="path" /> is null. </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path" /> contains a colon character (:) that is not part of a
        ///     drive label ("C:\").
        /// </exception>
        /// <exception cref="Win32Exception">An error occurred when opening the associated file. </exception>
        /// <exception cref="FileNotFoundException">The PATH environment variable has a string containing quotes.</exception>
        public static void SetDesktopWallpaper(string path, Style style = Style.Stretched, bool cleanup = false)
        {
            if (IsNullOrWhiteSpace(path))
            {
                SetLockScreenWallpaperAsDesktopWallpaper();
            }
            else
            {
                var appDataDirectory =
                    CreateDirectory(Combine(GetEnvironmentVariable("LOCALAPPDATA") ?? GetTempPath(),
                        "WallpaperUtilities"));
                if (style == Style.NoChange) return;

                var newPath = Combine(appDataDirectory.FullName, $"wallpaper{GetExtension(path)}");
                Copy(path, newPath, true);
                while (appDataDirectory.EnumerateFiles().All(file => file.FullName != newPath))
                    Sleep(WaitMilliseconds);
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
            if (cleanup && path != null)
                File.Delete(path);
        }

        /// <summary>
        /// </summary>
        /// <param name="pathWallpaper"></param>
        public static void SaveLockscreenWallpaper(string pathWallpaper)
        {
#if INDEPENDENT
            ManualCopyFile(GetCurrentLockScreenWallpaperPath().Item1, pathWallpaper);
#endif
            var sourcePath = GetCurrentLockScreenWallpaperPath().Item1;
            pathWallpaper = HandleFileNaming(sourcePath, pathWallpaper);
            Copy(sourcePath, pathWallpaper);
            WriteLine($"{sourcePath} copied to {pathWallpaper}");
        }

        /// <summary>
        /// </summary>
        /// <param name="pathWallpaper"></param>
        /// <exception cref="IOException">
        ///     The <see cref="T:Microsoft.Win32.RegistryKey" /> that contains the specified value has
        ///     been marked for deletion.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The user does not have the necessary registry rights.</exception>
        /// <exception cref="SecurityException">
        ///     The user does not have the permissions required to access the registry key in the
        ///     specified mode.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is null. </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="T:Microsoft.Win32.RegistryKey" /> is closed (closed keys
        ///     cannot be accessed).
        /// </exception>
        /// <exception cref="ArgumentException">Condition.</exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The path specified in <paramref name="sourceFileName" /> or
        ///     <paramref name="destFileName" /> is invalid (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="FileNotFoundException"><paramref name="sourceFileName" /> was not found. </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        ///     For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than
        ///     260 characters.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="sourceFileName" /> or <paramref name="destFileName" /> is in an
        ///     invalid format.
        /// </exception>
        public static void SaveDesktopWallpaper(string pathWallpaper)
        {
#if INDEPENDENT
            ManualCopyFile(GetCurrentDesktopWallpaperPath(), pathWallpaper);
#endif
            var sourcePath = GetCurrentDesktopWallpaperPath();
            pathWallpaper = HandleFileNaming(sourcePath, pathWallpaper);
            Copy(sourcePath, pathWallpaper);
            WriteLine($"{sourcePath} copied to {pathWallpaper}");
        }

        /// <summary>
        /// </summary>
        /// <param name="sourceName"></param>
        /// <param name="destinationName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static string HandleFileNaming(string sourceName, string destinationName)
        {
            if (sourceName == null)
                throw new ArgumentException("Parameter cannot be null", nameof(sourceName),
                    new NullReferenceException());
            if (Directory.Exists(destinationName))
                destinationName = Combine(destinationName,
                    $"({DateTime.Now.ToString("yyyy-MM-dd,hh.mm.ss,t,z")})_{GetFileName(sourceName)}" +
                    (IsNullOrEmpty(GetExtension(sourceName)) ? ".jpg" : string.Empty));
            else
                CreateDirectory(GetDirectoryName(destinationName) ?? GetDirectoryRoot(destinationName));
            return destinationName;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="s" /> is
        ///     null.
        /// </exception>
        /// <exception cref="FormatException">
        ///     <paramref name="s" /> is not in
        ///     the correct format.
        /// </exception>
        /// <exception cref="OverflowException">
        ///     <paramref name="s" /> represents
        ///     a number less than <see cref="F:System.Int32.MinValue" /> or greater
        ///     than <see cref="F:System.Int32.MaxValue" />.
        /// </exception>
        /// <exception cref="SecurityException">
        ///     The caller does not have the
        ///     required permission.
        /// </exception>
        /// <exception cref="IOException">
        ///     The directory specified by
        ///     <paramref name="path" /> is a file.-or-The network name is not
        ///     known.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     The caller does not
        ///     have the required permission.
        /// </exception>
        /// <exception cref="Win32Exception">
        ///     An error occurred when opening the
        ///     associated file.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     The PATH environment
        ///     variable has a string containing quotes.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The path encapsulated
        ///     in the <see cref="T:System.IO.DirectoryInfo" /> object is invalid
        ///     (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="InvalidOperationException">Condition.</exception>
        /// <exception cref="PathTooLongException">The fully qualified path and file name is 260 or more characters.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path" /> contains one or more of the invalid characters defined in
        ///     <see cref="M:System.IO.Path.GetInvalidPathChars" />.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path" /> contains a colon character (:) that is not part of a
        ///     drive label ("C:\").
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The process object has
        ///     already been disposed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The time-out value is
        ///     negative and is not equal to
        ///     <see cref="F:System.Threading.Timeout.Infinite" />.
        /// </exception>
        public static void Main(string[] args)
        {
            if (args.Length == 0 || !(args.Length > 0 &&
                                      new List<string> {"-d", "--desktop", "-l", "--lock screen"}.Contains(args[0])))
            {
                var nArgs = new List<string> {"--desktop", args.Length > 0 ? args[0] : ""};
                if (args.Length > 1)
                    nArgs.Add(args[1]);
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
                        SetDesktopWallpaper(args[1], args.Length > 2 ? (Style) int.Parse(args[2]) : Style.Stretched);
                        WriteLine($"Desktop Wallpaper was changed. {args[1]}");
                        break;
                    case "-l":
                    case "--lockscreen":
                        SetLockScreenWallpaper(args[1]);
                        WriteLine($"Lock screen Wallpaper was changed. {args[1]}");
                        break;
                }
            }
            else
            {
                WriteLine("Invalid arguments");
            }
        }

        /// <summary>
        /// </summary>
        private sealed class StyleData
        {
            /// <summary>
            /// </summary>
            private readonly bool _tileWallpaper;

            /// <summary>
            /// </summary>
            private readonly int _wallpaperStyle;

            /// <summary>
            /// </summary>
            /// <param name="wallpaperStyle"></param>
            /// <param name="tileWallpaper"></param>
            public StyleData(int wallpaperStyle, bool tileWallpaper = false)
            {
                _wallpaperStyle = wallpaperStyle;
                _tileWallpaper = tileWallpaper;
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public string WallpaperStyle()
            {
                return _wallpaperStyle.ToString();
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public string TileWallpaper()
            {
                return (_tileWallpaper ? 1 : 0).ToString();
            }
        }
    }
}
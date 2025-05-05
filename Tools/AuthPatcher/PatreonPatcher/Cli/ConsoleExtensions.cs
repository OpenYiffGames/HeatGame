
using System.CommandLine;
using System.CommandLine.IO;
using System.Runtime.InteropServices;

static class ConsoleExtensions
{
    public static void Write(this IConsole console, string value, ConsoleColor color)
    {
        if (NoColor || console.IsOutputRedirected || !PlataformSupportsAnsi())
        {
            console.Write(value);
            return;
        }
        string ansiColorCode = GetAnsiColorCode(color);
        console.Write(ansiColorCode);
        console.Write(value);
        console.Write(AnsiReset);
    }

    public static void WriteLine(this IConsole console, string value, ConsoleColor color)
    {
        if (NoColor || console.IsOutputRedirected || !PlataformSupportsAnsi())
        {
            console.WriteLine(value);
            return;
        }
        string ansiColorCode = GetAnsiColorCode(color);
        console.Write(ansiColorCode);
        console.Write(value);
        console.WriteLine(AnsiReset);
    }

    private const string AnsiReset = "\u001b[0m";

    public static bool NoColor => Environment.GetEnvironmentVariable("NO_COLOR") != null;

    private static bool PlataformSupportsAnsi()
    {
        var currentPlatform = Environment.OSVersion.Platform;
        if (currentPlatform == PlatformID.Win32NT)
        {
            return IsWindows10OrGreater();
        }
        if (currentPlatform == PlatformID.Unix)
        {
            return true;
        }
        if (currentPlatform == PlatformID.MacOSX)
        {
            return true;
        }
        return false;
    }

    private static bool IsWindows10OrGreater()
    {
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        return Environment.OSVersion.Version.Major >= 10;
    }

    private static string GetAnsiColorCode(ConsoleColor color)
    {
        const string commandBase = $"\u001b[";
        return color switch
        {
            ConsoleColor.Black => $"{commandBase}30m",
            ConsoleColor.DarkRed => $"{commandBase}31m",
            ConsoleColor.DarkGreen => $"{commandBase}32m",
            ConsoleColor.DarkYellow => $"{commandBase}33m",
            ConsoleColor.DarkBlue => $"{commandBase}34m",
            ConsoleColor.DarkMagenta => $"{commandBase}35m",
            ConsoleColor.DarkCyan => $"{commandBase}36m",
            ConsoleColor.Gray => $"{commandBase}37m",
            ConsoleColor.DarkGray => $"{commandBase}90m",
            ConsoleColor.Red => $"{commandBase}91m",
            ConsoleColor.Green => $"{commandBase}92m",
            ConsoleColor.Yellow => $"{commandBase}93m",
            ConsoleColor.Blue => $"{commandBase}94m",
            ConsoleColor.Magenta => $"{commandBase}95m",
            ConsoleColor.Cyan => $"{commandBase}96m",
            ConsoleColor.White => $"{commandBase}97m",
            _ => $"{commandBase}0m" // reset
        };
    }
}
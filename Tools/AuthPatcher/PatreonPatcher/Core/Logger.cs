namespace PatreonPatcher.Core;

internal static class Logger
{
    public static ILoggerWriter Writer { get; set; } = new ConsoleLogger();

    public static void Log(LogLevel level, string message)
    {
        Writer.Write(level, message);
    }
    public static void Info(string message)
    {
        Log(LogLevel.Info, message);
    }

    public static void Warning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    public static void Error(string message)
    {
        Log(LogLevel.Error, message);
    }

    public interface ILoggerWriter
    {
        void Write(LogLevel level, string message);
    }

    public class ConsoleLogger : ILoggerWriter
    {
        private static readonly Dictionary<LogLevel, ConsoleColor> Colors = new()
        {
            [LogLevel.Info] = ConsoleColor.White,
            [LogLevel.Warning] = ConsoleColor.Yellow,
            [LogLevel.Error] = ConsoleColor.Red
        };

        public void Write(LogLevel level, string message)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = Colors[level];
            Console.WriteLine($"[{level}] {message}");
            Console.ForegroundColor = color;
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
}
namespace PatreonPatcher.Core.Logging.Sinks;

internal class ConsoleLoggerSink : ILoggerSink
{
    public LogLevel LogLevel { get; set; }

    private static readonly Dictionary<LogLevel, ConsoleColor> Colors = new()
    {
        [LogLevel.Debug] = ConsoleColor.Blue,
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

    public void Dispose()
    {
    }
}
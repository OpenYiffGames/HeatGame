using PatreonPatcher.Core.Logging;
using System.CommandLine;

namespace PatreonPatcher.Cli;

internal class SystemConsoleLogSink : ILoggerSink
{
    private readonly IConsole _console;
    
    public LogLevel LogLevel { get; set; } = LogLevel.Debug;

    public SystemConsoleLogSink(IConsole console)
    {
        _console = console;
    }

    private static readonly Dictionary<LogLevel, ConsoleColor> Colors = new()
    {
        [LogLevel.Debug] = ConsoleColor.Blue,
        [LogLevel.Info] = ConsoleColor.White,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Error] = ConsoleColor.Red
    };

    public void Write(LogLevel level, string message)
    {
        var color = Colors[level];
        _console.WriteLine($"[{level}] {message}", color);
    }

    public void Dispose()
    {
    }
}
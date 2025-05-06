namespace PatreonPatcher.Core.Logging;

internal interface ILoggerSink : IDisposable
{
    LogLevel LogLevel { get; }
    void Write(LogLevel level, string message);
}

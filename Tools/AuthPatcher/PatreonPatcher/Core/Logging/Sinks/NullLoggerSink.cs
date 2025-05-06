namespace PatreonPatcher.Core.Logging.Sinks;

internal class NullLoggerSink : ILoggerSink
{
    public LogLevel LogLevel { get; set; } = LogLevel.Debug;

    public void Write(LogLevel level, string message) { }
    public void Dispose() { }
}

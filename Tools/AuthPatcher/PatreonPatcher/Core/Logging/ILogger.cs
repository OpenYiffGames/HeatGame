namespace PatreonPatcher.Core.Logging;

internal interface ILogger
{
    void Log(LogLevel level, string message);
}

using PatreonPatcher.Core.Logging.Sinks;

namespace PatreonPatcher.Core.Logging;

internal static class LoggerExtensions
{
    public static void Log(this ILogger logger, LogLevel level, string message)
    {
        logger.Log(level, message);
    }

    public static void LogInfo(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Info, message);
    }

    public static void LogDebug(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, message);
    }

    public static void LogError(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Error, message);
    }

    public static void LogWarning(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Warning, message);
    }

    public static Logger AddFileLogging(this Logger logger, string filePath, int maxSizeMB = 10, LogLevel logLevel = LogLevel.Info)
    {
        FileLoggerSink fileLogger = new(filePath, maxSizeBytes: 1024 * 1024 * maxSizeMB)
        {
            LogLevel = logLevel
        };
        logger.AddSynk(fileLogger);
        return logger;
    }

    public static Logger AddConsoleLogging(this Logger logger, LogLevel logLevel = LogLevel.Info)
    {
        ConsoleLoggerSink consoleLogger = new()
        {
            LogLevel = logLevel
        };
        logger.AddSynk(consoleLogger);
        return logger;
    }

    public static Logger AddOutputSink(this Logger logger, ILoggerSink sink)
    {
        logger.AddSynk(sink);
        return logger;
    }

}

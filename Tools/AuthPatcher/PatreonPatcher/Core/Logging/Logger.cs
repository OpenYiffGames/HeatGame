namespace PatreonPatcher.Core.Logging;

internal sealed class Logger : ILogger, IDisposable
{
    public LogLevel LogLevel { get; set; } = LogLevel.Debug;

    private readonly List<ILoggerSink> _writers = [];

    public void AddSynk(ILoggerSink writer)
    {
        _writers.Add(writer);
    }

    public void RemoveSink(ILoggerSink writer)
    {
        _writers.Remove(writer);
    }

    public void RemoveSinksBy(Type loggerType)
    {
        var writers = _writers.FindAll(w => w.GetType() == loggerType)
            .ToList();
        foreach (var item in writers)
        {
            RemoveSink(item);
        }
    }

    public void Log(LogLevel level, string message)
    {
        if (_writers.Count == 0)
        {
            return;
        }
        if (level < LogLevel)
        {
            return;
        }
        var writers = _writers
            .Where(x => x.LogLevel <= level);
        foreach (var writer in writers)
        {
            writer.Write(level, message);
        }
    }

    public void Dispose()
    {
        foreach (var writer in _writers)
        {
            writer.Dispose();
        }
    }
}
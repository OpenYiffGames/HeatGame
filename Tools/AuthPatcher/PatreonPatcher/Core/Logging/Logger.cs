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
        _ = _writers.Remove(writer);
    }

    public void RemoveSinksBy(Type loggerType)
    {
        List<ILoggerSink> writers = _writers.FindAll(w => w.GetType() == loggerType)
            .ToList();
        foreach (ILoggerSink? item in writers)
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
        IEnumerable<ILoggerSink> writers = _writers
            .Where(x => x.LogLevel <= level);
        foreach (ILoggerSink? writer in writers)
        {
            writer.Write(level, message);
        }
    }

    public void Dispose()
    {
        foreach (ILoggerSink writer in _writers)
        {
            writer.Dispose();
        }
    }
}
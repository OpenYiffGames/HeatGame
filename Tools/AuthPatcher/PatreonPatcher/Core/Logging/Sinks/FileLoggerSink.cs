using System.Text;

namespace PatreonPatcher.Core.Logging.Sinks;

internal class FileLoggerSink : ILoggerSink
{
    private readonly string _filePath;
    private readonly int _maxSizeBytes;
    private readonly StreamWriter _writer;

    public LogLevel LogLevel { get; set; } = LogLevel.Debug;

    public FileLoggerSink(string filePath, int maxSizeBytes = -1)
    {
        _filePath = filePath;
        _maxSizeBytes = maxSizeBytes;
        _writer = new StreamWriter(filePath, encoding: Encoding.UTF8, new FileStreamOptions()
        {
            Mode = FileMode.Append,
            Access = FileAccess.Write,
            Share = FileShare.Read,
        });
    }

    public void Write(LogLevel level, string message)
    {
        _writer.WriteLine($"[{level}] {message}");
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Dispose();
        if (File.Exists(_filePath))
        {
            var fileInfo = new FileInfo(_filePath);
            if (_maxSizeBytes < 0 || fileInfo.Length <= _maxSizeBytes)
            {
                return;
            }
            long excessBytes = fileInfo.Length - _maxSizeBytes;
            var fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using var ms = new MemoryStream();
            try
            {
                fs.Seek(excessBytes, SeekOrigin.Begin);
                fs.CopyTo(ms);
                fs.Close();
                fs = File.Create(_filePath);
                ms.Seek(0, SeekOrigin.Begin);
                ms.CopyTo(fs);
                fs.Close();
            }
            finally
            {
                fs.Dispose();
            }
        }
    }
}

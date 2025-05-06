using System.Collections;
using System.Text;

namespace PatreonPatcher.Core.Logging;

internal static class Log
{
    private static ILogger? _logger;

    public static ILogger Logger
    {
        get
        {
            if (_logger == null)
            {
                Logger logger = new();
                _ = Interlocked.CompareExchange(ref _logger, logger, null);
            }
            return _logger;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _ = Interlocked.Exchange(ref _logger, value);
        }
    }

    public static void WriteLog(LogLevel level, string message)
    {
        Logger.Log(level, message);
    }

    public static void WriteLog(LogLevel level, string message, params object[] args)
    {
        object[] stringyObjects = [.. args.Select(Stringify)];
        Logger.Log(level, string.Format(message, args: stringyObjects));
    }

    public static void Info(string message)
    {
        WriteLog(LogLevel.Info, message);
    }

    public static void Info(string message, params object[] args)
    {
        WriteLog(LogLevel.Info, message, args);
    }

    public static void Debug(string message)
    {
        WriteLog(LogLevel.Debug, message);
    }

    public static void Debug(string message, params object[] args)
    {
        WriteLog(LogLevel.Debug, message, args);
    }

    public static void Warning(string message)
    {
        WriteLog(LogLevel.Warning, message);
    }

    public static void Warning(string message, params object[] args)
    {
        WriteLog(LogLevel.Warning, message, args);
    }

    public static void Error(string message)
    {
        WriteLog(LogLevel.Error, message);
    }

    public static void Error(string message, params object[] args)
    {
        WriteLog(LogLevel.Error, message, args);
    }

    private static string Stringify(object obj)
    {
        if (obj is string str)
        {
            return str;
        }
        else if (obj is ICollection collection)
        {
            return StringifyCollection(collection);
        }
        else if (obj is IDictionaryEnumerator dictionary)
        {
            return StringifyDictionary(dictionary);
        }
        return obj.ToString() ?? obj.GetType().ToString();
    }

    public static string StringifyCollection(ICollection collection)
    {
        return collection is IDictionary dictionary
            ? StringifyDictionary(dictionary)
            : collection is IList list ? StringifyList(list) : StringifyEnumerable(collection);
    }

    public static string StringifyDictionary(IDictionary dictionary)
    {
        StringBuilder sb = new();
        _ = sb.Append('{');
        _ = StringifyEnumerable(dictionary, sb);
        _ = sb.Append('}');
        return sb.ToString();
    }

    public static string StringifyDictionary(IDictionaryEnumerator dictionary)
    {
        if (dictionary.Current is not null)
        {
            return StringifyDictionaryEntry(dictionary);
        }
        StringBuilder sb = new();
        _ = sb.Append('{');
        StringifyEnumerator(dictionary, sb, (e) => e);
        _ = sb.Append('}');
        return sb.ToString();
    }

    public static string StringifyDictionaryEntry(IDictionaryEnumerator dictionary)
    {
        if (dictionary.Current is null)
        {
            return StringifyDictionary(dictionary);
        }
        StringBuilder sb = new();
        _ = sb.Append(dictionary.Key);
        _ = sb.Append(": ");
        _ = sb.Append(dictionary.Value);
        return sb.ToString();
    }

    public static string StringifyList(IList collection)
    {
        StringBuilder sb = new();
        _ = sb.Append('[');
        _ = StringifyEnumerable(collection, sb);
        _ = sb.Append(']');
        return sb.ToString();
    }

    public static string StringifyEnumerable(IEnumerable enumerable)
    {
        IEnumerator enumerator = enumerable.GetEnumerator();
        return enumerator is IDictionaryEnumerator dictionaryEnumerator
            ? StringifyEnumerator(dictionaryEnumerator, (e) => e)
            : StringifyEnumerator(enumerator);
    }

    public static string StringifyEnumerable(IEnumerable enumerable, StringBuilder sb)
    {
        IEnumerator enumerator = enumerable.GetEnumerator();
        if (enumerator is IDictionaryEnumerator dictionaryEnumerator)
        {
            StringifyEnumerator(dictionaryEnumerator, sb, (e) => e);
        }
        else
        {
            StringifyEnumerator(enumerator, sb);
        }
        return sb.ToString();
    }

    private static void StringifyEnumerator<T>(
        T enumerator,
        StringBuilder sb,
        Func<T, object>? valueGetter = null) where T : IEnumerator
    {
        valueGetter ??= (T obj) => obj.Current;

        if (enumerator.MoveNext())
        {
            object value = valueGetter(enumerator);
            _ = sb.Append(Stringify(value));
        }
        while (enumerator.MoveNext())
        {
            _ = sb.Append(", ");
            _ = sb.Append(Stringify(valueGetter(enumerator)));
        }
    }

    private static string StringifyEnumerator<T>(
        T enumerator,
        Func<T, object>? valueGetter = null) where T : IEnumerator
    {
        StringBuilder sb = new();
        StringifyEnumerator(enumerator, sb, valueGetter);
        return sb.ToString();
    }
}
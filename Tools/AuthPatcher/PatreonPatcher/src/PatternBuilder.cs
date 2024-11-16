using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace PatreonPatcher;

partial class PatternBuilder
{
    readonly StringBuilder _builder = new();
    readonly HashSet<string> symbols = [];

    public PatternBuilder(string pattern)
    {
        var matches = TemplateRegex.Matches(pattern);
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var name = match.Groups["name"].Value;
            symbols.Add(name);
        }
        _builder.Append(pattern);
        _builder.Replace(" ", "");
    }

    [RequiresUnreferencedCode("This method uses reflection to get property values from the object")]
    public string Render(object? obj)
    {
        var symbolValues = new Dictionary<string, object?>();
        foreach (var symbol in symbols)
        {
            ArgumentNullException.ThrowIfNull(obj);
            var value = (obj.GetType().GetProperty(symbol)?.GetValue(obj))
                ?? throw new ArgumentException($"Missing value for symbol '{symbol}'");
            symbolValues[symbol] = value;
        }
        return Render(symbolValues);
    }

    public string Render(Dictionary<string, object?> values)
    {
        foreach (var symbol in symbols)
        {
            if (!values.TryGetValue(symbol, out var value))
            {
                throw new ArgumentException($"Missing value for symbol '{symbol}'");
            }

            string formatedValue = value switch
            {
                int i => i.ToString("X"),
                uint u => u.ToString("X"),
                long l => l.ToString("X"),
                ulong ul => ul.ToString("X"),
                short s => s.ToString("X"),
                ushort us => us.ToString("X"),
                byte b => b.ToString("X"),
                sbyte sb => sb.ToString("X"),
                string s => s,
                _ => throw new ArgumentException($"Invalid value type for symbol '{symbol}'")
            };
            static string Align(string value) => value.Length % 2 == 0 ? value : "0" + value;
            _builder.Replace($"{{{symbol}}}", Align(formatedValue));
        }
        return FormatPattern(_builder);
    }

    public string Render() => Render([]);

    public PatternBuilder Write<T>(T value) where T : INumber<T>
    {
        _builder.Append(value.ToString("X", new NumberFormatInfo()));
        return this;
    }

    public PatternBuilder WriteWildCard()
    {
        _builder.Append("??");
        return this;
    }

    private static string FormatPattern(StringBuilder @string)
    {
        int start = Math.Min(@string.Length, 2);
        for (int i = start; i < @string.Length; i += 3)
        {
            @string.Insert(i, ' ');
        }
        return @string.ToString();
    }

    private static readonly Regex TemplateRegex = CompileTemplateRegex();

    [GeneratedRegex(@"{(?<name>[a-zA-Z]\w+)}", RegexOptions.Compiled)]
    private static partial Regex CompileTemplateRegex();
}

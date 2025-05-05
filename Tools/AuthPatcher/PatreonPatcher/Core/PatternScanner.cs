namespace PatreonPatcher.Core;

internal class PatternScanner
{
    private readonly ushort[] pattern;

    public PatternScanner(string pattern)
    {
        string[] tokens = pattern.Split(' ');
        this.pattern = new ushort[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            ushort value;
            if (token == "??")
            {
                value = 0x0;
            }
            else
            {
                ReadOnlySpan<char> span = token.AsSpan()[^2..];
                if (!IsHexByte(span))
                {
                    throw new ArgumentException($"Invalid pattern token '{token}' at position: {i}", nameof(pattern));
                }
                value = (ushort)(0xFF00 | ushort.Parse(span, System.Globalization.NumberStyles.HexNumber));
            }
            this.pattern[i] = value;
        }
    }

    public int Find(byte[] data)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            if (Check(data, i))
            {
                return i;
            }
        }
        return -1;
    }

    private bool Check(byte[] data, int offset)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            ushort pattern = this.pattern[i];
            if ((data[offset + i] & (pattern >> 8)) != (pattern & 0xFF))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsHexByte(ReadOnlySpan<char> span)
    {
        if (span.Length != 2)
        {
            return false;
        }
        foreach (char c in span)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }
        return true;
    }
}
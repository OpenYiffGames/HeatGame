namespace PatreonPatcher.Core;

internal abstract class IPatchVersion : IEquatable<IPatchVersion>
{
    public abstract Guid Id { get; }
    public abstract int Minor { get; }
    public abstract int Major { get; }
    public abstract int Patch { get; }

    public virtual bool Equals(IPatchVersion? other)
    {
        return other is not null && other.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        uint hash = 0x811C9DC5;
        foreach (int i in new[] { Major, Minor, Patch })
        {
            int b = i;
            while (b > 0)
            {
                hash ^= (uint)b;
                hash *= 0x1000193;
                b >>= 8;
            }
        }
        foreach (byte b in Id.ToByteArray())
        {
            hash ^= b;
            hash *= 0x1000193;
        }
        return (int)hash;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as IPatchVersion);
    }
}

namespace Protocol;

public interface IMessage
{
    byte Version { get; }
    byte Type { get; }
}

public delegate bool PayloadParser(ReadOnlySpan<byte> payload, byte version, out IMessage message);

[AttributeUsage(AttributeTargets.Method)]
public sealed class ParserAttribute(byte version, byte type) : Attribute
{
    public byte Version { get; } = version;
    public byte Type { get; } = type;
}

public static class ParserRegistry
{
    private static readonly Dictionary<byte, PayloadParser?[]> ByVersion = new();

    public static void Register(byte version, byte type, PayloadParser parser)
    {
        if (!ByVersion.TryGetValue(version, out var table))
        {
            table = new PayloadParser?[256];
            ByVersion[version] = table;
        }
        table[type] = parser;
    }

    public static bool TryDispatch(byte version, byte type, ReadOnlySpan<byte> payload, out IMessage message)
    {
        message = null!;
        if (!ByVersion.TryGetValue(version, out var table)) return false;
        var p = table[type];
        return p is not null && p(payload, version, out message);
    }
}

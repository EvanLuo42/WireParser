using System.Buffers.Binary;
using Protocol;

namespace Demo;

public readonly record struct HelloMsg(byte Version, uint ClientId) : IMessage
{
    public byte Type => 1;

    [Parser(1, 1)]
    public static bool TryParse(ReadOnlySpan<byte> payload, byte version, out IMessage msg)
    {
        msg = null!;
        if (payload.Length < 4) return false;
        var id = BinaryPrimitives.ReadUInt32BigEndian(payload);
        msg = new HelloMsg(version, id);
        return true;
    }
}

public readonly record struct DataMsg(byte Version, ReadOnlyMemory<byte> Data) : IMessage
{
    public byte Type => 2;

    [Parser(1, 2)]
    public static bool TryParse(ReadOnlySpan<byte> payload, byte version, out IMessage msg)
    {
        msg = new DataMsg(version, payload.ToArray());
        return true;
    }
}
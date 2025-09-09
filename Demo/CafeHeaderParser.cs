using System.Buffers;
using System.Buffers.Binary;
using Protocol;

namespace Demo;

public readonly record struct CafeFrameHeader(ushort Magic, byte Version, byte Type, ushort Length) : IHeader
{
    private static int HeaderBytes => 2 + 1 + 1 + 2;
    public int FrameBytes => HeaderBytes + Length;
    
    public void GetVersionAndType(out byte version, out byte type)
    {
        version = Version;
        type = Type;
    }
}

public sealed class CafeHeaderParser : IHeaderParser<CafeFrameHeader>
{
    private const ushort ExpectedMagic = 0xCAFE;
    
    public bool TryReadHeader(ref SequenceReader<byte> reader, out CafeFrameHeader header)
    {
        header = default;
        
        Span<byte> buf = stackalloc byte[6];
        if (!reader.TryCopyTo(buf)) return false;
        reader.Advance(6);
        
        var magic = BinaryPrimitives.ReadUInt16BigEndian(buf);
        var ver = buf[2];
        var type = buf[3];
        var len = BinaryPrimitives.ReadUInt16BigEndian(buf[4..]);

        header = new CafeFrameHeader(magic, ver, type, len);
        return true;
    }

    public void Validate(in CafeFrameHeader header)
    {
        if (header.Magic != ExpectedMagic)
            throw new InvalidOperationException($"Bad magic: 0x{header.Magic:X4}");
    }

    public int GetPayloadLength(in CafeFrameHeader header) => header.Length;
}
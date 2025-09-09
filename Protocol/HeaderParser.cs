using System.Buffers;

namespace Protocol;

public interface IHeader
{
    void GetVersionAndType(out byte version, out byte type);
}

public interface IHeaderParser<T> where T : IHeader
{
    bool TryReadHeader(ref SequenceReader<byte> reader, out T header);

    void Validate(in T header);

    int GetPayloadLength(in T header);
}
using System.Buffers;
using System.IO.Pipelines;

namespace Protocol;

public class StreamReader<T>(PipeReader reader, IHeaderParser<T> headerParser) where T : IHeader
{
    public async IAsyncEnumerable<IMessage> ReadMessagesAsync()
    {
        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;
            var consumed = buffer.Start;
            var examined = buffer.End;

            try
            {
                while (TryReadOne(ref buffer, out var msg))
                {
                    consumed = buffer.Start;
                    if (msg != null) yield return msg;
                }

                examined = buffer.End;

                if (result.IsCompleted)
                {
                    break;
                }
            }
            finally
            {
                reader.AdvanceTo(consumed, examined);
            }
        }
    }
    
    private bool TryReadOne(ref ReadOnlySequence<byte> seq, out IMessage? message)
    {
        message = null;
        var seqReader = new SequenceReader<byte>(seq);
        
        if (!headerParser.TryReadHeader(ref seqReader, out var header))
            return false;
        
        headerParser.Validate(in header);
        
        var payloadLen = headerParser.GetPayloadLength(in header);
        if (seqReader.Remaining < payloadLen)
            return false;
        
        var payload = seqReader.Sequence.Slice(seqReader.Position, payloadLen);
        seqReader.Advance(payloadLen);
        
        header.GetVersionAndType(out var version, out var type);
        
        if (!ParserRegistry.TryDispatch(version, type, payload.FirstSpan, out var msg))
        {
            seq = seqReader.Sequence.Slice(seqReader.Position);
            return true;
        }

        message = msg;

        seq = seqReader.Sequence.Slice(seqReader.Position);
        return true;
    }
}
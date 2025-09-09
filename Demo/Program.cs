using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Reflection;
using Demo;
using Protocol;

ReflectionBootstrap.RegisterAllParsers(Assembly.GetExecutingAssembly());

var pipe = new Pipe();

_ = Task.Run(async () =>
{
    var writer = pipe.Writer;
    var buf = writer.GetSpan(64);

    var i = 0;
    
    // Frame 1: CA FE | 01 | 01 | 00 04 | payload (4 bytes)
    BinaryPrimitives.WriteUInt16BigEndian(buf.Slice(i, 2), 0xCAFE); // Magic

    i += 2;
    buf[i++] = 0x01; // Version
    buf[i++] = 0x01; // Type=Hello
    BinaryPrimitives.WriteUInt16BigEndian(buf.Slice(i, 2), 4); i += 2; // Length
    BinaryPrimitives.WriteUInt32BigEndian(buf.Slice(i, 4), 0x12345678); i += 4; // ClientId
    
    writer.Advance(i);
    await writer.FlushAsync();
    
    // Frame 2
    buf = writer.GetSpan(64);
    i = 0;
    BinaryPrimitives.WriteUInt16BigEndian(buf.Slice(i, 2), 0xCAFE); i += 2;
    buf[i++] = 0x01; // Version
    buf[i++] = 0x02; // Type=Data
    BinaryPrimitives.WriteUInt16BigEndian(buf.Slice(i, 2), 3); i += 2; // Length
    buf[i++] = 0xDE; 
    buf[i++] = 0xAD; 
    buf[i++] = 0xBE;
    
    writer.Advance(i);
    await writer.FlushAsync();

    await writer.CompleteAsync();
});

var headerParser = new CafeHeaderParser();
var streamReader = new StreamReader<CafeFrameHeader>(pipe.Reader, headerParser);

await foreach (var msg in streamReader.ReadMessagesAsync())
{
    switch (msg)
    {
        case HelloMsg h:
            Console.WriteLine($"[Hello] v={h.Version}, id=0x{h.ClientId:X8}");
            break;
        case DataMsg d:
            Console.WriteLine($"[Data] v={d.Version}, len={d.Data.Length}");
            break;
        default:
            Console.WriteLine($"[Unknown] {msg.GetType().Name}");
            break;
    }
}
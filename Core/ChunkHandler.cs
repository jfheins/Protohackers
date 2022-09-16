using System.Buffers;
using System.IO.Pipelines;
using System.Reflection.PortableExecutable;

namespace Core;

public abstract class ChunkHandler : Handler
{
    private readonly int chunkLength;

    public ChunkHandler(PipeReader reader, PipeWriter writer, int bytes) : base(reader, writer)
    {
        chunkLength = bytes;
    }

    protected abstract Task HandleChunk(byte[] chunk);

    public override async Task HandleClient()
    {
        while (true)
        {
            ReadResult result = await Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            while (buffer.Length >= chunkLength)
            {
                var chunk = buffer.Slice(0, chunkLength).ToArray();
                buffer = buffer.Slice(chunkLength);
                await HandleChunk(chunk);
            }

            // Tell the PipeReader how much of the buffer has been consumed.
            Reader.AdvanceTo(buffer.Start, buffer.End);

            // Stop reading if there's no more data coming.
            if (result.IsCompleted)
            {
                Console.WriteLine($"completed");
                break;
            }
        }
    }
}
using System.Buffers;
using System.IO.Pipelines;

namespace Core;

public abstract class DelimitedHandler : Handler
{
    private readonly byte delimiter;

    public DelimitedHandler(byte delimiter)
    {
        this.delimiter = delimiter;
    }

    protected abstract Task<bool> HandleChunk(byte[] chunk);

    public override async Task HandleClient()
    {
        var success = true;
        while (success)
        {
            try
            {
                ReadResult result = await Reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (success && buffer.TryFind(delimiter, out var pos))
                {
                    var chunk = buffer.Slice(0, pos.Value).ToArray();
                    buffer = buffer.Slice(buffer.GetPosition(1, pos.Value));
                    success = await HandleChunk(chunk);
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
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: " + ex.Message);
                break;
            }
        }
    }
}
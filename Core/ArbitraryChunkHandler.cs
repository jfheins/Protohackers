namespace Core;

public abstract class ArbitraryChunkHandler : Handler
{
    protected abstract Task HandleChunk(ReadOnlyMemory<byte> chunk);

    public override async Task HandleClient()
    {
        while (true)
        {
            var result = await Reader.ReadAsync();
            var buffer = result.Buffer;

            foreach (var chunk in buffer)
                await HandleChunk(chunk);

            // Tell the PipeReader how much of the buffer has been consumed.
            Reader.AdvanceTo(buffer.End, buffer.End);

            // Stop reading if there's no more data coming.
            if (result.IsCompleted)
            {
                Console.WriteLine("completed");
                break;
            }
        }
    }
}

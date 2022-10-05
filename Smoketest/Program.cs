// See https://aka.ms/new-console-template for more information
using Core;

await new TcpServer().StartAsync<EchoHandler>();

class EchoHandler : ArbitraryChunkHandler
{
    protected override async Task HandleChunk(ReadOnlyMemory<byte> chunk)
    {
        await WriteAsync(chunk);
    }
}

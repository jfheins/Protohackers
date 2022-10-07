using System.Net;
using System.Net.Sockets;

namespace Core;

public abstract class UdpHandler
{
    private Func<byte[], Task> _respond = null!;

    public abstract Task HandlePacket(byte[] chunk);

    internal UdpHandler Init(Socket s, EndPoint endpoint)
    {
        _respond = data => s.SendToAsync(data, endpoint);
        return this;
    }

    protected async Task WriteAsync(ReadOnlyMemory<byte> data)
    {
        await _respond(data.ToArray());
    }
}

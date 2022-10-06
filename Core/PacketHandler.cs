using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;

namespace Core;

public abstract class UdpHandler
{
    private Socket _socket;
    private EndPoint _endpoint;

    public abstract Task HandlePacket(byte[] chunk);

    internal UdpHandler Init(Socket s, SocketReceiveMessageFromResult msg)
    {
        _socket = s;
        _endpoint = msg.RemoteEndPoint;
        return this;
    }

    protected async Task WriteAsync(ReadOnlyMemory<byte> data)
    {
       await _socket.SendToAsync(data.ToArray(), _endpoint);
    }
}

using System.IO.Pipelines;
using System.Net.Sockets;
using System.Net;

namespace Core;

public class UdpServer
{
    private Socket _listenSocket;

    public UdpServer(int port = 18087)
    {
        _listenSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        _listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        Console.WriteLine("Listening on port 18087");
    }

    public async Task StartAsync<T>() where T : UdpHandler, new()
    {
        var buffer = new byte[4096];
        var handler = new T();
        while (true)
        {
            var sender = new IPEndPoint(IPAddress.Any, 0);
            var msg = await _listenSocket.ReceiveMessageFromAsync(buffer, sender);
            await new T().Init(_listenSocket, msg.RemoteEndPoint).HandlePacket(buffer[..msg.ReceivedBytes]);
        }
    }
}
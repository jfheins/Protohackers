using System.IO.Pipelines;
using System.Net.Sockets;
using System.Net;

namespace Core;

public class TcpServer
{
    private Socket _listenSocket;

    public TcpServer(int port = 18087)
    {
        _listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        Console.WriteLine("Listening on port 18087");
        _listenSocket.Listen(120);
    }

    public async Task StartAsync<T>() where T: Handler, new()
    {
        while (true)
        {
            var socket = await _listenSocket.AcceptAsync();
            _ = ProcessDataAsync(socket);
        }

        async Task ProcessDataAsync(Socket socket)
        {
            var remote = socket.RemoteEndPoint;
            Console.WriteLine($"[{remote}]: connected");
            var stream = new NetworkStream(socket);
            var reader = PipeReader.Create(stream);
            var writer = PipeWriter.Create(stream);

            await new T().Init(reader, writer).HandleClient();

            // Mark as complete.
            await reader.CompleteAsync();
            await writer.CompleteAsync();
            socket.Close();
            Console.WriteLine($"[{remote}]: disconnected");
        }
    }
}

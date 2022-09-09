// See https://aka.ms/new-console-template for more information
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;


var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
listenSocket.Bind(new IPEndPoint(IPAddress.Any, 18087));

Console.WriteLine("Listening on port 18087");

listenSocket.Listen(120);

while (true)
{
    var socket = await listenSocket.AcceptAsync();
    _ = ProcessDataAsync(socket);
}

static async Task ProcessDataAsync(Socket socket)
{
    Console.WriteLine($"[{socket.RemoteEndPoint}]: connected");

    // Create a PipeReader over the network stream
    var stream = new NetworkStream(socket);
    var reader = PipeReader.Create(stream);

    while (true)
    {
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;

        foreach (var block in buffer)
            await stream.WriteAsync(block);

        Console.WriteLine($"Echoed {buffer.Length} bytes");

        // Tell the PipeReader how much of the buffer has been consumed.
        reader.AdvanceTo(buffer.End, buffer.End);

        // Stop reading if there's no more data coming.
        if (result.IsCompleted)
        {
            Console.WriteLine($"completed");
            break;
        }
    }

    // Mark the PipeReader as complete.
    await reader.CompleteAsync();
    socket.Close();

    Console.WriteLine($"[{socket.RemoteEndPoint}]: disconnected");
}
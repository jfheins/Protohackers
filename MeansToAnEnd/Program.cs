using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
listenSocket.Bind(new IPEndPoint(IPAddress.Any, 18087));
Console.WriteLine("Listening on port 18087");
listenSocket.Listen(120);

while (true)
{
    var socket = await listenSocket.AcceptAsync();
    _ = ProcessDataAsync(socket);
}


async Task ProcessDataAsync(Socket socket)
{
    var remote = socket.RemoteEndPoint;
    Console.WriteLine($"[{remote}]: connected");

    var stream = new NetworkStream(socket);
    var reader = PipeReader.Create(stream);
    var w = PipeWriter.Create(stream);
    await ProcessClient(reader, w);
    // Mark the PipeReader as complete.
    await reader.CompleteAsync();
    socket.Close();

    Console.WriteLine($"[{remote}]: disconnected");
}

async Task ProcessClient(PipeReader reader, PipeWriter writer)
{
    var prices = new List<Insert>();
    while (true)
    {
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;

        byte[]? chunk;

        while ((chunk = ReadChunk(ref buffer, 9)) != null)
        {
            var msg = ParseMessage(chunk);
            if (msg == null)
                return;

            if (msg is Insert i)
                prices.Add(i);
            if (msg is Query q)
            {
                var mean = prices
                    .Where(it => it.Timestamp >= q.MinTime && it.Timestamp <= q.MaxTime)
                    .Select(it => it.Price).DefaultIfEmpty(0).Average();
                await writer.WriteAsync(new Response((int)mean));
            }
        }

        // Tell the PipeReader how much of the buffer has been consumed.
        reader.AdvanceTo(buffer.Start, buffer.End);

        // Stop reading if there's no more data coming.
        if (result.IsCompleted)
        {
            Console.WriteLine($"completed");
            break;
        }
    }
}

static byte[]? ReadChunk(ref ReadOnlySequence<byte> buffer, int length)
{
    if (!(buffer.Length >= length))
        return null;

    var chunk = buffer.Slice(0, length).ToArray();
    buffer = buffer.Slice(length);
    return chunk;
}

static object? ParseMessage(byte[] msg)
{
    Debug.Assert(msg.Length == 9);
    var type = (char)msg[0];
    Console.WriteLine($"Read {type}");
    var firstint = BinaryPrimitives.ReadInt32BigEndian(msg.AsSpan(1..5));
    var secondint = BinaryPrimitives.ReadInt32BigEndian(msg.AsSpan(5..9));
    if (type == 'I')
        return new Insert(firstint, secondint);
    if (type == 'Q')
        return new Query(firstint, secondint);
    return null;
}

record Insert(int Timestamp, int Price);
record Query(int MinTime, int MaxTime);
record Response(int Number)
{
    public static implicit operator ReadOnlyMemory<byte>(Response r)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, r.Number);
        return bytes.AsMemory();
    }
}
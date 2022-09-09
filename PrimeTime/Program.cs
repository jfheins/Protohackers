// See https://aka.ms/new-console-template for more information
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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

    var stream = new NetworkStream(socket);
    var reader = PipeReader.Create(stream);
    await ProcessClient(stream, reader);
    // Mark the PipeReader as complete.
    await reader.CompleteAsync();
    socket.Close();

    Console.WriteLine($"[{socket.RemoteEndPoint}]: disconnected");
}

static async Task ProcessClient(NetworkStream stream, PipeReader reader)
{
    while (true)
    {
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;

        while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
        {
            // Process the line.
            var response = ProcessLine(line);
            Console.WriteLine("Response: " + response?.prime);
            if (response is null)
            {
                await stream.WriteAsync(new byte[1]);
                return;
            }
            var json = JsonSerializer.Serialize(response);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(json));
            stream.WriteByte(10);
            await stream.FlushAsync();
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

static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
{
    SequencePosition? position = buffer.PositionOf((byte)'\n');

    if (position == null)
    {
        line = default;
        return false;
    }

    // Skip the line + the \n.
    line = buffer.Slice(0, position.Value);
    buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
    return true;
}

static Response? ProcessLine(in ReadOnlySequence<byte> buffer)
{
    Console.WriteLine("Request: " + Encoding.UTF8.GetString(buffer));
    var req = Deserialize(buffer);
    if (req?.method != "isPrime" || req.number is null)
        return null;

    Console.WriteLine("Request " + req.number);
    if (IsWhole(req.number.Value))
        return new Response("isPrime", IsPrime((long)req.number));
    else
        return new Response("isPrime", false);
}

static bool IsWhole(double x) => Math.Abs(x % 1) <= (double.Epsilon * 100);

static bool IsPrime(long x)
{
    if (x < 3) return x == 2;
    for (int i = 2; i <= Math.Sqrt(x); i++)
    {
        if (x % i == 0) return false;
    }
    return true;
}

static Request? Deserialize(in ReadOnlySequence<byte> buffer)
{
    try
    {
        var reader = new Utf8JsonReader(buffer);
        var req = JsonSerializer.Deserialize<Request>(ref reader);
        return req;
    }
    catch (Exception)
    {
        return null;
    }
}

record Request(string? method, double? number);
record Response(string method, bool prime);
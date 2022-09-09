﻿using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

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
    Console.WriteLine($"[{socket.RemoteEndPoint}]: connected");

    var stream = new NetworkStream(socket);
    var reader = PipeReader.Create(stream);
    await ProcessClient(stream, reader);
    // Mark the PipeReader as complete.
    await reader.CompleteAsync();
    socket.Close();

    Console.WriteLine($"[{socket.RemoteEndPoint}]: disconnected");
}

async Task ProcessClient(NetworkStream stream, PipeReader reader)
{
    while (true)
    {
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;

        while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
        {
            // Process the line.
            var response = ProcessLine(line);
            if (response is null)
            {
                await stream.WriteAsync(new byte[1]);
                return;
            }
            await JsonSerializer.SerializeAsync(stream, response, options);
            stream.WriteByte(10);
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

Response? ProcessLine(in ReadOnlySequence<byte> buffer)
{
    var req = Deserialize(buffer);
    if (req?.Method != "isPrime" || req.Number is null)
        return null;

    Console.WriteLine("Request " + req.Number);
    if (IsWhole(req.Number.Value))
        return new Response("isPrime", IsPrime((long)req.Number));
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

Request? Deserialize(in ReadOnlySequence<byte> buffer)
{
    try
    {
        var reader = new Utf8JsonReader(buffer);
        var req = JsonSerializer.Deserialize<Request>(ref reader, options);
        return req;
    }
    catch (Exception)
    {
        return null;
    }
}

record Request(string? Method, double? Number);
record Response(string Method, bool Prime);
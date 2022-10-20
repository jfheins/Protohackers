using Core;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

await new TcpServer().StartAsync<ProxyHandler>();


class ProxyHandler : DelimitedHandler
{
    private TcpClient serverConnection = new();
    private StreamReader _reader;
    private StreamWriter _writer;
    private Task? _readerTask;

    public ProxyHandler() : base(10)
    {
    }


    public override async Task HandleClient()
    {
        await serverConnection.ConnectAsync("chat.protohackers.com", 16963);
        _reader = new StreamReader(serverConnection.GetStream());
        _writer = new StreamWriter(serverConnection.GetStream());

        var c = new CancellationTokenSource();
        _readerTask = OtherDirection(_reader, c.Token);

        await base.HandleClient();
        c.Cancel();
        await _readerTask.ConfigureAwait(false);
        serverConnection.Close();
    }

    private async Task OtherDirection(StreamReader r, CancellationToken c)
    {
        await Task.Delay(50);
        while (!c.IsCancellationRequested)
        {
            try
            {
                var msg = await r.ReadLineAsync(c);
                Console.WriteLine("=> " + msg);
                msg = TransformMsg(msg);
                Console.WriteLine("=> " + msg);
                await WriteAsync(Encoding.UTF8.GetBytes(msg! + "\n"));
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    protected override async Task<bool> HandleChunk(byte[] chunk)
    {
        // when we receive, send on
        var msg = Encoding.UTF8.GetString(chunk);
        Console.WriteLine("<= " + msg);
        msg = TransformMsg(msg);
        Console.WriteLine("<= " + msg);
        await _writer.WriteAsync(msg! + "\n");
        await _writer.FlushAsync();

        return true;
    }

    private string TransformMsg(string m)
    {
        var newMsg = Regex.Replace(m, "(?<=(^|\\s))7([0-z]{25,34})(?=(\\s|$))", "7YWHMfk9JZe0LM0g1ZauHuiSxhI");
        return newMsg;
    }
}

using Core;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

await new TcpServer().StartAsync<ChatHandler>();


class ChatHandler : DelimitedHandler
{
    private static List<ChatHandler> clients = new();

    private string? username = null;

    public ChatHandler() : base(10)
    {
    }

    public override async Task HandleClient()
    {
        await WriteAsync(Encoding.UTF8.GetBytes("Welcome\r\n"));
        await base.HandleClient();
        clients.Remove(this);

        if (username != null)
        {
            var outMsg = Encoding.UTF8.GetBytes($"* {username} has left the room\r\n");
            foreach (var c in clients)
            {
                await c.WriteAsync(outMsg);
            }
        }
    }

    protected override async Task<bool> HandleChunk(byte[] chunk)
    {
        if (username == null)
        {
            username ??= Encoding.UTF8.GetString(chunk);

            if (username.Length == 0 || !username.All(it => char.IsAsciiLetterOrDigit(it)))
                return false;

            var msg = Encoding.UTF8.GetBytes($"* {username} has entered the room\r\n");
            foreach (var c in clients)
            {
                await c.WriteAsync(msg);
            }
            msg = Encoding.UTF8.GetBytes($"* The room contains: { string.Join(", ", clients.Select(it => it.username)) }\r\n");
            await WriteAsync(msg);
            clients.Add(this);
        }
        else
        {
            var message = Encoding.UTF8.GetString(chunk);
            var outMsg = Encoding.UTF8.GetBytes($"[{ username }] { message }\r\n");
            foreach (var c in clients.Where(it => it != this))
            {
                await c.WriteAsync(outMsg);
            }
        }
        return true;
    }
}

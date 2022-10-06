using Core;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

await new UdpServer().StartAsync<DbHandler>();


class DbHandler : UdpHandler
{
    private static Dictionary<string, string> _db = new() { { "version", "2.0" } };

    public override async Task<bool> HandlePacket(byte[] chunk)
    {
        var message = Encoding.UTF8.GetString(chunk);
        if (message.Contains('='))
        {
            // insert
            var kvp = message.Split('=', 2);
            Console.WriteLine($"Inserting { kvp[0] }");
            if (kvp[0] != "version")
                _db[kvp[0]] = kvp[1];
        }
        else
        {
            Console.WriteLine($"Reading { message }");
            _db.TryGetValue(message, out var value);
            value ??= "";
            await WriteAsync(Encoding.UTF8.GetBytes($"{message}={value}"));
        }
        return true;
    }
}

using Core;
using System.Buffers.Binary;
using System.Diagnostics;

await new TcpServer().StartAsync<PriceHandler>();

class PriceHandler : ChunkHandler
{
    private readonly List<Insert> prices = new();

    public PriceHandler() : base(9) { }

    protected override async Task HandleChunk(byte[] chunk)
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
            await WriteAsync(new Response((int)mean));
        }
    }
    static object? ParseMessage(byte[] msg)
    {
        Debug.Assert(msg.Length == 9);
        var type = (char)msg[0];
        //Console.WriteLine($"Read {type}");
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
}


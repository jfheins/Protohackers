using Core;
using System.Text.Json;

await new TcpServer().StartAsync<PrimeHandler>();

class PrimeHandler : DelimitedHandler
{
    public PrimeHandler() : base(10)
    {
    }

    private readonly JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly byte[] LineFeed = new byte[] { 10 };

    protected override async Task HandleChunk(byte[] chunk)
    {
        var response = ProcessLine(chunk);
        if (response is null)
        {
            await Writer.WriteAsync(LineFeed);
            return;
        }
        await JsonSerializer.SerializeAsync(Writer.AsStream(true), response, options);
        await Writer.WriteAsync(LineFeed);
    }


    Response? ProcessLine(byte[] buffer)
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

    Request? Deserialize(byte[] buffer)
    {
        try
        {
            return JsonSerializer.Deserialize<Request>(buffer, options);
        }
        catch (Exception)
        {
            return null;
        }
    }

    record Request(string? Method, double? Number);
    record Response(string Method, bool Prime);
}

using System.IO.Pipelines;

namespace Core;

public abstract class Handler
{
    public Handler(PipeReader reader, PipeWriter writer)
    {
        Reader = reader;
        Writer = writer;
    }

    protected PipeReader Reader { get; }
    protected PipeWriter Writer { get; }

    public abstract Task HandleClient();
}
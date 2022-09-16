using System.IO.Pipelines;

namespace Core;

public abstract class Handler
{
    protected PipeReader Reader { get; private set; } = null!;
    protected PipeWriter Writer { get; private set; } = null!;

    public abstract Task HandleClient();

    internal Handler Init(PipeReader r, PipeWriter w)
    {
        Reader = r;
        Writer = w;
        return this;
    }
}
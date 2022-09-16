using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Core;

public static partial class BuffersExtensions
{
    public static bool TryFind<T>(in this ReadOnlySequence<T> source, T value, [NotNullWhen(true)] out SequencePosition? pos) where T : IEquatable<T>
    {
        pos = source.PositionOf(value);
        return pos != null;
    }
}
#nullable enable
using System.Collections.Generic;

namespace io.github.azukimochi;

internal static class SpanExt
{
    public static bool Any<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        foreach (var item in span)
        {
            if (predicate(item)) return true;
        }
        return false;
    }

    public static bool Contains<T>(this ReadOnlySpan<T> span, T value, IEqualityComparer<T>? equalityComparer = null)
    {
        equalityComparer ??= EqualityComparer<T>.Default;
        foreach (var item in span)
        {
            if (equalityComparer.Equals(item, value))
                return true;
        }
        return false;
    }

    public static bool Contains(this ReadOnlySpan<string> span, string value, StringComparison comparisonType = StringComparison.Ordinal)
    {
        foreach (var item in span)
        {
            if (value.Equals(item, comparisonType))
                return true;
        }
        return false;
    }
}
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace io.github.azukimochi;

internal readonly ref struct ChangeCheckScope 
{
    private readonly Span<bool> reference;

    public ChangeCheckScope(out bool changed)
    {
        Unsafe.SkipInit(out changed);
        reference = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(changed), 1);
        EditorGUI.BeginChangeCheck();
    }

    public void Dispose()
    {
        if (reference.IsEmpty)
            return;
        reference[0] |= EditorGUI.EndChangeCheck();
    }
}
namespace io.github.azukimochi;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class DisabledAttribute : Attribute
{
    public DisabledAttribute(string reason = null) => Reason = reason;

    public string Reason { get; }
}
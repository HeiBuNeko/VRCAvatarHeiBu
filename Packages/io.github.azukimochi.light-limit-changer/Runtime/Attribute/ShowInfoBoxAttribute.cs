namespace io.github.azukimochi;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
internal sealed class ShowInfoBoxAttribute : Attribute
{
    public ShowInfoBoxAttribute(RuntimeMessageType type, string message)
    {
        Type = type;
        Message = message;
    }

    public RuntimeMessageType Type { get; }
    public string Message { get; }
}
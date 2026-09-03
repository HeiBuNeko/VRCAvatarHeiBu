namespace io.github.azukimochi;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class GroupHeaderAttribute : Attribute
{
    public GroupHeaderAttribute(string label = null) => Label = label;

    public string Label { get; }

    public bool Separator { get; set; } = true;

    public bool UseBoldFont { get; set; } = true;
}
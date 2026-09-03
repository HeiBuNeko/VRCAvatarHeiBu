namespace io.github.azukimochi;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class ColorPropertySettingsAttribute : Attribute
{
    public bool HDR { get; set; } = false;
    public bool Alpha { get; set; } = true;
}
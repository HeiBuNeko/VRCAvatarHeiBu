namespace io.github.azukimochi;

[Serializable]
public sealed class DebugOptions
{
    [TogglePopout]
    public bool DisplayDebugInformation = false;

    public bool Animation = true;
    public bool Component = true;
    public bool Renderers = true;
    public bool Materials = true;
}
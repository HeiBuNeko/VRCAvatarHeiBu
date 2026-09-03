namespace io.github.azukimochi;

internal struct ConfigureAnimationContext
{
    public string AnimationName;
    public string BlendTreeName;
    public string BlendParameter;
    public Metadata.ParameterInfo ParameterInfo;
    public ReadOnlyMemory<Renderer> Renderers;
    public string PropertyName;
    public Vector2? Range;
    public float? Value;
}

internal struct OverrideMaterialValueContext
{
    public Metadata.ParameterInfo ParameterInfo;
    public string PropertyName;
    public Material[] Materials;
}
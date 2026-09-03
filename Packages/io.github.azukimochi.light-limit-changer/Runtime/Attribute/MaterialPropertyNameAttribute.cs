namespace io.github.azukimochi;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
internal sealed class MaterialPropertyNameAttribute : PropertyAttribute
{
    public MaterialPropertyNameAttribute(string shader, params string[] names)
    {
        Shader = shader;
        Names = names;
    }

    public string Shader { get; }
    public string[] Names { get; }
}
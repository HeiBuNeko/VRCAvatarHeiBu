namespace io.github.azukimochi;

internal readonly struct ShaderProperty : IEquatable<ShaderProperty>
{
    public readonly string ShaderName;
    public readonly string PropertyName;

    public ShaderProperty(string shaderName, string propertyName)
    {
        ShaderName = shaderName;
        PropertyName = propertyName;
    }

    public override bool Equals(object obj) => obj is ShaderProperty x && Equals(x);

    public bool Equals(ShaderProperty other)
        => ShaderName == other.ShaderName && PropertyName == other.PropertyName;

    public override int GetHashCode() => ToString().GetHashCode();

    public override string ToString() => $"{ShaderName}.{PropertyName}";
}
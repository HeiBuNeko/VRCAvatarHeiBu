namespace Numeira;

public interface IBlendTree
{
    void Append(IBlendTree blendTree, float? threshold = null);
    void Build(BlendTree blendTree, float? threshold = null);
    string Name { get; }
    string? DirectBlendParameter => null;
}

internal interface IBlendTreeFactory
{
    BlendTree? Build();
}

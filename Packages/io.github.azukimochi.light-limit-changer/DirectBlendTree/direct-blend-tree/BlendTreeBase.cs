namespace Numeira;

public abstract class BlendTreeBase : IBlendTree, IBlendTreeFactory
{
    public List<(IBlendTree BlendTree, float? threshold)> Children { get; } = new();

    public string Name { get; set; } = "";

    public virtual void Append(IBlendTree blendTree, float? threshold = null)
    {
        Children.Add((blendTree, threshold));
    }

    private BlendTree? cache;

    void IBlendTree.Build(BlendTree blendTree, float? threshold) => Build(blendTree, threshold);
    protected virtual void Build(BlendTree blendTree, float? threshold = null)
    {
        cache = cache != null ? cache : Build();
        if (cache == null)
            return;
        blendTree.AddChild(cache, threshold ?? 0);
    }

    BlendTree? IBlendTreeFactory.Build() => Build();
    protected abstract BlendTree? Build();

}

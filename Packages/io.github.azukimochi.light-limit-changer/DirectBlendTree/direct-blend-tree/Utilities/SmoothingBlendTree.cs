namespace Numeira;

public sealed class ExponentialSmoothingBlendTree : SmoothingBlendTree
{
    public string SmoothAmountParameterName { get; set; } = "";

    protected override BlendTree? Build()
    {
        var tree = new BlendTree
        {
            blendParameter = SmoothAmountParameterName,
            name = Name
        };

        var a = new BlendTree()
        {
            blendParameter = InputParameterName,
            name = InputParameterName,
        };

        var b = new BlendTree()
        {
            blendParameter = OutputParameterName,
            name = OutputParameterName,
        };

        var zero = CreateAAPClip(OutputParameterName, 0);
        var one = CreateAAPClip(OutputParameterName, 1);

        a.AddChild(zero, 0);
        a.AddChild(one, 1);

        b.AddChild(zero, 0);
        b.AddChild(one, 1);

        tree.AddChild(a, 0);
        tree.AddChild(b, 1);

        return tree;
    }
}

public abstract class SmoothingBlendTree : IBlendTree, IBlendTreeFactory
{
    public string Name { get; set; } = "";

    public string InputParameterName { get; set; } = "";
    public string OutputParameterName { get; set; } = "";

    public void Append(IBlendTree blendTree, float? threshold = null)
    { }

    protected static EditorCurveBinding CreateAAPBinding(string name)
        => new() { path = "", propertyName = name, type = typeof(Animator) };

    protected static AnimationClip CreateAAPClip(string name, float value)
    {
        var motion = new AnimationClip() { name = $"{name} {value}" };
        var bind = CreateAAPBinding(name);
        AnimationUtility.SetEditorCurve(motion, bind, AnimationCurve.Constant(0, 0, value));
        return motion;
    }

    private BlendTree? cache;
    public void Build(BlendTree blendTree, float? threshold = null)
    {
        cache = cache != null ? cache : Build();
        if (cache == null)
            return;
        blendTree.AddChild(cache, threshold ?? 0);
    }

    protected abstract BlendTree? Build();

    BlendTree? IBlendTreeFactory.Build() => Build();
}

namespace Numeira;

public static class BlendTreeExt
{
    public static SimpleBlendTree AddBlendTree(this IBlendTree blendTree, string name = "", float? threshold = null)
        => blendTree.Add(new SimpleBlendTree() { Name = name }, threshold);

    public static DirectBlendTree AddDirectBlendTree(this IBlendTree blendTree, string name = "", float? threshold = null)
        => blendTree.Add(new DirectBlendTree() { Name = name }, threshold);

    public static MotionBranch AddMotion(this IBlendTree blendTree, Motion motion, float? threshold = null)
        => blendTree.Add(new MotionBranch(motion), threshold);

    public static ExponentialSmoothingBlendTree AddExponentialSmoothing(this IBlendTree blendTree, float? threshold = null)
        => blendTree.Add(new ExponentialSmoothingBlendTree(), threshold);

    public static MotionTimeBranch AddMotionTime(this IBlendTree blendTree, AnimationClip motion, float? threshold = null)
        => blendTree.Add(new MotionTimeBranch(motion), threshold);

    private static T Add<T>(this IBlendTree blendTree, T value, float? threshold) where T : IBlendTree
    {
        blendTree.Append(value, threshold);
        return value;
    }

    public static T? Find<T>(this BlendTreeBase blendTree, ReadOnlySpan<char> name) where T : IBlendTree
    {
        foreach (var (child, _) in blendTree.Children.AsSpan())
        {
            if (child is T x && name.Equals(x.Name, StringComparison.Ordinal))
            {
                return x;
            }
        }
        return default;
    }
}
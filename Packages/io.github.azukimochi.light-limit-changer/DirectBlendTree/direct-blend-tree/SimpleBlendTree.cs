namespace Numeira;

public sealed class SimpleBlendTree : BlendTreeBase
{
    public string BlendParameter { get; set; } = "";

    protected override BlendTree? Build()
    {
        var tree = new BlendTree();
        tree.blendParameter = BlendParameter;
        tree.name = Name;
        tree.useAutomaticThresholds = false;

        var children = Children.AsSpan();
        for (int i = 0; i < children.Length; i++)
        {
            float t = children[i].threshold ?? (children.Length == 0 ? 0 : (i / (children.Length - 1)));
            children[i].BlendTree.Build(tree, t);
        }
        return tree;
    }
}

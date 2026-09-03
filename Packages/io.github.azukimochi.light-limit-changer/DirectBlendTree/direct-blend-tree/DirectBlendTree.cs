using System.Reflection;
using System.Reflection.Emit;

namespace Numeira;
public class DirectBlendTree : BlendTreeBase
{
    public static string DefaultDirectBlendParameter { get; set; } = "1";

    public string DirectBlendParameter { get; set; } = DefaultDirectBlendParameter;
    public bool NormalizedBlendValues { get; set; } = false;

    public BlendTree ToBlendTree(Object? assetContainer)
    {
        HashSet<Object>? container = null;
        if (assetContainer != null) 
            container = new HashSet<Object>();

        var tree = ToBlendTree(container);
        if (container != null)
        {
            foreach(Object obj in container)
            {
                AssetDatabase.AddObjectToAsset(obj, assetContainer);
            }
        }
        return tree;
    }

    public BlendTree ToBlendTree(HashSet<Object>? assetContainer = null)
    {
        var tree = (this as IBlendTreeFactory).Build()!;
        tree.name = $"{tree.name}(WD On)";
        void Recursive(Motion motion)
        {
            assetContainer?.Add(motion);

            if (motion is BlendTree tree)
            {
                var children = tree.children;
                foreach (var x in children.AsSpan())
                {
                    Recursive(x.motion);
                }
            }
        }
        Recursive(tree);
        return tree;
    }

    private static void SetNormalizedBlendValues(BlendTree blendTree, bool value)
    {
        using var so = new SerializedObject(blendTree);
        so.FindProperty("m_NormalizedBlendValues").boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    protected override BlendTree? Build()
    {
        var blendTree = new BlendTree();
        blendTree.name = Name;
        blendTree.blendType = BlendTreeType.Direct;
        SetNormalizedBlendValues(blendTree, NormalizedBlendValues);
        var children = Children.AsSpan();
        foreach (var (child, _) in children)
        {
            child.Build(blendTree);
        }

        int count = blendTree.children.Length;
        for (int i = 0; i < count; i++)
        {
            blendTree.SetDirectBlendTreeParameter(i, children[i].BlendTree.DirectBlendParameter ?? DirectBlendParameter);
        }
        return blendTree;
    }
}

internal static class BlendTreeAccessor
{
    private delegate void SetDirectBlendTreeParameterDelegate(BlendTree blendTree, int index, string name);
    private static readonly SetDirectBlendTreeParameterDelegate _SetDirectBlendTreeParameter;

    static BlendTreeAccessor()
    {
        _SetDirectBlendTreeParameter = MakeAccessor<SetDirectBlendTreeParameterDelegate>(nameof(SetDirectBlendTreeParameter));
    }

    public static void SetDirectBlendTreeParameter(this BlendTree blendTree, int index, string name)
        => _SetDirectBlendTreeParameter.Invoke(blendTree, index, name);

    private static T MakeAccessor<T>(string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) where T : Delegate
    {
        var invoke = typeof(T).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var parameters = invoke.GetParameters().Select(x => x.ParameterType).ToArray();
        var method = new DynamicMethod(typeof(T).Name, invoke.ReturnType, parameters);
        var original = typeof(BlendTree).GetMethod(name, bindingFlags);
        var il = method.GetILGenerator();
        for( var i = 0; i < parameters.Length; i++)
        {
            if (i < 4)
            {
                il.Emit(i switch
                {
                    0 => OpCodes.Ldarg_0,
                    1 => OpCodes.Ldarg_1,
                    2 => OpCodes.Ldarg_2,
                    _ => OpCodes.Ldarg_3,
                });
            }
            else
            {
                il.Emit(i switch
                {
                    < byte.MaxValue => OpCodes.Ldarg_S,
                    _ => OpCodes.Ldarg,
                }, i);
            }
        }

        il.Emit(original.IsStatic || !original.IsVirtual ? OpCodes.Call : OpCodes.Callvirt, original);
        il.Emit(OpCodes.Ret);

        return (T)method.CreateDelegate(typeof(T));
    }
}

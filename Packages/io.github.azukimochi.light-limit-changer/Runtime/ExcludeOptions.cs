using System.Collections.Generic;
using System.Linq;

namespace io.github.azukimochi;

[Serializable]
public sealed class ExcludeOptions
{
    internal const string SystemParameter_MaterialOverride = "system/material/override-parameters";
    internal const string SystemParameter_MaterialTextureBake = "system/material/texture-bake";

    /// <summary>
    /// 対象のオブジェクト 基本GameObjectかMaterial
    /// </summary>
    public Object Object;

    /// <summary>
    /// 子のオブジェクトを含めるか
    /// </summary>
    public bool IncludeChildren;

    /// <summary>
    /// マテリアルの値上書きをスキップする
    /// </summary>
    public bool SkipOverrideParameters = true;

    /// <summary>
    /// マテリアルのテクスチャ焼き込みをスキップする
    /// </summary>
    public bool SkipTextureBake = true;

    /// <summary>
    /// アニメーションしない
    /// </summary>
    public bool SkipAnimation = true;

    public ExcludeOptions() { }

    public ExcludeOptions(Object @object)
    {
        Object = @object;
    }

    public IEnumerable<Object> GetTargetObjects()
    {
        if (Object == null) 
            yield break;

        yield return Object;
        if (Object is not GameObject gameObject || !IncludeChildren)
        {
            yield break;
        }

        foreach(var children in gameObject.GetComponentsInChildren<Transform>(true).Skip(1))
        {
            yield return children.gameObject;
        }
    }
}
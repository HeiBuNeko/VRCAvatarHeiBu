using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Numeira;

namespace io.github.azukimochi;

internal interface ILightLimitChangerProcessorReceiver
{
    void Initialize(LightLimitChangerProcessor processor);
}

[InitializeOnLoad]
internal abstract class ShaderProcessor : ILightLimitChangerProcessorReceiver, IDisposable
{
    protected const string MaterialAnimationKeyPrefix = "material.";

    internal static ShaderProcessor[] DefinedShaderProcessors { get; }

    static ShaderProcessor()
    {
        DefinedShaderProcessors = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => typeof(ShaderProcessor).IsAssignableFrom(x) && !x.IsAbstract).Select(x => Activator.CreateInstance(x) as ShaderProcessor).ToArray();
    }

    protected LightLimitChangerProcessor Processor => processor;
    private LightLimitChangerProcessor processor;

    protected LightLimitChangerComponent Component => processor.Component;

    void ILightLimitChangerProcessorReceiver.Initialize(LightLimitChangerProcessor processor) 
    { 
        this.processor = processor;
    }

    /// <summary>
    /// プロセッサーのID
    /// </summary>
    public abstract string QualifiedName { get; }

    /// <summary>
    /// 表示名　いつか使うかも
    /// </summary>
    public virtual string DisplayName => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(QualifiedName);

    protected virtual bool NeedSkipTextureBake(Material material)
        => Processor.Component.General.ColorControl.Enable == false ||
           Processor.Excludes.TryGetValue(material, out var options) && options is { SkipAnimation: false, SkipTextureBake: true };

    /// <summary>
    /// 入力されたマテリアルが対象かどうかを判定する
    /// </summary>
    /// <param name="material"></param>
    /// <returns></returns>
    public virtual bool IsTargetMaterial(Material material) => false;

    /// <summary>
    /// マテリアルが複製された際に呼び出される
    /// </summary>
    /// <param name="material">対象になるマテリアルの一覧</param>
    public virtual void OnMaterialCloned(IEnumerable<Material> materials) { }

    /// <summary>
    /// マテリアルの正規化（テクスチャの焼き込みなど）を行う
    /// </summary>
    /// <param name="material"></param>
    public virtual void NormalizeMaterial(Material material) { }

    private readonly string[] singleArray = new string[1];

    /// <summary>
    /// パラメーターの種類か名前から操作対象の名前を取得する
    /// </summary>
    public virtual ReadOnlySpan<string> GetMaterialPropertyNames(string id)
    {
        if (id is Metadata.ID.General.ColorControl.ColorTemperature.Id)
        {
            singleArray[0] = "_Color";
            return singleArray;
        }
        return default;
    }

    /// <summary>
    /// アニメーションを設定する
    /// </summary>
    public virtual void ConfigureAnimation(in ConfigureAnimationContext context, DirectBlendTree blendTree)
    {
        AnimationClip clip;
        if (context.BlendParameter is null)
        {
            clip = blendTree.Find<MotionBranch>(context.AnimationName)?.Motion as AnimationClip;
            if (clip is null)
            {
                clip = new AnimationClip() { name = context.AnimationName };
                blendTree.AddMotion(clip);
            }
        }
        else
        {
            clip = blendTree.Find<MotionTimeBranch>(context.BlendTreeName ?? Path.GetFileName(context.ParameterInfo.Id))?.Motion as AnimationClip;
            if (clip is null)
            {
                clip = new AnimationClip() { name = context.AnimationName };
                var tree = blendTree.AddMotionTime(clip);
                tree.Name = context.BlendTreeName ?? Path.GetFileName(context.ParameterInfo.Id);
                tree.BlendParameter = context.BlendParameter;
            }
        }
        ConfigureAnimation(context, clip);
    }

    /// <summary>
    /// アニメーションを設定する
    /// </summary>
    public virtual void ConfigureAnimation(in ConfigureAnimationContext context, AnimationClip motion)
    {
        string propertyName = context.PropertyName;
        var parameterInfo = context.ParameterInfo;

        if (context.Value is { } value)
        {
            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{propertyName}", AnimationUtils.ConstantCurve(value));
            return;
        }

        if (parameterInfo.Id is GeneralControlIDs.LightDirection)
            return;

        if (parameterInfo.Id is GeneralControlIDs.ColorTemperature)
        {
            var color = Color.white;
            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{propertyName}.r", AnimationUtils.LinearCurve(color.r * 0.6f, color.r, color.r));
            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{propertyName}.g", AnimationUtils.LinearCurve(color.g * 0.95f, color.g, color.g * 0.8f));
            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{propertyName}.b", AnimationUtils.LinearCurve(color.b, color.b, color.b * 0.6f));
            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{propertyName}.a", AnimationUtils.ConstantCurve(color.a));
            return;
        }

        var (min, max) = context.Range;
        context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{propertyName}", AnimationUtils.LinearCurve(min, max));
    }

    /// <summary>
    /// 光源の向き制御用のアニメーションを生成する
    /// </summary>
    public virtual void ConfigureLightDirectionControl(ReadOnlyMemory<Renderer> renderers, string propertyName, AnimationClip @default, AnimationClip x, AnimationClip y, AnimationClip z, AnimationClip w, AnimationClip other)
    {
        const float INITIAL_LIGHT_DIRECTION_X = 0.001f;
        const float INITIAL_LIGHT_DIRECTION_Y = 0.002f;
        const float INITIAL_LIGHT_DIRECTION_Z = 0.001f;

        renderers.AnimateAllFloat(@default, $"{MaterialAnimationKeyPrefix}{propertyName}.x", AnimationUtils.ConstantCurve(INITIAL_LIGHT_DIRECTION_X));
        renderers.AnimateAllFloat(@default, $"{MaterialAnimationKeyPrefix}{propertyName}.y", AnimationUtils.ConstantCurve(INITIAL_LIGHT_DIRECTION_Y));
        renderers.AnimateAllFloat(@default, $"{MaterialAnimationKeyPrefix}{propertyName}.z", AnimationUtils.ConstantCurve(INITIAL_LIGHT_DIRECTION_Z));
        renderers.AnimateAllFloat(@default, $"{MaterialAnimationKeyPrefix}{propertyName}.w", AnimationUtils.ConstantCurve(0));

        renderers.AnimateAllFloat(x, $"{MaterialAnimationKeyPrefix}{propertyName}.x", AnimationUtils.LinearCurve(stackalloc float[] {    0,  1000,     0, -1000,    0 }));
        renderers.AnimateAllFloat(y, $"{MaterialAnimationKeyPrefix}{propertyName}.y", AnimationUtils.LinearCurve(stackalloc float[] {       -2000,     0,  2000,      }));
        renderers.AnimateAllFloat(z, $"{MaterialAnimationKeyPrefix}{propertyName}.z", AnimationUtils.LinearCurve(stackalloc float[] { 1000,     0, -1000,     0, 1000 }));
        renderers.AnimateAllFloat(w, $"{MaterialAnimationKeyPrefix}{propertyName}.w", AnimationUtils.ConstantCurve(0));
    }

    public virtual void OverrideMaterialValue(in OverrideMaterialValueContext context)
    {
        var t = context.ParameterInfo.ParameterType;
        var propertyName = context.PropertyName;
        var parameter = context.ParameterInfo.Get(Component);
        if (string.IsNullOrEmpty(propertyName))
            return;

        if (OverrideKnownPropertyValue(context))
            return;
        
        var suffix = propertyName.Length < 2 ? ReadOnlySpan<char>.Empty : propertyName.AsSpan()[^2..];
        IEnumerable<Material> materials;
        if (!suffix.IsEmpty && suffix[0] == '.')
        {
            var value = t == typeof(bool) ? parameter.GetValueDirect<bool>() ? 1 : 0 : parameter.GetValueDirect<float>();
            var isColor = suffix[1..].IndexOfAny("rgba") != -1;
            propertyName = propertyName[..^2];
            materials = context.Materials.Where(x => x.HasProperty(propertyName));
            if (isColor)
            {
                foreach (var mat in materials)
                {
                    var temp = mat.GetColor(propertyName);
                    Unsafe.Add(ref Unsafe.As<Color, float>(ref temp), "rgba".IndexOf(suffix[1])) = value;
                    mat.SetColor(propertyName, temp);
                }
            }
            else // vector
            {
                foreach (var mat in materials)
                {
                    var temp = mat.GetVector(propertyName);
                    Unsafe.Add(ref Unsafe.As<Vector4, float>(ref temp), "xyzw".IndexOf(suffix[1])) = value;
                    mat.SetVector(propertyName, temp);
                }
            }
            return;
        }

        materials = context.Materials.Where(x => x.HasProperty(propertyName));
        if (t == typeof(int))
        {
            var value = parameter.GetValueDirect<int>();
            foreach (var mat in materials)
                mat.SetInt(propertyName, value);
        }
        else if (t == typeof(float))
        {
            var value = parameter.GetValueDirect<float>();
            foreach (var mat in materials)
                mat.SetFloat(propertyName, value);
        }
        else if (t == typeof(bool))
        {
            var value = parameter.GetValueDirect<bool>() ? 1 : 0;
            foreach (var mat in materials)
            {
                var idx = mat.shader.FindPropertyIndex(propertyName);
                if (idx == -1)
                    continue;
                if (mat.shader.GetPropertyType(idx) == UnityEngine.Rendering.ShaderPropertyType.Float)
                    mat.SetFloat(propertyName, value);
                else
                    mat.SetInt(propertyName, value);
            }
        }
        else if (t == typeof(Vector4))
        {
            var value = parameter.GetValueDirect<Vector4>();
            foreach (var mat in materials)
                mat.SetVector(propertyName, value);
        }
        else if (t == typeof(Color))
        {
            var value = parameter.GetValueDirect<Color>();
            foreach (var mat in materials)
                mat.SetColor(propertyName, value);
        }
    }

    protected bool OverrideKnownPropertyValue(in OverrideMaterialValueContext context)
    {
        if (context.ParameterInfo.Name == nameof(LightingSettings.LightDirection))
        {
            foreach (var mat in context.Materials)
            {
                if (mat.HasProperty(context.PropertyName))
                    mat.SetVector(context.PropertyName, Vector4.zero);
            }
            return true;
        }

        if (context.ParameterInfo.Name == nameof(ColorControlSettings.ColorTemperature))
        {
            foreach (var mat in context.Materials)
            {
                if (mat.HasProperty(context.PropertyName))
                    mat.SetColor(context.PropertyName, Utils.GetColorTempertured(context.ParameterInfo.Get(Component).GetValueDirect<float>()));
            }
            return true;
        }
        return false;
    }

    public virtual void ModifyExpressionMenuTree(Transform menuRoot) { }

    /// <summary>
    /// コンポーネントを初期化した後に呼ばれる
    /// </summary>
    public virtual void OnResetComponent(LightLimitChangerComponent component)
    {

    }

    public override bool Equals(object obj) => obj is ShaderProcessor proc && proc.QualifiedName == this.QualifiedName;

    public override int GetHashCode() => this.QualifiedName.GetHashCode();

    public virtual void Dispose() { }
}

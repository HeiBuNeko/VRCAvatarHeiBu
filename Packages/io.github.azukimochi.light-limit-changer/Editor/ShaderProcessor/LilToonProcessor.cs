using System.Collections.Generic;
using nadena.dev.ndmf;

namespace io.github.azukimochi;

internal sealed class LilToonProcessor : ShaderProcessor
{
    public override string QualifiedName => BuiltinSupportedShaders.LilToon;
    public override string DisplayName => "LilToon";

    #region ShaderProperties

    public const string _LightMinLimit = "_LightMinLimit";
    public const string _LightMaxLimit = "_LightMaxLimit";
    public const string _AsUnlit = "_AsUnlit";
    public const string _MainTexHSVG = "_MainTexHSVG";
    public const string _Color = "_Color";
    public const string _Color2nd = "_Color2nd";
    public const string _Color3rd = "_Color3rd";
    public const string _MainTex = "_MainTex";
    public const string _Main2ndTex = "_Main2ndTex";
    public const string _Main3rdTex = "_Main3rdTex";
    public const string _AlphaMask = "_AlphaMask";
    public const string _MainGradationTex = "_MainGradationTex";
    public const string _MainGradationStrength = "_MainGradationStrength";
    public const string _MainColorAdjustMask = "_MainColorAdjustMask";
    public const string _MonochromeLighting = "_MonochromeLighting";
    public const string _EmissionBlend = "_EmissionBlend";
    public const string _Emission2ndBlend = "_Emission2ndBlend";
    public const string _VertexLigthStrength = "_VertexLightStrength";
    public const string _ShadowEnvStrength = "_ShadowEnvStrength";

    #endregion
    
    public override bool IsTargetMaterial(Material material)
    {
        if (material == null)
            return false;
        return CheckShaderIslilToon(material.shader);
    }

    public override void OnResetComponent(LightLimitChangerComponent component)
    {
        component.LilToon.VRCLightVolumes.RimBorder.Value = 3;
    }

    // https://github.com/lilxyzw/lilToon/blob/ae6a81da053e8cd001f089fd02cc6402afab5701/Assets/lilToon/Editor/lilMaterialUtils.cs#L711-L717//
    // Originally under MIT License
    // Copyright (c) 2020-2024 lilxyzw
    private static bool CheckShaderIslilToon(Shader shader)
    {
        if (shader == null) return false;
        if (shader.name.Contains("lilToon") || shader.name.Contains("lts_pass")) return true;
        string shaderPath = AssetDatabase.GetAssetPath(shader);
        return !string.IsNullOrEmpty(shaderPath) && shaderPath.Contains(".lilcontainer");
    }

    public SerializedProperty GetMaterialProperty(SerializedProperty properties, ISettings settings, Parameter parameter)
    {
        if (settings is LightingSettings lightingSettings)
        {
            if (parameter == lightingSettings.MinLight)
                return properties.FindPropertyRelative(_LightMinLimit);
        }
        return null;
    }

    public override void ConfigureAnimation(in ConfigureAnimationContext context, AnimationClip motion)
    {
        if (context.ParameterInfo.Id is GeneralControlIDs.LightDirection)
        {
            // ライト方向は(0,0,0)にしてはいけない
            const float INITIAL_LIGHT_DIRECTION_X = 0.001f;
            const float INITIAL_LIGHT_DIRECTION_Y = 0.002f;
            const float INITIAL_LIGHT_DIRECTION_Z = 0.001f;

            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{context.PropertyName}.x",
                AnimationUtils.LinearCurveZeroDisabled(initialValue: INITIAL_LIGHT_DIRECTION_X, 0, 1000, 0, -1000, 0));

            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{context.PropertyName}.y",
                AnimationUtils.LinearCurveZeroDisabled(initialValue: INITIAL_LIGHT_DIRECTION_Y, 0, 500, -500, 500, 0));

            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{context.PropertyName}.z",
                AnimationUtils.LinearCurveZeroDisabled(initialValue: INITIAL_LIGHT_DIRECTION_Z, 1000, 0, -1000, 0, 1000));

            float directionMode = Component.General.LightingControl.LightDirectionMode switch
            {
                LightDirectionMode.Local => 1,
                LightDirectionMode.World => 0,
                _ => 0,
            };

            context.Renderers.AnimateAllFloat(motion, $"{MaterialAnimationKeyPrefix}{context.PropertyName}.w",
                AnimationUtils.ConstantCurve(directionMode));

            return;
        }

        ConfigureAnimationContext ctx = context;

        if (context.ParameterInfo.Id == GeneralControlIDs.ColorHue)
            ctx.Range *= 0.5f;

        base.ConfigureAnimation(ctx, motion);
    }

    public override void ConfigureLightDirectionControl(ReadOnlyMemory<Renderer> renderers, string propertyName, AnimationClip @default, AnimationClip x, AnimationClip y, AnimationClip z, AnimationClip w, AnimationClip other)
    {
        base.ConfigureLightDirectionControl(renderers, propertyName, @default, x, y, z, w, other);

        float directionMode = Component.General.LightingControl.LightDirectionMode switch
        {
            LightDirectionMode.World => 0,
            LightDirectionMode.Local => 1,
            _ => 0,
        };

        renderers.AnimateAllFloat(w, $"{MaterialAnimationKeyPrefix}{propertyName}.w", AnimationUtils.ConstantCurve(directionMode));
    }

    public override void OnMaterialCloned(IEnumerable<Material> materials)
    {
        // いまのところlilMultiで動かないのは逆光ライトだけっぽいので、逆光ライトが無効ならエラーを出さないようにする
        var useBacklight = Processor.Component.LilToon.Backlight.UseBacklight;
        if (!useBacklight.Enable)
            return;

        NdmfMessage lilMultiWarning = null;
        foreach (var material in materials)
        {
            if (material?.shader?.name.Contains("Multi", StringComparison.OrdinalIgnoreCase) != true)
                continue;

            (lilMultiWarning ??= NdmfMessage.Create(ErrorSeverity.Information, "error:liltoon/exist-lilmulti"))
                .AddReference(ObjectRegistry.GetReference(material));
        }
        if (lilMultiWarning == null)
            return;

        ErrorReport.ReportError(lilMultiWarning);
    }

    public override void NormalizeMaterial(Material material)
    {
        ConfigureLilMultiKeywords(material);

        NormalizeMainTextures(material);

        NormalizeEmissionMask(material);
    }

    private void NormalizeMainTextures(Material material)
    {
        if (NeedSkipTextureBake(material))
            return;

        var colorCtrl = Component.General.ColorControl;
        if (!colorCtrl.Enable)
            return;

        if (!colorCtrl.ColorTemperature.Enable &&
            !colorCtrl.Hue.Enable && !colorCtrl.Saturation.Enable && !colorCtrl.Brightness.Enable && !colorCtrl.Gamma.Enable)
            return;

        Material bakeMat = null;
        try
        {
            bool flag = false;
            var mode = (AlphaMaskMode)material.Get("_AlphaMaskMode", 0);
            var maintex = material.Get<Texture>(_MainTex, Texture2D.whiteTexture);
            flag |= NormalizeMainColor(material, ref bakeMat, mode);
            flag |= NormalizeMainTexHSVG(material, ref bakeMat);
            flag &= maintex is Texture2D;

            if (!flag)
                return;

            var baked = Processor.TextureBaker.GetOrBake(maintex, bakeMat, mode == AlphaMaskMode.Replace ? false : null);
            if (Processor.Component.General.AllowForceOverrideSameReference)
            {
                // メインカラーとアルファマスクに同じ画像が参照されいるケースに対応
                var alphaMask = material.Get<Texture>(_AlphaMask, null);

                TextureBaker.ReplaceTextures(material, maintex, baked);

                if (alphaMask != null && maintex == alphaMask)
                {
                    material.SetTexture(_AlphaMask, maintex);
                }
            }
            material.SetTexture(_MainTex, baked);
        }
        finally
        {
            if (bakeMat != null)
                Object.DestroyImmediate(bakeMat);
        }
    }

    private bool NormalizeMainColor(Material material, ref Material bakeMaterial, AlphaMaskMode mode)
    {
        var color = material.GetColor("_Color");

        if (color == Color.white)
            return false;

        bakeMaterial ??= new Material(Shader.Find("Hidden/ltsother_baker"));

        if (mode == AlphaMaskMode.Replace)
            color.a = 1;

        bakeMaterial.SetColor("_Color", color);
        material.SetColor("_Color", Color.white);

        if (color == Color.white)
            return false;

        return true;
    }

    public bool NormalizeMainTexHSVG(Material material, ref Material bakeMaterial)
    {
        var colorCtrl = Processor.Component.General.ColorControl;
        var defaultHSVG = new Vector4(0, 1, 1, 1);
        var hsvg = material.Get(_MainTexHSVG, defaultHSVG);

        if (hsvg == defaultHSVG)
        {
            material.SetTexture(_MainColorAdjustMask, null);
            return false;
        }

        bakeMaterial ??= new Material(Shader.Find("Hidden/ltsother_baker"));

        var masktex = material.Get(_MainColorAdjustMask, Texture2D.whiteTexture);
        material.SetTexture(_MainColorAdjustMask, null);
        bakeMaterial.SetTexture(_MainColorAdjustMask, masktex);
        material.SetVector(_MainTexHSVG, defaultHSVG);
        bakeMaterial.SetVector(_MainTexHSVG, hsvg);
        return true;
    }

    private void NormalizeEmissionMask(Material material)
    {
        if (!Component.General.LightingControl.EmissionStrength.Enable)
            return;

        Process("Emission");
        Process("Emission2nd");

        void Process(string propertyNameBase)
        {
            var enablePropertyName = $"_Use{propertyNameBase}";
            var maskStrengthPropertyName = $"_{propertyNameBase}Blend";

            if (material.Get(enablePropertyName, 0) == 0)
                return;

            var maskTextureName = $"{maskStrengthPropertyName}Mask";
            var maskTexture = material.Get<Texture2D>(maskTextureName, null);
            var maskBlendMode = material.Get<int>($"{maskStrengthPropertyName}Mode", 1);

            var blend = material.Get(maskStrengthPropertyName, 1f);

            if (Mathf.Approximately(blend, 1f))
                return;

            Texture2D newMask = Processor.TextureBaker.EditMaskTexture(maskTexture ?? AssetUtils.WhiteTexture, new(1, 1, 1, blend));
            material.SetTexture(maskTextureName, newMask);
            material.SetFloat(maskStrengthPropertyName, 1f);
        }
    }

    public void ConfigureLilMultiKeywords(Material material)
    {
        if (!material.shader.name.Contains("Multi", StringComparison.OrdinalIgnoreCase))
            return;

        var c = Processor.Component;
        if (Metadata.GetMetadataFromType<LilToonSettings.LilDistanceFadeSettings>().Parameters.Any(x => x.Get(Component).IsAnimated))
            material.EnableKeyword("_FADING_ON");   // 距離フェード

        if (Metadata.GetMetadataFromType<LilToonSettings.LilBacklightSettings>().Parameters.Any(x => x.Get(Component).IsAnimated))
            material.EnableKeyword("ANTI_FLICKER"); // 逆光ライト

        // Note:
        // ANTI_FLICKERキーワードを有効にすると_UseBacklightが常に1にされるため、
        // 有効／無効のアニメーションは動かなくなる
        // https://github.com/lilxyzw/lilToon/blob/f10e9fe4d8bf8283014e371f2b8017c986903087/Assets/lilToon/Shader/Includes/lil_common.hlsl#L34C1-L34C31
    }

    public override void ModifyExpressionMenuTree(Transform menuRoot)
    {
        if (Component.LilToon.Backlight.AddLightDirectionMenu)
        {
            var lightDirection = menuRoot.Find("Lighting/LightDirection");
            var backlight = menuRoot.Find("LilToon/Backlight");
            if (lightDirection != null && backlight != null)
            {
                Object.Instantiate(lightDirection.gameObject, backlight).name = lightDirection.name;
            }
        }
    }

    private enum AlphaMaskMode
    {
        None = 0,
        Replace = 1,
    }
}
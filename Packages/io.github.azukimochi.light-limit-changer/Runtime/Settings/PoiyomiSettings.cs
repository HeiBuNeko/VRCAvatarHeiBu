using System.Collections.Generic;

namespace io.github.azukimochi;

[Serializable]
[MenuIcon(Icons.Poiyomi)]
[ShaderFeature(BuiltinSupportedShaders.Poiyomi)]
[SettingOptions(id: "poiyomi", menuPath: "Poiyomi", parameterPrefix: "Poiyomi", enabled: false)]
public sealed partial class PoiyomiSettings : Settings<PoiyomiSettings>, ISettingsProvider
{
    [GeneralControl(GeneralControlType.MaxLight)]
    [MinMaxRange(0, 10)]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_LightingAdditiveLimit")]
    [MenuIcon(Icons.Light_Max)]
    [ControlOverrider]
    public Parameter<float> MaxAdditiveLight = new(1) {Enable =  false};

    [GeneralControl(GeneralControlType.Monochrome)]
    [MenuIcon(Icons.Monochrome)]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_LightingAdditiveMonochromatic")]
    [Range(0, 1)]
    [ControlOverrider]
    public Parameter<float> MonochromeAdditive = new(0) {Enable = false};

    public PPAnimationSettings PPAnimations = new();
    public BacklightSettings Backlight = new();
    public SSAOSettings SSAO = new();

    IEnumerable<ISettingsProvider> ISettingsProvider.Children
    {
        get
        {
            yield return PPAnimations;
            yield return Backlight;
            yield return SSAO;
        }
    }

    [Serializable]
    [MenuIcon(Icons.Poiyomi)]
    [ShaderFeature(BuiltinSupportedShaders.Poiyomi)]
    [SettingOptions(id: "pp-animations", menuPath: "PP-Animations", parameterPrefix: "PPAnimations")]
    public sealed class PPAnimationSettings : Settings<PPAnimationSettings>
    {
        [MenuIcon(Icons.Light)]
        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_PPLightingMultiplier")]
        [MinMaxRange(0, 5)]
        public Parameter<float> LightingMultiplier = new(1) { Enable = true };

        [MenuIcon(Icons.Light)]
        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_PPLightingAddition")]
        [MinMaxRange(-5, 5)]
        public Parameter<float> LightingAddition = new(0) { Enable = true };

        [MenuIcon(Icons.Emission)]
        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_PPEmissionMultiplier")]
        [MinMaxRange(0, 5)]
        [ControlOverrider]
        public Parameter<float> EmissionMultiplier = new(1) { Enable = true };

        [MenuIcon(Icons.Color)]
        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_PPFinalColorMultiplier")]
        [MinMaxRange(0, 5)]
        public Parameter<float> FinalColorMultiplier = new(1) { Enable = true };
    }

    [Serializable]
    [MenuIcon(Icons.Backlight)]
    [ShaderFeature(BuiltinSupportedShaders.Poiyomi)]
    [SettingOptions(id: "backlight", menuPath: "Backlight", parameterPrefix: "Backlight")]
    [ShowInfoBox(RuntimeMessageType.Tips, "category:poiyomi/backlight/tips")]
    public sealed class BacklightSettings : Settings<BacklightSettings>
    {
        // パラメーター名は基本的にlilToonの逆光と同じ。
        // UseBacklightがBacklightEnabledに対応してそうなのと、PoiyomiにはBacklightColorTex等があるらしい？

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_BacklightColor")]
        [MenuIcon(Icons.Color)]
        [MinMaxRange(0, 20)]
        public Parameter<Color> BacklightColor = new(Color.white) { Enable = true, MinMaxRange = new(0, 2) };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_BacklightMainStrength")]
        [MenuIcon(Icons.BacklightMainStrength)]
        [Range(0, 1)]
        public Parameter<float> BacklightMainStrength = new(0.0f) { Enable = true, Animation = false };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_BacklightBorder")]
        [MenuIcon(Icons.BacklightBorder)]
        [Range(0, 1)]
        public Parameter<float> BacklightBorder = new(0.65f) { Enable = true };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_BacklightBlur")]
        [MenuIcon(Icons.BacklightBlur)]
        [Range(0, 1)]
        public Parameter<float> BacklightBlur = new(0.05f) { Enable = true };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_BacklightDirectivity")]
        [MenuIcon(Icons.BacklightDirectivity)]
        [MinMaxRange(0, 10)]
        public Parameter<float> BacklightDirectivity = new(5.0f) { Enable = true, MinMaxRange = new(0, 5) };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_BacklightViewStrength")]
        [MenuIcon(Icons.BacklightViewStrength)]
        [Range(0, 1)]
        public Parameter<float> BacklightViewStrength = new(0.0f) { Enable = true };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_BacklightReceiveShadow")]
        [MenuIcon(Icons.UseBacklight)]
        public Parameter<bool> ReceiveShadow = new(true) { Enable = true, Animation = false };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_BacklightBackfaceMask")]
        [MenuIcon(Icons.UseBacklight)]
        public Parameter<bool> BackfaceMask = new(true) { Enable = true, Animation = false };
        
        [GroupHeader("common:label/options")]

        [TogglePopout]
        public bool AddLightDirectionMenu = true;
    }

    [Serializable]
    [MenuIcon(Icons.ShadowEnvStrength)]
    [ShaderFeature(BuiltinSupportedShaders.Poiyomi)]
    [SettingOptions(id: "ssao", menuPath: "SSAO", parameterPrefix: "SSAO", enabled: false)]
#if !POIYOMI_PRO
    [Disabled("category:poiyomi/poiyomi-pro/disabled/reason")]
#endif
    public sealed class SSAOSettings : Settings<SSAOSettings>
    {
        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_SSAOAnimationToggle")]
        [MenuIcon(Icons.UseBacklight)]
        public Parameter<bool> Toggle = new(true) { Enable = true, Animation = true, Saved = false, };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_SSAOIntensity")]
        [MinMaxRange(0, 5)]
        public Parameter<float> Intensity = new(1f) { Enable = false, Animation = true, Saved = false, MinMaxRange = new(0, 5), };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_SSAORadius")]
        [MinMaxRange(0.001f, 0.05f)]
        public Parameter<float> Radius = new(0.002f) { Enable = false, Animation = true, Saved = false, MinMaxRange = new(0.001f, 0.05f), };

        [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_SSAOQuality")]
        [MinMaxRange(1, 10)]
        public Parameter<float> Quality = new(2.4f) { Enable = false, Animation = true, Saved = false, MinMaxRange = new(1, 10) };
    }
}

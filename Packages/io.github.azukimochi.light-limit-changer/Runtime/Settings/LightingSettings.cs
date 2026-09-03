namespace io.github.azukimochi;

[Serializable]
[MenuIcon(Icons.Light)]
[SettingOptions(id: "lighting", menuPath: "Lighting", parameterPrefix: "Light")]
public sealed partial class LightingSettings : Settings<LightingSettings>
{
    /// <summary>
    /// 明るさの下限
    /// </summary>
    [MinMaxRange(0, 1)]
    [GeneralControl(GeneralControlType.MinLight)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_LightMinLimit")]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_LightingMinLightBrightness")]
    [MaterialPropertyName(BuiltinSupportedShaders.UnlitWF, "_GL_LevelMin")]
    [MenuIcon(Icons.Light_Min)]
    public Parameter<float> MinLight = 0.05f;

    /// <summary>
    /// 明るさの上限
    /// </summary>
    [MinMaxRange(0, 10)]
    [GeneralControl(GeneralControlType.MaxLight)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_LightMaxLimit")]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_LightingCap", "_LightingAdditiveLimit")]
    [MenuIcon(Icons.Light_Max)]
    public Parameter<float> MaxLight = 1;

    /// <summary>
    /// 光の色の無視具合
    /// </summary>
    [GeneralControl(GeneralControlType.Monochrome)]
    [MenuIcon(Icons.Monochrome)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_MonochromeLighting")]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_LightingMonochromatic", "_LightingAdditiveMonochromatic")]
    [Range(0, 1)]
    public Parameter<float> Monochrome = 0;

    /// <summary>
    /// 光の無視具合
    /// </summary>
    [GeneralControl(GeneralControlType.Unlit)]
    [MenuIcon(Icons.Unlit)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_AsUnlit")]
    [Range(0, 1)]
    public Parameter<float> Unlit = 0;
    
    /// <summary>
    /// エミッションの強度
    /// </summary>
    [GeneralControl(GeneralControlType.EmissionStrength)]
    [MenuIcon(Icons.Emission)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_EmissionBlend", "_Emission2ndBlend")]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_PPEmissionMultiplier")]
    [Range(0, 1)]
    public Parameter<float> EmissionStrength = 1;

    /// <summary>
    /// ライトの向き
    /// </summary>
    [GeneralControl(GeneralControlType.LightDirection)]
    [MenuIcon(Icons.Emission)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_LightDirectionOverride")]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_LightngForcedDirection"/*Typoではない*/)]
    [Range(0, 1)]
    public Parameter<float> LightDirection = 0;
    
    [GroupHeader("common:label/options")]

    public LightDirectionMode LightDirectionMode = LightDirectionMode.World;

    public bool AllowAdvancedLightDirectionControl;
}

public enum LightDirectionMode
{
    World,
    Local,
}

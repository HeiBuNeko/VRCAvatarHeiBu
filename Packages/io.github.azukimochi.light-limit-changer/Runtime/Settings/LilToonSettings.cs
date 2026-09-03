using System.Collections.Generic;

namespace io.github.azukimochi;

[Serializable]
[MenuIcon(Icons.LilToon)]
[SettingOptions(id: "lilToon", menuPath: "LilToon", parameterPrefix: "LilToon")]
public sealed partial class LilToonSettings : Settings<LilToonSettings>, ISettingsProvider
{
    /// <summary>
    /// 影色への環境光影響度
    /// </summary>
    [ShaderFeature(BuiltinSupportedShaders.LilToon)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_ShadowEnvStrength")]
    [MenuIcon(Icons.ShadowEnvStrength)]
    [Range(0, 1)]
    public Parameter<float> ShadowEnvStrength = new (0) {Animation = false} ;

    /// <summary>
    /// 頂点ライトの強度
    /// </summary>
    [ShaderFeature(BuiltinSupportedShaders.LilToon)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_VertexLightStrength")]
    [MenuIcon(Icons.VertexLightStrength)]
    [Range(0, 1)]
    public Parameter<float> VertexLightStrength = new (0) {Animation = false};

    /// <summary>
    /// 距離フェード
    /// </summary>
    public LilDistanceFadeSettings DistanceFade;

    /// <summary>
    /// 逆光ライト
    /// </summary>
    public LilBacklightSettings Backlight;

    /// <summary>
    /// 影
    /// </summary>
    public LilShadowSettings Shadow;

    /// <summary>
    /// リムライト
    /// </summary>
    public LilRimlightSettings Rimlight;

    /// <summary>
    /// VRC Light Volumes
    /// </summary>
    public LilVRCLightVolumesSettings VRCLightVolumes;

    IEnumerable<ISettingsProvider> ISettingsProvider.Children
    {
        get
        {
            yield return DistanceFade;
            yield return Backlight;
            yield return Shadow;
            yield return Rimlight;
            yield return VRCLightVolumes;
        }
    }
}

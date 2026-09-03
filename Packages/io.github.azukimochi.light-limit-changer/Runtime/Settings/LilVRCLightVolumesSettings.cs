namespace io.github.azukimochi;

partial class LilToonSettings
{
    [Serializable]
    [MenuIcon(Icons.Light)]
    [SettingOptions(id: "vrc-light-volumes", menuPath: "VRC LV", parameterPrefix: "VRCLV", enabled: true)]
    [ShaderFeature(BuiltinSupportedShaders.LilToon)]
#if !LILTOON_SUPPORTS_VRCLV_CONTROL
    [Disabled(reason: "category:lilToon/vrc-light-volumes/disabled/reason")]
#endif
    public sealed class LilVRCLightVolumesSettings : Settings<LilVRCLightVolumesSettings>
    {
        [Range(0, 3)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_EnvRimBorder")]
        public Parameter<float> RimBorder = 0.85f;

        [Range(0, 1)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_EnvRimBlur")]
        public Parameter<float> RimBlur = 0.35f;
    }
}

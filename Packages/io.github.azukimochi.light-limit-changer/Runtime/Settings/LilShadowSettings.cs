namespace io.github.azukimochi;

partial class LilToonSettings
{
    /// <summary>
    /// 影設定
    /// 将来的にlilToonの影設定(_UseShadow, _ShadowBorder等)の制御項目を追加していく想定
    /// </summary>
    [Serializable]
    [MenuIcon(Icons.ShadowEnvStrength)]
    [SettingOptions(id: "shadow", menuPath: "Shadow", parameterPrefix: "Shadow", enabled: false)]
    public sealed class LilShadowSettings : Settings<LilShadowSettings>
    {
        /// <summary>
        /// 影を受け取る (Cast Shadow)
        /// 影色1/2/3の_ShadowReceiveを連動して制御する
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_ShadowReceive", "_Shadow2ndReceive", "_Shadow3rdReceive")]
        [MenuIcon(Icons.ShadowEnvStrength)]
        [ShowInfoBox(RuntimeMessageType.Warning, "settings:lilToon/shadow/receive-cast-shadow/caution")]
        [Range(0, 1)]
        public Parameter<float> ReceiveCastShadow = new(0);
    }
}

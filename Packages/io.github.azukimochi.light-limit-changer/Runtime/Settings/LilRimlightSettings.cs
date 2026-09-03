namespace io.github.azukimochi;

partial class LilToonSettings
{
    [Serializable]
    [MenuIcon(Icons.Backlight)]
    [SettingOptions(id: "rimlight", menuPath: "Rimlight", parameterPrefix: "Rimlight", enabled: false)]
    [ShowInfoBox(RuntimeMessageType.Tips, "category:lilToon/rimlight/tips")]

    public class LilRimlightSettings : Settings<LilRimlightSettings>
    {
        /// <summary>
        /// リムライトを有効にする
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_UseRim")]
        [MenuIcon(Icons.UseBacklight)]
        public Parameter<bool> UseRimLight = new(true) { Enable = false }; 

        /// <summary>
        /// リムライトのカラー
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_RimColor")]
        [MenuIcon(Icons.Color)]
        [ColorPropertySettings(HDR = true)]
        public Parameter<Color> Color = new(new Color(0.66f, 0.5f, 0.48f, 0.3f)) { Enable = true, Animation = false };

        /// <summary>
        /// メインカラーの強度
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_RimMainStrength")]
        [MenuIcon(Icons.ShadowEnvStrength)]
        [Range(0, 1)]
        public Parameter<float> MainColorStrength = new(0.0f) { Enable = true, Animation = false };
        
        /// <summary>
        /// ライトの明るさを反映
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_RimEnableLighting")]
        [MenuIcon(Icons.ShadowEnvStrength)]
        [Range(0, 1)]
        public Parameter<float> RimEnableLighting = new(1.0f) { Enable = true, Animation = false };
        
        /// <summary>
        /// 影部分で無効化
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_RimShadowMask")]
        [MenuIcon(Icons.ShadowEnvStrength)]
        [Range(0, 1)]
        public Parameter<float> RimShadowMask = new(0.5f) { Enable = true, Animation = false };
        
        /// <summary>
        /// 裏面で無効化
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_RimBackfaceMask")]
        [MenuIcon(Icons.ShadowEnvStrength)]
        public Parameter<bool> RimBackfaceMask = new(true) { Enable = true, Animation = false };
        
        /// <summary>
        /// 範囲
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_RimBorder")]
        [MenuIcon(Icons.BacklightBorder)]
        [Range(0, 1)]
        public Parameter<float> RimBorder = new(0.5f) { Enable = true, Animation = false };
        
        /// <summary>
        /// ぼかし
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_RimBlur")]
        [MenuIcon(Icons.BacklightBlur)]
        [Range(0, 1)]
        public Parameter<float> RimBlur = new(0.65f) { Enable = true, Animation = false };
        
        /// <summary>
        /// リムライトの細さ
        /// </summary>
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_RimFresnelPower")]
        [MenuIcon(Icons.BacklightViewStrength)]
        [Range(0.01f, 50)]
        public Parameter<float> RimFresnelPower = new(3.5f) { Enable = true, Animation = false  };
    }
}
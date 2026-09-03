namespace io.github.azukimochi;

partial class LilToonSettings
{
    // TODO: 正式版出すときにここの名前変えたい（DistanceFadeSettings）
    [Serializable]
    [MenuIcon(Icons.DistanceFade)]
    [SettingOptions(id: "distance-fade", menuPath: "DistanceFade", parameterPrefix: "DistanceFade", enabled: false)]
    [ShowInfoBox(RuntimeMessageType.Tips, "category:lilToon/distance-fade/tips")]
    public sealed class LilDistanceFadeSettings : Settings<LilDistanceFadeSettings>
    {
        /// <summary>
        /// 距離フェード
        /// </summary>

        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_DistanceFadeColor")]
        [MenuIcon(Icons.Color)]
        [MinMaxRange(0, 5)]
        [ColorPropertySettings(HDR = true, Alpha = true)]
        public Parameter<Color> FadeColor = new(Color.black) { Enable = true, Animation = false };

        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [VectorField(VectorField.X)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_DistanceFade.x")]
        [MenuIcon(Icons.DistanceFadeX)]
        [MinMaxRange(0, 10)]
        public Parameter<float> Start = new(0.1f) { Enable = true, Animation = false };

        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [VectorField(VectorField.Y)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_DistanceFade.y")]
        [MenuIcon(Icons.DistanceFadeY)]
        [MinMaxRange(0, 10)]
        public Parameter<float> End = new(0.01f) { Enable = true, Animation = false };

        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [VectorField(VectorField.Z)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_DistanceFade.z")]
        [MenuIcon(Icons.DistanceFadeZ)]
        [MinMaxRange(0, 2)]
        public Parameter<float> Strength = new(0.0f) { Enable = true, Animation = false };

        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [VectorField(VectorField.W)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_DistanceFade.w")]
        [MenuIcon(Icons.DistanceFadeW)]
        public Parameter<bool> BackfaceForceShadow = new(false) { Enable = true, Animation = false };
        
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_DistanceFadeRimColor")]
        [MenuIcon(Icons.Color)]
        [ColorPropertySettings(HDR = true, Alpha = false)]
        public Parameter<Color> RimColor = new(Color.black) { Enable = true, Animation = false };
        
        [ShaderFeature(BuiltinSupportedShaders.LilToon)]
        [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_DistanceFadeRimFresnelPower")]
        [MenuIcon(Icons.BacklightViewStrength)]
        [Range(0.01f, 50)]
        public Parameter<float> RimFresnelPower = new(5.0f) { Enable = true, Animation = false };
    }
}
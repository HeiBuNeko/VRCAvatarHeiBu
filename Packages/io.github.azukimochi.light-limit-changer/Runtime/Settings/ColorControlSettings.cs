namespace io.github.azukimochi;

[Serializable]
[MenuIcon(Icons.Color)]
[SettingOptions(id: "color-control", menuPath: "Color Control", parameterPrefix: "Color")]
[ShowInfoBox(RuntimeMessageType.Info, "category:color-control/texture-bake-information")]
[ShowInfoBox(RuntimeMessageType.Tips, "category:color-control/tips")]
public sealed class ColorControlSettings : Settings<ColorControlSettings>
{
    [GroupHeader("category:color-control/group/color-adjust" /* 色調補正 */)]

    [GeneralControl(GeneralControlType.ColorControlHue)]
    [VectorField(VectorField.X)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_MainTexHSVG.x")]
    [MenuIcon(Icons.Hue)]
    [Range(-1, 1)]
    public Parameter<float> Hue = 0;

    [GeneralControl(GeneralControlType.ColorControlSaturation)]
    [VectorField(VectorField.Y)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_MainTexHSVG.y")]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_Saturation")]
    [MenuIcon(Icons.Saturation)]
    [Range(0, 2)]
    public Parameter<float> Saturation = 1;

    [GeneralControl(GeneralControlType.ColorControlBrightness)]
    [VectorField(VectorField.Z)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_MainTexHSVG.z")]
    [MenuIcon(Icons.Brightness)]
    [Range(0, 2)]
    public Parameter<float> Brightness = 1;

    [GeneralControl(GeneralControlType.ColorControlGamma)]
    [VectorField(VectorField.W)]
    [MaterialPropertyName(BuiltinSupportedShaders.LilToon, "_MainTexHSVG.w")]
    [MaterialPropertyName(BuiltinSupportedShaders.Poiyomi, "_MainGamma")]
    [MenuIcon(Icons.Gamma)]
    [Range(0.01f, 2)]
    public Parameter<float> Gamma = 1;

    [GroupHeader("category:color-control/group/color-change" /* 色変更 */)]

    [GeneralControl(GeneralControlType.ColorTemperature)]
    [DisableInitialValueSlider]
    [DisableOverwriteMaterialValue]
    [MenuIcon(Icons.ColorTemperature)]
    [Range(-1, 1)]
    public Parameter<float> ColorTemperature = 0;
}

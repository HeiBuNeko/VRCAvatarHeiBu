namespace io.github.azukimochi;

public enum GeneralControlType
{
    // Lighting
    MinLight = 1,
    MaxLight,
    Monochrome,
    Unlit,
    EmissionStrength,
    LightDirection,

    // Color Control
    ColorControlHue,
    ColorControlSaturation,
    ColorControlBrightness,
    ColorControlGamma,
    ColorTemperature,
}

public static class GeneralControlIDs
{
    public const string MinLight = "lighting/min-light";
    public const string MaxLight = "lighting/max-light";
    public const string Monochrome = "lighting/monochrome";
    public const string Unlit = "lighting/unlit";
    public const string EmissionStrength = "lighting/emission-strength";
    public const string LightDirection = "lighting/light-direction";

    public const string ColorHue = "color-control/hue";
    public const string ColorSaturation = "color-control/saturation";
    public const string ColorBrightness = "color-control/brightness";
    public const string ColorGamma = "color-control/gamma";
    public const string ColorTemperature = "color-control/color-temperature";
}
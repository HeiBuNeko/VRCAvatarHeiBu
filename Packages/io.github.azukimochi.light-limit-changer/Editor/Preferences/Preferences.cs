namespace io.github.azukimochi;

internal static class Preferences
{
    internal const string PathRoot = "LightLimitChanger/";

    public static GlobalPrefs Global => GlobalPrefs.Instance;

    public static LocalPrefs Local => LocalPrefs.Instance;

    static Preferences()
    {
        EditorApplication.quitting += () =>
        {
            Global.Save();
            Local.Save();
        };
    }
}
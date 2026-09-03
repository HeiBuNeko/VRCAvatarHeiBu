namespace io.github.azukimochi
{
    internal sealed class GlobalPrefs : BasePrefs<GlobalPrefs>
    {
        public override bool UsePreferenceDirectory => true;
        public override string FilePath => $"{Preferences.PathRoot}GlobalPrefs";
        public byte[] Token;
    }
}
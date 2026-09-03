namespace io.github.azukimochi
{
    [FilePath("ProjectSettings/" + Preferences.PathRoot + nameof(LocalPrefs), FilePathAttribute.Location.ProjectFolder)]
    internal sealed class LocalPrefs : BasePrefs<LocalPrefs>
    {
        public override bool UsePreferenceDirectory => false;
        public override string FilePath => $"ProjectSettings/{Preferences.PathRoot}LocalPrefs";

        public InspectorMode InspectorMode;

        public ErrorSuppression ErrorSuppression = new();
    }
}
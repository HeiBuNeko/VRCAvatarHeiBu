#if UNITY_EDITOR
using UnityEditor;
#endif

namespace io.github.azukimochi;

[Serializable]
public struct SemVer : IEquatable<SemVer>, IComparable<SemVer>
{
    public int Major;
    public int Minor;
    public int Patch;
    public string Label;

    public SemVer(int major, int minor, int patch, string label = "")
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Label = label;
    }

    public static SemVer Parse(ReadOnlySpan<char> value)
    {
        var ranges = (stackalloc Range[6]);
        int count = value.SplitAny(ranges, ".-");
        if (count < 3)
            return default;

        var major = value[ranges[0]];
        var minor = value[ranges[1]];
        var patch = value[ranges[2]];
        var label = count < 4 ? "" : value[ranges[3].Start..].ToString();
        if (label != null)
        {
            var buildMeta = label.IndexOf('+');
            if (buildMeta > 0)
            {
                label = label[..buildMeta];
            }
        }

        return new SemVer(int.Parse(major), int.Parse(minor), int.Parse(patch), label);
    }

    public readonly int CompareTo(SemVer other)
    {
        var major = Major.CompareTo(other.Major);
        if (major != 0)
            return major;

        var minor = Minor.CompareTo(other.Minor);
        if (minor != 0)
            return minor;

        int patch = Patch.CompareTo(other.Patch);
        if (patch != 0)
            return patch;

        if (Label.Length == 0 && other.Label.Length != 0)
            return 1;
        else if (Label.Length != 0 && other.Label.Length == 0)
            return -1;
        else
            return Label.AsSpan().CompareTo(other.Label, StringComparison.OrdinalIgnoreCase);
    }

    public readonly override bool Equals(object obj)
    {
        if (obj is not SemVer other)
            return false;

        return Equals(other);
    }

    public readonly bool Equals(SemVer other)
    {
        return
            this.Major == other.Major &&
            this.Minor == other.Minor &&
            this.Patch == other.Patch &&
            this.Label == other.Label;
    }

    public readonly override string ToString() => $"{Major}.{Minor}.{Patch}{(Label.AsSpan().IsEmpty ? "" : "-")}{Label}";

    public readonly override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Label);

    public readonly bool IsDefault => Label is null && Major == 0 && Minor == 0 && Patch == 0;

    public static implicit operator SemVer(int major) => new(major, 0, 0);

    public static bool operator >(SemVer left, SemVer right) => left.CompareTo(right) > 0;
    public static bool operator <(SemVer left, SemVer right) => left.CompareTo(right) < 0;
    public static bool operator >=(SemVer left, SemVer right) => left.CompareTo(right) >= 0;
    public static bool operator <=(SemVer left, SemVer right) => left.CompareTo(right) <= 0;
    public static bool operator ==(SemVer left, SemVer right) => left.Equals(right);
    public static bool operator !=(SemVer left, SemVer right) => !left.Equals(right);
#if UNITY_EDITOR
    public static void Set(SerializedProperty property, SemVer value)
    {
        property.FindPropertyRelative(nameof(Major)).intValue = value.Major;
        property.FindPropertyRelative(nameof(Minor)).intValue = value.Minor;
        property.FindPropertyRelative(nameof(Patch)).intValue = value.Patch;
        property.FindPropertyRelative(nameof(Label)).stringValue = value.Label;
    }
#endif
}

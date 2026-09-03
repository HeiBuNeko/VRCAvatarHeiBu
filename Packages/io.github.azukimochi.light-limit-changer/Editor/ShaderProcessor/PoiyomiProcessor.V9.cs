namespace io.github.azukimochi;

internal partial class PoiyomiProcessor
{
    internal class V9 : V8
    {
        protected override int MinimumMajorVersion => 9;
        public override int GetHashCode() => HashCode.Combine(9, base.GetHashCode());
    }
}

namespace io.github.azukimochi;

internal partial class PoiyomiProcessor
{
    internal class V10 : V9
    {
        protected override int MinimumMajorVersion => 10;
        public override int GetHashCode() => HashCode.Combine(10, base.GetHashCode());
    }
}

namespace io.github.azukimochi;

internal static class AssetDisposer
{
    public static AssetDisposer<T> Create<T>(T asset) where T : Object
    {
        return new AssetDisposer<T>(asset);
    }
}

internal readonly ref struct AssetDisposer<T> where T : Object
{
    public readonly T Asset;

    public AssetDisposer(T asset)
    {
        Asset = asset;
    }

    public void Dispose()
    {
        if (Asset != null)
            Object.DestroyImmediate(Asset);
    }
}

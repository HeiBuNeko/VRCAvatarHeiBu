namespace io.github.azukimochi;

internal static class AssetUtils
{
    public static T FromGUID<T>(string guid) where T : Object
    {
        if (string.IsNullOrEmpty(guid))
            return null;
        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
            return null;
        return AssetDatabase.LoadAssetAtPath<T>(path); 
    }

    public static Texture2D BlackTexture
    {
        get
        {
            if (blackTexture == null)
                blackTexture = FromGUID<Texture2D>("b2473a6e1e869a44e92a63b92026f56d");
            return blackTexture;
        }
    }
    private static Texture2D blackTexture;

    public static Texture2D WhiteTexture
    {
        get
        {
            if (whiteTexture == null)
                whiteTexture = FromGUID<Texture2D>("16ca6778089992e42896e8d38ca7d136");
            return whiteTexture;
        }
    }
    private static Texture2D whiteTexture;
}
namespace io.github.azukimochi;

internal static class MaterialExt
{
    public static T Get<T>(this Material material, string propertyName, T defaultValue = default)
    {
        if (material.TryGet(propertyName, out T value))
        {
            return value;
        }
        return defaultValue;
    }

    public static bool TryGet<T>(this Material material, string propertyName, out T value)
    {
        Debug.Assert(typeof(T) == typeof(int) || typeof(T) == typeof(float) || typeof(T) == typeof(Color) || typeof(T) == typeof(Vector4) || typeof(Texture).IsAssignableFrom(typeof(T)), $"Type {typeof(T)} is not supported.");

        if (!material.HasProperty(propertyName))
        {
            value = default;
            return false;
        }

        if (typeof(T) == typeof(int))
        {
            value = (T)(object)material.GetInt(propertyName);
            return true;
        }

        if (typeof(T) == typeof(float))
        {
            value = (T)(object)material.GetFloat(propertyName);
            return true;
        }

        if (typeof(T) == typeof(Color))
        {
            value = (T)(object)material.GetColor(propertyName);
            return true;
        }

        if (typeof(T) == typeof(Vector4))
        {
            value = (T)(object)material.GetVector(propertyName);
            return true;
        }

        if (typeof(Texture).IsAssignableFrom(typeof(T)))
        {
            var t = material.GetTexture(propertyName);
            if (t is T)
            {
                value = (T)(object)t;
                return true;
            }
        }

        value = default;
        return false;
    }
}
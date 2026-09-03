namespace io.github.azukimochi;

internal static class Utils
{
    /// <summary>
    /// 値を範囲内に丸める
    /// </summary>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static float NormalizeInRange(float value, float min, float max)
        => (value - min) / (max - min);

    /// <summary>
    /// 温度ナイズされた色を返す
    /// </summary>
    /// <param name="value">-1から1</param>
    /// <returns></returns>
    public static Color GetColorTempertured(float value)
    {
        var white = Color.white;
        var cold = new Color(0.6f, 0.95f, 1, 1);
        var warm = new Color(1, 0.8f, 0.6f, 1);
        if (value == 0)
            return white;
        return value < 0 ? Color.Lerp(cold, white, Math.Abs(value)) : Color.Lerp(white, warm, value); 
    }
}
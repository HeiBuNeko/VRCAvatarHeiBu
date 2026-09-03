namespace io.github.azukimochi;

internal static class ColorExt 
{
    public static string GetColorCodeRGB(this Color color)
    {
        if (color == Color.white)
        {
            return "#FFFFFF";
        }
        else if (color == Color.black)
        {
            return "#000000";
        }

        return $"#{ColorUtility.ToHtmlStringRGB(color)}";
    }
}
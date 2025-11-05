using System;

namespace com.ams.avatarmodifysupport.utils
{
    public static class PathUtility
    {
        internal static string ConvertToUnityPath(string absolutePath)
        {
            int startIndex = absolutePath.IndexOf("Assets/", StringComparison.Ordinal);
            if (startIndex == -1) startIndex = absolutePath.IndexOf("Assets\\", StringComparison.Ordinal);
            if (startIndex == -1) return "";

            return absolutePath.Substring(startIndex);
        }
    }
}
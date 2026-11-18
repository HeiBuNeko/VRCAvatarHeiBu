using UnityEditor;
using UnityEngine;

namespace RedNightWorks.NadeSystem
{
    internal static class NadeSystemVersion
    {
        // Version.txt is the shared, editable source for both editor interfaces.
        private const string VersionAssetGuid = "1159777535054e6185257aee6d71ef56";

        internal static string DisplayText
        {
            get
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    AssetDatabase.GUIDToAssetPath(VersionAssetGuid));
                return asset != null ? asset.text.Trim() : string.Empty;
            }
        }
    }
}

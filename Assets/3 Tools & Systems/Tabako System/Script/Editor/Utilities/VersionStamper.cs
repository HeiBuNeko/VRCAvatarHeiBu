using System.Text.RegularExpressions;
using net.satania_shopping.tabakosystem.runtime;
using UnityEditor;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class VersionStamper
    {
        private static readonly Regex s_releaseDate = new Regex(@"\s*\(\s*\d{4}[-/.]\d{1,2}[-/.]\d{1,2}\s*\)");

        public static void Stamp(TabakoScriptBehaviour script)
        {
            if (script == null)
                return;

            string version = VersionChecker.CurrentVersion;
            if (string.IsNullOrEmpty(version))
                return;

            var so = new SerializedObject(script);
            var created = so.FindProperty("createdVersion");
            var lastSaved = so.FindProperty("lastSavedVersion");
            if (created == null || lastSaved == null)
                return;

            if (string.IsNullOrEmpty(created.stringValue))
                created.stringValue = version;
            lastSaved.stringValue = version;
            so.ApplyModifiedProperties();
        }

        public static string Display(string version, string unknownText)
        {
            if (string.IsNullOrEmpty(version))
                return unknownText;

            string withoutDate = s_releaseDate.Replace(version, "").Trim();
            return withoutDate.Length > 0 ? withoutDate : unknownText;
        }
    }
}

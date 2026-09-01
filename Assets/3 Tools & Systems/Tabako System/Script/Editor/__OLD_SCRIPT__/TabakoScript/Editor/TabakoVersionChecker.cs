//#define DEBUG_TABAKO_EDITOR

using System.Net;
using UnityEditor;
using UnityEngine;

namespace satania.tabakosystem
{
    public static class TabakoVersionChecker
    {
        const string URL__VERSION = @"http://files.satania.net/Tabako/Version";

#if DEBUG_TABAKO_EDITOR
        const string URL__VERSIONDEBUG = @"http://files.satania.net/Tabako/VersionDebug";
#endif

        public class TabakoVersionSingleton : ScriptableSingleton<TabakoVersionSingleton>
        {
            public string installedVersion = "v0.0.0 (YYYY-MM-DD)";
            public string latestVersion = "v0.0.0 (YYYY-MM-DD)";
            public bool canUpdate;
        }

        static TabakoVersionSingleton tvs => TabakoVersionSingleton.instance;

        public static string InstalledVersion => tvs.installedVersion;
        public static string LatestVersion => tvs.latestVersion;
        public static bool CanUpdate => tvs.canUpdate;

        [InitializeOnLoadMethod]
        static void TabakoVersionCheckerLoad()
        {
            LoadVersion();

            try
            {
                using (WebClient client = new WebClient())
                {
                    string installedVersion = InstalledVersion.Substring(0, InstalledVersion.IndexOf(' '));

#if DEBUG_TABAKO_EDITOR
                    tvs.latestVersion = client.DownloadString(URL__VERSIONDEBUG);
#else
                    tvs.latestVersion = client.DownloadString(URL__VERSION);
#endif

                    tvs.canUpdate = !installedVersion.Equals(LatestVersion.Substring(0, LatestVersion.IndexOf(' ')));
                }
            }
            catch (System.Exception ex)
            {
                tvs.installedVersion = "WebClient ERROR";

                Debug.LogError($"[TabakoVersionChecker] Error: {ex.Message}");
            }
        }

        public static void LoadVersion()
        {
            tvs.latestVersion = "v0.0.0 (YYYY-MM-DD)";
            tvs.installedVersion = "v0.0.0 (YYYY-MM-DD)";

            string versionPath = AssetDatabase.GUIDToAssetPath("304af541c31f7f74aaa5aa355d182512");
            if (!string.IsNullOrEmpty(versionPath))
            {
                string loadedText = System.IO.File.ReadAllText(versionPath);

                if (!string.IsNullOrEmpty(loadedText))
                    tvs.installedVersion = loadedText;
                else
                    tvs.installedVersion = "ReadFile ERROR";
            }
        }
    }
}
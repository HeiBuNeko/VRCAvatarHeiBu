//#define DEBUG_TABAKO_EDITOR

using System;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    public enum VersionComparisonResult
    {
        /// 比較対象より新しい
        GreaterThan,
        /// 比較対象より古い
        LessThan,
        /// 同じバージョン
        EqualTo,
        /// パース失敗
        Invalid
    }

    public static class VersionChecker
    {
        private const string URL__VERSION = @"http://files.satania.net/Tabako/Version";

#if DEBUG_TABAKO_EDITOR
        private const string URL__VERSIONDEBUG = @"http://files.satania.net/Tabako/VersionDebug";
#endif

        private static string versionTextPath => AssetDatabase.GUIDToAssetPath("50326b29b42c81a4999e3c6fbe9e0638");

        public class TabakoVersionSingleton : ScriptableSingleton<TabakoVersionSingleton>
        {
            public string installedVersion = "v0.0.0 (YYYY-MM-DD)";
            public string latestVersion = "v0.0.0 (YYYY-MM-DD)";
            public VersionComparisonResult versionComparisonResult = VersionComparisonResult.Invalid;
        }

        private static TabakoVersionSingleton tvs => TabakoVersionSingleton.instance;

        internal static string CurrentVersion => tvs.installedVersion;
        internal static string LatestVersion => tvs.latestVersion;

        internal static VersionComparisonResult ComparisonResult => tvs.versionComparisonResult;

        internal static void LoadVersion()
        {
            tvs.latestVersion = "v0.0.0 (YYYY-MM-DD)";
            tvs.installedVersion = "v0.0.0 (YYYY-MM-DD)";

            if (!string.IsNullOrEmpty(versionTextPath))
            {
                string loadedText = System.IO.File.ReadAllText(versionTextPath);

                if (!string.IsNullOrEmpty(loadedText))
                    tvs.installedVersion = loadedText;
                else
                    tvs.installedVersion = "ReadFile ERROR";
            }
        }

        public static VersionComparisonResult CompareVersions(string inputWithVersion, string versionToCompare)
        {
            // 正規表現で x.x.xを取り出す
            var inputMatch = Regex.Match(inputWithVersion, @"\d+\.\d+\.\d+");
            var versionMatch = Regex.Match(versionToCompare, @"\d+\.\d+\.\d+");

            if (!inputMatch.Success || !versionMatch.Success)
            {
                // inputWithVersion からバージョンを抽出できなかった
                return VersionComparisonResult.Invalid;
            }

            try
            {
                // 抽出した文字列と、比較対象の文字列からVersionオブジェクトを作成
                var versionFromInput = new Version(inputMatch.Value);
                var versionToCompareObj = new Version(versionMatch.Value);

                // Versionオブジェクト同士を比較
                int comparison = versionFromInput.CompareTo(versionToCompareObj);

                if (comparison > 0)
                {
                    return VersionComparisonResult.GreaterThan;
                }
                else if (comparison < 0)
                {
                    return VersionComparisonResult.LessThan;
                }
                else
                {
                    return VersionComparisonResult.EqualTo;
                }
            }
            catch (Exception)
            {
                // Versionオブジェクトの作成に失敗した (形式が不正など)
                return VersionComparisonResult.Invalid;
            }
        }

        private static void CheckVersion()
        {
            LoadVersion();

            try
            {
                using (WebClient client = new WebClient())
                {
                    string installedVersion = CurrentVersion.Substring(0, CurrentVersion.IndexOf(' '));

#if DEBUG_TABAKO_EDITOR
                    tvs.latestVersion = client.DownloadString(URL__VERSIONDEBUG);
#else
                    tvs.latestVersion = client.DownloadString(URL__VERSION);
#endif

                    tvs.versionComparisonResult = CompareVersions(installedVersion, tvs.latestVersion);
                }
            }
            catch (System.Exception ex)
            {
                tvs.installedVersion = "WebClient ERROR";

                Debug.LogError($"[TabakoVersionChecker] Error: {ex.Message}");
            }
        }

        [MenuItem(MenuItemDictionary.p_RecheckVersion, priority = 100)]
        private static void ReloadVersion()
        {
            CheckVersion();

            EditorUtility.DisplayDialog("Satania Tabako System", $"現在のさたにあ式タバコの最新バージョンは \n{LatestVersion.Replace($"{ '\n'}", string.Empty)}\nです。"
                + "\n\n 使用中のバージョン: " + CurrentVersion, "OK");
        }

        [MenuItem(MenuItemDictionary.p_OpenBoothPage, priority = 101)]
        private static void OpenBoothPage()
        {
            EditorGUIUtils.OpenBoothUrl();
        }

        [InitializeOnLoadMethod]
        private static void TabakoVersionCheckerLoad()
        {
            CheckVersion();
        }
    }
}
//#define DEBUG_TABAKO_EDITOR

using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

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
        private const string URL__VERSION = @"https://saturnianjp.github.io/Satabako_Scripts/version.txt";

#if DEBUG_TABAKO_EDITOR
        private const string URL__VERSIONDEBUG = @"https://saturnianjp.github.io/Satabako_Scripts/version-debug.txt";
#endif

        //サーバーに繋がらないときにエディタを待たせないよう短めにしておく
        private const int k_RequestTimeoutSeconds = 5;

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

        /// <summary>
        /// 最新バージョンを非同期で取得する。onCompleted には取得に成功したかどうかが渡される
        /// </summary>
        private static void CheckVersion(Action<bool> onCompleted = null)
        {
            LoadVersion();

#if DEBUG_TABAKO_EDITOR
            var request = UnityWebRequest.Get(URL__VERSIONDEBUG);
#else
            var request = UnityWebRequest.Get(URL__VERSION);
#endif
            request.timeout = k_RequestTimeoutSeconds;

            request.SendWebRequest().completed += _ =>
            {
                bool succeeded;
                using (request)
                {
                    succeeded = OnVersionReceived(request);
                }

                onCompleted?.Invoke(succeeded);
            };
        }

        private static bool OnVersionReceived(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                tvs.versionComparisonResult = VersionComparisonResult.Invalid;
                Debug.LogWarning($"[TabakoVersionChecker] 最新バージョンの取得に失敗しました: {request.error}");
                return false;
            }

            tvs.latestVersion = request.downloadHandler.text.Trim();
            tvs.versionComparisonResult = CompareVersions(CurrentVersion, tvs.latestVersion);

#if DEBUG_TABAKO_EDITOR
            Debug.Log($"[TabakoVersionChecker] installed={CurrentVersion} latest={tvs.latestVersion} result={tvs.versionComparisonResult} url={request.url}");
#endif

            //開いている Inspector にアップデート通知を反映する
            InternalEditorUtility.RepaintAllViews();
            return true;
        }

        [MenuItem(MenuItemDictionary.p_RecheckVersion, priority = 100)]
        private static void ReloadVersion()
        {
            CheckVersion(succeeded =>
            {
                string message = succeeded
                    ? $"現在のさたにあ式タバコの最新バージョンは \n{LatestVersion}\nです。"
                    : "最新バージョンの取得に失敗しました。";

                EditorUtility.DisplayDialog("Satania Tabako System", message
                    + "\n\n 使用中のバージョン: " + CurrentVersion, "OK");
            });
        }

        [MenuItem(MenuItemDictionary.p_OpenBoothPage, priority = 101)]
        private static void OpenBoothPage()
        {
            EditorGUIUtils.OpenBoothUrl();
        }

        [InitializeOnLoadMethod]
        private static void TabakoVersionCheckerLoad()
        {
            //ドメインリロードの最中を避け、エディタの初期化が終わってから取得する
            EditorApplication.delayCall += () => CheckVersion();
        }
    }
}

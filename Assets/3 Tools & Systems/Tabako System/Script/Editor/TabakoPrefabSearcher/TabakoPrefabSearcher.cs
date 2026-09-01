using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using VRC.SDK3.Avatars.Components;
using net.satania_shopping.tabakosystem.runtime;
using JetBrains.Annotations;
using System;

namespace net.satania_shopping.tabakosystem.editor
{
    [Flags]
    public enum PrefabSearchMode
    {
        [InspectorName("プレハブの名前")] PrefabName = 1,
        [InspectorName("アバターの名前")] AvatarName = 1 << 1,
        [InspectorName("ショップの名前")] ShopName = 1 << 2
    }

    public class TabakoPrefabSearcher : EditorWindow
    {
        private struct PrefabPair
        {
            internal GameObject prefab;
            internal TabakoScriptBehaviour tabakoScriptBehaviour;
        }

        private static string searchRegex;
        private Vector2 scrollPosition = new Vector2(0, 0);

        private static VRCAvatarDescriptor avatar;

        private static bool ignoreCase = true;

        private static List<PrefabPair> prefabPairs = new List<PrefabPair>();
        private static List<PrefabPair> matchedPrefabPairs = new List<PrefabPair>();

        private static PrefabSearchMode searchMode = PrefabSearchMode.PrefabName | PrefabSearchMode.AvatarName | PrefabSearchMode.ShopName;

        private void OnEnable()
        {
            ReloadPrefabs();
        }

#if DEBUG_TABAKO_EDITOR
        [MenuItem("さたにあしょっぴんぐ/さたにあ式タバコ/Copy Url (Debug)")]
        public static void CopyUrl()
        {
            List<string> urls = new List<string>();
            int emptyUrlCount = 0;
            if (prefabPairs.Count == 0)
                _ReloadPrefabsStatic();

            foreach (var p in prefabPairs)
            {
                if (string.IsNullOrEmpty(p.tabakoScriptBehaviour.AvatarURL))
                {
                    emptyUrlCount++;
                    continue;
                }

                if (urls.Contains(p.tabakoScriptBehaviour.AvatarURL))
                    continue;

                urls.Add(p.tabakoScriptBehaviour.AvatarURL);
            }

            EditorGUIUtility.systemCopyBuffer = string.Join('\n', urls.ToArray());
            EditorUtility.DisplayDialog("CopyUrl", $"{urls.Count}個のURLをコピーしました！" + $"\n{emptyUrlCount}個の空白なURLをスキップしました。", "OK");
        }

        [MenuItem("さたにあしょっぴんぐ/さたにあ式タバコ/Copy Url With Shop Name (Debug)")]
        public static void CopyUrlWithShopName()
        {
            string copy = "";
            foreach (var p in prefabPairs)
            {
                if (!string.IsNullOrEmpty(copy))
                    copy += '\n';

                copy += $"{p.tabakoScriptBehaviour.AvatarURL} | {p.tabakoScriptBehaviour.ShopName} | {p.prefab.name}";
            }

            EditorGUIUtility.systemCopyBuffer = copy;
        }
#endif

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            ignoreCase = EditorGUILayout.ToggleLeft(new GUIContent("大文字・小文字を無視する"), ignoreCase);
            searchMode = (PrefabSearchMode)EditorGUILayout.EnumFlagsField("検索モード", searchMode);
            searchRegex = EditorGUILayout.TextField("検索ワード (Search World)", searchRegex);
            if (EditorGUI.EndChangeCheck())
            {
                SearchPrefabs(searchRegex);
            }

            EditorGUI.BeginChangeCheck();
            avatar = EditorGUILayout.ObjectField("アバター (Avatar)", avatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            if (EditorGUI.EndChangeCheck())
            {
                if (avatar != null)
                {
                    var scene = avatar.gameObject.scene;
                    if (!scene.IsValid())
                    {
                        EditorUtility.DisplayDialog(titleContent.text, "シーン内に存在するオブジェクトを選択してください！", "OK");
                        avatar = null;
                    }
                    else
                    {
                        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (prefabStage != null && prefabStage.IsPartOfPrefabContents(avatar.gameObject))
                        {
                            EditorUtility.DisplayDialog(titleContent.text, "シーン内に存在するオブジェクトを選択してください！", "OK");
                            avatar = null;
                        }
                    }
                }
            }

            if (matchedPrefabPairs != null && matchedPrefabPairs.Count > 0)
            {
                DrawUILine(Color.gray);
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);

                foreach (var prefabPair in matchedPrefabPairs)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(prefabPair.prefab, typeof(GameObject), false);
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(avatar == null);
                    if (GUILayout.Button("アバターに入れる", GUILayout.Width(100)) && avatar != null)
                    {
                        var tabakoGO = PrefabUtility.InstantiatePrefab(prefabPair.prefab, avatar.transform) as GameObject;
                        Selection.activeGameObject = tabakoGO;
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

            }
        }

        [MenuItem(MenuItemDictionary.p_PrefabSearcher, priority = 30)]
        private static void OpenWindow()
        {
            var w = GetWindow<TabakoPrefabSearcher>("Tabako Prefab Searcher");
            w.minSize = new Vector2(450, 300);
        }

        [MenuItem(MenuItemDictionary.p_PrefabSearcherGameObject, validate = false)]
        private static void OpenFromHierarchy()
        {
            var w = GetWindow<TabakoPrefabSearcher>("Tabako Prefab Searcher");
            w.minSize = new Vector2(450, 300);

            GameObject go = Selection.activeGameObject;
            avatar = go.GetComponent<VRCAvatarDescriptor>();

            EditorApplication.delayCall = () =>
            {
                SearchPrefabs(searchRegex);
            };
        }

        [MenuItem(MenuItemDictionary.p_PrefabSearcherGameObject, validate = true)]
        private static bool OpenFromHierarchyValidate()
        {
            //プレイモード時は表示しない
            if (EditorApplication.isPlayingOrWillChangePlaymode || Selection.activeGameObject == null)
                return false;

            VRCAvatarDescriptor avatar = Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();

            return avatar != null;
        }

        private static void _ReloadPrefabsStatic()
        {
            prefabPairs.Clear();

            try
            {
                var folderPath = AssetDatabase.GUIDToAssetPath("bfb7a0af85122c744bf851766b4577a0");
                var guids = AssetDatabase.FindAssets("t:prefab", new string[] { folderPath });

                var len = guids.Length;
                for (int i = 0; i < len; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        $"",
                        $"Loading Tabako Prefab... ({i + 1})",
                        i / len);

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    TabakoScriptBehaviour ts = prefab.GetComponent<TabakoScriptBehaviour>();

                    if (ts == null) continue;
                    prefabPairs.Add(new PrefabPair() { prefab = prefab, tabakoScriptBehaviour = ts });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ReloadPrefabs()
        {
            prefabPairs.Clear();

            try
            {
                var folderPath = AssetDatabase.GUIDToAssetPath("bfb7a0af85122c744bf851766b4577a0");
                var guids = AssetDatabase.FindAssets("t:prefab", new string[] { folderPath });

                var len = guids.Length;
                for (int i = 0; i < len; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        $"{titleContent.text}",
                        $"Loading Tabako Prefab... ({i + 1})",
                        i / len);

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    TabakoScriptBehaviour ts = prefab.GetComponent<TabakoScriptBehaviour>();

                    if (ts == null) continue;
                    prefabPairs.Add(new PrefabPair() { prefab = prefab, tabakoScriptBehaviour = ts });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // https://forum.unity.com/threads/horizontal-line-in-editor-window.520812/#post-3534861
        public static void DrawUILine(Color color, int thickness = 2, int padding = 4)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        static string CleanSearchInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\.@-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters,
            // we should return Empty.
            catch
            {
                return String.Empty;
            }
        }

        private static bool IsAvatarInfoMatch([NotNull] TabakoScriptBehaviour tabakoScript, string regex, RegexOptions regexOptions = RegexOptions.None)
        {
            regex = CleanSearchInput(regex);

            if (string.IsNullOrEmpty(regex))
                return false;

            Match m = null;
            if (searchMode.HasFlag(PrefabSearchMode.PrefabName))
            {
                m = Regex.Match(tabakoScript.name, regex, regexOptions);
                if (m.Success)
                    return true;
            }

            if (searchMode.HasFlag(PrefabSearchMode.ShopName))
            {
                if (!string.IsNullOrEmpty(tabakoScript.ShopName))
                {
                    m = Regex.Match(tabakoScript.ShopName, regex, regexOptions);
                    if (m.Success)
                        return true;
                }
            }

            if (searchMode.HasFlag(PrefabSearchMode.AvatarName))
            {
                foreach (var name in tabakoScript.AvatarNamesForSearcher)
                {
                    if (string.IsNullOrEmpty(name))
                        continue;

                    m = Regex.Match(name, regex, regexOptions);
                    if (m.Success)
                        return true;
                }
            }

            return false;
        }

        private static void SearchPrefabs(string regex)
        {
            if (string.IsNullOrEmpty(regex) && !string.IsNullOrEmpty(searchRegex))
                regex = searchRegex;

            if (string.IsNullOrEmpty(regex))
                return;

            matchedPrefabPairs.Clear();

            foreach (var prefabPair in prefabPairs)
            {
                if (prefabPair.prefab == null || prefabPair.tabakoScriptBehaviour == null) continue;

                if (prefabPair.tabakoScriptBehaviour.IsPrefabObsolute)
                    continue;

                if (IsAvatarInfoMatch(prefabPair.tabakoScriptBehaviour, regex, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None))
                {
                    matchedPrefabPairs.Add(prefabPair);
                }
            }
        }
    }
}
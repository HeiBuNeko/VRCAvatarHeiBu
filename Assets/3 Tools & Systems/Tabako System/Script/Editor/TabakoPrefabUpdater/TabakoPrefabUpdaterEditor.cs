using satania.tabakosystem;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static net.satania_shopping.tabakosystem.editor.TabakoPrefabDataContainer;
using net.satania_shopping.tabakosystem.runtime;
using static net.satania_shopping.tabakosystem.runtime.TabakoScriptBehaviour;

namespace net.satania_shopping.tabakosystem.editor
{
    public class TabakoPrefabUpdaterEditor : EditorWindow
    {
        private GameObject tabakoPrefabAsset; //更新前のプレハブ

        private const string VRCHeadChopGameObjectName = "VRCHeadChop";

        private PrefabVersion prefabVersion = PrefabVersion._1;
        private static string path_blankPrefab => AssetDatabase.GUIDToAssetPath("9e34634a453bd584fb1e143ad8e0e622");
        private static GameObject blankPrefabAsset => AssetDatabase.LoadAssetAtPath<GameObject>(path_blankPrefab);

        [MenuItem(MenuItemDictionary.p_PrefabUpdater, priority = 40)]
        private static void OpenWindow()
        {
            var w = GetWindow<TabakoPrefabUpdaterEditor>("プレハブ変換ツール");
            w.minSize = w.maxSize = new Vector2(500, 200);
            w.Show();
        }

        private void OnEnable()
        {
            tabakoPrefabAsset = null;
            prefabVersion = PrefabVersion._1;
        }

        private PrefabVersion GetPrefabVersion(GameObject prefab)
        {
            if (prefab == null)
                return PrefabVersion._1;

            //VRCHeadChopがない場合は古いと判定
            if (prefab.transform.Find(VRCHeadChopGameObjectName) == null)
            {
                return PrefabVersion._1;
            }

            //新しい方のBehaviourがない場合は_2
            if (prefab.GetComponent<TabakoScriptBehaviour>() == null)
            {
                return PrefabVersion._2;
            }

            return PrefabVersion._3;
        }

        private void OnChangePrefab()
        {
            prefabVersion = GetPrefabVersion(tabakoPrefabAsset);
        }

        private void DrawMessage()
        {
            if (prefabVersion == PrefabVersion._1)
            {
                EditorGUILayout.HelpBox("このPrefabはバージョン1.3.2以下で作られたものです。", MessageType.Warning);
            }
            else if (prefabVersion == PrefabVersion._2)
            {
                EditorGUILayout.HelpBox("このPrefabはバージョン1.5.4以下で作られたものです。", MessageType.Warning);
            }
            else if (prefabVersion == PrefabVersion._3)
            {
                EditorGUILayout.HelpBox("このPrefabはバージョン2.0.0以上のバージョンで作成されたPrefabです。", MessageType.Info);
            }
        }

        private void UpdatePrefab(GameObject prefab, string savePath)
        {
            GameObject instantPrefab = (GameObject)PrefabUtility.InstantiatePrefab(blankPrefabAsset);

            try
            {
                var dataContainer = GetDataContainer(prefab.transform, GetPrefabVersion(prefab));
                if (dataContainer != null && instantPrefab != null)
                {
                    dataContainer.CreatePrefabByContainer(instantPrefab);
                    PrefabUtility.SaveAsPrefabAssetAndConnect(instantPrefab, savePath, InteractionMode.AutomatedAction);
                }
            }
            finally
            {
                DestroyImmediate(instantPrefab);
            }
        }

        /// <summary>
        /// まとめてアプデ用
        /// </summary>
        private void UpdatePrefabs()
        {
            //__oldPrefabs
            string folderPath = AssetDatabase.GUIDToAssetPath("bfb7a0af85122c744bf851766b4577a0");
            string[] guids = AssetDatabase.FindAssets("t:prefab", new string[] { folderPath });
            string[] paths = guids
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < paths.Length; i++)
            {
                EditorUtility.DisplayProgressBar(
                    $"{titleContent.text}",
                    $"Updating Tabako Prefab... ({i + 1})\n" + paths[i],
                    i / paths.Length);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                TabakoScript ts = prefab.GetComponent<TabakoScript>();
                if (ts == null)
                    continue;

                UpdatePrefab(prefab, paths[i]);
            }

            AssetDatabase.StopAssetEditing();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                new GUIContent("古いバージョンで作成した非対応アバターPrefabはこちらに入れてください。"),
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            tabakoPrefabAsset = EditorGUILayout.ObjectField("古いPrefab", tabakoPrefabAsset, typeof(GameObject), false) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                OnChangePrefab();
            }

            if (tabakoPrefabAsset != null)
                DrawMessage();

            EditorGUI.BeginDisabledGroup(tabakoPrefabAsset == null || prefabVersion == PrefabVersion._3);

            GUILayout.Space(15);

            if (GUILayout.Button("古いプレハブを更新"))
            {
                string outputPath = EditorUtility.SaveFilePanelInProject(
                    "Save Tabako Prefab",
                    Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(tabakoPrefabAsset)),
                    "prefab",
                    "タバコのプレハブを保存する場所を選んでください。");

                if (string.IsNullOrEmpty(outputPath))
                    return;

                UpdatePrefab(tabakoPrefabAsset, outputPath);
            }
            EditorGUI.EndDisabledGroup();

            //if (GUILayout.Button("全てアプデ"))
            //{
            //    UpdatePrefabs();
            //}
        }
    }
}
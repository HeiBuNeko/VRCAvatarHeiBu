using net.satania_shopping.tabakosystem.editor;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace net.satania_shopping.tabakosystem.utils
{
    public static class UnsupportedAvatarUtils
    {
        public static readonly string templatePrefabGuid = "9e34634a453bd584fb1e143ad8e0e622"; //アバター対応用プレハブのGUID
        public static GameObject TemplatePrefab => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(templatePrefabGuid));

        [MenuItem(MenuItemDictionary.p_UnsupportedAvatarAddTemplate, validate = true)]
        private static bool OpenFromHierarchyValidate()
        {
            //プレイモード時は表示しない
            if (EditorApplication.isPlayingOrWillChangePlaymode || Selection.activeGameObject == null)
                return false;

            if (TemplatePrefab == null)
                return false;

            VRCAvatarDescriptor avatar = Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();

            return avatar != null;
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedAvatarAddTemplate, validate = false)]
        private static void AddTemplate()
        {
            GameObject go = Selection.activeGameObject;

            var tabakoGO = PrefabUtility.InstantiatePrefab(TemplatePrefab, go.transform) as GameObject;
            Selection.activeGameObject = tabakoGO;
        }
    }
}

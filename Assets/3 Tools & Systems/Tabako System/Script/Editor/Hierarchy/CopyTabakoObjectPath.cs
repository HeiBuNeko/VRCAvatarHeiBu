using net.satania_shopping.tabakosystem.runtime;
using satania.tabakosystem;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    public class CopyTabakoObjectPath
    {
        private static string GetObjectPath(Transform target, Transform root)
        {
            return AnimationUtility.CalculateTransformPath(target, root);
        }

        [MenuItem(MenuItemDictionary.p_CopyRelativePath, false, 22)]
        private static void CopyPath()
        {
            GameObject s = Selection.activeGameObject;
            TabakoScriptBehaviour tabakoScript = s.GetComponentInParent<TabakoScriptBehaviour>(true);
            string path = GetObjectPath(s.transform, tabakoScript.transform);

            if (!string.IsNullOrEmpty(path))
            {
                EditorGUIUtility.systemCopyBuffer = path;
                Debug.Log("[<color=green>Satania Tabako</color>] Copied.");
            }
        }

        [MenuItem(MenuItemDictionary.p_CopyRelativePath, true, 22)]
        private static bool ValidateCopyPath()
        {
            GameObject s = Selection.activeGameObject;
            if (s == null)
                return false;

            TabakoScriptBehaviour tabakoScript = s.GetComponentInParent<TabakoScriptBehaviour>(true);
            if (tabakoScript == null)
            {
                return false;
            }

            return true;
        }

        [MenuItem(MenuItemDictionary.p_CopyRelativePathOldScript, false, 22)]
        private static void CopyPathOLD()
        {
            GameObject s = Selection.activeGameObject;
            TabakoScript tabakoScript = s.GetComponentInParent<TabakoScript>(true);
            string path = GetObjectPath(s.transform, tabakoScript.transform);

            if (!string.IsNullOrEmpty(path))
            {
                EditorGUIUtility.systemCopyBuffer = path;
                Debug.Log("[<color=green>Satania Tabako</color>] Copied.");
            }
        }

        [MenuItem(MenuItemDictionary.p_CopyRelativePathOldScript, true, 22)]
        private static bool ValidateCopyPathOldScript()
        {
            GameObject s = Selection.activeGameObject;
            if (s == null)
                return false;

            TabakoScript _oldScript = s.GetComponentInParent<TabakoScript>(true);
            if (_oldScript == null)
                return false;

            return true;
        }
    }
}
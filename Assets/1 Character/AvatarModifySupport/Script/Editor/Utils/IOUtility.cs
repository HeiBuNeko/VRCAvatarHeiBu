using UnityEditor;
using System.IO;
using UnityObject = UnityEngine.Object;

namespace com.ams.avatarmodifysupport.utils
{
    internal static class IOUtility
    {
        internal static UnityObject SaveFile(UnityObject file, string filepath, bool isPing)
        {
            string AssetPath = AssetDatabase.GetAssetPath(file);
            if (AssetPath == null || string.IsNullOrEmpty(AssetPath))
                AssetDatabase.CreateAsset(file, filepath);
            else
                AssetDatabase.CopyAsset(AssetPath, filepath);

            if (!File.Exists(filepath))
                return default;

            AssetDatabase.ImportAsset(filepath, ImportAssetOptions.ForceUpdate);

            UnityObject savedFile = AssetDatabase.LoadAssetAtPath<UnityObject>(filepath);

            if (savedFile != null && isPing)
                EditorGUIUtility.PingObject(savedFile);

            return savedFile;
        }
    }

}


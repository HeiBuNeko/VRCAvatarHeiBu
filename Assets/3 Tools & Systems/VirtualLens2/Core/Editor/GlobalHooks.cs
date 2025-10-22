using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualLens2
{
    [InitializeOnLoad]
    internal class GlobalHooks
    {
        static GlobalHooks()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;

            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                MigrateVirtualLensSettings(SceneManager.GetSceneAt(i));
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            MigrateVirtualLensSettings(scene);
        }

        private static void MigrateVirtualLensSettings(Scene scene)
        {
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"Failed to migrate VirtualLens2: {scene.name}");
                return;
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<VirtualLensSettings>())
                {
                    var so = new SerializedObject(component);
                    SettingsMigrator.Migrate(so);
                }
            }
        }
    }
}

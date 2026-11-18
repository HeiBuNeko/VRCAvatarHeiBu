using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class MergeArmatureLockUtil
    {
        private const string k_configTypeName = "nadena.dev.modular_avatar.core.armature_lock.ArmatureLockConfig";
        private const string k_boneSyncMenu = "Tools/Modular Avatar/Sync Bones in Edit Mode";

        public static bool AnyUnlocked(ModularAvatarMergeArmature[] merges)
        {
            foreach (var merge in merges)
            {
                if (merge != null && merge.LockMode == ArmatureLockMode.NotLocked)
                    return true;
            }
            return false;
        }

        public static void LockAll(ModularAvatarMergeArmature[] merges)
        {
            foreach (var merge in merges)
            {
                if (merge == null || merge.LockMode != ArmatureLockMode.NotLocked)
                    continue;

                merge.LockMode = ArmatureLockMode.BaseToMerge;
                merge.enabled = false;
                merge.enabled = true;
            }
        }

        public static bool IsGlobalBoneSyncEnabled
        {
            get
            {
                var property = GlobalEnableProperty(out object instance);
                if (property == null || instance == null)
                    return true;

                return property.GetValue(instance) is bool enabled ? enabled : true;
            }
        }

        public static void ToggleGlobalBoneSync()
        {
            if (!EditorApplication.ExecuteMenuItem(k_boneSyncMenu))
            {
                var property = GlobalEnableProperty(out object instance);
                if (property != null && instance != null && property.CanWrite)
                    property.SetValue(instance, true);
            }
        }

        private static PropertyInfo GlobalEnableProperty(out object instance)
        {
            instance = null;
            var type = typeof(ModularAvatarMergeArmature).Assembly.GetType(k_configTypeName);
            if (type == null)
                return null;

            var instanceProperty = type.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            instance = instanceProperty != null ? instanceProperty.GetValue(null) : null;
            if (instance == null)
                return null;

            return type.GetProperty("GlobalEnable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}

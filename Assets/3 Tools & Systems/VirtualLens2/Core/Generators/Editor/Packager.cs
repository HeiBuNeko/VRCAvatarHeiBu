#if VL2_DEVELOPMENT

using UnityEditor;

namespace VirtualLens2.Generators
{
    public static class Packager
    {
        [MenuItem("Window/Logilabo/VirtualLens2/Generate Package")]
        static void ExportPackage()
        {
            // Remove unnecessary files
            AssetDatabase.DeleteAsset("Assets/VirtualLens2/Settings/ProjectSettings.asset");
            foreach (var s in AssetDatabase.GetSubFolders("Assets/VirtualLens2/Artifacts"))
            {
                AssetDatabase.DeleteAsset(s);
            }
            // Export files as an unitypackage
            string[] files = { "Assets/VirtualLens2" };
            var major = Constants.Version / 10000;
            var minor = Constants.Version / 100 % 100;
            var patch = Constants.Version % 100;
            AssetDatabase.ExportPackage(
                files, $"VirtualLens2_v{major}.{minor}.{patch}.unitypackage",
                ExportPackageOptions.Recurse | ExportPackageOptions.Default);
        }
    }
}

#endif
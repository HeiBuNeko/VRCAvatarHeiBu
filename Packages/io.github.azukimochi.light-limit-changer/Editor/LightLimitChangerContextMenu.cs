using System.Linq;
using UnityEditor.SceneManagement;
using VRC.SDK3.Avatars.Components;

namespace io.github.azukimochi;

[InitializeOnLoad]
internal static class LightLimitChangerContextMenu
{
    private const string MenuRoot = "GameObject/" + LightLimitChanger.Title + "/";
    private const int MenuPriority = -100;

    private const string SetupMenuPath = MenuRoot + "Setup";
    private const string SetupInitMenuPath = MenuRoot + "Setup (Default)";

    static LightLimitChangerContextMenu()
    {
        const string PathRoot = LightLimitChanger.Title + "/" + "Preset/";
        SceneHierarchyHooks.addItemsToGameObjectContextMenu += (menu, gameObject) =>
        {
            if (!Selection.gameObjects.Any(x => x.TryGetComponent<VRCAvatarDescriptor>(out _)))
                return;

            // TODO: 保存済みのプリセットを追加できるようにする

            // Localプリセットは無しの方向で
            //foreach(var key in PresetManager.Local.Keys)
            //{
            //    menu.AddItem(new($"{PathRoot}{key} [Local]"), false, static context => Setup(component => PresetManager.Local.TryLoad(context as string, component)), key);
            //}

            foreach (var key in PresetManager.Global.Keys)
            {
                menu.AddItem(new($"{PathRoot}{key}"), false, static context => Setup(component => PresetManager.Global.TryLoad(context as string, component)), key);
            }
        };

        return;
    }

    [MenuItem(SetupMenuPath, false, MenuPriority)]
    public static void Setup() => Setup(null);

    [MenuItem(SetupInitMenuPath, false, MenuPriority + 1)]
    public static void SetupInit() => Setup(component =>
    {
        var go = new GameObject("LLC_INITIAL");
        go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        var initialComponent = go.AddComponent<LightLimitChangerComponent>();
        EditorUtility.CopySerializedIfDifferent(initialComponent, initialComponent);
        try
        {
            Visit(Metadata.Root);

            void Visit(Metadata metadata)
            {
                if (metadata is Metadata.SettingsInfo settings)
                {
                    foreach(var child in settings.Children)
                    {
                        Visit(child); 
                    }
                }
                else if (metadata is Metadata.FieldInfo fieldInfo)
                {
                    fieldInfo.Copy(initialComponent, component);
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(go);
        }

        foreach(var processor in ShaderProcessor.DefinedShaderProcessors)
        {
            processor.OnResetComponent(component);
        }

        EditorUtility.SetDirty(component);
    });

    [MenuItem(SetupMenuPath, true, MenuPriority)]
    [MenuItem(SetupInitMenuPath, true, MenuPriority + 1)]
    public static bool SetupValidate()
    {
        return Selection.gameObjects.Any(x => x.TryGetComponent<VRCAvatarDescriptor>(out _));
    }


    private static void Setup(Action<LightLimitChangerComponent> factory)
    {
        var selection = Selection.objects;
        foreach (ref var select in selection.AsSpan())
        {
            var avatarRoot = select as GameObject;
            if (avatarRoot == null || !avatarRoot.TryGetComponent<VRCAvatarDescriptor>(out var descripter))
            {
                select = null;
                continue;
            }

            if (avatarRoot.GetComponentInChildren<LightLimitChangerComponent>(true) is { } llc)
            {
                // TODO: 重複インストールの警告
                EditorGUIUtility.PingObject(llc.gameObject);
                select = llc.gameObject;
                continue;
            }

            var result = PresetManager.Setup(avatarRoot, factory);
            if (result == null)
            {
                select = null;
                continue;
            }

            select = result;

            EditorGUIUtility.PingObject(result);
        }
        Selection.objects = selection;
    }
}

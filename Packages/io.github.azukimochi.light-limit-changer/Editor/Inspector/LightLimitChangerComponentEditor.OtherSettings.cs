using System.Linq;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf.runtime;
using nadena.dev.ndmf.util;
using Target = io.github.azukimochi.LightLimitChangerComponent;

namespace io.github.azukimochi;

partial class LightLimitChangerComponentEditor
{
    private void DrawOtherSettingsGUI()
    {
        CategoryLabel(L10n.TrStr("category:other-settings"));

        DrawExcludeOptions();

        DrawOption("settings:other/writedefault",
            serializedObject.FindProperty("WriteDefaults"),
            L10n.TrStr("common:/label/write-defaults", "Write Defaults"));

        DrawOption("settings:other/target_shader",
            serializedObject.FindProperty("TargetShader"));
        
        DrawOption("settings:other/include-particle-system",
            serializedObject.FindProperty("General.IncludeParticleSystem"),
            "settings:other/include-particle-system");

        DrawParameterCompressionOptions();

        DrawOption("settings:other/allow-force-override-same-reference",
            serializedObject.FindProperty($"General.{nameof(GeneralSettings.AllowForceOverrideSameReference)}"));

        DrawMenuSettings();

        DrawMeshSettings();

        EditorGUILayout.Space();
        DrawErrorSuppressionSettings();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty($"{nameof(Target.DebugOptions)}"));
    }

    private static bool IsErrorSuppressionOpen = false;
    private static void DrawErrorSuppressionSettings()
    {
        IsErrorSuppressionOpen = EditorGUILayout.Foldout(IsErrorSuppressionOpen, L10n.Tr("settings:other/error-suppression/title"), true);
        if (!IsErrorSuppressionOpen)
            return;

        EditorGUI.BeginChangeCheck();
        var suppression = Preferences.Local.ErrorSuppression;
        suppression.MixedShaderWarning = EditorGUILayout.Toggle(
            L10n.Tr("settings:other/error-suppression/mixed-shader-warning/label"),
            suppression.MixedShaderWarning);
        if (EditorGUI.EndChangeCheck())
        {
            Preferences.Local.Save();
        }
        EditorGUILayoutUtils.HelpBox(L10n.Tr("settings:other/error-suppression/mixed-shader-warning/warning"), MessageType.Warning);
    }

    private static void DrawOption(string keyPrefix, SerializedProperty property, string label = null)
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(L10n.Tr($"{keyPrefix}/label", L10n.TrStr(keyPrefix)), EditorStyles.boldLabel);

        var l = L10n.Tr(label ?? $"{keyPrefix}/label");
        if (property.propertyType == SerializedPropertyType.Boolean)
            TogglePopoutDrawer.PopupCheckbox(EditorGUILayout.GetControlRect(), property, l);
        else
            EditorGUILayout.PropertyField(property, l);
        if (ShowDescriptions)
        {
            EditorGUILayoutUtils.HelpBox(L10n.Tr($"{keyPrefix}/description"), MessageType.Info);
        }
    }

    private void DrawParameterCompressionOptions()
    {
        var property = serializedObject.FindProperty($"General.{nameof(GeneralSettings.CompressionExpressionParameters)}");
        DrawOption("settings:other/compress-expression-parameters", property);

        using var _ = DisableScope.If(!property.boolValue);

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty($"General.{nameof(GeneralSettings.ParameterSyncFrequency)}"),
            L10n.Tr("settings:other/parameter-sync-frequency/label"));

        if (ShowDescriptions)
        {
            EditorGUILayoutUtils.HelpBox(L10n.Tr("settings:other/parameter-sync-frequency/description"), MessageType.Info);
        }

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty($"General.{nameof(GeneralSettings.AddManualSyncButton)}"),
            L10n.Tr("settings:other/add-manual-sync-button/label"));

        if (ShowDescriptions)
        {
            EditorGUILayoutUtils.HelpBox(L10n.Tr("settings:other/add-manual-sync-button/description"), MessageType.Info);
        };
    }

    private void DrawDynamicPresetSettings()
    {
        var enableProp = serializedObject.FindProperty($"General.{nameof(GeneralSettings.GenerateDynamicPreset)}");
        var countProp = serializedObject.FindProperty($"General.{nameof(GeneralSettings.DynamicPresetSlotCount)}");
        TogglePopoutDrawer.PopupCheckbox(EditorGUILayout.GetControlRect(), enableProp, L10n.Tr("settings:other/dynamic-preset/generate-dynamic-preset"));
        using (DisableScope.If(!enableProp.boolValue))
            EditorGUILayout.PropertyField(countProp, L10n.Tr("settings:other/dynamic-preset/dynamic-preset-slot-count"));

        if (ShowDescriptions)
        {
            EditorGUILayoutUtils.HelpBox(L10n.Tr($"settings:other/dynamic-preset/description"), MessageType.Info);
        }
    }

    private void DrawExcludeOptions()
    {
        var component = target as Target;

        EditorGUILayout.Space();

        var excludeProp = serializedObject.FindProperty(nameof(Target.Excludes));
        {
            var position = EditorGUILayout.GetControlRect();
            using (var scope = new PropertyScope(position, L10n.Tr($"settings:other/excludes/label"), excludeProp))
                EditorGUI.LabelField(position, scope.Label, EditorStyles.boldLabel);
        }

        using var headerScope = new ShurikenHeaderGroupScope(excludeProp, L10n.TrStr("common:label/open"));
        if (!headerScope.IsOpened)
            return;

        EditorGUILayout.LabelField(L10n.Tr("settings:other/excludes/exclude-object"), EditorStyles.boldLabel);
        Iterate<GameObject>();
        Iterate<Material>();
        EditorGUILayout.Space();

        AddSection<GameObject>();
        AddSection<Material>();

        DrawSeparator();

        /*
        //static void InitContextMenu(GenericMenu menu, Target component, ExcludeOptions options)
        //{
        //    StringBuilder sb = new();
        //    static void AppendPath(StringBuilder sb, SettingsFieldInfo info)
        //    {
        //        if (info.Parent is { } parent)
        //            AppendPath(sb, parent);
        //        sb.Append(L10n.TrStr($"category:{info.Id}"));
        //        sb.Append("/");
        //    }

        //    foreach (var (settings, parameter) in component.EnumerateAllSettings().SelectMany(x => x.AllParameterFields().Select(y => (Settings: SettingsFieldInfo.GetInfo(x.GetType()), Parameter: new ParameterInfo(x, y)))))
        //    {
        //        sb.Clear();
        //        sb.Append("操作から除外/");
        //        AppendPath(sb, settings);
        //        sb.Append(L10n.TrStr($"settings:{settings.Id}/{parameter.Name.ToKebabCase()}/label"));
        //        var propertyName = $"{settings.Id}/{parameter.Name.ToKebabCase()}";
        //        var index = options.ExcludeParameters?.IndexOf(propertyName) ?? -1;
        //        menu.AddItem(new GUIContent(sb.ToString()), index == -1, () =>
        //        {
        //            Undo.RecordObject(component, "Change Exclude Parameter");
        //            if (index == -1)
        //            {
        //                options.ExcludeParameters.Add(propertyName);
        //            }
        //            else
        //            {
        //                options.ExcludeParameters.RemoveAt(index);
        //            }
        //            EditorUtility.SetDirty(component);
        //        });
        //    }
        //}
        //*/

        void Iterate<T>() where T : Object
        {
            var excludes = component.Excludes;

            EditorGUI.BeginChangeCheck();
            try
            {
                var span = excludes.AsSpan();
                int remove = -1;
                for (int i = 0; i < span.Length; i++)
                {
                    ref var options = ref span[i];
                    if (options?.Object is not T obj)
                        continue;

                    bool objectHasChanged = false;
                    EditorGUILayout.BeginHorizontal();

                    var property = serializedObject.FindProperty($"Excludes.Array.data[{i}]");
                    using (new ChangeCheckScope(out objectHasChanged))
                        obj = EditorGUILayout.ObjectField(obj, typeof(T), true) as T;

                    void Toggle(GUIContent content, ref bool value)
                    {
                        EditorGUI.BeginChangeCheck();
                        var v = EditorGUILayout.ToggleLeft(content, value, GUILayout.Width(EditorStyles.toggle.CalcSize(content).x + 4));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(component, "Change Parameter");
                            value = v;
                        }
                    }

                    if (typeof(T) == typeof(GameObject))
                    {
                        Toggle(L10n.Tr("settings:other/excludes/include-children", "下層を含める"), ref options.IncludeChildren);
                    }
                    else if (typeof(T) == typeof(Material))
                    {
                        Toggle(L10n.Tr("common:label/animation"), ref options.SkipAnimation);
                        using var _ = DisableScope.If(options.SkipAnimation);
                        Toggle(L10n.Tr("common:label/parameter-override"), ref options.SkipOverrideParameters);
                        Toggle(L10n.Tr("common:label/texture-bake"), ref options.SkipTextureBake);
                    }

                    using (new ChangeCheckScope(out objectHasChanged))
                        if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                            obj = null;

                    EditorGUILayout.EndHorizontal();
                    var rect = GUILayoutUtility.GetLastRect();
                    rect.width += 1000;
                    rect.x -= 500;
                    EditorGUI.BeginProperty(rect, GUIContent.none, property);
                    EditorGUI.EndProperty();

                    if (objectHasChanged)
                    {
                        if (obj == null)
                        {
                            Undo.RecordObject(component, "Remove Item");
                            remove = i;
                            break;
                        }
                        else if (excludes.Find(x => x.Object == obj) == null)
                        {
                            Undo.RecordObject(component, "Change Item");
                            options.Object = obj;
                        }
                    }
                }
                if (remove != -1)
                    excludes.RemoveAt(remove);
            }
            finally
            {
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(component);
                }
            }
        }

        void AddSection<T>() where T : Object
        {
            var excludes = component.Excludes;
            EditorGUI.BeginChangeCheck();
            EditorGUILayoutUtils.MultipleObjectInputField<T>(L10n.Tr($"settings:other/excludes/add-section/{typeof(T).Name.ToKebabCase()}"), array =>
            {
                var items = array.Where(x => excludes.Find(y => y.Object == x) == null);
                if (!items.Any())
                    return;

                Undo.RecordObject(component, "Add Items");
                excludes.AddRange(items.Select(x => new ExcludeOptions(x)));
            });
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(component);
            }

        }
    }

    private void DrawMeshSettings()
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(L10n.Tr($"settings:other/mesh-settings/title"), EditorStyles.boldLabel);
        var meshSettings = serializedObject.FindProperty(nameof(Target.MeshSettings));
        var enableProp = meshSettings.FindPropertyRelative(nameof(MeshSettings.EnableMeshSettigsOverride));
        TogglePopoutDrawer.PopupCheckbox(EditorGUILayout.GetControlRect(), enableProp, L10n.Tr("settings:other/mesh-settings/override-settings", "Override Avatar Mesh Settings"));

        using var disabled = DisableScope.If(!enableProp.boolValue);

        var anchorProp = meshSettings.FindPropertyRelative(nameof(MeshSettings.Anchor));
        var rootBoneProp = meshSettings.FindPropertyRelative(nameof(MeshSettings.RootBone));

        DoProp(L10n.Tr("Anchor Override"), anchorProp, HumanBodyBones.Chest);
        DoProp(L10n.Tr("Root Bone"), rootBoneProp, HumanBodyBones.Hips);
        EditorGUILayout.PropertyField(meshSettings.FindPropertyRelative(nameof(MeshSettings.Bounds)));

        if (ShowDescriptions)
        {
            EditorGUILayoutUtils.HelpBox(L10n.Tr($"settings:other/mesh-settings/description"), MessageType.Info);
        }

        void DoProp(GUIContent label, SerializedProperty property, HumanBodyBones defaultBone)
        {
            var referencePathProp = property.FindPropertyRelative(nameof(AvatarObjectReference.referencePath));
            var referencePath = referencePathProp.stringValue;
            if (!string.IsNullOrEmpty(referencePath))
            {
                EditorGUILayout.PropertyField(property);
                return;
            }

            Transform defaultTarget = null;
            try
            {
                var avatarRoot = RuntimeUtil.FindAvatarInParents(Component.transform);
                if (avatarRoot != null)
                {
                    var animator = avatarRoot.GetComponent<Animator>();
                    if (animator != null && animator.avatar != null)
                    {
                        defaultTarget = animator.GetBoneTransform(defaultBone);
                    }
                }
            }
            catch { }

            EditorGUI.BeginChangeCheck();
            var obj = EditorGUILayout.ObjectField(label, defaultTarget, typeof(Transform), true) as Transform;
            if (EditorGUI.EndChangeCheck())
            {
                referencePathProp.stringValue = obj switch
                {
                    not null => obj.gameObject.AvatarRootPath() switch
                    {
                        "" => AvatarObjectReference.AVATAR_ROOT,
                        { } value => value,
                        _ => "",
                    },
                    _ => "",
                };
            }
        }
    }

    private void DrawMenuSettings()
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(L10n.Tr($"settings:other/menu-settings/title"), EditorStyles.boldLabel);

        TogglePopoutDrawer.PopupCheckbox(EditorGUILayout.GetControlRect(),
            serializedObject.FindProperty($"General.{nameof(GeneralSettings.GenerateDefaultPreset)}"),
            L10n.Tr("settings:other/generate-default-preset"));

        DrawDynamicPresetSettings();

        TogglePopoutDrawer.PopupCheckbox(EditorGUILayout.GetControlRect(), 
            serializedObject.FindProperty($"MenuSettings.{nameof(MenuSettings.UnfoldGroupMenus)}"), 
            L10n.Tr("settings:other/menu-settings/unfold-group-menus"));

    }
}

using System.Collections.Generic;
using System.Linq;

namespace io.github.azukimochi;

partial class LightLimitChangerComponentEditor
{
    internal void DoPropertyGUI<TSettings>(SerializedProperty property) where TSettings : ISettings
        => DoPropertyGUI(Metadata.GetMetadataFromType<TSettings>() ,property, parentSerializedObject);

    internal static void DoPropertyGUI(Metadata.SettingsInfo info, SerializedProperty property, SerializedObject parentObject)
    {
        if (!property.hasChildren)
            return;

        using var scope = new ShurikenHeaderGroupScope(property, L10n.TrStr($"category:{info.Id}"),
            drawToggle: parentObject == null,
            menuCallback: menu => DoHeaderContextMenu(info, property, menu));

        var enableProp = parentObject != null ? null : property.FindPropertyRelative(nameof(ITogglable.Enable));
        if (enableProp != null)
        {
            EditorGUI.BeginDisabledGroup(!enableProp.boolValue);
        }

        try
        {
            if (!scope.IsOpened)
                return;

            if (info.Disabled is not null)
            {
                EditorGUILayoutUtils.HelpBox(L10n.Tr(info.Disabled), MessageType.Warning);
                EditorGUILayout.Space();
            }
            EditorGUI.BeginDisabledGroup(info.Disabled is not null);

            if (ShowDescriptions)
            {
                EditorGUILayoutUtils.HelpBox(L10n.Tr($"category:{info.Id}/description"), MessageType.Info);
                EditorGUILayout.Space();
            }

            DoBatchSettings(info, property, parentObject);

            string currentGroup = null;
            SerializedProperty previous = null;
            foreach (var (child, i) in info.OrderBy(x => (uint)(x.Kind - 1)).Select((x, i) => (x, i)))
            {
                var prop = property.FindPropertyRelative(child.Name);
                switch (child)
                {
                    case Metadata.SettingsInfo settings:
                        {
                            DrawSeparator();
                            DoPropertyGUI(settings, prop, parentObject);
                        }
                        break;
                    case Metadata.FieldInfo field:
                        {
                            if (field.HideInInspector)
                                continue;

                            if (IsAdvancedMode && previous?.isExpanded == true)
                            {
                                EditorGUILayout.Space(20);
                            }

                            if (field.Header is { } header)
                            {
                                if (currentGroup != header.Label && i != 0)
                                {
                                    EditorGUILayout.Space();
                                }
                                currentGroup = header.Label;

                                EditorGUILayout.LabelField(L10n.Tr(header.Label), EditorStyles.boldLabel);
                            }

                            var labelKey = $"settings:{field.Id}/label";
                            var descKey = $"settings:{field.Id}/description";

                            if (field is Metadata.ParameterInfo parameter)
                                ParameterDrawer.DrawLayout(prop, L10n.TempContent(L10n.TrStr(labelKey).Replace("\n", " ")), !parameter.DisableInitialValueSlider, parameter.Range, parameter.MinMaxRange, IsAdvancedMode, parentObject);
                            else
                                EditorGUILayout.PropertyField(prop, L10n.Tr(labelKey));

                            if (prop.isExpanded && IsAdvancedMode && ShowDescriptions)
                            {
                                var content = L10n.Tr(descKey, "");
                                if (!string.IsNullOrEmpty(content.text))
                                    EditorGUILayoutUtils.HelpBox(content, MessageType.Info);
                            }

                            // フィールドに付与された[ShowInfoBox]を描画する
                            // パラメータが無効な場合は影響が無いため表示しない
                            if (field.Messages.Length != 0 &&
                                prop.FindPropertyRelative(nameof(ITogglable.Enable)) is not { boolValue: false })
                            {
                                foreach (var x in field.Messages)
                                {
                                    EditorGUILayoutUtils.HelpBox(L10n.Tr(x.Message), (MessageType)x.Type);
                                }
                            }

                            previous = prop;
                        }
                        break;
                }
            }

            if (parentObject == null)
            {
                if (info.Messages.Length != 0)
                {
                    DrawSeparator();
                    foreach (var x in info.Messages)
                    {
                        EditorGUILayoutUtils.HelpBox(L10n.Tr(x.Message), (MessageType)x.Type);
                    }
                }
            }

            EditorGUI.EndDisabledGroup();
        }
        finally
        {
            if (enableProp != null)
            {
                EditorGUI.EndDisabledGroup();
            }
        }

    }

    internal static void DoHeaderContextMenu(Metadata.SettingsInfo info, SerializedProperty property, GenericMenu menu)
    {
        menu.AddItem(new(L10n.TrStr("common:label/copy")), false, () =>
        {
            GUIUtility.systemCopyBuffer = (property.boxedValue as ISettings).ToJson();
        });

        bool hasNestedSettings = !info.Settings.IsEmpty;

        if (hasNestedSettings)
        {
            menu.AddItem(new(L10n.TrStr("common:label/copy-with-child-settings")), false, () =>
            {
                GUIUtility.systemCopyBuffer = (property.boxedValue as ISettings).ToJson(true);
            });
        }

        bool canPaste = false;
        try
        {
            canPaste = JsonUtility.FromJson<JsonParsingBuffer>(GUIUtility.systemCopyBuffer)?.Type == info.Type.Name;
        }
        catch
        {
            canPaste = false;
        }

        if (canPaste)
        {
            menu.AddItem(new(L10n.TrStr("common:label/paste")), false, () =>
            {
                property.boxedValue = JsonUtility.FromJson(GUIUtility.systemCopyBuffer, info.Type);
                using var undo = new UndoScope("Paste settings from clipboard");
                property.serializedObject.ApplyModifiedProperties();
            });
        }
        else
        {
            menu.AddDisabledItem(new(L10n.TrStr("common:label/paste")));
        }

        menu.AddSeparator("");
        menu.AddItem(new(L10n.TrStr("common:label/reset")), false, () =>
        {
            var obj = new GameObject
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };
            try
            {
                using var so = new SerializedObject(obj.AddComponent<LightLimitChangerComponent>());
                var defProp = so.FindProperty(property.propertyPath);
                property.serializedObject.CopyFromSerializedPropertyIfDifferent(defProp);
                property.serializedObject.ApplyModifiedProperties();
            }
            finally
            {
                DestroyImmediate(obj);
            }
        });
    }

    internal static List<SerializedProperty> propertiesList = new();
    internal static void DoBatchSettings(Metadata.SettingsInfo node, SerializedProperty property, SerializedObject parentObject)
    {
        var list = propertiesList;
        list.Clear();
        foreach(var x in node.Children)
        {
            if (x.Kind is Metadata.TypeKind.Parameter)
                list.Add(property.FindPropertyRelative(x.Name));
        }
        ParameterDrawer.DrawBatchSettings(list.AsSpan(), parentObject != null);
    }

    private sealed class JsonParsingBuffer
    {
        public string Type;
    }
}

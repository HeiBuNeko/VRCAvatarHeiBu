using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.runtime;
using UnityEditorInternal;
using Target = io.github.azukimochi.LightLimitChangerComponent;

namespace io.github.azukimochi;

[CustomEditor(typeof(Target))]
internal sealed partial class LightLimitChangerComponentEditor : Editor
{
    public static InspectorMode CurrentMode 
    {
        get => Preferences.Local.InspectorMode;
        set => Preferences.Local.InspectorMode = value;
    }

    public static bool IsAdvancedMode => CurrentMode >= InspectorMode.Advanced;

    public static bool ShowDescriptions => CurrentMode == InspectorMode.Description;

    public bool IsPresetComponent => parentSerializedObject != null;
    public bool PresetMode => CurrentMode == InspectorMode.Preset && !IsPresetComponent;

    private Target Component;

    private SerializedObject parentSerializedObject;
    private Transform parentTransform;
    private bool isDuplicateInstalled;

    private static bool editorPresetsExpanded = false;
    private static bool loadChildRuntimePresets = true;
    private Rect dialogDisplayPosition;
    internal static GUIStyle TabButtonStyle => _largeButtonStyle ??= new(GUI.skin.button) { fixedHeight = EditorGUIUtility.singleLineHeight * 1.25f };
    private static GUIStyle _largeButtonStyle;
    private static readonly GUIContent[] TabContents = new GUIContent[] { new(), new(), new(), new() };

    private ReorderableList presetList;
    private Target currentPresetRoot;

    public void OnEnable()
    {
        var target = (Target)base.target;
        Component = target;
        parentTransform = target.transform.parent;
        if (parentTransform?.GetComponent<Target>() is { } parent)
        {
            parentSerializedObject = new SerializedObject(parent);
        }
        isDuplicateInstalled = IsDuplicateInstall(target.transform);

        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    public void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;

        if (parentSerializedObject != null)
        {
            parentSerializedObject.Dispose();
        }
    }

    private void OnHierarchyChanged()
    {
        var target = Component;

        if (isDuplicateInstalled)
        {
            isDuplicateInstalled = IsDuplicateInstall(target.transform);
        }

        if (target.transform.parent == parentTransform)
            return;

        parentTransform = target.transform.parent;

        if (parentSerializedObject != null)
        {
            parentSerializedObject.Dispose();
            parentSerializedObject = null;
        }

        if (parentTransform?.GetComponent<Target>() is { } parent)
        {
            parentSerializedObject = new SerializedObject(parent);
        }

        isDuplicateInstalled = IsDuplicateInstall(target.transform);
    }

    public override void OnInspectorGUI()
    {
        var target = Component;
        serializedObject.Update();
        try
        {
            CategoryLabel($"{LightLimitChanger.Title} {LightLimitChanger.Version}");
            EditorGUILayout.Space();
            ShowLink();
            EditorGUILayout.Space();
            DrawLanguagePicker();
            using var __ = DrawAuthenticationMessage();

            if (!IsPresetComponent)
            {
                DrawTabSelector();
            }

            if (isDuplicateInstalled)
            {
                EditorGUILayoutUtils.HelpBox(L10n.Tr("error:duplicate-installed"), MessageType.Warning);
                EditorGUILayout.Space();
            }
            using var ___ = DisableScope.If(isDuplicateInstalled);

            if (!IsPresetComponent)
            {
                OnEditorPresetGUI(target);
            }
            else
            {
                CategoryLabel(L10n.TrStr("runtime-preset:inspector/title"));

                EditorGUILayoutUtils.HelpBox(L10n.Tr("runtime-preset:inspector/description"), MessageType.Info);
                parentSerializedObject.Update();
            }
            EditorGUILayout.Space();


            if (PresetMode)
            {
                OnRuntimePresetGUI(target);
                return;
            }

            CategoryLabel(L10n.TrStr("category:general-settings"));
            if(ShowDescriptions) 
                EditorGUILayoutUtils.HelpBox(L10n.Tr("category:general-settings/description"), MessageType.Info);
            EditorGUILayout.Space();

            DoPropertyGUI<LightingSettings>(serializedObject.FindProperty("General.LightingControl"));
            DoPropertyGUI<ColorControlSettings>(serializedObject.FindProperty("General.ColorControl"));

            CategoryLabel(L10n.TrStr("category:material-settings"));
            EditorGUILayout.Space();

            DoPropertyGUI<LilToonSettings>(serializedObject.FindProperty("LilToon"));
            DoPropertyGUI<PoiyomiSettings>(serializedObject.FindProperty("Poiyomi"));
            //DoPropertyGUI<UnlitWFSettings>(serializedObject.FindProperty("UnlitWF"));

            if (IsPresetComponent)
            {
                return;
            }

            DrawFavoriteMenuGUI();

            EditorGUILayout.Space();

            DrawOtherSettingsGUI();
        }
        finally
        {
            if (serializedObject.hasModifiedProperties)
            {
                SemVer.Set(serializedObject.FindProperty(nameof(Target.Version)), LightLimitChanger.Version);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    internal void OnEditorPresetGUI(Target target)
    {
        CategoryLabel(L10n.TrStr("editor-preset:inspector/title"));
        EditorGUILayout.Space();

        using var scope = new ShurikenHeaderGroupScope(ref editorPresetsExpanded, L10n.TrStr("common:label/open"));
        if (!scope.IsOpened)
            return;

        var presetManager = PresetManager.Global;

        string removeKey = null;
        var keys = presetManager.Keys;
        foreach (var key in keys)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key, EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L10n.Tr("common:label/load")))
            {
                presetManager.TryLoad(key, target, !IsPresetComponent && loadChildRuntimePresets);
            }
            if (GUILayout.Button(L10n.Tr("common:label/remove")))
            {
                removeKey = key;
            }
            EditorGUILayout.EndHorizontal();
            DrawSeparator();
        }

        if (!keys.Any())
        {
            EditorGUILayout.LabelField(L10n.Tr("editor-preset:inspector/label/preset-not-found"), EditorStyles.largeLabel);
        }

        if (removeKey is not null)
            presetManager.Remove(removeKey);

        if (!IsPresetComponent)
        {
            loadChildRuntimePresets = EditorGUILayout.ToggleLeft(L10n.Tr("editor-preset:inspector/label/load-child-presets"), loadChildRuntimePresets);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(L10n.Tr("common:label/reflesh"), TabButtonStyle))
        {
            Preferences.Local.Reflesh();
            Preferences.Global.Reflesh();
        }

        if (GUILayout.Button(L10n.Tr("common:label/save"), TabButtonStyle))
        {
            PresetManager.ShowSavePresetDialog(target, dialogDisplayPosition);
        }
        if (Event.current.type == EventType.Repaint) 
            dialogDisplayPosition = GUILayoutUtility.GetLastRect();


        EditorGUILayout.EndHorizontal();
    }

    internal void OnRuntimePresetGUI(Target target)
    {
        ReorderableList InitializePresetList()
        {
            var reorder = new ReorderableList(new List<Target>(), typeof(Target));
            reorder.onReorderCallback = OnReorder;
            reorder.onAddCallback = OnAdd;
            reorder.onRemoveCallback = OnRemove;
            reorder.onCanRemoveCallback = OnCanRemove;
            reorder.drawElementCallback = DrawElement;
            reorder.drawNoneElementCallback = rect => EditorGUI.LabelField(rect, L10n.Tr("runtime-preset:inspector/none-presets"));
            reorder.elementHeight = EditorGUIUtility.singleLineHeight * 1.3f;
            reorder.multiSelect = true;
            reorder.headerHeight = 0;

            void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                var list = reorder.list as List<Target>;
                var p = rect;
                p.height = EditorGUIUtility.singleLineHeight * 1.2f;
                p.y += (rect.height - p.height) / 2;
                EditorGUIUtils.ObjectDisplayField(p, list[index].gameObject, typeof(GameObject));
            }

            void OnAdd(ReorderableList list)
            {
                if (currentPresetRoot == null)
                    return;

                var obj = PresetManager.Setup(RuntimeUtil.FindAvatarInParents(currentPresetRoot.transform)?.gameObject);
                obj.name = $"{L10n.TrStr("common:label/preset")} {list.count + 1}";
                obj.transform.parent = currentPresetRoot.transform;

                if (obj.GetComponent<MAMenuInstaller>() is { } mami)
                {
                    DestroyImmediate(mami);
                }

                Undo.RegisterCreatedObjectUndo(obj, "Create Preset");
            }

            bool OnCanRemove(ReorderableList reorder) => reorder.count > 0;

            void OnRemove(ReorderableList reorder)
            {
                var list = reorder.list as List<Target>;
                if (list.Count == 0)
                    return;

                var indices = reorder.selectedIndices;
                using var undo = new UndoScope("Remove Preset");
                if (indices.Count == 0)
                {
                    Undo.DestroyObjectImmediate(list[^1].gameObject);
                    return;
                }
                else
                {
                    foreach (var index in indices)
                    {
                        Undo.DestroyObjectImmediate(list[index].gameObject);
                    }
                }
            }

            void OnReorder(ReorderableList reorder)
            {
                var list = reorder.list as List<Target>;
                var span = list.AsSpan();
                for (int i = 0; i < span.Length; i++)
                {
                    var target = list[i];
                    Undo.SetSiblingIndex(target.transform, i, "Change Preset Order");
                }
            }


            return reorder;
        }

        CategoryLabel(L10n.TrStr("common:label/preset"));
        EditorGUILayoutUtils.HelpBox(L10n.Tr("runtime-preset:list/inspector/description"), MessageType.Info);
        EditorGUILayout.Space();

        var root = IsPresetComponent ? target.transform.parent?.GetComponent<Target>() : target;
        if (root == null)
            return;
            
        var reorder = presetList ??= InitializePresetList();
        var list = reorder.list as List<Target>;
        list.Clear();
        foreach(var child in root.GetComponentsInChildren<Target>(true).Skip(1))
        {
            list.Add(child);
        }
        currentPresetRoot = root;
        reorder.DoLayoutList();
    }

    private bool IsDuplicateInstall(Transform transform)
    {
        var parentTransform = transform.parent;
        if (parentTransform == null || RuntimeUtil.FindAvatarInParents(parentTransform) is not { } avatar) // 🤔 Outside of the avatar...?
            return false;

        if (parentTransform.TryGetComponent<Target>(out _))
            return IsDuplicateInstall(parentTransform);

        var components = avatar.GetComponentsInChildren<Target>(true);
        if (components.Length <= 1)
            return false;

        return components[0].transform != transform;
    }

    internal static void CategoryLabel(string title) => EditorGUILayout.LabelField(title, Styles.CategoryLabel.Value);

    internal static void DrawSeparator()
    {
        var position = EditorGUILayout.GetControlRect(false, 12);
        position.y += 4;
        position.height = 1;
        position.width -= 4;
        position.x += 2;
        DrawSeparator(position);
    }

    internal static void DrawSeparator(Rect position) => EditorGUI.DrawRect(position, Color.gray with { a = 0.25f });

    private static DisableScope DrawAuthenticationMessage()
    {
        if (Authenticator.Verified)
            return DisableScope.Enable();

        EditorGUILayoutUtils.HelpBox(L10n.Tr("auth:message/not-verified"), MessageType.Error);
        if (GUILayout.Button(L10n.Tr("auth:label/open-authenticator"), TabButtonStyle))
        {
            Authenticator.Open();
        }
        EditorGUILayout.Space(16);

        return DisableScope.Disable();
    }

    private static void DrawTabSelector()
    {
        var tabs = TabContents;
        _ = tabs.Length;
        tabs[0].text = L10n.TrStr("category:mode/basic");
        tabs[1].text = L10n.TrStr("category:mode/advanced");
        tabs[2].text = L10n.TrStr("category:mode/description");
        //tabs[3].text = L10n.TrStr("category:mode/preset");
        tabs[3].text = L10n.TrStr("category:mode/preset");

        EditorGUI.BeginChangeCheck();
        var mode = (InspectorMode)GUILayout.Toolbar((int)CurrentMode, tabs, TabButtonStyle);
        if (EditorGUI.EndChangeCheck())
        {
            CurrentMode = mode;
            Preferences.Local.Save();
        }

        EditorGUILayout.Space();
    }

    internal static void DrawLanguagePicker()
    {
        var position = EditorGUILayout.GetControlRect(true, L10n.Localization.GetDrawLanguagePickerHeight());
        var p = position;
        p.width = EditorGUIUtility.labelWidth;
        EditorGUI.LabelField(p, /*L10n.Tr("common:language")*/ "Language");
        position.x += p.width + 4;
        position.width -= p.width + 4;
        L10n.Localization.DrawLanguagePicker(position);
    }

    public static void ShowLink()
    {
        EditorGUILayout.LabelField(L10n.Tr("link:description"), EditorStyles.boldLabel);
        DrawWebButton(L10n.TrStr("link:documentation"),
            "https://azukimochi.github.io/LLC-Docs/docs/v2/description/disc_intro");
        DrawWebButton(L10n.TrStr("link:feedback"),
            "https://docs.google.com/forms/d/e/1FAIpQLSfFP7gQ3EDwFn3pX7i0SioJo2oho2Eo0p84MiQy2vPEU0rq6Q/viewform");
        DrawWebButton(L10n.TrStr("link:discord"),
            "https://discord.com/invite/aR383QA3nf");
    }

    /*
     * Quouted from https://github.com/lilxyzw/lilToon/blob/2ef370dc444172787c075ec3a822438c2bee26cb/Assets/lilToon/Editor/lilEditorGUI.cs#L65
     *
     * Copyright (c) 2020-2023 lilxyzw
     *
     * Full Licence: https://github.com/lilxyzw/lilToon/blob/master/LICENSE
     */
    private static void DrawWebButton(string text, string URL)
    {
        var position = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
        var icon = EditorGUIUtility.IconContent("BuildSettings.Web.Small");
        icon.text = text;

        var style = new GUIStyle(EditorStyles.label) { padding = new RectOffset() };
        style.normal.textColor = style.focused.textColor;
        style.hover.textColor = style.focused.textColor;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 13;
        if (GUI.Button(position, icon, style))
        {
            Application.OpenURL(URL);
        }
    }
}

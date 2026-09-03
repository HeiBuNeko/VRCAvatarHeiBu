namespace io.github.azukimochi;

partial class PresetManager
{
    public static void ShowSavePresetDialog(LightLimitChangerComponent component, Rect? position = null)
    {
        var window = ScriptableObject.CreateInstance<SavePresetDialog>();
        //window.titleContent = new GUIContent(L10n.TrStr("preset-save-dialog:title"));
        window.Component = component;
        var rect = position ?? GUILayoutUtility.GetLastRect();
        var pos = GUIUtility.GUIToScreenPoint(rect.center - new Vector2(rect.width / 2.5f, 0));
        window.ShowAsDropDown(new(pos, Vector2.zero), new Vector2(350,110));
    }

    private sealed class SavePresetDialog : EditorWindow
    {
        public LightLimitChangerComponent Component;
        private GUIStyle _marginedStyle;

        public void OnEnable()
        {
            _marginedStyle = new GUIStyle() { margin = new RectOffset(8, 8, 8, 8)};
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical(_marginedStyle);
            {
                var rect = GUIUtility.ScreenToGUIRect(position);

                const float OutlineWidth = 1;
                var color = EditorStyles.centeredGreyMiniLabel.normal.textColor;

                EditorGUI.DrawRect(rect with { width = OutlineWidth, }, color);
                EditorGUI.DrawRect(rect with { width = OutlineWidth, x = rect.x + rect.width - OutlineWidth }, color);

                EditorGUI.DrawRect(rect with { height = OutlineWidth, }, color);
                EditorGUI.DrawRect(rect with { height = OutlineWidth, y = rect.y + rect.height - OutlineWidth }, color);
            }
            try
            {
                {
                    var rect = EditorGUILayout.GetControlRect();
                    EditorGUI.LabelField(rect, L10n.Tr("preset-save-dialog:label/target-component"));
                    rect.width -= EditorGUIUtility.labelWidth + 4;
                    rect.x += EditorGUIUtility.labelWidth + 4;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.ObjectField(rect, GUIContent.none, Component, typeof(LightLimitChangerComponent), true);
                    EditorGUI.EndDisabledGroup();
                }
                LightLimitChangerComponentEditor.DrawSeparator();

                GUILayout.FlexibleSpace();

                if (Component == null)
                    return;


                var presetName = Component.PresetName;
                EditorGUI.BeginChangeCheck();
                presetName = EditorGUILayoutUtils.TextField(L10n.Tr("preset-save-dialog:label/preset-name"), presetName, Component.name);
                if (EditorGUI.EndChangeCheck())
                {
                    Component.PresetName = presetName;
                    EditorUtility.SetDirty(Component);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(L10n.Tr("preset-save-dialog:label/save"), LightLimitChangerComponentEditor.TabButtonStyle))
                {
                    Global.Update(presetName, new EditorPreset(Component));
                    Close();
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }
    }
}


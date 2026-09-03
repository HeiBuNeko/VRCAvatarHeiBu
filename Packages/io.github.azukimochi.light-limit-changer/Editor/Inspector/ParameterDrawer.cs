namespace io.github.azukimochi;

[CustomPropertyDrawer(typeof(Parameter<>), true)]
internal sealed class ParameterDrawer : PropertyDrawer
{
    private static readonly Lazy<Vector2> FoldoutStyleSize =
        new(() => EditorStyles.foldout.CalcSize(GUIContent.none), false);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        => Draw(position, property, label, false, null, null, false);

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => GetPropertyHeight(property, true, true);

    public static float GetPropertyHeight(SerializedProperty property, bool advancedMode = false, bool showMinMaxRangeSlider = false)
    {
        var lineCount = 1;
        if (advancedMode)
        {
            if (property.isExpanded)
            {
                lineCount++;

                if (showMinMaxRangeSlider)
                    lineCount++;
            }
        }
        return EditorGUIUtility.singleLineHeight * lineCount;
    }

    public static void DrawLayout(SerializedProperty property, GUIContent label, bool showInitialSlider = true, Vector2? range = null, Vector2? minMaxRange = null, bool advancedMode = false, SerializedObject parentSerializedObject = null)
    {
        var position = EditorGUILayout.GetControlRect(label != null, GetPropertyHeight(property, parentSerializedObject == null && advancedMode, minMaxRange.HasValue));

        if (parentSerializedObject != null)
        {
            DrawPresetSlider(position, property, label, showInitialSlider, range, minMaxRange, parentSerializedObject);
        }
        else
        {
            Draw(position, property, label, showInitialSlider, range, minMaxRange, advancedMode, parentSerializedObject);
        }
    }

    private static readonly (string, SerializedProperty)[] contentsBuffer = new (string, SerializedProperty)[4];
    public static void Draw(Rect position, SerializedProperty property, GUIContent label, bool showInitialSlider = true, Vector2? range = null, Vector2? minMaxRange = null, bool advancedMode = false, SerializedObject parentSerializedObject = null)
    {
        using var scope = new PropertyScope(position, label, property);
        var valueProp = property.FindPropertyRelative(nameof(Parameter<float>.Value));
        var minMaxRangeProp = property.FindPropertyRelative(nameof(Parameter.MinMaxRange));
        var enableProp = property.FindPropertyRelative(nameof(Parameter.Enable));
        var isAnimatedProp = property.FindPropertyRelative(nameof(Parameter.Animation));
        var savedProp = property.FindPropertyRelative(nameof(Parameter.Saved));
        var syncedProp = property.FindPropertyRelative(nameof(Parameter.Synced));
        position.height = EditorGUIUtility.singleLineHeight;

        var p = position;
        p.width = EditorGUIUtility.labelWidth;
        bool enable;

        if (advancedMode)
        {
            p.x += FoldoutStyleSize.Value.x;
            p.width -= FoldoutStyleSize.Value.x;
            property.isExpanded = EditorGUI.Foldout(p, property.isExpanded, scope.Label, true);
            enable = enableProp.boolValue;
        }
        else
        {
            enable = EditorGUI.ToggleLeft(p, scope.Label, enableProp.boolValue);
            enableProp.boolValue = enable;
        }

        p = position;
        p.x += EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15;
        p.width -= EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15;

        EditorGUI.BeginDisabledGroup(!enable);
        try
        {
            using (DisableScope.If(!showInitialSlider))
            {
                Vector2? r = null;
                if (range.HasValue)
                    r = range.Value;
                else if (minMaxRange.HasValue)
                    r = minMaxRangeProp.vector2Value;

                if (valueProp.propertyType == SerializedPropertyType.Boolean)
                {
                    TogglePopoutDrawer.PopupCheckbox(p, valueProp, GUIContent.none);
                }
                else if (valueProp.propertyType == SerializedPropertyType.Color)
                {
                    ColorField(p, valueProp, GUIContent.none, range: r);
                }
                else if (r is { } v)
                {
                    EditorGUI.BeginChangeCheck();
                    var value = EditorGUI.Slider(p, GUIContent.none, valueProp.floatValue, v.x, v.y);
                    if (EditorGUI.EndChangeCheck())
                    {
                        valueProp.floatValue = value;
                    }
                }
                else
                {
                    EditorGUI.PropertyField(p, valueProp, GUIContent.none);
                }
            }

            DrawPrefixLabel(p, L10n.Tr($"common:label/{(isAnimatedProp.boolValue ? "initial-value" : "override-value")}"));

            if (!property.isExpanded || !advancedMode)
                return;

            if (showInitialSlider && minMaxRange is { } minMax)
            {
                position.y += EditorGUIUtility.singleLineHeight;
                p = position;
                p.x += EditorGUIUtility.labelWidth;
                p.width -= EditorGUIUtility.labelWidth;
                MinMaxSlider(p, minMaxRangeProp, GUIContent.none, minMax);
                DrawPrefixLabel(p, L10n.Tr("common:label/range"));
            }

            position.y += EditorGUIUtility.singleLineHeight;
            p = position;

            p.x += EditorGUIUtility.labelWidth;
            p.width -= EditorGUIUtility.labelWidth;
            DrawPrefixLabel(p, L10n.Tr("common:label/option"));

            //p = p with { width = p.width / 4 };
        }
        finally
        {
            EditorGUI.EndDisabledGroup();
        }
        var contents = contentsBuffer;
        contents[0] = (L10n.TrStr("common:label/enable"), enableProp);
        contents[1] = (L10n.TrStr("common:label/animation"), isAnimatedProp);
        contents[2] = (L10n.TrStr("common:label/saved"), savedProp);
        contents[3] = (L10n.TrStr("common:label/synced"), syncedProp);
        DrawOptionButtons(p, contents);
    }
    private static void DrawPresetSlider(Rect position, SerializedProperty property, GUIContent label, bool showInitialSlider, Vector2? range, Vector2? minMaxRange, SerializedObject parentSerializedObject)
    {
        using var scope = new PropertyScope(position, label, property);
        var sourceProperty = parentSerializedObject.FindProperty(property.propertyPath);
        if (sourceProperty == null)
            return;

        var valueProp = property.FindPropertyRelative(nameof(Parameter<float>.Value));
        var enableProp = property.FindPropertyRelative(nameof(Parameter.Enable));
        var minMaxRangeProp = sourceProperty.FindPropertyRelative(nameof(Parameter.MinMaxRange));

        bool enable = sourceProperty.FindPropertyRelative(nameof(Parameter.Enable)).boolValue && sourceProperty.FindPropertyRelative(nameof(Parameter.Animation)).boolValue;
        EditorGUI.BeginDisabledGroup(!enable);

        position.height = EditorGUIUtility.singleLineHeight;
        var p = position;


        p.width = EditorGUIUtility.labelWidth;
        EditorGUI.BeginChangeCheck();
        enable &= EditorGUI.ToggleLeft(p, scope.Label, enableProp.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            enableProp.boolValue = enable;
        }

        p = position;
        p.x += EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15;
        p.width -= EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15;

        EditorGUI.BeginDisabledGroup(!enable);
        if (!enable)
            valueProp = sourceProperty.FindPropertyRelative(nameof(Parameter<float>.Value));
        try
        {
            Vector2? r = null;
            if (range.HasValue)
                r = range.Value;
            else if (minMaxRange.HasValue)
                r = minMaxRangeProp.vector2Value;

            if (valueProp.propertyType == SerializedPropertyType.Boolean)
            {
                TogglePopoutDrawer.PopupCheckbox(p, valueProp, GUIContent.none);
            }
            else if (valueProp.propertyType == SerializedPropertyType.Color)
            {
                ColorField(p, valueProp, GUIContent.none, range: r);
            }
            else if (r is { } v)
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUI.Slider(p, GUIContent.none, valueProp.floatValue, v.x, v.y);
                if (EditorGUI.EndChangeCheck())
                {
                    valueProp.floatValue = value;
                }
            }
            else
            {
                EditorGUI.PropertyField(p, valueProp, GUIContent.none);
            }

            DrawPrefixLabel(p, L10n.Tr($"common:label/preset-value"));
        }
        finally
        {
            EditorGUI.EndDisabledGroup();
        }
    }

    private static void DrawPrefixLabel(Rect position, GUIContent label)
    {
        position.width = EditorStyles.label.CalcSize(label).x;
        position.x -= position.width + 8;
        EditorGUI.LabelField(position, label);
    }

    private static readonly string[] labelBuffer = new string[4];
    private static void GetPositionForDrawingOptions(Rect p, ReadOnlySpan<string> labels, Span<Rect> destination)
    {
        if (labels.Length > destination.Length)
            return;

        var widths = (stackalloc float[labels.Length]);
        float summary = 0f;
        for (int i = 0; i < labels.Length; i++)
        {
            summary += widths[i] = EditorStyles.toggle.CalcSize(L10n.TempContent(labels[i])).x + 8f;
        }
        float margin = MathF.Max(8, (p.width - summary) / (labels.Length - 1));

        foreach(ref var x in widths[..^1])
        {
            x += margin;
        }
        summary += margin * (labels.Length - 1);

        if (p.width < summary)
        {
            var x = (summary - p.width) / labels.Length;
            foreach (ref var y in widths)
            {
                y -= x;
            }
        }

        for (int i = 0; i < labels.Length; i++)
        {
            var p2 = p;
            p2.width = widths[i];
            destination[i] = p2;
            p.x += widths[i];
        }
    }

    private static void DrawOptionButtons(Rect p, ReadOnlySpan<(string Label, SerializedProperty Property)> properties)
    {
        var labels = labelBuffer.AsSpan(0, properties.Length);
        _ = labels.Length;
        for (int i = 0; i < properties.Length; i++)
        {
            labels[i] = properties[i].Label;
        }

        var positions = (stackalloc Rect[properties.Length]);
        GetPositionForDrawingOptions(p, labels, positions);

        Draw(positions[0], properties[0]); // Enable
        using var scope = DisableScope.If(!properties[0].Property.boolValue);
        Draw(positions[1], properties[1]); // Animation
        using var scope2 = DisableScope.If(!properties[1].Property.boolValue);
        for (int i = 2; i < properties.Length; i++)
        {
            Draw(positions[i], properties[i]);
        }

        static void Draw(Rect p, (string Label, SerializedProperty Property) property)
        {
            var (label, prop) = property;
            EditorGUI.BeginChangeCheck();
            var v = EditorGUI.ToggleLeft(p, L10n.TempContent(label), prop.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                prop.boolValue = v;
            }
        }
    }

    private static GUIStyle boldFoldLabel;
    
    private static readonly string[] BatchTargets = { nameof(Parameter.Enable), nameof(Parameter.Animation), nameof(Parameter.Saved), nameof(Parameter.Synced) };
    internal static void DrawBatchSettings(ReadOnlySpan<SerializedProperty> properties, bool isPresetMode = false)
    {
        var line = EditorGUILayout.GetControlRect(true);
        var p = line with { width = EditorGUIUtility.labelWidth };
        var showEnableOnly = isPresetMode || !LightLimitChangerComponentEditor.IsAdvancedMode;

        // ラベル
        if (showEnableOnly)
        {
            EditorGUI.LabelField(p, L10n.Tr("common:label/batch-settings"), EditorStyles.boldLabel);
        }
        else
        {
            p.x += FoldoutStyleSize.Value.x;
            bool expanded = false;
            foreach (var property in properties)
            {
                if (!property.isExpanded)
                    continue;
                expanded = true;
                break;
            }
            EditorGUI.BeginChangeCheck();
            expanded = EditorGUI.Foldout(p with { width = p.width - FoldoutStyleSize.Value.x }, expanded, L10n.Tr("common:label/batch-settings"), true, 
                boldFoldLabel ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var property in properties)
                {
                    property.isExpanded = expanded;
                }
            }
        }
        line.width -= p.width;
        line.x += p.width;
        p = line;

        var batchTargets = BatchTargets;
        var labels = labelBuffer;
        for (int i = 0; i < batchTargets.Length; i++)
        {
            labels[i] = L10n.TrStr($"common:label/{batchTargets[i].ToLowerInvariant()}");
        }
        var positions = (stackalloc Rect[4]);
        GetPositionForDrawingOptions(p, labels, positions);

        for (int i = 0; i < labels.Length; i++)
        {
            var propertyName = batchTargets[i];
            var flag = GetValue(properties, propertyName);
            EditorGUI.showMixedValue = flag is null;
            EditorGUI.BeginChangeCheck();
            flag = EditorGUI.ToggleLeft(positions[i], L10n.TempContent(labels[i]), flag ?? false);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var x in properties)
                {
                    x.FindPropertyRelative(propertyName).boolValue = flag.Value;
                }
            }
            EditorGUI.showMixedValue = false;
            if (showEnableOnly)
                break;
        }

        LightLimitChangerComponentEditor.DrawSeparator();

        static bool? GetValue(ReadOnlySpan<SerializedProperty> properties, string name)
        {
            bool? flag = null;
            foreach (var x in properties)
            {
                var value = x.FindPropertyRelative(name).boolValue;
                if (flag == null)
                    flag = value;
                else if (flag != value)
                    return null;
            }
            return flag;
        }
    }


    public static void MinMaxSlider(Rect position, SerializedProperty property, GUIContent label, Vector2 range)
    {
        if (property.propertyType != SerializedPropertyType.Vector2)
        {
            return;
        }
        //using var scope = new PropertyScope(position, label, property);
        var vector = property.vector2Value;

        position = EditorGUI.PrefixLabel(position, label);
        position.x -= EditorGUI.indentLevel * 15f;
        position.width += EditorGUI.indentLevel * 15f;


        float floatFieldWidth = Mathf.Max(position.width * 0.1f, 50);
        float padding = 10f;

        var left = position with { width = floatFieldWidth };
        var mid = position with { width = position.width - (floatFieldWidth * 2 + padding * 2), x = position.x + left.width + padding };
        var right = position with { width = floatFieldWidth, x = position.x + left.width + mid.width + padding * 2 };

        EditorGUI.BeginChangeCheck();
        var f = EditorGUI.FloatField(left, GUIContent.none, vector.x);
        if (EditorGUI.EndChangeCheck())
        {
            vector.x = f; //Mathf.Clamp(f, range.x, vector.y);
            property.vector2Value = vector;
        }
        EditorGUI.BeginChangeCheck();
        var temp = vector;
        EditorGUI.MinMaxSlider(mid, GUIContent.none, ref temp.x, ref temp.y, Math.Min(temp.x, range.x), Math.Max(temp.y, range.y));
        if (EditorGUI.EndChangeCheck())
        {
            const float N = 0.025f;
            vector.x = Mathf.Ceil(temp.x / N) * N;
            vector.y = Mathf.Ceil(temp.y / N) * N;
            property.vector2Value = vector;
        }

        EditorGUI.BeginChangeCheck();
        f = EditorGUI.FloatField(right, GUIContent.none, vector.y);
        if (EditorGUI.EndChangeCheck())
        {
            vector.y = f; //Mathf.Clamp(f, vector.x, range.y);
            property.vector2Value = vector;
        }
    }

    public static void ColorField(Rect position, SerializedProperty property, GUIContent label, bool hdr = true, Vector2? range = default)
    {
        static void ClampColor(ref Color color, Vector2? range)
        {
            if (range is not { } v)
                return;

            color.r = Mathf.Clamp(color.r, v.x, v.y);
            color.g = Mathf.Clamp(color.g, v.x, v.y);
            color.b = Mathf.Clamp(color.b, v.x, v.y);
        }

        var color = property.colorValue;

        const float TextFieldWidth = 58;

        var p = position with { width = position.width - TextFieldWidth - 4 } ;

        ClampColor(ref color, range);
        var intensity = Mathf.Max(color.maxColorComponent, 1);

        EditorGUI.BeginChangeCheck();
        var value = EditorGUI.ColorField(p, label, color, true, true, hdr);
        bool flag = EditorGUI.EndChangeCheck();
        if (flag)
        {
            //ChangeMinMaxRange(value, rangeOrProperty);
            ClampColor(ref value, range);
        }
        var color2 = (value / intensity) with { a = value.a };
        p.x += p.width + 4;

        p.width = TextFieldWidth;
        EditorGUI.BeginChangeCheck();
        var hex = EditorGUI.DelayedTextField(p, GUIContent.none, color2.GetColorCodeRGB());
        if (EditorGUI.EndChangeCheck() && ColorUtility.TryParseHtmlString(hex, out color2))
        {
            value = (color2 * intensity) with { a = value.a };
            flag = true;
        }

        if (flag)
        {
            property.colorValue = value;
        }
    }
}

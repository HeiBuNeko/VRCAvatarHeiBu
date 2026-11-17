#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Profiling;
using VRC.SDK3.Avatars.Components;
using static VRC.SDK3A.Editor.FaceTrackingDrawerUtility;

namespace VRC.SDK3A.Editor
{
    /// <summary>The same behavior editor is used by the Avatar Descriptor and standalone authoring fields.</summary>
    // The general structure is similar to FaceTrackingBlendshapesSetDrawer, you can see the comments there for more context on how the drawer works.
    [CustomPropertyDrawer(typeof(VRCAvatarDescriptor.FaceTrackingParameterBehavior))]
    public class FaceTrackingParameterBehaviorDrawer : PropertyDrawer
    {
        const int MaxInputs = FaceTrackingMappingValidator.MaxParameterBehaviorInputs;
        static readonly string[] TargetNames = { "Forward to Animator", "Apply Blend Set Weight", "Apply to Blendshape", "Limit Blendshape (Max)", "Limit Blendshape (Min)" };
        static readonly string[] CombineNames = { "Max", "Min", "Add", "Multiply", "Custom" };
        static readonly string[] KeyNames = FaceTrackingPickers.KeyNames;
        static readonly GUIContent Invert = new GUIContent("", "Uses 1 - value");
        static readonly GUIContent BlendshapePickerTitle = new GUIContent("Select Blendshape");
        static readonly GUIContent WeightBlendSetLabel = new GUIContent("Bendshapes Set (Optional)",
            "This blendshapes set's weight will control how strongly to apply. Leave empty to always fully apply");
        static readonly GUIContent ExpressionLabel = new GUIContent("Expression",
            "Use tracking key names, Voice (voice volume, 0-1), and viseme keys such as Viseme_aa (0-1).\nVisemes require LipSync Mode 'Viseme BlendShape Face Tracking'.\nExample: max(EyeClosedLeft, Viseme_aa) * 0.5\nSupported: + - * / abs() min() max() clamp()\nOutput is clamped between -1 and 1.");
        static readonly ProfilerMarker DrawMarker = new ProfilerMarker("VRC.FaceTracking.ParameterBehaviorDrawer");
        static float HeaderHeight => Line + EditorStyles.helpBox.padding.vertical;

        class State
        {
            public SerializedProperty property;
            public SerializedProperty targetMode;
            public SerializedProperty outputParameterName;
            public SerializedProperty targetBlendSetName;
            public SerializedProperty weightBlendSetName;
            public SerializedProperty targetBlendshapeName;
            public SerializedProperty combineMode;
            public SerializedProperty customExpression;
            public SerializedProperty keys, names, inverted, array;
            public SerializedProperty[] inputKeys, inputNames, inputInverted;
            public int arrayIndex;
            public FaceTrackingEditorContext context;
            public string targetWarning, inputWarning;
            public GUIContent targetWarningContent = new GUIContent(), inputWarningContent = new GUIContent();
            public string countLabel;
            public int count;
            public float width;
            public bool custom;
            public bool BlendshapeMode => targetMode.intValue == (int)VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyToBlendshape
                || targetMode.intValue == (int)VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMax
                || targetMode.intValue == (int)VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMin;
            
            int _revision = -1;
            int _tick = -1;
            int _catalogVersion = -1;
            int _dataRevision = -1;
            FaceTrackingEditorContext.MeshCatalog _catalog;
            public FaceTrackingEditorContext.MeshCatalog Catalog => context.Catalog;

            public void Bind(SerializedProperty next)
            {
                if (property == null || property.serializedObject != next.serializedObject || _revision != FaceTrackingEditorContext.BindingsRevision)
                {
                    property = next.Copy();
                    targetMode = property.FindPropertyRelative("targetMode");
                    outputParameterName = property.FindPropertyRelative("outputParameterName");
                    targetBlendSetName = property.FindPropertyRelative("targetBlendSetName");
                    weightBlendSetName = property.FindPropertyRelative("weightBlendSetName");
                    targetBlendshapeName = property.FindPropertyRelative("targetBlendshapeName");
                    combineMode = property.FindPropertyRelative("combineMode");
                    customExpression = property.FindPropertyRelative("customExpression");
                    keys = property.FindPropertyRelative("inputTrackingKeys");
                    names = property.FindPropertyRelative("inputTrackingKeyNames");
                    inverted = property.FindPropertyRelative("inputInverted");
                    
                    ArrayElement(property, out array, out arrayIndex);
                    context = FaceTrackingEditorContext.ForProperty(property);
                    inputKeys = null;
                    _revision = FaceTrackingEditorContext.BindingsRevision;
                    _tick = -1;
                }
                Refresh();
            }

            public void Refresh(bool force = false)
            {
                var catalog = Catalog;
                if (!force && _tick == FaceTrackingEditorContext.Tick && _catalog == catalog && _catalogVersion == catalog.Version && _dataRevision == FaceTrackingEditorContext.Revision)
                    return;
                
                _tick = FaceTrackingEditorContext.Tick;
                _catalog = catalog;
                _catalogVersion = catalog.Version;
                _dataRevision = FaceTrackingEditorContext.Revision;
                int size = keys.arraySize;
                if (size != count || countLabel == null)
                {
                    countLabel = "(" + size + ")";
                }
                count = size;
                custom = combineMode.intValue == (int)VRCAvatarDescriptor.ParameterBehaviorCombineMode.Custom;
                inputWarning = FaceTrackingMappingValidator.GetParameterBehaviorInputWarning(custom, customExpression.stringValue, count);
                targetWarning = FaceTrackingMappingValidator.GetParameterBehaviorTargetWarning(
                    (VRCAvatarDescriptor.ParameterBehaviorTargetMode)targetMode.intValue,
                    targetBlendSetName.stringValue, targetBlendshapeName.stringValue, context.SourceAvatar != null, catalog);
                targetWarningContent.text = targetWarning;
                inputWarningContent.text = inputWarning;
                if (targetWarning != null || inputWarning != null)
                    property.isExpanded = true;
            }

            public void BindInputs()
            {
                if (inputKeys != null && inputKeys.Length == keys.arraySize && names.arraySize == keys.arraySize && inverted.arraySize == keys.arraySize)
                    return;
                
                int oldNames = names.arraySize, oldFlags = inverted.arraySize;
                if (oldNames != keys.arraySize) names.arraySize = keys.arraySize;
                if (oldFlags != keys.arraySize) inverted.arraySize = keys.arraySize;
                inputKeys = new SerializedProperty[keys.arraySize]; inputNames = new SerializedProperty[keys.arraySize]; inputInverted = new SerializedProperty[keys.arraySize];
                for (int i = 0; i < keys.arraySize; i++)
                {
                    inputKeys[i] = keys.GetArrayElementAtIndex(i); inputNames[i] = names.GetArrayElementAtIndex(i); inputInverted[i] = inverted.GetArrayElementAtIndex(i);
                    if (i >= oldNames) inputNames[i].stringValue = InputName(inputKeys[i].intValue);
                    if (i >= oldFlags) inputInverted[i].boolValue = false;
                }
            }
        }

        readonly Dictionary<(int target, string path), State> _states = new Dictionary<(int, string), State>();

        State GetState(SerializedProperty property)
        {
            var key = (property.serializedObject.targetObject.GetInstanceID(), property.propertyPath);
            if (!_states.TryGetValue(key, out var state))
            {
                _states.Add(key, state = new State());
            }
            state.Bind(property);
            return state;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property) => false;
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => FaceTrackingDrawerUtility.CreatePropertyGUI(this, property);
        internal void DrawInspectorGUI(Rect rect, SerializedProperty property)
        {
            OnGUI(new Rect(rect.x, rect.y, rect.width, GetPropertyHeight(property, GUIContent.none)), property, GUIContent.none);
        }

        static float WarningHeight(GUIContent content, float width) => string.IsNullOrEmpty(content.text) ? 0f : Mathf.Max(38f, EditorStyles.helpBox.CalcHeight(content, Mathf.Max(100f, width - 35f))) + EditorGUIUtility.standardVerticalSpacing;
        static float InputsHeight(State state) => state.custom ? Step : Step * (state.count + 2) + (state.count == MaxInputs ? 40f : 0f);
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var state = GetState(property);
            bool embedded = label == GUIContent.none || string.IsNullOrEmpty(label.text);
            float width = state.width > 0 ? state.width : EditorGUIUtility.currentViewWidth - 40f;
            float height = HeaderHeight + (embedded ? 0f : Step);
            if (property.isExpanded)
            {
                height += InputsHeight(state) + WarningHeight(state.targetWarningContent, width) + WarningHeight(state.inputWarningContent, width) + 5f
                    + (embedded ? 0f : state.context.AuthoringHeight(false)) + (state.BlendshapeMode ? Step : 0f);
            }
            return height;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using var sample = DrawMarker.Auto();
            EditorGUI.BeginProperty(position, label, property);
            int indent = EditorGUI.indentLevel;
            try
            {
                bool embedded = label == GUIContent.none || string.IsNullOrEmpty(label.text);
                var state = GetState(property);
                state.width = position.width;
                EditorGUI.indentLevel = 0;
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && position.Contains(Event.current.mousePosition))
                {
                    var menu = new GenericMenu();
                    var entry = property.Copy();
                    menu.AddItem(new GUIContent("Copy Behavior"), false, () => Copy(entry, true));
                    menu.ShowAsContext(); Event.current.Use();
                }
                if (!embedded)
                {
                    var heading = TakeLine(ref position);
                    EditorGUI.LabelField(heading, label);
                    if (GUI.Button(new Rect(heading.xMax - 134f, heading.y, 64f, Line), CopyContent)) Copy(property, true);
                    if (GUI.Button(new Rect(heading.xMax - 64f, heading.y, 64f, Line), PasteContent) && Paste(property, true) >= 0) return;
                }
                var header = new Rect(position.x, position.y, position.width, HeaderHeight);
                GUI.Box(header, GUIContent.none, EditorStyles.helpBox);
                position.y += HeaderHeight;
                
                var row = EditorStyles.helpBox.padding.Remove(header);
                row.height = Line;
                
                // Compensate for Unity's foldout offset to keep the arrow inside its reserved slot.
                var foldoutRect = EditorStyles.foldout.margin.Add(new Rect(row.x, row.y, 20f, Line));
                if (EditorGUIUtility.hierarchyMode)
                {
                    foldoutRect.xMin += EditorStyles.foldout.padding.left - EditorStyles.label.padding.left;
                }
                
                bool warning = state.targetWarning != null || state.inputWarning != null;
                using (new EditorGUI.DisabledScope(warning))
                {
                    property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
                }
                
                // Draw row header: Mode, ParameterName, Combine Behavior (Max, Multiply, etc.) 
                {
                    float controls = state.array != null ? 25f : 0f;
                    float combineWidth = embedded ? 70f : Mathf.Min(70f, row.width * 0.13f);
                    var combineRect = new Rect(row.xMax - controls - 30f - combineWidth, row.y, combineWidth, Line);
                    
                    using (var c = new EditorGUI.ChangeCheckScope())
                    {
                        float modeWidth = embedded ? 150f : Mathf.Min(150f, row.width * 0.27f);
                        var modeRect = new Rect(row.x + 20f, row.y, modeWidth, Line);
                        var targetRect = new Rect(modeRect.xMax + 4f, row.y, combineRect.x - modeRect.xMax - 8f, Line);
                        state.targetMode.intValue = EditorGUI.Popup(modeRect, Mathf.Clamp(state.targetMode.intValue, 0, TargetNames.Length - 1), TargetNames);
                        DrawTarget(targetRect, state);
                        state.combineMode.intValue = EditorGUI.Popup(combineRect, Mathf.Clamp(state.combineMode.intValue, 0, CombineNames.Length - 1), CombineNames);
                        // If target mode or combine mode changed - refresh the state to update warnings and input count
                        if (c.changed)
                        {
                            state.Refresh(true);
                        }
                    }
                    
                    EditorGUI.LabelField(new Rect(combineRect.xMax + 2f, row.y, 28f, Line), state.custom ? "" : state.countLabel);
                    if (state.array != null && DrawListButtons(new Rect(row.xMax - controls, row.y, controls, Line), state))
                    {
                        return;
                    }
                }
                
                if (!property.isExpanded) return;
                // If not drawn inside VRC Avatar Descriptor, draw the source avatar field and mesh selector if needed
                if (!embedded) state.context.DrawAuthoring(ref position, false);
                
                DrawWarning(ref position, state.targetWarningContent);
                DrawWarning(ref position, state.inputWarningContent);
                position.x += 15f; position.width -= 15f;
                
                if (state.BlendshapeMode)
                {
                    var weightRow = TakeLine(ref position);
                    float labelWidth = Mathf.Min(190f, weightRow.width * 0.55f);
                    EditorGUI.LabelField(new Rect(weightRow.x, weightRow.y, labelWidth, Line), WeightBlendSetLabel);
                    weightRow.xMin += labelWidth;
                    EditorGUI.PropertyField(weightRow, state.weightBlendSetName, GUIContent.none);
                }
                DrawInputs(position, state);
            }
            finally
            {
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
            }
        }

        static void DrawWarning(ref Rect position, GUIContent content)
        {
            float height = WarningHeight(content, position.width);
            if (height == 0f) return;
            EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, height - EditorGUIUtility.standardVerticalSpacing), content.text, MessageType.Warning);
            position.y += height;
        }

        static void DrawTarget(Rect rect, State state)
        {
            var mode = (VRCAvatarDescriptor.ParameterBehaviorTargetMode)state.targetMode.intValue;
            if (mode == VRCAvatarDescriptor.ParameterBehaviorTargetMode.ForwardToAnimator)
            {
                EditorGUI.BeginChangeCheck();
                string name = EditorGUI.TextField(rect, state.outputParameterName.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    name = name.Replace(" ", "_");
                    state.outputParameterName.stringValue = name.Length > 64 ? name.Substring(0, 64) : name;
                }
                return;
            }
            if (mode == VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyBlendSetWeight)
            {
                state.targetBlendSetName.stringValue = EditorGUI.TextField(rect, state.targetBlendSetName.stringValue);
                return;
            }
            DrawBlendshapePicker(rect, state.targetBlendshapeName, state.Catalog, BlendshapePickerTitle);
        }

        static bool DrawListButtons(Rect rect, State state)
        {
            if (!GUI.Button(new Rect(rect.x, rect.y, 25f, Line), "-"))
                return false;
            
            var expanded = new bool[state.array.arraySize];
            for (int i = 0; i < expanded.Length; i++) expanded[i] = state.array.GetArrayElementAtIndex(i).isExpanded;
            Undo.SetCurrentGroupName("Remove Parameter Behavior");
            state.array.DeleteArrayElementAtIndex(state.arrayIndex);
            for (int i = state.arrayIndex; i < state.array.arraySize; i++) state.array.GetArrayElementAtIndex(i).isExpanded = expanded[i + 1];
            FaceTrackingEditorContext.InvalidateBindings();
            return true;
        }

        static void DrawInputs(Rect position, State state)
        {
            if (state.custom)
            {
                EditorGUI.PropertyField(TakeLine(ref position), state.customExpression, ExpressionLabel);
                return;
            }
            state.BindInputs();
            var header = TakeLine(ref position);
            EditorGUI.LabelField(new Rect(header.x, header.y, 45f, Line), "Invert", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(header.x + 48f, header.y, header.width - 48f, Line), "Input Tracking Keys", EditorStyles.boldLabel);
            for (int i = 0; i < state.inputKeys.Length; i++)
            {
                var row = TakeLine(ref position);
                var inverted = state.inputInverted[i];
                EditorGUI.BeginChangeCheck();
                inverted.boolValue = EditorGUI.Toggle(new Rect(row.x, row.y, 40f, Line), Invert, inverted.boolValue);
                if (EditorGUI.EndChangeCheck()) Undo.SetCurrentGroupName(inverted.boolValue ? "Invert Input" : "Uninvert Input");
                if (GUI.Button(new Rect(row.x + 48f, row.y, row.width - 76f, Line),
                        InputName(state.inputKeys[i].intValue), EditorStyles.popup))
                {
                    FaceTrackingPickers.OpenTrackingKey(state.inputKeys[i], state.inputNames[i], !state.Catalog.UseAllKeys);
                }
                
                if (GUI.Button(new Rect(row.xMax - 25f, row.y, 25f, Line), "-"))
                {
                    Undo.SetCurrentGroupName("Remove Input Key");
                    state.keys.DeleteArrayElementAtIndex(i); state.names.DeleteArrayElementAtIndex(i); state.inverted.DeleteArrayElementAtIndex(i);
                    state.inputKeys = null;
                    state.Refresh(true);
                    break;
                }
            }
            var add = TakeLine(ref position); add.width = 100f;
            using (new EditorGUI.DisabledScope(state.keys.arraySize >= MaxInputs))
            {
                if (GUI.Button(add, "+ Add Input"))
                {
                    Undo.SetCurrentGroupName("Add Input Key");
                    int index = state.keys.arraySize++;
                    state.names.arraySize = state.inverted.arraySize = state.keys.arraySize;
                    state.keys.GetArrayElementAtIndex(index).intValue = 0;
                    state.names.GetArrayElementAtIndex(index).stringValue = InputName(0);
                    state.inverted.GetArrayElementAtIndex(index).boolValue = false;
                    state.inputKeys = null;
                    state.Refresh(true);
                }
            }

            if (state.keys.arraySize == MaxInputs)
            {
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width, 40f), "Maximum of 32 inputs per behavior reached.", MessageType.Info);
            }
        }

        static string InputName(int index) {
            if (index == (int) VRCAvatarDescriptor.FaceTrackingKey.Max)
            {
                return FaceTrackingExpressionValidator.VoiceInputName;
            }

            if (index >= VRCAvatarDescriptor.FaceTrackingVisemeInputStart 
                && index < VRCAvatarDescriptor.FaceTrackingVisemeInputStart + FaceTrackingPickers.VisemeNames.Length)
            {
                return FaceTrackingPickers.VisemeNames[index - VRCAvatarDescriptor.FaceTrackingVisemeInputStart];
            }

            if (index >= 0 && index < (int) VRCAvatarDescriptor.FaceTrackingKey.Max)
            {
                return KeyNames[index];
            }
            
            return "(Invalid)";
        }
    }
}
#endif

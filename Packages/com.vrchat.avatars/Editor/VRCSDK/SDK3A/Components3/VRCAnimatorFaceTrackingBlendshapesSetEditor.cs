#if VRC_SDK_VRCSDK3
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRC.SDK3.Avatars.Components;
using static VRC.SDKBase.VRC_AnimatorFaceTrackingBlendshapesSet;

[CustomEditor(typeof(VRCAnimatorFaceTrackingBlendshapesSet))]
public class AnimatorFaceTrackingBlendshapesSetEditor : Editor
{
    private const int MaxBlendSetsPerBehavior = 64;
    private string[] parameterNames;
    private int selectedBlendSet = -1;
    private int prevBlendSetCount;

    void OnEnable()
    {
        var blendSetsProp = serializedObject.FindProperty("blendSets");
        prevBlendSetCount = blendSetsProp.arraySize;
        UpdateParameters();
    }

    void UpdateParameters()
    {
        var controller = GetCurrentController();
        if (controller == null) return;

        var names = new List<string>();
        foreach (var p in controller.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Trigger) names.Add(p.name);
        }
        parameterNames = names.ToArray();
    }

    static AnimatorController GetCurrentController()
    {
        var toolType = Type.GetType(
            "UnityEditor.Graphs.AnimatorControllerTool, UnityEditor.Graphs"
        );
        if (toolType == null) return null;
        // find the window only if already open; GetWindow would force it open as a side effect
        var windows = Resources.FindObjectsOfTypeAll(toolType);
        if (windows == null || windows.Length == 0) return null;
        var tool = windows[0];
        if (tool == null) return null;
        var prop = toolType.GetProperty(
            "animatorController",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
        );
        if (prop == null) return null;
        return prop.GetValue(tool, null) as AnimatorController;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // detect new elements and set defaults once
        var blendSetsProp = serializedObject.FindProperty("blendSets");
        int count = blendSetsProp.arraySize;
        if (count > prevBlendSetCount)
        {
            for (int i = prevBlendSetCount; i < count; i++)
            {
                var element = blendSetsProp.GetArrayElementAtIndex(i);
                var typeProp = element.FindPropertyRelative("type");
                var sourceParamProp = element.FindPropertyRelative("sourceParameter");
                var nameProp = element.FindPropertyRelative("blendshapesSetName");
                var weightProp = element.FindPropertyRelative("weight");
                var convertRangeProp = element.FindPropertyRelative("convertRange");
                var sourceMinProp = element.FindPropertyRelative("sourceMin");
                var sourceMaxProp = element.FindPropertyRelative("sourceMax");
                var destMinProp = element.FindPropertyRelative("destMin");
                var destMaxProp = element.FindPropertyRelative("destMax");
                var lerpSpeedProp = element.FindPropertyRelative("lerpSpeed");

                typeProp.enumValueIndex = (int)SourceType.Value;
                lerpSpeedProp.floatValue = 0f;
                sourceParamProp.stringValue = "";
                convertRangeProp.boolValue = false;
                sourceMinProp.floatValue = 0f;
                sourceMaxProp.floatValue = 1f;
                destMinProp.floatValue = 0f;
                destMaxProp.floatValue = 1f;

                nameProp.stringValue = "";
                weightProp.floatValue = 1f;
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            count = blendSetsProp.arraySize;
        }
        prevBlendSetCount = count;

        if (parameterNames == null) UpdateParameters();

        EditorGUILayout.HelpBox(
            "This behavior modifies the weight of face tracking Blendshapes Sets " +
            "based on either a constant value or an animator parameter.\n\n" +
            "Blendshapes Sets are created in the Face Tracking section of the Avatar Descriptor. " +
            "Weight values will be clamped between 0 and 1.",
            MessageType.Info);

        if (count > MaxBlendSetsPerBehavior)
            EditorGUILayout.HelpBox($"Only the first {MaxBlendSetsPerBehavior} entries are processed. Remove extra entries to match runtime behavior.", MessageType.Warning);

        var editable = new InspectorUtil.EditableArray
        {
            array = blendSetsProp,
            maxElements = MaxBlendSetsPerBehavior,
            onDrawElement = DrawBlendSet
        };
        InspectorUtil.DrawEditableArray(this, editable, ref selectedBlendSet);

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("debugString"),
            new GUIContent("Debug Message"));

        serializedObject.ApplyModifiedProperties();
    }

    void DrawBlendSet(SerializedProperty blendSets, int index)
    {
        var element = blendSets.GetArrayElementAtIndex(index);
        var typeProp = element.FindPropertyRelative("type");
        var sourceParamProp = element.FindPropertyRelative("sourceParameter");
        var nameProp = element.FindPropertyRelative("blendshapesSetName");
        var weightProp = element.FindPropertyRelative("weight");
        var convertRangeProp = element.FindPropertyRelative("convertRange");
        var sourceMinProp = element.FindPropertyRelative("sourceMin");
        var sourceMaxProp = element.FindPropertyRelative("sourceMax");
        var destMinProp = element.FindPropertyRelative("destMin");
        var destMaxProp = element.FindPropertyRelative("destMax");
        var lerpSpeedProp = element.FindPropertyRelative("lerpSpeed");

        // Source type dropdown (Value or Parameter)
        EditorGUILayout.PropertyField(typeProp);

        // Blendshapes Set name
        EditorGUILayout.PropertyField(
            nameProp,
            new GUIContent("Blendshapes Set Name"));
        
        if (string.IsNullOrEmpty(nameProp.stringValue))
        {
            EditorGUILayout.HelpBox(
                "Blendshapes Set Name is required. Enter the name of a set defined in the Avatar Descriptor's Face Tracking section.",
                MessageType.Warning);
        }

        if ((SourceType)typeProp.enumValueIndex == SourceType.Parameter)
        {
            // select which parameter to read
            DrawParameterDropdown(sourceParamProp, "Source Parameter");

            // convertRange toggle
            EditorGUILayout.PropertyField(
                convertRangeProp,
                new GUIContent("Convert Range"));

            if (convertRangeProp.boolValue)
            {
                EditorGUI.indentLevel++;

                // Source range
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Source Parameter Range");
                EditorGUILayout.LabelField("Min", GUILayout.Width(64));
                sourceMinProp.floatValue = EditorGUILayout.FloatField(sourceMinProp.floatValue);
                EditorGUILayout.LabelField("Max", GUILayout.Width(64));
                sourceMaxProp.floatValue = EditorGUILayout.FloatField(sourceMaxProp.floatValue);
                EditorGUILayout.EndHorizontal();

                // Destination range
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Apply as Weight Range");
                EditorGUILayout.LabelField("Min", GUILayout.Width(64));
                destMinProp.floatValue = EditorGUILayout.FloatField(destMinProp.floatValue);
                EditorGUILayout.LabelField("Max", GUILayout.Width(64));
                destMaxProp.floatValue = EditorGUILayout.FloatField(destMaxProp.floatValue);
                EditorGUILayout.EndHorizontal();

                destMinProp.floatValue = Mathf.Clamp01(destMinProp.floatValue);
                destMaxProp.floatValue = Mathf.Clamp01(destMaxProp.floatValue);

                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUILayout.Slider(
                weightProp,
                0f, 1f,
                new GUIContent("Apply Weight"));
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Lerp Speed", "Rate of weight change per second (0-100). 0 = instant, higher values = faster transitions, values between 0~1 = very slow transitions."));
        float lerpSpeedVal = EditorGUILayout.DelayedFloatField(Mathf.Clamp(lerpSpeedProp.floatValue, 0f, 100f), GUILayout.MaxWidth(60f));
        lerpSpeedVal = Mathf.Clamp(lerpSpeedVal, 0f, 100f);
        lerpSpeedProp.floatValue = lerpSpeedVal;
        EditorGUILayout.EndHorizontal();
    }


    int DrawParameterDropdown(SerializedProperty prop, string label)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);

        int idx = -1;
        if (parameterNames != null)
        {
            EditorGUI.BeginChangeCheck();
            idx = Array.IndexOf(parameterNames, prop.stringValue);
            idx = EditorGUILayout.Popup(idx, parameterNames);
            if (EditorGUI.EndChangeCheck() && idx >= 0)
                prop.stringValue = parameterNames[idx];
        }

        prop.stringValue = EditorGUILayout.TextField(prop.stringValue);
        EditorGUILayout.EndHorizontal();

        if (idx < 0 && !string.IsNullOrEmpty(prop.stringValue))
        {
            EditorGUILayout.HelpBox(
                "Parameter '" + prop.stringValue + "' not found. Define it in the Animator window's Parameters tab.",
                MessageType.Warning);
        }

        return idx;
    }
}
#endif

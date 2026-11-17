#if VRC_SDK_VRCSDK3
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace VRC.SDK3A.Editor
{
    internal static class FaceTrackingDrawerUtility
    {
        internal const int MaxParameterBehaviors = 128;
        internal static float Line => EditorGUIUtility.singleLineHeight;
        internal static float Step => Line + EditorGUIUtility.standardVerticalSpacing;
        internal static readonly GUIContent CopyContent = new GUIContent("Copy");
        internal static readonly GUIContent PasteContent = new GUIContent("Paste");

        // UIElements wrapper for the IMGUI Property Drawer code
        internal static VisualElement CreatePropertyGUI(PropertyDrawer drawer, SerializedProperty property)
        {
            var field = property.Copy();
            var label = new GUIContent(property.displayName, property.tooltip);
            var container = new IMGUIContainer(() =>
            {
                field.serializedObject.UpdateIfRequiredOrScript();
                Rect position = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(field, label));
                drawer.OnGUI(position, field, label);
                field.serializedObject.ApplyModifiedProperties();
            });
            container.TrackPropertyValue(field, _ => container.MarkDirtyRepaint());
            return container;
        }

        internal static Rect TakeLine(ref Rect position)
        {
            var line = new Rect(position.x, position.y, position.width, Line);
            position.y += Step;
            return line;
        }

        /// <summary>
        /// Extracts the parent array and the element index from an element property path, if the property is an array element
        /// Returns true if the property is an array element and the parent array was found; false otherwise
        /// </summary>
        internal static bool ArrayElement(SerializedProperty property, out SerializedProperty array, out int index)
        {
            string path = property.propertyPath;
            int start = path.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            if (start >= 0 && path.EndsWith("]", StringComparison.Ordinal)
                && int.TryParse(path.Substring(start + 12, path.Length - start - 13), out index))
            {
                array = property.serializedObject.FindProperty(path.Substring(0, start));
                return array != null && array.isArray;
            }
            array = null;
            index = -1;
            return false;
        }

        internal static void Copy(SerializedProperty property, bool behavior)
        {
            property.serializedObject.ApplyModifiedProperties();
            if (property.isArray && property.serializedObject.targetObject is VRCAvatarDescriptor descriptor)
            {
                EditorGUIUtility.systemCopyBuffer = behavior
                    ? FaceTrackingClipboard.SerializeParameterBehaviors(descriptor.faceTrackingParameterBehaviors, descriptor)
                    : FaceTrackingClipboard.SerializeBlendshapesSets(descriptor.faceTrackingBlendshapesSetList, descriptor);
                return;
            }
            var blocks = new List<string>();
            int count = property.isArray ? property.arraySize : 1;
            for (int i = 0; i < count; i++)
            {
                var entry = property.isArray ? property.GetArrayElementAtIndex(i) : property;
                var source = FaceTrackingEditorContext.ForProperty(entry).SourceAvatar;
                blocks.Add(behavior
                    ? FaceTrackingClipboard.SerializeParameterBehaviors(new[] { (VRCAvatarDescriptor.FaceTrackingParameterBehavior)entry.boxedValue }, source)
                    : FaceTrackingClipboard.SerializeBlendshapesSets(new[] { (VRCAvatarDescriptor.FaceTrackingBlendshapesSet)entry.boxedValue }, source));
            }
            EditorGUIUtility.systemCopyBuffer = string.Join(Environment.NewLine + Environment.NewLine, blocks);
        }

        // A list appends; an individual configuration field replaces its one value.
        internal static int Paste(SerializedProperty property, bool behavior)
        {
            string title = behavior ? "Paste Parameter Behaviors" : "Paste Blendshapes Sets";
            IList values;
            List<string> sources;
            string error;
            bool success;
            if (behavior)
            {
                success = FaceTrackingClipboard.TryParseParameterBehaviors(EditorGUIUtility.systemCopyBuffer, out var parsed, out sources, out error);
                values = parsed;
            }
            else
            {
                success = FaceTrackingClipboard.TryParseBlendshapesSets(EditorGUIUtility.systemCopyBuffer, out var parsed, out sources, out error);
                values = parsed;
            }
            if (!success)
            {
                EditorUtility.DisplayDialog(title, error, "OK");
                return -1;
            }
            var array = property.isArray ? property : null;
            if (array == null)
                ArrayElement(property, out array, out _);
            int first = array != null ? array.arraySize : 0;
            int max = array != null ? (behavior ? MaxParameterBehaviors : 128) : 1;
            if (values.Count > max - first)
            {
                EditorUtility.DisplayDialog(title, $"Cannot paste {values.Count} configurations here. The maximum is {max}; no configurations were pasted.", "OK");
                return -1;
            }
            Undo.SetCurrentGroupName(title);
            if (array != null)
                array.arraySize += values.Count;
            bool authoringChanged = false;
            for (int i = 0; i < values.Count; i++)
            {
                var destination = array != null ? array.GetArrayElementAtIndex(first + i) : property;
                destination.boxedValue = values[i];
                destination.isExpanded = true;
                if (!(property.serializedObject.targetObject is VRCAvatarDescriptor) && !string.IsNullOrEmpty(sources[i]))
                {
                    FaceTrackingEditorContext.ForProperty(destination).ApplyClipboardSource(sources[i], false);
                    authoringChanged = true;
                }
            }
            if (authoringChanged)
                FaceTrackingAuthoringState.instance.Flush();
            property.serializedObject.ApplyModifiedProperties();
            FaceTrackingEditorContext.InvalidateBindings();
            return first;
        }

        internal static void DrawBlendshapePicker(Rect rect, SerializedProperty blendshapeName,
            FaceTrackingEditorContext.MeshCatalog catalog, GUIContent title)
        {
            string displayName = blendshapeName.stringValue;
            if (string.IsNullOrEmpty(displayName))
                displayName = "- None -";
            
            int mouseButton = Event.current.button;
            if (!GUI.Button(rect, displayName, EditorStyles.popup))
                return;
            
            blendshapeName.serializedObject.ApplyModifiedProperties();
            void SelectBlendshape(string selectedName)
            {
                blendshapeName.stringValue = selectedName;
                blendshapeName.serializedObject.ApplyModifiedProperties();
                FaceTrackingEditorContext.Invalidate();
            }

            // Right-clicking on the blendshape name dropdown opens the custom blendshape input dialog
            if (mouseButton == 1)
            {
                FaceTrackingPickers.OpenCustomBlendshapeName(blendshapeName.stringValue, SelectBlendshape);
            }
            else
            {
                FaceTrackingPickers.OpenBlendshapes(title, catalog.GetBlendshapeNames(), SelectBlendshape);
            }
        }
    }
}
#endif

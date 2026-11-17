#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace VRC.SDK3A.Editor
{
    internal static class FaceTrackingPickers
    {
        internal static readonly string[] KeyNames = CreateKeyNames();
        internal static readonly string[] VisemeNames = Enumerable.Range(0, (int)VRCAvatarDescriptor.Viseme.Count)
            .Select(i => VRCAvatarDescriptor.GetFaceTrackingVisemeInputName((VRCAvatarDescriptor.Viseme)i)).ToArray();
        
        static string[] CreateKeyNames()
        {
            var names = new string[(int)VRCAvatarDescriptor.FaceTrackingKey.Max];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = ((VRCAvatarDescriptor.FaceTrackingKey)i).ToString();
            }
            return names;
        }
        
        internal static readonly (string label, int tab, int[] keys)[] Categories =
        {
            ("Eye Gaze", 1, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.EyeLookOutRight, VRCAvatarDescriptor.FaceTrackingKey.EyeLookDownLeft)),
            ("Eye", 1, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.EyeClosedRight, VRCAvatarDescriptor.FaceTrackingKey.EyeWideLeft)),
            ("Brow", 2, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.BrowPinchRight, VRCAvatarDescriptor.FaceTrackingKey.BrowOuterUpLeft)),
            ("Nose", 3, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.NasalDilationRight, VRCAvatarDescriptor.FaceTrackingKey.NasalConstrictLeft)
                .Concat(KeyRange(VRCAvatarDescriptor.FaceTrackingKey.NoseSneerRight, VRCAvatarDescriptor.FaceTrackingKey.NoseSneerLeft)).ToArray()),
            ("Cheek", 4, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.CheekSquintRight, VRCAvatarDescriptor.FaceTrackingKey.CheekSuckLeft)),
            ("Jaw", 5, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.JawOpen, VRCAvatarDescriptor.FaceTrackingKey.MouthClosed)),
            ("Lip", 6, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.LipSuckUpperRight, VRCAvatarDescriptor.FaceTrackingKey.LipPuckerLowerLeft)),
            ("Mouth", 7, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.MouthUpperUpRight, VRCAvatarDescriptor.FaceTrackingKey.MouthUpperDeepenLeft)
                .Concat(KeyRange(VRCAvatarDescriptor.FaceTrackingKey.MouthLowerDownRight, VRCAvatarDescriptor.FaceTrackingKey.MouthTightenerLeft)).ToArray()),
            ("Tongue", 8, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.TongueOut, VRCAvatarDescriptor.FaceTrackingKey.TongueTwistLeft)),
            ("Throat", 9, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.SoftPalateClose, VRCAvatarDescriptor.FaceTrackingKey.NeckFlexLeft)),
            ("Other", 0, KeyRange(VRCAvatarDescriptor.FaceTrackingKey.NeckFlexLeft + 1, VRCAvatarDescriptor.FaceTrackingKey.Max - 1))
        };

        static int[] KeyRange(VRCAvatarDescriptor.FaceTrackingKey start, VRCAvatarDescriptor.FaceTrackingKey end)
            => Enumerable.Range((int)start, (int)end - (int)start + 1).ToArray();

        static TrackingKeyPicker _trackingKeyPicker;

        ///<summary>
        /// Opens a search window for the blendshape names, with an option to type a custom name
        /// </summary>
        internal static void OpenBlendshapes(GUIContent title, string[] blendshapeNames, Action<string> onSelect)
        {
            AvatarEditorHelper.OpenSearchWindowPopup(title, blendshapeNames,
                index => onSelect?.Invoke(index < 0 ? "" : blendshapeNames[index]),
                new GUIContent("Type Custom Name..."), () => OpenCustomBlendshapeName("", onSelect));
        }

        ///<summary>
        /// Opens a search window for the supported Face Tracking Keys (VRCAvatarDescriptor.FaceTrackingKey)
        /// </summary>
        internal static void OpenTrackingKey(SerializedProperty key, SerializedProperty name, bool basicOnly)
        {
            key.serializedObject.ApplyModifiedProperties();
            if (_trackingKeyPicker == null)
                _trackingKeyPicker = ScriptableObject.CreateInstance<TrackingKeyPicker>();
            _trackingKeyPicker.key = key.Copy();
            _trackingKeyPicker.nameProperty = name.Copy();
            _trackingKeyPicker.basicOnly = basicOnly;
            SearchWindow.Open(new SearchWindowContext(ScreenMousePosition()), _trackingKeyPicker);
        }

        internal static void OpenCustomBlendshapeName(string value, Action<string> onConfirm) => CustomBlendshapeNameWindow.Show(ScreenMousePosition(), value, onConfirm);
        static Vector2 ScreenMousePosition() => Event.current != null ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition) : new Vector2(400f, 300f);

        class TrackingKeyPicker : ScriptableObject, ISearchWindowProvider
        {
            public SerializedProperty key;
            public SerializedProperty nameProperty;
            public bool basicOnly;
            public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
            {
                var entries = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("Select Tracking Key"), 0) };
                foreach (var category in Categories)
                {
                    if (category.keys.Length == 0)
                        continue;
                    entries.Add(new SearchTreeGroupEntry(new GUIContent(category.label), 1));
                    foreach (int i in category.keys)
                    {
                        if (!basicOnly || VRCAvatarDescriptor.IsBasicFaceTrackingKey(i))
                            entries.Add(new SearchTreeEntry(new GUIContent(KeyNames[i])) { level = 2, userData = i });
                    }
                }
                entries.Add(new SearchTreeGroupEntry(new GUIContent("Built-In"), 1));
                entries.Add(new SearchTreeEntry(new GUIContent(FaceTrackingExpressionValidator.VoiceInputName)) { level = 2, userData = (int)VRCAvatarDescriptor.FaceTrackingKey.Max });
                entries.Add(new SearchTreeGroupEntry(new GUIContent("Visemes"), 1));
                for (int i = 0; i < VisemeNames.Length; i++)
                    entries.Add(new SearchTreeEntry(new GUIContent(VisemeNames[i])) { level = 2, userData = VRCAvatarDescriptor.FaceTrackingVisemeInputStart + i });
                return entries;
            }
            public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
            {
                key.intValue = (int)entry.userData;
                nameProperty.stringValue = entry.name;
                key.serializedObject.ApplyModifiedProperties();
                FaceTrackingEditorContext.Invalidate();
                return true;
            }
        }

        /// <summary>
        /// Shows a raw text input window for entering a custom blendshape name
        /// </summary>
        class CustomBlendshapeNameWindow : EditorWindow
        {
            Action<string> _onConfirm;
            string _value;
            bool _focus = true;
            public static void Show(Vector2 position, string value, Action<string> onConfirm)
            {
                var window = CreateInstance<CustomBlendshapeNameWindow>();
                window.titleContent = new GUIContent("Custom Blendshape Name");
                window._value = value ?? "";
                window._onConfirm = onConfirm;
                window.position = new Rect(position.x, position.y, 430f, 170f);
                window.ShowUtility();
            }
            void OnGUI()
            {
                EditorGUILayout.LabelField("Blendshape Name", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Applies to every configured face tracking mesh containing this exact name, including blendshapes added later by build-time tools.", MessageType.Info);
                bool confirm = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
                bool cancel = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;
                if (confirm || cancel)
                    Event.current.Use();
                GUI.SetNextControlName("CustomNameField");
                _value = EditorGUILayout.TextField(_value);
                if (_focus)
                {
                    EditorGUI.FocusTextInControl("CustomNameField");
                    _focus = false;
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("OK") || confirm)
                {
                    string name = _value.Trim();
                    if (name.Length > 0)
                    {
                        _onConfirm?.Invoke(name);
                        Close();
                    }
                }
                if (GUILayout.Button("Cancel") || cancel)
                    Close();
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif

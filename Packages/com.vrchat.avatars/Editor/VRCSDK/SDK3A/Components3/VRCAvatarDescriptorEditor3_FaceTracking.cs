#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using VRC.SDK3A.Editor;

public partial class AvatarDescriptorEditor3 : Editor
{
    const string FaceTrackingFoldoutPrefsKey = "VRCSDK3_AvatarDescriptorEditor3_FaceTrackingFoldout";
    const string LipSyncFoldoutPrefsKey = "VRCSDK3_AvatarDescriptorEditor3_LipSyncFoldout";
    const string BlendshapesSetsFoldoutPrefsKey = "VRCSDK3_AvatarDescriptorEditor3_FaceTrackingBlendshapesSetsFoldout";
    const string ParameterBehaviorsFoldoutPrefsKey = "VRCSDK3_AvatarDescriptorEditor3_FaceTrackingParameterBehaviorsFoldout";
    const int MaxFaceTrackingMeshes = 16;
    const int MaxBlendshapesSets = 128;
    const int MaxParameterBehaviors = FaceTrackingDrawerUtility.MaxParameterBehaviors;
    readonly FaceTrackingBlendshapesSetDrawer _blendshapesSetDrawer = new FaceTrackingBlendshapesSetDrawer();
    readonly FaceTrackingParameterBehaviorDrawer _parameterBehaviorDrawer = new FaceTrackingParameterBehaviorDrawer();
    ReorderableList _parameterBehaviorList;
    SerializedProperty _propFaceTrackingMeshes;
    SerializedProperty _propBlendshapesSetList;
    SerializedProperty _propParameterBehaviors;
    int _faceTrackingSelectedBlendshapesSet;
    string[] _setOptionsCache;
    int _setOptionsRevision = -1;
    int _meshPathCatalogRevision = -1;
    int _meshPathHierarchyRevision = -1;
    string _meshPathWarning;

    internal enum AlertSection { FaceTracking, Meshes, BlendshapeSets, ParameterBehaviors, LipSync }

    sealed class AlertNavigation
    {
        internal VRCAvatarDescriptor avatar;
        internal AlertSection section;
        internal int index;
        internal double expiresAt;
    }

    static AlertNavigation _requestedAlertNavigation;
    AlertNavigation _activeAlertNavigation;
    IMGUIContainer _inspectorGUI;
    Vector2 _inspectorGUIOrigin;
    AlertSection _alertSection;
    int _alertIndex;
    bool _alertScrollPending;
    double _alertHighlightUntil;

    internal static void SelectAlert(VRCAvatarDescriptor avatar, AlertSection section, int index = -1)
    {
        if (avatar == null) return;
        
        _requestedAlertNavigation = new AlertNavigation
        {
            avatar = avatar, section = section, index = index,
            expiresAt = EditorApplication.timeSinceStartup + 5
        };
        
        switch (section)
        {
            case AlertSection.LipSync:
                EditorPrefs.SetBool(LipSyncFoldoutPrefsKey, true);
                break;
            // BlendshapeSets and ParameterBehaviors alerts also need to open the FaceTrackingFoldout itself if it was closed
            case AlertSection.BlendshapeSets:
                EditorPrefs.SetBool(FaceTrackingFoldoutPrefsKey, true);
                EditorPrefs.SetBool(BlendshapesSetsFoldoutPrefsKey, true);
                break;
            case AlertSection.ParameterBehaviors:
                EditorPrefs.SetBool(FaceTrackingFoldoutPrefsKey, true);
                EditorPrefs.SetBool(ParameterBehaviorsFoldoutPrefsKey, true);
                break;
            default:
                EditorPrefs.SetBool(FaceTrackingFoldoutPrefsKey, true);
                break;
        }
       
        InternalEditorUtility.SetIsInspectorExpanded(avatar, true);
        Selection.activeGameObject = avatar.gameObject;
        EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        foreach (AvatarDescriptorEditor3 editor in Resources.FindObjectsOfTypeAll<AvatarDescriptorEditor3>())
        {
            if (editor.target == avatar)
                editor.Repaint();
        }
    }

    // Prepares for jumping to a particular spot of the inspector when an alert is clicked
    void PrepareAlertNavigation()
    {
        _inspectorGUIOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
        if (_requestedAlertNavigation != null && EditorApplication.timeSinceStartup > _requestedAlertNavigation.expiresAt)
            _requestedAlertNavigation = null;
        AlertNavigation request = _requestedAlertNavigation;
        // Apply expansion before layout measures the behavior list, including when the avatar was already selected.
        if (Event.current.type != EventType.Layout || request == null || request.avatar != target || request == _activeAlertNavigation)
            return;
        
        _activeAlertNavigation = request;
        _alertSection = request.section;
        _alertIndex = request.index;
        _alertScrollPending = true;
        _alertHighlightUntil = 0;
        if (!avatarDescriptor.enableFaceTracking && _alertSection != AlertSection.LipSync)
            _alertSection = AlertSection.FaceTracking;
        if (_alertSection == AlertSection.ParameterBehaviors)
        {
            if (_alertIndex >= 0 && _alertIndex < _propParameterBehaviors.arraySize)
            {
                _propParameterBehaviors.GetArrayElementAtIndex(_alertIndex).isExpanded = true;
                _parameterBehaviorList = null;
                GetParameterBehaviorList().index = _alertIndex;
            }
            else
            {
                _alertIndex = -1;
            }
        }
        if (_alertSection == AlertSection.BlendshapeSets)
        {
            if (_alertIndex >= 0 && _alertIndex < _propBlendshapesSetList.arraySize)
            {
                _faceTrackingSelectedBlendshapesSet = _alertIndex;
            }
            _blendshapesSetDrawer.RevealInspectorMappings();
        }
    }

    void DrawAlertTarget(AlertSection section, Rect rect, int index = -1)
    {
        if (Event.current.type != EventType.Repaint || _activeAlertNavigation == null || _alertSection != section
            || (section == AlertSection.ParameterBehaviors && _alertIndex != index))
        {
            return;
        }
        
        if (_alertScrollPending && EditorApplication.timeSinceStartup <= _activeAlertNavigation.expiresAt)
        {
            _alertScrollPending = false;
            if (_inspectorGUI != null)
            {
                AlertNavigation request = _activeAlertNavigation;
                Vector2 position = GUIUtility.GUIToScreenPoint(rect.position) - _inspectorGUIOrigin;
                // Layout and the Inspector's content height must settle before changing its scroll offset.
                _inspectorGUI.schedule.Execute(() =>
                {
                    if (this == null || _inspectorGUI.panel == null || _activeAlertNavigation != request
                        || _requestedAlertNavigation != request)
                        return;
                    ScrollView scrollView = _inspectorGUI.GetFirstAncestorOfType<ScrollView>();
                    if (scrollView != null)
                    {
                        float y = _inspectorGUI.ChangeCoordinatesTo(scrollView.contentContainer, position).y;
                        scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, Mathf.Max(0, y - 12));
                    }
                    _alertHighlightUntil = EditorApplication.timeSinceStartup + 2;
                    Repaint();
                });
            }
            else
            {
                GUI.ScrollTo(rect);
                _alertHighlightUntil = EditorApplication.timeSinceStartup + 2;
            }
        }
        if (EditorApplication.timeSinceStartup < _alertHighlightUntil)
        {
            rect.height = EditorGUIUtility.singleLineHeight + 4;
            EditorGUI.DrawRect(rect, new Color(1f, 0.65f, 0.1f, 0.2f));
            Repaint();
        }
    }

    void Init_FaceTracking()
    {
        _propFaceTrackingMeshes = serializedObject.FindProperty("faceTrackingMeshes");
        _propBlendshapesSetList = serializedObject.FindProperty("faceTrackingBlendshapesSetList");
        _propParameterBehaviors = serializedObject.FindProperty("faceTrackingParameterBehaviors");
        _meshPathCatalogRevision = -1;
        FaceTrackingEditorContext.Invalidate();
    }

    void DrawInspector_FaceTracking()
    {
        bool expanded = Foldout(FaceTrackingFoldoutPrefsKey, "Face Tracking", false);
        DrawAlertTarget(AlertSection.FaceTracking, GUILayoutUtility.GetLastRect());
        if (!expanded) return;
        
        SerializedProperty enabled = serializedObject.FindProperty("enableFaceTracking");
        if (GUILayout.Button(enabled.boolValue ? "Disable" : "Enable"))
        {
            enabled.boolValue = !enabled.boolValue;
        }
        
        if (!enabled.boolValue)
        {
            Separator();
            return;
        }

        SerializedProperty allKeys = serializedObject.FindProperty("useAllTrackingKeys");
        using (var c = new EditorGUI.ChangeCheckScope())
        {
            EditorGUILayout.PropertyField(allKeys, new GUIContent("Use All Tracking Keys", "Signals that this avatar supports actual hardware face tracking (via webcam, OSC, etc) and unlocks all the associated tracking keys in blendshape sets and parameter behaviors. Leave unchecked if only using advanced viseme support or advanced automatic eyelook support - eye look, blink, and viseme mapping work without it."));
            if (c.changed)
            {
                serializedObject.ApplyModifiedProperties();
                FaceTrackingEditorContext.Invalidate();
            }
        }

        if (!allKeys.boolValue)
        {
            EditorGUILayout.HelpBox("If the avatar is only using this face tracking section for advanced viseme support or advanced automatic eyelook support, leave this unchecked. Checking this signals that this avatar supports actual hardware face tracking (via webcam, OSC, etc), and unlocks all the associated tracking keys.", MessageType.Info);
        }

        DrawFaceTrackingMeshList();
        bool hasMesh = false;
        for (int i = 0; i < _propFaceTrackingMeshes.arraySize; i++)
        {
            SkinnedMeshRenderer mesh = _propFaceTrackingMeshes.GetArrayElementAtIndex(i).objectReferenceValue as SkinnedMeshRenderer;
            if (mesh != null && mesh.sharedMesh != null)
            {
                hasMesh = true;
                break;
            }
        }
        if (hasMesh)
        {
            DrawFaceTrackingDampening(allKeys.boolValue);
            DrawBlendshapesSetCollection();
        }
        else if (_alertSection == AlertSection.BlendshapeSets)
        {
            // Set mappings are hidden until a valid mesh is assigned; reveal the prerequisite instead.
            _alertSection = AlertSection.Meshes;
            Repaint();
        }
        GUILayout.Space(10);
        DrawParameterBehaviorCollection(allKeys.boolValue);
        Separator();
    }

    void DrawFaceTrackingMeshList()
    {
        bool hasMesh = false;
        for (int i = 0; i < _propFaceTrackingMeshes.arraySize; i++)
        {
            SkinnedMeshRenderer mesh = _propFaceTrackingMeshes.GetArrayElementAtIndex(i).objectReferenceValue as SkinnedMeshRenderer;
            hasMesh |= mesh != null && mesh.sharedMesh != null;
        }
        if (hasMesh)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset To Default", GUILayout.Width(120)) && EditorUtility.DisplayDialog("Reset to Default", "This will erase any face tracking settings. Are you sure?", "OK", "Cancel"))
            {
                _propFaceTrackingMeshes.ClearArray();
                _propBlendshapesSetList.ClearArray();
                _faceTrackingSelectedBlendshapesSet = 0;
                _blendshapesSetDrawer.ResetInspectorView();
                serializedObject.ApplyModifiedProperties();
                FaceTrackingEditorContext.InvalidateBindings();
            }
            EditorGUILayout.EndHorizontal();
        }
        BeginBox(_propFaceTrackingMeshes.arraySize > 1 ? "Face Tracking Meshes" : "Face Tracking Mesh");
        DrawAlertTarget(AlertSection.Meshes, GUILayoutUtility.GetLastRect());
        
        EditorGUI.BeginChangeCheck();
        HandleFaceTrackingMeshDrop(GUILayoutUtility.GetLastRect());
        for (int i = 0; i < _propFaceTrackingMeshes.arraySize; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                SerializedProperty mesh = _propFaceTrackingMeshes.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(mesh, GUIContent.none);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    mesh.objectReferenceValue = null;
                    _propFaceTrackingMeshes.DeleteArrayElementAtIndex(i--);
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_propFaceTrackingMeshes.arraySize >= MaxFaceTrackingMeshes))
            {
                if (GUILayout.Button("Add Mesh", GUILayout.Width(80)))
                {
                    int index = _propFaceTrackingMeshes.arraySize++;
                    _propFaceTrackingMeshes.GetArrayElementAtIndex(index).objectReferenceValue = null;
                }
            }
        }
        HandleFaceTrackingMeshDrop(GUILayoutUtility.GetLastRect());
        
        if (_propFaceTrackingMeshes.arraySize >= MaxFaceTrackingMeshes)
            EditorGUILayout.HelpBox($"Maximum of {MaxFaceTrackingMeshes} face tracking meshes reached.", MessageType.Info);
        
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            FaceTrackingEditorContext.Invalidate();
        }
        
        // Cache mesh-path warnings until the mesh catalog or hierarchy changes,
        // including asset changes and undo/redo.
        var catalog = FaceTrackingEditorContext.ForProperty(_propFaceTrackingMeshes).Catalog;
        if (_meshPathCatalogRevision != catalog.Version || _meshPathHierarchyRevision != FaceTrackingEditorContext.HierarchyRevision)
        {
            List<string> pathIssues = FaceTrackingMappingValidator.CollectAmbiguousMeshPaths((VRCAvatarDescriptor)target);
            _meshPathWarning = pathIssues.Count > 0
                ? FaceTrackingMappingValidator.DuplicateMeshPathWarning + "\n" + string.Join("\n", pathIssues)
                : null;
            _meshPathCatalogRevision = catalog.Version;
            _meshPathHierarchyRevision = FaceTrackingEditorContext.HierarchyRevision;
        }
        if (_meshPathWarning != null)
            EditorGUILayout.HelpBox(_meshPathWarning, MessageType.Warning);
        
        EndBox();
        GUILayout.Space(5);
    }

    void HandleFaceTrackingMeshDrop(Rect dropRect)
    {
        var evt = Event.current;
        if ((evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) ||
            !dropRect.Contains(evt.mousePosition))
            return;

        var existing = new HashSet<SkinnedMeshRenderer>();
        for (int i = 0; i < _propFaceTrackingMeshes.arraySize; i++)
            existing.Add(_propFaceTrackingMeshes.GetArrayElementAtIndex(i).objectReferenceValue as SkinnedMeshRenderer);

        var additions = new List<SkinnedMeshRenderer>();
        foreach (var dragged in DragAndDrop.objectReferences)
        {
            if (_propFaceTrackingMeshes.arraySize + additions.Count >= MaxFaceTrackingMeshes)
                break;

            var renderer = dragged as SkinnedMeshRenderer;
            if (renderer == null && dragged is GameObject gameObject)
                renderer = gameObject.GetComponent<SkinnedMeshRenderer>();

            if (renderer != null && existing.Add(renderer))
                additions.Add(renderer);
        }

        DragAndDrop.visualMode = additions.Count > 0 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
        if (evt.type == EventType.DragPerform && additions.Count > 0)
        {
            DragAndDrop.AcceptDrag();
            foreach (var renderer in additions)
            {
                int index = _propFaceTrackingMeshes.arraySize++;
                _propFaceTrackingMeshes.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            }
            GUI.changed = true;
        }
        evt.Use();
    }

    void DrawFaceTrackingDampening(bool allKeys)
    {
        SerializedProperty lipSync = serializedObject.FindProperty("lipSync");
        SerializedProperty mode = serializedObject.FindProperty("visemeDampenMode");
        if (!allKeys)
        {
            EditorGUILayout.HelpBox("Viseme Dampen uses hardware face tracking data. Enable 'Use All Tracking Keys' (at the top of the Face Tracking section) to configure it.", MessageType.Info);
        }
        else if (lipSync.intValue != (int) VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShapeFaceTracking)
        {
            EditorGUILayout.HelpBox("Viseme Dampen requires LipSync Mode 'Viseme BlendShape Face Tracking'. Saved dampening settings have no effect in other lip-sync modes.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.PropertyField(mode, new GUIContent("Viseme Dampen", "None: No dampening. Dampen Tracking: Active visemes dampen mouth-related tracking. Dampen Visemes: Tracking dampens visemes."));
            if (mode.intValue != (int)VRCAvatarDescriptor.VisemeDampenMode.None)
                EditorGUILayout.Slider(serializedObject.FindProperty("visemeDampenAmount"), 0f, 1f, new GUIContent("Dampen Amount", "Amount (0-1) for the selected dampening mode."));
        }
    }

    string NextBlendshapesSetName()
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < _propBlendshapesSetList.arraySize; i++)
        {
            names.Add(_propBlendshapesSetList.GetArrayElementAtIndex(i).FindPropertyRelative("setName").stringValue);
        }
        for (int i = 1; ; i++)
        {
            string name = "unnamed" + i;
            if (!names.Contains(name)) return name;
        }
    }

    void DrawBlendshapesSetCollection()
    {
        SerializedProperty sets = _propBlendshapesSetList;
        if (sets.arraySize == 0)
        {
            sets.arraySize = 1;
            SerializedProperty first = sets.GetArrayElementAtIndex(0);
            first.boxedValue = new VRCAvatarDescriptor.FaceTrackingBlendshapesSet { setName = "unnamed1", setWeight = 1f };
            FaceTrackingEditorContext.InvalidateBindings();
        }
        _faceTrackingSelectedBlendshapesSet = Mathf.Clamp(_faceTrackingSelectedBlendshapesSet, 0, sets.arraySize - 1);
        BeginBox("", hasFoldouts: true);
        
        EditorGUILayout.BeginHorizontal();
        
        // Blendshape Sets Foldout
        bool expanded = Foldout(BlendshapesSetsFoldoutPrefsKey, "Blendshapes Sets", true);
        DrawAlertTarget(AlertSection.BlendshapeSets, GUILayoutUtility.GetLastRect());
        if (!expanded)
        {
            EditorGUILayout.EndHorizontal();
            EndBox();
            return;
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Copy All", GUILayout.Width(70)))
            FaceTrackingDrawerUtility.Copy(sets, false);
        
        if (GUILayout.Button("Copy Selected", GUILayout.Width(110)))
            FaceTrackingDrawerUtility.Copy(sets.GetArrayElementAtIndex(_faceTrackingSelectedBlendshapesSet), false);
        
        using (new EditorGUI.DisabledScope(sets.arraySize >= MaxBlendshapesSets))
        {
            if (GUILayout.Button("Paste", GUILayout.Width(60)))
            {
                int first = FaceTrackingDrawerUtility.Paste(sets, false);
                if (first >= 0)
                    _faceTrackingSelectedBlendshapesSet = first;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_setOptionsCache == null || _setOptionsCache.Length != sets.arraySize || _setOptionsRevision != FaceTrackingEditorContext.Revision)
        {
            _setOptionsCache = new string[sets.arraySize];
            for (int i = 0; i < sets.arraySize; i++)
            {
                string name = sets.GetArrayElementAtIndex(i).FindPropertyRelative("setName").stringValue;
                _setOptionsCache[i] = string.IsNullOrEmpty(name) ? (i + 1).ToString() : (i + 1) + ": " + name;
            }
            _setOptionsRevision = FaceTrackingEditorContext.Revision;
        }
        
        // Individual Blendshape Set Header
        EditorGUILayout.BeginHorizontal();
        GUI.SetNextControlName("BlendshapeSetDropdown");
        _faceTrackingSelectedBlendshapesSet = EditorGUILayout.Popup("Selected Blendshapes Set", _faceTrackingSelectedBlendshapesSet, _setOptionsCache, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
        using (new EditorGUI.DisabledScope(sets.arraySize >= MaxBlendshapesSets))
        {
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                string name = NextBlendshapesSetName();
                Undo.SetCurrentGroupName("Add Blendshapes Set");
                sets.InsertArrayElementAtIndex(_faceTrackingSelectedBlendshapesSet);
                sets.GetArrayElementAtIndex(++_faceTrackingSelectedBlendshapesSet).FindPropertyRelative("setName").stringValue = name;
                FaceTrackingEditorContext.InvalidateBindings();
            }
        }
        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            bool last = sets.arraySize == 1;
            string action = last ? "Clear" : "Remove";
            if (EditorUtility.DisplayDialog(action + " Blendshapes Set", action + " Blendshapes Set " + (_faceTrackingSelectedBlendshapesSet + 1) + "?", action, "Cancel"))
            {
                Undo.SetCurrentGroupName("Remove Blendshapes Set");
                if (last)
                {
                    SerializedProperty set = sets.GetArrayElementAtIndex(0);
                    float weight = set.FindPropertyRelative("setWeight").floatValue;
                    set.boxedValue = new VRCAvatarDescriptor.FaceTrackingBlendshapesSet { setWeight = weight };
                    set.FindPropertyRelative("setName").stringValue = NextBlendshapesSetName();
                }
                else
                {
                    sets.DeleteArrayElementAtIndex(_faceTrackingSelectedBlendshapesSet);
                }
                
                _faceTrackingSelectedBlendshapesSet = Mathf.Clamp(_faceTrackingSelectedBlendshapesSet, 0, sets.arraySize - 1);
                serializedObject.ApplyModifiedProperties();
                FaceTrackingEditorContext.InvalidateBindings();
                GUI.FocusControl("BlendshapeSetDropdown");
            }
        }
        using (new EditorGUI.DisabledScope(_faceTrackingSelectedBlendshapesSet == 0))
        {
            if (GUILayout.Button("Move Up", GUILayout.Width(80)))
            {
                Undo.SetCurrentGroupName("Move Blendshapes Set");
                sets.MoveArrayElement(_faceTrackingSelectedBlendshapesSet, _faceTrackingSelectedBlendshapesSet - 1);
                _faceTrackingSelectedBlendshapesSet--;
                FaceTrackingEditorContext.InvalidateBindings();
            }
        }
        using (new EditorGUI.DisabledScope(_faceTrackingSelectedBlendshapesSet >= sets.arraySize - 1))
        {
            if (GUILayout.Button("Move Down", GUILayout.Width(80)))
            {
                Undo.SetCurrentGroupName("Move Blendshapes Set");
                sets.MoveArrayElement(_faceTrackingSelectedBlendshapesSet, _faceTrackingSelectedBlendshapesSet + 1);
                _faceTrackingSelectedBlendshapesSet++;
                FaceTrackingEditorContext.InvalidateBindings();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // Selected blendshape sets embedded inspector
        SerializedProperty selected = sets.GetArrayElementAtIndex(_faceTrackingSelectedBlendshapesSet);
        _blendshapesSetDrawer.DrawInspectorGUI(selected);
        SerializedProperty setName = selected.FindPropertyRelative("setName");
        if (_blendshapesSetDrawer.NameEdited && string.IsNullOrEmpty(setName.stringValue))
        {
            setName.stringValue = NextBlendshapesSetName();
        }
        
        if (serializedObject.hasModifiedProperties)
        {
            serializedObject.ApplyModifiedProperties();
            FaceTrackingEditorContext.Invalidate();
        }
        EndBox();
    }

    void DrawParameterBehaviorCollection(bool allKeys)
    {
        SerializedProperty behaviors = _propParameterBehaviors;
        BeginBox("", hasFoldouts: true);
        
        // Parameter Behaviors foldout
        EditorGUILayout.BeginHorizontal();
        bool expanded = Foldout(ParameterBehaviorsFoldoutPrefsKey, "Parameter Behaviors", true);
        DrawAlertTarget(AlertSection.ParameterBehaviors, GUILayoutUtility.GetLastRect());
        if (!expanded)
        {
            EditorGUILayout.EndHorizontal();
            EndBox();
            return;
        }
        GUILayout.FlexibleSpace();
        using (new EditorGUI.DisabledScope(behaviors.arraySize == 0))
        {
            if (GUILayout.Button("Delete All", GUILayout.Width(70)) && EditorUtility.DisplayDialog("Delete All Parameter Behaviors", "Warning: This will delete all parameter behaviors on this Avatar. Are you sure you want to continue?", "Delete All", "Cancel"))
            {
                Undo.SetCurrentGroupName("Delete All Parameter Behaviors");
                behaviors.ClearArray();
                FaceTrackingEditorContext.InvalidateBindings();
            }

            if (GUILayout.Button("Copy All", GUILayout.Width(70)))
            {
                FaceTrackingDrawerUtility.Copy(behaviors, true);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if (!allKeys)
            EditorGUILayout.HelpBox("Eye look, blink, Voice, and Viseme_* inputs work without 'Use All Tracking Keys'. Visemes require LipSync Mode 'Viseme BlendShape Face Tracking'. Enable 'Use All Tracking Keys' to use hardware face tracking inputs.", MessageType.Info);
        
        EditorGUILayout.HelpBox("Define behaviors that combine tracking inputs and apply them to animator parameters, blendshape sets, or individual blendshapes. Forwarding to animator parameters does not require a face tracking mesh.", MessageType.Info);
        if (behaviors.arraySize == 0)
        {
            EditorGUILayout.LabelField("No parameter behaviors defined.");
        }
        else
        {
            // Parameter Behaviors list
            GetParameterBehaviorList().DoLayoutList();
        }

        // Paste/Add parameter behavior buttons
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(behaviors.arraySize >= MaxParameterBehaviors))
            {
                if (GUILayout.Button("Paste", GUILayout.Width(60)))
                    FaceTrackingDrawerUtility.Paste(behaviors, true);
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    Undo.SetCurrentGroupName("Add Parameter Behavior");
                    int index = behaviors.arraySize++;
                    SerializedProperty added = behaviors.GetArrayElementAtIndex(index);
                    added.boxedValue = new VRCAvatarDescriptor.FaceTrackingParameterBehavior
                    {
                        outputParameterName = "NewParameter" + behaviors.arraySize,
                        inputTrackingKeys = Array.Empty<int>(), inputTrackingKeyNames = Array.Empty<string>(), inputInverted = Array.Empty<bool>()
                    };
                    added.isExpanded = true;
                    FaceTrackingEditorContext.InvalidateBindings();
                }
            }
        }
        
        if (behaviors.arraySize >= MaxParameterBehaviors)
            EditorGUILayout.HelpBox($"Maximum of {MaxParameterBehaviors} parameter behaviors reached.", MessageType.Info);
        
        EndBox();
    }

    // Simplified reorderable list for parameter behaviors. Skips element management buttons
    ReorderableList GetParameterBehaviorList()
    {
        SerializedProperty behaviors = _propParameterBehaviors;
        if (_parameterBehaviorList == null
            || _parameterBehaviorList.serializedProperty.propertyPath != behaviors.propertyPath
            || !ReferenceEquals(_parameterBehaviorList.serializedProperty.serializedObject, behaviors.serializedObject))
        {
            _parameterBehaviorList = new ReorderableList(behaviors.serializedObject, behaviors, draggable: true, displayHeader: false, displayAddButton: false, displayRemoveButton: false)
            {
                // No header, footer, or outer background: the surrounding box and the Paste/+ buttons below provide them
                headerHeight = 6f,
                footerHeight = 0f,
                showDefaultBackground = false,
                elementHeightCallback = ParameterBehaviorElementHeight,
                drawElementCallback = DrawParameterBehaviorElement,
                // Elements now live at different property paths after a move
                onReorderCallback = list => FaceTrackingEditorContext.InvalidateBindings(),
            };
        }
        return _parameterBehaviorList;
    }

    float ParameterBehaviorElementHeight(int index)
    {
        SerializedProperty behaviors = _propParameterBehaviors;
        if (index < 0 || index >= behaviors.arraySize)
            return EditorGUIUtility.singleLineHeight;
        return _parameterBehaviorDrawer.GetPropertyHeight(behaviors.GetArrayElementAtIndex(index), GUIContent.none);
    }

    void DrawParameterBehaviorElement(Rect rect, int index, bool active, bool focused)
    {
        SerializedProperty behaviors = _propParameterBehaviors;
        // The array can shrink mid-frame from a behavior's delete button; skip stale trailing indices
        if (index < 0 || index >= behaviors.arraySize) return;
        
        _parameterBehaviorDrawer.DrawInspectorGUI(rect, behaviors.GetArrayElementAtIndex(index));
        DrawAlertTarget(AlertSection.ParameterBehaviors, rect, index);
    }
}
#endif

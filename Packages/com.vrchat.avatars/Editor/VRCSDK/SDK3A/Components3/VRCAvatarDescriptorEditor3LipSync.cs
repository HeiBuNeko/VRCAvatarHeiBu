#if VRC_SDK_VRCSDK3
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VRC.SDKBase;
using VRC.SDK3A.Editor;

public partial class AvatarDescriptorEditor3 : Editor
{
    SkinnedMeshRenderer visimeSelectedMesh;
    string[] vismeBlendShapeNames;

    public void DrawLipSync()
    {
        bool expanded = Foldout("VRCSDK3_AvatarDescriptorEditor3_LipSyncFoldout", "LipSync");
        DrawAlertTarget(AlertSection.LipSync, GUILayoutUtility.GetLastRect());
        if (expanded)
        {
            var mesh = serializedObject.FindProperty("VisemeSkinnedMesh");
            var blendShapes = serializedObject.FindProperty("VisemeBlendShapes");
            var mode = serializedObject.FindProperty("lipSync");

            EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));
            switch ((VRC_AvatarDescriptor.LipSyncStyle)mode.intValue)
            {
                case VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.Default:
                    if (GUILayout.Button("Auto Detect!"))
                        AutoDetectLipSync();
                    break;

                case VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape:
                    EditorGUILayout.PropertyField(mesh, new GUIContent("Face Mesh"));
                    if (mesh.objectReferenceValue != null)
                    {
                        DetermineVisimeBlendShapeNames(mesh.objectReferenceValue as SkinnedMeshRenderer);
                        AvatarEditorHelper.DrawBlendshapesPopupButton(serializedObject.FindProperty("MouthOpenBlendShapeName"), vismeBlendShapeNames, new GUIContent("Jaw Flap Blend Shape"));
                    }
                    break;

                case VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.JawFlapBone:
                    DrawJawBone();
                    break;

                case VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape:
                    bool shouldRefreshVisemes = false;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(mesh, new GUIContent("Face Mesh"));
                    if (EditorGUI.EndChangeCheck())
                        shouldRefreshVisemes = true;

                    if (mesh.objectReferenceValue != null)
                    {
                        DetermineVisimeBlendShapeNames(mesh.objectReferenceValue as SkinnedMeshRenderer);

                        if (blendShapes.arraySize != (int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.Count)
                            blendShapes.arraySize = (int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.Count;
                        for (int i = 0; i < (int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.Count; ++i)
                        {
                            string title = "Viseme: " + ((VRC.SDKBase.VRC_AvatarDescriptor.Viseme)i).ToString();
                            AvatarEditorHelper.DrawBlendshapesPopupButton(blendShapes.GetArrayElementAtIndex(i), vismeBlendShapeNames, new GUIContent(title));
                        }

                        if (shouldRefreshVisemes)
                            AutoDetectVisemes();
                    }
                    break;

                case VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeParameterOnly:
                    break;

                case VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShapeFaceTracking:
                    EditorGUILayout.HelpBox(
                        "Viseme blendshapes are driven by audio analysis but mapped through the Face Tracking blendshape sets system. Configure meshes and viseme mappings per set in the Face Tracking section.",
                        MessageType.Info);
                    break;
            }
            Separator();
        }
    }
    public void DrawSceneLipSync()
    {
        var transformProp = serializedObject.FindProperty("lipSyncJawBone");
        DrawRotationState(serializedObject.FindProperty("lipSyncJawClosed"));
        DrawRotationState(serializedObject.FindProperty("lipSyncJawOpen"));

        void DrawRotationState(SerializedProperty property)
        {
            //Check if active
            if (!IsActiveProperty(property))
                return;

            //Draw handle
            DrawRotationHandles(transformProp, property);
        }
    }

    void DrawJawBone()
    {
        var lipSyncJawClosed = serializedObject.FindProperty("lipSyncJawClosed");
        var lipSyncJawOpen = serializedObject.FindProperty("lipSyncJawOpen");
        var lipSyncJawBone = serializedObject.FindProperty("lipSyncJawBone");

        //Transform
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(lipSyncJawBone, new GUIContent("Jaw Bone"));
        if(EditorGUI.EndChangeCheck())
        {
            if(lipSyncJawBone.objectReferenceValue != null)
            {
                var transform = lipSyncJawBone.objectReferenceValue as Transform;
                lipSyncJawClosed.quaternionValue = transform.localRotation;
                lipSyncJawOpen.quaternionValue = transform.localRotation;
            }
        }

        //Rotation states
        EditorGUILayout.LabelField("Rotation States");
        EditorGUI.indentLevel += 1;
        GUI.enabled = lipSyncJawBone.objectReferenceValue != null;
        {
            DrawRotationState("Closed", lipSyncJawClosed);
            DrawRotationState("Open", lipSyncJawOpen);
        }
        GUI.enabled = true;
        EditorGUI.indentLevel -= 1;

        void DrawRotationState(string name, SerializedProperty property)
        {
            GUILayout.BeginHorizontal();
            {
                //Vector
                EditorGUI.BeginChangeCheck();
                var result = EditorGUILayout.Vector3Field(name, EditorQuaternionToVector3(property.quaternionValue));
                if (EditorGUI.EndChangeCheck())
                {
                    property.quaternionValue = Quaternion.Euler(result);
                    SetActive();
                }

                //Edit Button
                bool isActiveProperty = IsActiveProperty(property);
                GUI.backgroundColor = isActiveProperty ? _activeButtonColor : Color.white;
                if (GUILayout.Button(isActiveProperty ? "Return" : "Preview", EditorStyles.miniButton, GUILayout.MaxWidth(PreviewButtonWidth), GUILayout.Height(PreviewButtonHeight)))
                {
                    if (isActiveProperty)
                        SetActiveProperty(null);
                    else
                        SetActive();
                }
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndHorizontal();

            void SetActive()
            {
                //Set active
                SetActiveProperty(property);

                //Record restore point
                var prevBone = lipSyncJawBone.objectReferenceValue as Transform;
                var prevRotation = prevBone.transform.localRotation;
                System.Action restore = () =>
                {
                    if (prevBone != null)
                        prevBone.localRotation = prevRotation;
                };
                activePropertyRestore.Add(restore);

                //Set
                prevBone.localRotation = property.quaternionValue;
            }
        }
    }

    void DetermineVisimeBlendShapeNames(SkinnedMeshRenderer mesh)
    {
        if (mesh != null && mesh != visimeSelectedMesh)
        {
            visimeSelectedMesh = mesh;
            vismeBlendShapeNames = AvatarEditorHelper.GetBlendshapes(mesh);
        }
    }

    void AutoDetectVisemes()
    {
        //Guarentee array size
        var blendshapes = serializedObject.FindProperty("VisemeBlendShapes");
        if(blendshapes.arraySize != (int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.Count)
            blendshapes.arraySize = (int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.Count;

        // prioritize strict - but fallback to looser - naming and don't touch user-overrides

        string[] blendShapes = vismeBlendShapeNames;

        for (int v = 0; v < blendshapes.arraySize; v++)
        {
            var element = blendshapes.GetArrayElementAtIndex(v);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                string viseme = ((VRC.SDKBase.VRC_AvatarDescriptor.Viseme)v).ToString().ToLowerInvariant();

                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant() == "vrc.v_" + viseme)
                    {
                        element.stringValue = s;
                        goto next;
                    }
                }
                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant() == "v_" + viseme)
                    {
                        element.stringValue = s;
                        goto next;
                    }
                }
                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant().EndsWith(viseme))
                    {
                        element.stringValue = s;
                        goto next;
                    }
                }
                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant() == viseme)
                    {
                        element.stringValue = s;
                        goto next;
                    }
                }
                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant().Contains(viseme))
                    {
                        element.stringValue = s;
                        goto next;
                    }
                }
                next: { }
            }
            
        }
    }
    
    class SearchComparer : IComparer<string>
    {
        string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        
        public SearchComparer(string searchString)
        {
            _searchString = searchString;
        }
        private readonly string _searchString;
        public int Compare(string x, string y)
        {
            if (x == null || y == null)
            {
                return 0;
            }
            //-1 is they're out of order, 0 is order doesn't matter, 1 is they're in order

            x = ReplaceFirst(x, "const ", "");
            y = ReplaceFirst(y, "const ", "");
            
            int xIndex = x.IndexOf(_searchString, StringComparison.InvariantCultureIgnoreCase);
            int yIndex = y.IndexOf(_searchString, StringComparison.InvariantCultureIgnoreCase);
            int compareIndex = xIndex.CompareTo(yIndex);
            if (compareIndex != 0) return compareIndex;
            
            string xDiff = ReplaceFirst(x, _searchString, "");
            string yDiff = ReplaceFirst(y, _searchString, "");
            return string.Compare(xDiff, yDiff, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    private void AutoDetectLipSync()
    {
        SkinnedMeshRenderer[] renderers = avatarDescriptor.GetComponentsInChildren<SkinnedMeshRenderer>();

        var mode = serializedObject.FindProperty("lipSync");
        var blendshapes = serializedObject.FindProperty("VisemeBlendShapes");
        var mesh = serializedObject.FindProperty("VisemeSkinnedMesh");
        var jawBone = serializedObject.FindProperty("lipSyncJawBone");

        string[] baseVisemeNames = Enum.GetNames(typeof(VRC_AvatarDescriptor.Viseme));
        int visemeCount = baseVisemeNames.Length - 1;
        string[] reversedVisemeNames = new string[visemeCount];
        string[] reversedVVisemeNames = new string[visemeCount];
        for (int i = 0; i < visemeCount; i++)
        {
            string visemeName = baseVisemeNames[i];
            char[] tmpArray = visemeName.ToLowerInvariant().ToCharArray();
            Array.Reverse(tmpArray);
            reversedVisemeNames[i] = new string(tmpArray);
            reversedVVisemeNames[i] = $"{reversedVisemeNames[i]}_v";
        }

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer.sharedMesh.blendShapeCount <= 0)
                continue;

            if (renderer.sharedMesh.blendShapeCount >= visemeCount)
            {
                string[] rendererBlendShapeNames = new string[renderer.sharedMesh.blendShapeCount];
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    rendererBlendShapeNames[i] = renderer.sharedMesh.GetBlendShapeName(i);
                }
                
                string[] visemeStrings = new string[visemeCount];
                int foundVisemes = 0;

                string[] reversedRendererNames = new string[rendererBlendShapeNames.Length];
                Dictionary<string, string> reverseMap = new Dictionary<string, string>();
                
                for (int i = 0; i < rendererBlendShapeNames.Length; i++)
                {
                    string rendererBlendShapeName = rendererBlendShapeNames[i];
                    char[] tmpArray = rendererBlendShapeName.ToLowerInvariant().ToCharArray();
                    Array.Reverse(tmpArray);
                    reversedRendererNames[i] = new string(tmpArray);
                    if (reverseMap.ContainsKey(reversedRendererNames[i]))
                    {
                        continue;
                    }
                    reverseMap.Add(reversedRendererNames[i], rendererBlendShapeName);
                }

                for (int i = 0; i < reversedVisemeNames.Length; i++)
                {
                    string visemeName = reversedVisemeNames[i];
                    string vVisemeName = reversedVVisemeNames[i];
                    
                    List<string> matchingStrings = new List<string>();
                    foreach (string reversedRendererName in reversedRendererNames)
                    {
                        if (reversedRendererName.Contains(vVisemeName))
                        {
                            matchingStrings.Add(reversedRendererName);
                        }
                    }
                    if (matchingStrings.Count == 0)
                    {
                        foreach (string reversedRendererName in reversedRendererNames)
                        {
                            if (reversedRendererName.Contains(visemeName))
                            {
                                matchingStrings.Add(reversedRendererName);
                            }
                        }
                    }

                    matchingStrings.Sort(new SearchComparer(visemeName));
        
                    if (matchingStrings.Count <= 0) continue;
                    
                    visemeStrings[i] = reverseMap[matchingStrings[0]];
                    foundVisemes++;
                }

                //Threshold to see if we did a good enough job to bother showing the user
                if (foundVisemes > 2)
                {
                    mode.intValue = (int)VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
                    mesh.objectReferenceValue = renderer;
                    jawBone.objectReferenceValue = null;
                    blendshapes.arraySize = visemeStrings.Length;
                    for (int i = 0; i < visemeStrings.Length; i++)
                        blendshapes.GetArrayElementAtIndex(i).stringValue = visemeStrings[i];
                    return;
                }
            }

            //Jaw Flap Blendshape
            mode.intValue = (int)VRC_AvatarDescriptor.LipSyncStyle.JawFlapBlendShape;
            mesh.objectReferenceValue = renderer;
            jawBone.objectReferenceValue = null;
            blendshapes.arraySize = 0;
            return;
        }

        //Jaw Flap Bone
        var jawBoneTransform = avatarDescriptor.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Jaw);
        if (jawBoneTransform != null)
        {
            mode.intValue = (int)VRC_AvatarDescriptor.LipSyncStyle.JawFlapBone;
            jawBone.objectReferenceValue = jawBoneTransform;
            mesh.objectReferenceValue = null;
            blendshapes.arraySize = 0;
        }
    }
}
#endif

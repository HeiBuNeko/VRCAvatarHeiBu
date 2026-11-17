#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3A.Editor;

/// <summary>
/// Validates face tracking blendshape names across the configured meshes.
/// Follows runtime mapping rules so SDK warnings reflect in-game behavior.
/// </summary>
public static class FaceTrackingMappingValidator
{
    // Meshes beyond this limit are ignored at runtime.
    const int MaxFaceTrackingMeshes = 16;
    internal const int MaxParameterBehaviorInputs = 32;
    public const string DuplicateMeshPathWarning = "Two meshes with the same path cannot both be used for Face Tracking. One will be ignored. Rename one of the meshes so their paths are different.";

    internal static string GetParameterBehaviorInputWarning(bool custom, string expression, int inputCount)
    {
        if (custom)
        {
            return expression != null && expression.Length > FaceTrackingExpressionValidator.MaxExpressionLength
                ? $"Expression exceeds the runtime limit of {FaceTrackingExpressionValidator.MaxExpressionLength} characters and will be ignored." : null;
        }

        if (inputCount == 0)
        {
            return "Add at least one input tracking key - this behavior has no effect without inputs.";
        }
        
        return inputCount > MaxParameterBehaviorInputs
            ? $"Only the first {MaxParameterBehaviorInputs} inputs are processed. Remove extra inputs to match runtime behavior." : null;
    }

    internal static string GetParameterBehaviorTargetWarning(VRCAvatarDescriptor.ParameterBehaviorTargetMode mode,
        string setName, string blendshapeName,
        bool hasSourceAvatar, FaceTrackingEditorContext.MeshCatalog catalog)
    {
        if (mode == VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyBlendSetWeight && hasSourceAvatar &&
            !catalog.HasSet(setName))
        {
            return $"Blend set '{setName}' does not exist in the blendshapes set list - this behavior will be ignored.";
        }

        if (mode != VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyToBlendshape
            && mode != VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMax
            && mode != VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMin)
        {
            return null;
        }

        if (string.IsNullOrEmpty(blendshapeName))
        {
            return "Select a blendshape for this behavior's target - it has no effect without one.";
        }

        if (catalog.Meshes.Count > 0 && !catalog.HasBlendshape(blendshapeName))
        {
            return $"Blendshape '{blendshapeName}' was not found on any current face tracking mesh. It will still apply at runtime if the blendshape is added (e.g. by an external build-time tool).";
        }
        return null;
    }

    internal static void CollectParameterBehaviorWarnings(VRCAvatarDescriptor descriptor, Action<string, int> onWarning)
    {
        if (descriptor == null || !descriptor.enableFaceTracking || descriptor.faceTrackingParameterBehaviors == null)
            return;
        
        FaceTrackingEditorContext.MeshCatalog catalog = new FaceTrackingEditorContext.MeshCatalog();
        catalog.Refresh(descriptor.faceTrackingMeshes, descriptor);
        for (int i = 0; i < descriptor.faceTrackingParameterBehaviors.Count; i++)
        {
            VRCAvatarDescriptor.FaceTrackingParameterBehavior behavior = descriptor.faceTrackingParameterBehaviors[i];
            if (behavior == null) continue;
            
            string targetWarning = GetParameterBehaviorTargetWarning(behavior.targetMode, behavior.targetBlendSetName,
                behavior.targetBlendshapeName, true, catalog);
            string inputWarning = GetParameterBehaviorInputWarning(
                behavior.combineMode == VRCAvatarDescriptor.ParameterBehaviorCombineMode.Custom,
                behavior.customExpression, behavior.inputTrackingKeys?.Length ?? 0);
            if (targetWarning != null)
            {
                onWarning($"Face Tracking parameter behavior #{i + 1}: {targetWarning}", i);
            }

            if (inputWarning != null)
            {
                onWarning($"Face Tracking parameter behavior #{i + 1}: {inputWarning}", i);
            }
        }
    }

    public class MappingIssues
    {
        // Mappings exist but no valid meshes are assigned, so nothing resolves
        public bool HasMappingsButNoMeshes;
        // Human-readable descriptions of name-based blendshape mappings that will not resolve
        public readonly List<string> UnresolvedBlendshapes = new List<string>();
        public readonly List<string> AmbiguousMeshPaths = new List<string>();
        internal int FirstUnresolvedSetIndex = -1;
        internal int FirstUnresolvedBehaviorIndex = -1;
    }

    static List<SkinnedMeshRenderer> GetValidMeshes(VRCAvatarDescriptor descriptor)
    {
        List<SkinnedMeshRenderer> validMeshes = new List<SkinnedMeshRenderer>();
        if (descriptor.faceTrackingMeshes == null)
            return validMeshes;
        
        int meshCount = System.Math.Min(descriptor.faceTrackingMeshes.Count, MaxFaceTrackingMeshes);
        for (int i = 0; i < meshCount; i++)
        {
            SkinnedMeshRenderer mesh = descriptor.faceTrackingMeshes[i];
            if (mesh != null && mesh.sharedMesh != null)
                validMeshes.Add(mesh);
        }
        return validMeshes;
    }

    static HashSet<string> GetBlendshapeNames(List<SkinnedMeshRenderer> meshes)
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        foreach (SkinnedMeshRenderer mesh in meshes)
        {
            for (int i = 0; i < mesh.sharedMesh.blendShapeCount; i++)
            {
                names.Add(mesh.sharedMesh.GetBlendShapeName(i));
            }
        }
        return names;
    }

    static bool HasAnyBlendshapeMapping(VRCAvatarDescriptor descriptor)
    {
        if (descriptor.faceTrackingBlendshapesSetList != null)
        {
            foreach (VRCAvatarDescriptor.FaceTrackingBlendshapesSet set in descriptor.faceTrackingBlendshapesSetList)
            {
                if (set == null)
                    continue;
                
                if (HasAnyEntry(set.faceTrackingBlendshapeNames) || HasAnyEntry(set.visemeBlendshapeNames))
                    return true;
            }
        }
        if (descriptor.faceTrackingParameterBehaviors != null)
        {
            foreach (VRCAvatarDescriptor.FaceTrackingParameterBehavior behavior in descriptor.faceTrackingParameterBehaviors)
            {
                if (behavior == null || string.IsNullOrEmpty(behavior.targetBlendshapeName))
                    continue;
                
                VRCAvatarDescriptor.ParameterBehaviorTargetMode targetMode = behavior.targetMode;
                if (targetMode == VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyToBlendshape ||
                    targetMode == VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMax ||
                    targetMode == VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMin)
                    return true;
            }
        }
        return false;
    }

    static bool HasAnyEntry(string[] names)
    {
        if (names == null)
            return false;
        
        foreach (string name in names)
        {
            if (!string.IsNullOrEmpty(name))
                return true;
        }
        return false;
    }

    internal static List<string> CollectAmbiguousMeshPaths(VRCAvatarDescriptor descriptor)
    {
        var paths = new List<string>();
        if (descriptor == null)
            return paths;

        CollectAmbiguousMeshPaths(descriptor, GetValidMeshes(descriptor), paths);
        return paths;
    }

    static void CollectAmbiguousMeshPaths(VRCAvatarDescriptor descriptor, List<SkinnedMeshRenderer> meshes, List<string> paths)
    {
        if (meshes.Count == 0) return;

        Dictionary<string, Transform> transformsByAnimationPath = new Dictionary<string, Transform>(StringComparer.Ordinal);
        foreach (Transform transform in descriptor.GetComponentsInChildren<Transform>(true))
        {
            string path = UnityEditor.AnimationUtility.CalculateTransformPath(transform, descriptor.transform);
            if (!transformsByAnimationPath.ContainsKey(path))
                transformsByAnimationPath.Add(path, transform);
        }
        foreach (SkinnedMeshRenderer mesh in meshes)
        {
            string path = UnityEditor.AnimationUtility.CalculateTransformPath(mesh.transform, descriptor.transform);
            if (!transformsByAnimationPath.TryGetValue(path, out Transform firstTransform) || firstTransform != mesh.transform)
                paths.Add($"'{path}' ({mesh.name})");
        }
    }

    public static MappingIssues CollectIssues(VRCAvatarDescriptor descriptor)
    {
        MappingIssues issues = new MappingIssues();
        if (descriptor == null || !descriptor.enableFaceTracking)
            return issues;

        List<SkinnedMeshRenderer> meshes = GetValidMeshes(descriptor);
        CollectAmbiguousMeshPaths(descriptor, meshes, issues.AmbiguousMeshPaths);
        if (meshes.Count == 0)
        {
            issues.HasMappingsButNoMeshes = HasAnyBlendshapeMapping(descriptor);
            return issues;
        }

        HashSet<string> blendshapeNames = GetBlendshapeNames(meshes);

        void CheckSetMapping(string blendshapeName, string where)
        {
            if (!string.IsNullOrEmpty(blendshapeName) && !blendshapeNames.Contains(blendshapeName))
                issues.UnresolvedBlendshapes.Add($"'{blendshapeName}' (all meshes, {where})");
        }

        if (descriptor.faceTrackingBlendshapesSetList != null)
        {
            for (int s = 0; s < descriptor.faceTrackingBlendshapesSetList.Count; s++)
            {
                VRCAvatarDescriptor.FaceTrackingBlendshapesSet set = descriptor.faceTrackingBlendshapesSetList[s];
                if (set == null) continue;
                
                string setLabel = string.IsNullOrEmpty(set.setName) ? $"set #{s + 1}" : $"set '{set.setName}'";
                int unresolvedBeforeSet = issues.UnresolvedBlendshapes.Count;

                if (set.faceTrackingBlendshapeNames != null)
                {
                    for (int i = 0; i < set.faceTrackingBlendshapeNames.Length; i++)
                    {
                        string keyLabel = i < (int)VRCAvatarDescriptor.FaceTrackingKey.Max
                            ? $"key {((VRCAvatarDescriptor.FaceTrackingKey)i).ToString()}"
                            : $"key #{i}";
                        CheckSetMapping(set.faceTrackingBlendshapeNames[i], $"{setLabel}, {keyLabel}");
                    }
                }

                if (set.visemeBlendshapeNames != null)
                {
                    for (int i = 0; i < set.visemeBlendshapeNames.Length; i++)
                    {
                        string keyLabel = i < (int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.Count
                            ? $"viseme {((VRC.SDKBase.VRC_AvatarDescriptor.Viseme)i).ToString()}"
                            : $"viseme #{i}";
                        CheckSetMapping(set.visemeBlendshapeNames[i], $"{setLabel}, {keyLabel}");
                    }
                }
                if (issues.FirstUnresolvedSetIndex < 0 && issues.UnresolvedBlendshapes.Count > unresolvedBeforeSet)
                    issues.FirstUnresolvedSetIndex = s;
            }
        }

        if (descriptor.faceTrackingParameterBehaviors != null)
        {
            for (int b = 0; b < descriptor.faceTrackingParameterBehaviors.Count; b++)
            {
                VRCAvatarDescriptor.FaceTrackingParameterBehavior behavior = descriptor.faceTrackingParameterBehaviors[b];
                if (behavior == null) continue;
                
                VRCAvatarDescriptor.ParameterBehaviorTargetMode targetMode = behavior.targetMode;
                if (targetMode != VRCAvatarDescriptor.ParameterBehaviorTargetMode.ApplyToBlendshape &&
                    targetMode != VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMax &&
                    targetMode != VRCAvatarDescriptor.ParameterBehaviorTargetMode.LimitBlendshapeMin)
                {
                    continue;
                }

                string where = $"parameter behavior #{b + 1}";
                if (!string.IsNullOrEmpty(behavior.targetBlendshapeName) &&
                    !blendshapeNames.Contains(behavior.targetBlendshapeName))
                {
                    issues.UnresolvedBlendshapes.Add($"'{behavior.targetBlendshapeName}' (all meshes, {where} - the behavior will be ignored)");
                    if (issues.FirstUnresolvedBehaviorIndex < 0)
                        issues.FirstUnresolvedBehaviorIndex = b;
                }
            }
        }

        return issues;
    }
}
#endif

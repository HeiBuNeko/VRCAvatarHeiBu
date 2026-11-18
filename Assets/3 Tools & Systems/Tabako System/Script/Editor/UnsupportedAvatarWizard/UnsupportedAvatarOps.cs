using System;
using System.Collections.Generic;
using net.satania_shopping.tabakosystem.ndmf;
using net.satania_shopping.tabakosystem.runtime;
using net.satania_shopping.tabakosystem.utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;
using static net.satania_shopping.tabakosystem.SataniaTabakoRequireMotion;

namespace net.satania_shopping.tabakosystem.editor
{
    internal enum GestureKind
    {
        Case,
        Tabako,
        Lighter,
    }

    internal enum PrefabSaveMode
    {
        NotPrefabInstance,
        NotOutermostRoot,
        MissingAsset,
        NewVariant,
        UpdateVendorVariant,
        UpdateUserVariant,
    }

    internal static class UnsupportedAvatarOps
    {
        public const string VendorPrefabFolderGuid = "bfb7a0af85122c744bf851766b4577a0";

        public static bool IsConstraintEditModeEnabled => EditorPrefs.GetBool("vrcConstraintsInEditMode", true);

        public static TabakoScriptBehaviour[] FindScripts(Transform avatarRoot)
        {
            return avatarRoot == null ? new TabakoScriptBehaviour[0] : avatarRoot.GetComponentsInChildren<TabakoScriptBehaviour>(true);
        }

        public static TabakoScriptBehaviour AddTemplate(Transform avatarRoot, bool registerUndo = true)
        {
            GameObject template = UnsupportedAvatarUtils.TemplatePrefab;
            if (template == null || avatarRoot == null)
                return null;

            var go = PrefabUtility.InstantiatePrefab(template, avatarRoot) as GameObject;
            if (go == null)
                return null;

            if (registerUndo)
                Undo.RegisterCreatedObjectUndo(go, "Add Satabako template");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            return go.GetComponent<TabakoScriptBehaviour>();
        }

        public static AnimationClip ResolveGestureClip(Transform avatarRoot, GestureKind kind, out bool overridden, out Motion unsupportedOverride)
        {
            overridden = false;
            unsupportedOverride = null;

            OverrideMotion target;
            string guid;
            switch (kind)
            {
                case GestureKind.Case: target = OverrideMotion.Gesture_Case; guid = PluginDefinition.k_case_guid; break;
                case GestureKind.Tabako: target = OverrideMotion.Gesture_Tabacco; guid = PluginDefinition.k_tabacco_guid; break;
                default: target = OverrideMotion.Gesture_Lighter; guid = PluginDefinition.k_lighter_guid; break;
            }

            if (avatarRoot != null)
            {
                foreach (var overrideGesture in avatarRoot.GetComponentsInChildren<IOverrideHandGesture>(true))
                {
                    if (overrideGesture.OverrideMotion != target || overrideGesture.CustomMotion == null)
                        continue;

                    if (overrideGesture.CustomMotion is AnimationClip clip)
                    {
                        overridden = true;
                        return clip;
                    }

                    unsupportedOverride = overrideGesture.CustomMotion;
                }
            }

            return AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static OverrideMotion ToOverrideMotion(GestureKind kind)
        {
            switch (kind)
            {
                case GestureKind.Case: return OverrideMotion.Gesture_Case;
                case GestureKind.Tabako: return OverrideMotion.Gesture_Tabacco;
                default: return OverrideMotion.Gesture_Lighter;
            }
        }

        public static AnimationClip DefaultGestureClip(GestureKind kind)
        {
            string guid = kind == GestureKind.Case ? PluginDefinition.k_case_guid : kind == GestureKind.Tabako ? PluginDefinition.k_tabacco_guid : PluginDefinition.k_lighter_guid;
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static IOverrideHandGesture FindGestureOverride(Transform templateRoot, GestureKind kind)
        {
            if (templateRoot == null)
                return null;

            var target = ToOverrideMotion(kind);
            foreach (var o in templateRoot.GetComponentsInChildren<IOverrideHandGesture>(true))
            {
                if (o.OverrideMotion == target)
                    return o;
            }
            return null;
        }

        public static bool HasGestureOverrideOutside(Transform avatarRoot, Transform templateRoot, GestureKind kind)
        {
            if (avatarRoot == null)
                return false;

            var target = ToOverrideMotion(kind);
            foreach (var o in avatarRoot.GetComponentsInChildren<IOverrideHandGesture>(true))
            {
                if (o.OverrideMotion == target && o.CustomMotion != null && (templateRoot == null || !o.transform.IsChildOf(templateRoot)))
                    return true;
            }
            return false;
        }

        public static GameObject GestureHost(TabakoTemplateRefs refs, GestureKind kind)
        {
            string label = kind == GestureKind.Case ? "ケース" : kind == GestureKind.Tabako ? "タバコ" : "ライター";
            GameObject host = null;
            foreach (var t in refs.Root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == label + "ジェスチャー")
                {
                    host = t.gameObject;
                    break;
                }
            }

            if (host == null)
            {
                GameObject mesh = kind == GestureKind.Case ? refs.CaseMesh : kind == GestureKind.Tabako ? refs.TabakoMesh : refs.LighterMesh;
                host = mesh != null ? PrefabUtility.GetNearestPrefabInstanceRoot(mesh) : null;
                if (host != null && (host == refs.Root.gameObject || !host.transform.IsChildOf(refs.Root)))
                    host = null;
            }

            if (host != null)
            {
                var existing = host.GetComponent<IOverrideHandGesture>();
                if (existing == null || existing.OverrideMotion == ToOverrideMotion(kind))
                    return host;
            }

            string name = $"ジェスチャー ({kind})";
            var child = refs.Root.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Add gesture override");
                go.transform.SetParent(refs.Root, false);
                child = go.transform;
            }
            return child.gameObject;
        }

        public static IOverrideHandGesture SetGestureOverride(TabakoTemplateRefs refs, GestureKind kind, Motion motion)
        {
            if (refs == null || refs.Root == null)
                return null;

            var existing = FindGestureOverride(refs.Root, kind);
            if (motion == null)
            {
                if (existing != null)
                {
                    Undo.DestroyObjectImmediate(existing);
                    RefreshScene();
                }
                return null;
            }

            var component = existing != null ? existing : Undo.AddComponent<IOverrideHandGesture>(GestureHost(refs, kind));
            Undo.RecordObject(component, "Set gesture override");
            component.OverrideMotion = ToOverrideMotion(kind);
            component.CustomMotion = motion;
            EditorUtility.SetDirty(component);
            return component;
        }

        #region Contact
        public static void CopyContactShape(ContactBase from, ContactBase to)
        {
            if (from == null || to == null || from == to)
                return;

            SetContactShape(to, from.position, from.rotation, from.radius, from.height, from.shapeType);
        }

        public static void SetContactShape(ContactBase contact, Vector3 position, Quaternion rotation, float radius, float height, ContactBase.ShapeType? shapeType = null)
        {
            if (contact == null)
                return;

            Undo.RecordObject(contact, "Set contact shape");
            contact.position = position;
            contact.rotation = rotation;
            contact.radius = radius;
            contact.height = height;
            if (shapeType.HasValue)
                contact.shapeType = shapeType.Value;
            EditorUtility.SetDirty(contact);
        }
        #endregion

        #region Constraint
        public static float[] GetSourceWeights(VRCParentConstraint constraint)
        {
            if (constraint == null)
                return new float[0];

            var sources = constraint.Sources;
            var weights = new float[sources.Count];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = sources[i].Weight;
            return weights;
        }

        public static void SetSourceWeights(VRCParentConstraint constraint, IList<float> weights)
        {
            if (constraint == null || weights == null)
                return;

            var sources = constraint.Sources;
            for (int i = 0; i < sources.Count && i < weights.Count; i++)
            {
                var source = sources[i];
                source.Weight = weights[i];
                sources[i] = source;
            }

            constraint.Sources = sources;
            constraint.ApplyConfigurationChanges();
            EditorUtility.SetDirty(constraint);
        }

        public static bool HasSourcesOverride(VRCParentConstraint constraint)
        {
            if (constraint == null || !PrefabUtility.IsPartOfPrefabInstance(constraint))
                return false;

            var property = new SerializedObject(constraint).FindProperty("Sources");
            return property != null && property.prefabOverride;
        }

        public static void DriveParentConstraint(VRCParentConstraint constraint)
        {
            if (constraint == null)
                return;

            var sources = constraint.Sources;
            float total = 0f;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].SourceTransform != null)
                    total += sources[i].Weight;
            }
            if (Mathf.Abs(total) <= 1e-6f)
                return;

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            bool first = true;
            for (int i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source.SourceTransform == null || Mathf.Abs(source.Weight) <= 1e-6f)
                    continue;
                float w = source.Weight / total;
                position += source.SourceTransform.TransformPoint(source.ParentPositionOffset) * w;
                Quaternion r = source.SourceTransform.rotation * Quaternion.Euler(source.ParentRotationOffset);
                rotation = first ? r : Quaternion.Slerp(rotation, r, w);
                first = false;
            }

            Transform target = constraint.TargetTransform != null ? constraint.TargetTransform : constraint.transform;
            target.SetPositionAndRotation(position, rotation);
        }

        public static void DriveFollower(IFollowSatabakoObject follower)
        {
            if (follower == null)
                return;
            Transform target = follower.target;
            if (target == null)
                return;
            follower.transform.SetPositionAndRotation(target.position, target.rotation);
            if (follower.AutoFixLocalScale)
                IFollowSatabakoObject.MatchScale(follower.transform, target);
        }

        public static void RevertSourcesOverride(VRCParentConstraint constraint)
        {
            if (constraint == null || !PrefabUtility.IsPartOfPrefabInstance(constraint))
                return;

            var so = new SerializedObject(constraint);
            var property = so.FindProperty("Sources");
            if (property == null || !property.prefabOverride)
                return;

            PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
            constraint.ApplyConfigurationChanges();
        }
        #endregion

        #region Placement
        public static Action<Transform, string> RecordTransform = DefaultRecordTransform;
        public static void DefaultRecordTransform(Transform target, string undoName) => Undo.RecordObject(target, undoName);
        public static Action<Transform> TransformMoved;

        public static void CommitTransform(Transform target)
        {
            if (target == null)
                return;
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        public static void ApplyPose(Transform target, PlacementEstimator.Pose pose, string undoName)
        {
            if (target == null)
                return;

            RecordTransform(target, undoName);
            target.SetPositionAndRotation(pose.position, pose.rotation);
            CommitTransform(target);
            TransformMoved?.Invoke(target);
            RefreshScene();
        }

        public static void ApplyPosition(Transform target, Vector3 position, string undoName)
        {
            if (target == null)
                return;

            RecordTransform(target, undoName);
            target.position = position;
            CommitTransform(target);
            TransformMoved?.Invoke(target);
            RefreshScene();
        }

        public static void MirrorRightToLeft(TabakoTemplateRefs refs)
        {
            if (refs == null)
                return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            MirrorPair(refs.CaseR, refs.CaseL, refs.Root);
            MirrorPair(refs.TabakoR, refs.TabakoL, refs.Root);
            MirrorPair(refs.LighterR, refs.LighterL, refs.Root);

            CopyContactShape(refs.SenderR, refs.SenderL);
            CopyContactShape(refs.CaseReceiverR, refs.CaseReceiverL);

            if (refs.PocketR != null && refs.PocketL != null)
            {
                Transform left = refs.PocketL.transform;
                RecordTransform(left, "Mirror pocket");
                Vector3 local = refs.Root.InverseTransformPoint(refs.PocketR.transform.position);
                local.x = -local.x;
                left.position = refs.Root.TransformPoint(local);
                CommitTransform(left);
                TransformMoved?.Invoke(left);
                CopyContactShape(refs.PocketR, refs.PocketL);
            }

            Undo.SetCurrentGroupName("Mirror to left");
            Undo.CollapseUndoOperations(group);
            RefreshScene();
        }

        public static void MirrorToLeft(Transform right, Transform left, Transform root)
        {
            MirrorPair(right, left, root);
            RefreshScene();
        }

        private static void MirrorPair(Transform right, Transform left, Transform root)
        {
            if (right == null || left == null || root == null)
                return;

            RecordTransform(left, "Mirror to left");
            left.SetPositionAndRotation(right.position, right.rotation);
            TransformUtility.MirrorX(left, root);
            CommitTransform(left);
            TransformMoved?.Invoke(left);
        }

        public static void SetCaseScale(TabakoTemplateRefs refs, float scale)
        {
            if (refs == null)
                return;

            SetUniformScale(refs.CT_Case, scale);
            SetUniformScale(refs.CT_Tabako, scale);
            RefreshScene();
        }

        public static void SetLighterScale(TabakoTemplateRefs refs, float scale)
        {
            if (refs == null)
                return;

            SetUniformScale(refs.CT_Lighter, scale);
            RefreshScene();
        }

        private static void SetUniformScale(Transform target, float scale)
        {
            if (target == null)
                return;

            Undo.RecordObject(target, "Set scale");
            target.localScale = Vector3.one * scale;
            EditorUtility.SetDirty(target);
        }
        #endregion

        #region Scene
        public static void SetRenderersVisible(GameObject go, bool visible)
        {
            if (go == null)
                return;

            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                renderer.forceRenderingOff = !visible;
        }

        public static void ClearSelectionInScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            var keep = new List<UnityEngine.Object>();
            bool changed = false;
            foreach (var o in Selection.objects)
            {
                var go = o as GameObject;
                if (go == null && o is Component c)
                    go = c.gameObject;
                if (go != null && go.scene == scene)
                    changed = true;
                else
                    keep.Add(o);
            }
            if (!changed)
                return;

            Selection.objects = keep.ToArray();
            RebuildInspectorTrackers();
        }

        private static void RebuildInspectorTrackers()
        {
            var shared = ActiveEditorTracker.sharedTracker;
            shared.ForceRebuild();

            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                ActiveEditorTracker tracker = null;
                for (var type = window.GetType(); type != null && tracker == null; type = type.BaseType)
                {
                    var field = type.GetField("m_Tracker", flags);
                    if (field != null && field.FieldType == typeof(ActiveEditorTracker))
                        tracker = field.GetValue(window) as ActiveEditorTracker;
                }
                if (tracker != null && !tracker.Equals(shared))
                    tracker.ForceRebuild();
            }
        }

        public static void SetVisible(GameObject go, bool visible)
        {
            if (go == null)
                return;

            if (visible)
                SceneVisibilityManager.instance.Show(go, true);
            else
                SceneVisibilityManager.instance.Hide(go, true);
        }

        public static void RefreshScene()
        {
            EditorApplication.QueuePlayerLoopUpdate();

            try
            {
                if (IsConstraintEditModeEnabled)
                    VRCDynamicsScheduler.UpdateConstraints(true);
            }
            catch (Exception)
            {
            }

            SceneView.RepaintAll();
        }
        #endregion

        #region Save
        public static PrefabSaveMode DetectSaveMode(GameObject root, out string nearestAssetPath)
        {
            nearestAssetPath = null;
            if (root == null || !PrefabUtility.IsPartOfNonAssetPrefabInstance(root))
                return PrefabSaveMode.NotPrefabInstance;
            if (!PrefabUtility.IsOutermostPrefabInstanceRoot(root))
                return PrefabSaveMode.NotOutermostRoot;
            if (PrefabUtility.IsPrefabAssetMissing(root))
                return PrefabSaveMode.MissingAsset;

            nearestAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            string guid = AssetDatabase.AssetPathToGUID(nearestAssetPath);
            if (guid == UnsupportedAvatarUtils.templatePrefabGuid)
                return PrefabSaveMode.NewVariant;

            string vendorFolder = AssetDatabase.GUIDToAssetPath(VendorPrefabFolderGuid);
            if (!string.IsNullOrEmpty(vendorFolder) && nearestAssetPath.StartsWith(vendorFolder + "/", StringComparison.Ordinal))
                return PrefabSaveMode.UpdateVendorVariant;

            return PrefabSaveMode.UpdateUserVariant;
        }

        public static void TransferPrefabModifications(GameObject fromInstanceRoot, GameObject toInstanceRoot, Transform fromAvatarRoot, Transform toAvatarRoot)
        {
            if (fromInstanceRoot == null || toInstanceRoot == null)
                return;

            var modifications = PrefabUtility.GetPropertyModifications(fromInstanceRoot);
            if (modifications == null)
                return;

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(toInstanceRoot);
            var result = new List<PropertyModification>();
            foreach (var m in modifications)
            {
                if (m == null || m.target == null)
                    continue;
                if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.GetAssetPath(m.target) != assetPath)
                    continue;

                var copy = new PropertyModification
                {
                    target = m.target,
                    propertyPath = m.propertyPath,
                    value = m.value,
                    objectReference = m.objectReference,
                };
                if (copy.objectReference != null && !EditorUtility.IsPersistent(copy.objectReference))
                    copy.objectReference = RemapReference(copy.objectReference, fromInstanceRoot, toInstanceRoot, fromAvatarRoot, toAvatarRoot);
                result.Add(copy);
            }

            PrefabUtility.SetPropertyModifications(toInstanceRoot, result.ToArray());
        }

        private static UnityEngine.Object RemapReference(UnityEngine.Object reference, GameObject fromInstanceRoot, GameObject toInstanceRoot, Transform fromAvatarRoot, Transform toAvatarRoot)
        {
            Transform t = reference is GameObject go ? go.transform : (reference as Component)?.transform;
            if (t == null)
                return reference;

            if (t.IsChildOf(fromInstanceRoot.transform))
            {
                var assetObject = PrefabUtility.GetCorrespondingObjectFromSource(reference);
                if (assetObject != null)
                {
                    foreach (var candidate in EnumerateObjects(toInstanceRoot.transform))
                    {
                        if (candidate.GetType() == reference.GetType() && PrefabUtility.GetCorrespondingObjectFromSource(candidate) == assetObject)
                            return candidate;
                    }
                }
            }

            if (fromAvatarRoot != null && toAvatarRoot != null && t.IsChildOf(fromAvatarRoot))
            {
                string path = RelativePath(fromAvatarRoot, t);
                Transform mapped = string.IsNullOrEmpty(path) ? toAvatarRoot : toAvatarRoot.Find(path);
                if (mapped != null)
                {
                    if (reference is GameObject)
                        return mapped.gameObject;
                    if (reference is Component component)
                    {
                        var siblings = t.GetComponents(component.GetType());
                        int index = System.Array.IndexOf(siblings, component);
                        var targets = mapped.GetComponents(component.GetType());
                        if (index >= 0 && index < targets.Length)
                            return targets[index];
                    }
                }
            }

            return reference;
        }

        private static IEnumerable<UnityEngine.Object> EnumerateObjects(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                yield return t.gameObject;
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c != null)
                        yield return c;
                }
            }
        }

        private static string RelativePath(Transform root, Transform t)
        {
            var parts = new List<string>();
            for (Transform cur = t; cur != null && cur != root; cur = cur.parent)
                parts.Insert(0, cur.name);
            return string.Join("/", parts);
        }

        public static GameObject InstantiateIntoAvatar(GameObject asset, Transform avatarRoot, GameObject replace, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, string name, out bool replaced)
        {
            replaced = false;
            if (asset == null || avatarRoot == null)
                return null;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            if (replace != null)
            {
                bool deletable = !PrefabUtility.IsPartOfNonAssetPrefabInstance(replace) || PrefabUtility.IsOutermostPrefabInstanceRoot(replace);
                if (deletable)
                    Undo.DestroyObjectImmediate(replace);
                else
                {
                    Undo.RecordObject(replace, "Disable old template");
                    replace.SetActive(false);
                }
                replaced = true;
            }

            var go = PrefabUtility.InstantiatePrefab(asset, avatarRoot) as GameObject;
            if (go == null)
                return null;

            Undo.RegisterCreatedObjectUndo(go, "Add Satabako prefab");
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            if (!string.IsNullOrEmpty(name))
                go.name = name;

            Undo.SetCurrentGroupName("Add Satabako prefab");
            Undo.CollapseUndoOperations(group);
            EditorUtility.SetDirty(go);
            return go;
        }

        public static GameObject SaveAsNewVariant(GameObject root, string assetPath, out bool success)
        {
            success = false;
            if (root == null || string.IsNullOrEmpty(assetPath))
                return null;

            return PrefabUtility.SaveAsPrefabAssetAndConnect(root, assetPath, InteractionMode.UserAction, out success);
        }

        public static void ApplyOverrides(GameObject root)
        {
            if (root == null)
                return;

            PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);
        }

        public static void RecordInstanceModifications(GameObject root)
        {
            if (root == null)
                return;

            Undo.FlushUndoRecordObjects();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c != null && PrefabUtility.IsPartOfPrefabInstance(c))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                }
            }
        }

        public static void SetSearcherInfo(TabakoScriptBehaviour script, string[] avatarNames, string shopName, string avatarUrl)
        {
            if (script == null)
                return;

            var so = new SerializedObject(script);
            var names = so.FindProperty("avatarNamesForSearcher");
            if (names != null)
            {
                avatarNames = avatarNames ?? new string[0];
                names.arraySize = avatarNames.Length;
                for (int i = 0; i < avatarNames.Length; i++)
                    names.GetArrayElementAtIndex(i).stringValue = avatarNames[i];
            }

            var shop = so.FindProperty("shopName");
            if (shop != null)
                shop.stringValue = shopName ?? "";

            var url = so.FindProperty("avatarURL");
            if (url != null)
                url.stringValue = avatarUrl ?? "";

            so.ApplyModifiedProperties();
        }
        #endregion
    }

    internal sealed class TabakoWeightScope : IDisposable
    {
        private readonly VRCParentConstraint _constraint;
        private readonly float[] _saved;
        private bool _disposed;

        public VRCParentConstraint Constraint => _constraint;
        public int ActiveSource { get; }

        public TabakoWeightScope(VRCParentConstraint constraint, int activeSource)
        {
            _constraint = constraint;
            ActiveSource = activeSource;
            _saved = UnsupportedAvatarOps.GetSourceWeights(constraint);

            var weights = new float[_saved.Length];
            if (activeSource >= 0 && activeSource < weights.Length)
                weights[activeSource] = 1f;

            UnsupportedAvatarOps.SetSourceWeights(constraint, weights);
            UnsupportedAvatarOps.RefreshScene();
        }

        public void Reapply()
        {
            if (_disposed || _constraint == null)
                return;
            var weights = new float[_saved.Length];
            if (ActiveSource >= 0 && ActiveSource < weights.Length)
                weights[ActiveSource] = 1f;
            UnsupportedAvatarOps.SetSourceWeights(_constraint, weights);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_constraint == null)
                return;

            UnsupportedAvatarOps.SetSourceWeights(_constraint, _saved);
            UnsupportedAvatarOps.RefreshScene();
        }
    }
}

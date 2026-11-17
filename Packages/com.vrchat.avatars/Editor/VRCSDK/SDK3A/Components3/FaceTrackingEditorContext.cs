#if VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace VRC.SDK3A.Editor
{
    /// <summary>
    /// Optional, editor-only context for configuration fields. References are stored in the project's
    /// Library and are never added to an uploaded configuration. Unresolvable scene references are ignored.
    /// </summary>
    [InitializeOnLoad]
    public sealed class FaceTrackingEditorContext
    {
        // ConditionalWeakTables are used to avoid leaking references to scene objects when the context is no longer needed.
        static readonly ConditionalWeakTable<UnityEngine.Object, Dictionary<string, FaceTrackingEditorContext>> Contexts = new ConditionalWeakTable<UnityEngine.Object, Dictionary<string, FaceTrackingEditorContext>>();
        static readonly ConditionalWeakTable<VRCAvatarDescriptor, MeshCatalog> DescriptorMeshes = new ConditionalWeakTable<VRCAvatarDescriptor, MeshCatalog>();
        
        // Cache invalidation backing properties
        internal static int Tick { get; private set; }
        internal static int Revision { get; private set; }
        internal static int BindingsRevision { get; private set; }
        internal static int AssetsRevision { get; private set; }
        internal static int HierarchyRevision { get; private set; }
        
        static FaceTrackingEditorContext()
        {
            EditorApplication.update += () => Tick++;
            EditorApplication.hierarchyChanged += () => { HierarchyRevision++; Invalidate(); };
            EditorApplication.projectChanged += AssetsChanged;
            Undo.undoRedoPerformed += AssetsChanged;
            EditorSceneManager.sceneSaved += _ => PersistSavedSceneReferences();
        }

        internal static void Invalidate() => Revision++;
        internal static void InvalidateBindings() { BindingsRevision++; Invalidate(); }
        static void AssetsChanged() { AssetsRevision++; HierarchyRevision++; InvalidateBindings(); }

        // Flush stored scene references to disk for a valid context when the scene is saved
        static void PersistSavedSceneReferences()
        {
            bool changed = false;
            foreach (var owner in Contexts)
            {
                foreach (FaceTrackingEditorContext context in owner.Value.Values)
                {
                    if (string.IsNullOrEmpty(context._entry.key) || FaceTrackingAuthoringState.instance.Find(context._entry.key) == null)
                        continue;
                    
                    context.Store(false);
                    changed = true;
                }
            }
            if (changed) FaceTrackingAuthoringState.instance.Flush();
        }

        readonly UnityEngine.Object _owner;
        readonly string _path;
        readonly FaceTrackingAuthoringState.Entry _entry;
        readonly List<SkinnedMeshRenderer> _manualMeshes = new List<SkinnedMeshRenderer>();
        readonly MeshCatalog _manualCatalog = new MeshCatalog();
        VRCAvatarDescriptor _source;
        VRCAvatarDescriptor _inferred;
        int _hierarchyRevision = -1;
        bool _showMeshes = true;
        
        FaceTrackingEditorContext(SerializedProperty property)
        {
            _owner = property.serializedObject.targetObject;
            _path = property.propertyPath;
            if (_owner is VRCAvatarDescriptor)
            {
                _entry = new FaceTrackingAuthoringState.Entry();
                return;
            }
            string ownerId = ObjectId(_owner);
            if (string.IsNullOrEmpty(ownerId))
            {
                // we store the sessions in SessionState to avoid creating a new unique state key every assembly reload
                const string sessionKey = "VRC.FaceTracking.AuthoringSession";
                string session = SessionState.GetString(sessionKey, "");
                if (string.IsNullOrEmpty(session))
                {
                    session = Guid.NewGuid().ToString("N");
                    SessionState.SetString(sessionKey, session);
                }
                ownerId = session + ":" + _owner.GetInstanceID();
            }
            string key = ownerId + ":" + _path;
            _entry = FaceTrackingAuthoringState.instance.Find(key) ?? new FaceTrackingAuthoringState.Entry { key = key };
            _source = ResolveSourceAvatar(_entry.source);
            foreach (string mesh in _entry.meshes)
            {
                _manualMeshes.Add(ResolveObject(mesh) as SkinnedMeshRenderer);
            }
        }

        /// <summary>Returns the authoring context for a serialized configuration field.</summary>
        public static FaceTrackingEditorContext ForProperty(SerializedProperty property)
        {
            Dictionary<string, FaceTrackingEditorContext> contexts = Contexts.GetOrCreateValue(property.serializedObject.targetObject);
            if (!contexts.TryGetValue(property.propertyPath, out FaceTrackingEditorContext context))
                contexts.Add(property.propertyPath, context = new FaceTrackingEditorContext(property));
            
            return context;
        }

        /// <summary>The destination descriptor always supplies context for its own configurations.</summary>
        public VRCAvatarDescriptor SourceAvatar
        {
            get
            {
                if (_owner is VRCAvatarDescriptor descriptor)
                    return descriptor;
                if (_entry.overrideSource)
                {
                    if (_source == null && _hierarchyRevision != HierarchyRevision)
                    {
                        _hierarchyRevision = HierarchyRevision;
                        _source = ResolveSourceAvatar(_entry.source);
                    }
                    return _source;
                }
                if (_hierarchyRevision != HierarchyRevision)
                {
                    _hierarchyRevision = HierarchyRevision;
                    _inferred = _owner is Component component ? component.GetComponentInParent<VRCAvatarDescriptor>(true) : null;
                }
                return _inferred;
            }
            set
            {
                // If the context is not owned by a descriptor, the source can be overriden and the context will get stored in the project library
                if (_owner is VRCAvatarDescriptor) return;
                _entry.overrideSource = true;
                _source = value;
                _entry.source = ObjectId(value);
                Store();
            }
        }

        /// <summary>Supplies fallback meshes when no source Avatar is assigned.</summary>
        public void SetMeshes(IEnumerable<SkinnedMeshRenderer> meshes)
        {
            if (_owner is VRCAvatarDescriptor) return;
            
            _manualMeshes.Clear();
            if (meshes != null)
                _manualMeshes.AddRange(meshes);
            
            Store();
        }

        /// <summary>Resolves a clipboard source only if that object is available in the current project.</summary>
        public static VRCAvatarDescriptor ResolveSourceAvatar(string globalObjectId) => ResolveObject(globalObjectId) as VRCAvatarDescriptor;

        internal static string ObjectId(UnityEngine.Object value)
        {
            if (value == null) return null;
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(value);
            return id.identifierType == 0 || id.targetObjectId == 0 || id.assetGUID.Equals(default(GUID)) ? null : id.ToString();
        }

        static UnityEngine.Object ResolveObject(string text)
            => !string.IsNullOrEmpty(text) && GlobalObjectId.TryParse(text, out GlobalObjectId id)
                ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) : null;

        internal void ApplyClipboardSource(string source, bool flush = true)
        {
            if (_owner is VRCAvatarDescriptor || string.IsNullOrEmpty(source))
                return;
            _source = ResolveSourceAvatar(source);
            _entry.overrideSource = true;
            _entry.source = source;
            Store(flush);
        }

        /// <summary>
        /// Updates the FaceTracking Authoring State singleton with the current state
        /// Will flush the changes to disk by default
        /// </summary>
        void Store(bool flush = true)
        {
            string ownerId = ObjectId(_owner);
            if (!string.IsNullOrEmpty(ownerId))
                _entry.key = ownerId + ":" + _path;
            
            if (_source != null)
                _entry.source = ObjectId(_source);
            
            _entry.meshes.Clear();
            
            foreach (SkinnedMeshRenderer mesh in _manualMeshes)
            {
                _entry.meshes.Add(ObjectId(mesh));
            }
            
            FaceTrackingAuthoringState.instance.Store(_entry, flush);
            Invalidate();
        }

        internal MeshCatalog Catalog
        {
            get
            {
                VRCAvatarDescriptor source = SourceAvatar;
                MeshCatalog catalog = source != null ? DescriptorMeshes.GetOrCreateValue(source) : _manualCatalog;
                catalog.Refresh(source != null ? source.faceTrackingMeshes : _manualMeshes, source);
                return catalog;
            }
        }

        /// <summary>
        /// Returns the height of the inspector UI for this context, including space for the mesh list if requested
        /// </summary>
        internal float AuthoringHeight(bool meshes)
        {
            if (!meshes || SourceAvatar != null)
                return FaceTrackingDrawerUtility.Step;
            int lineCount = 2;
            if (_showMeshes)
                lineCount += _manualMeshes.Count + 1;
            return FaceTrackingDrawerUtility.Step * lineCount;
        }

        /// <summary>
        /// Draws all the necessary fields to author face tracking outside an Avatar Descriptor.
        /// When called with meshes: true, Mesh list will be drawn when the source avatar is not set.
        /// (If source avatar is set, it's intended that meshes for that avatar's descriptor are being referenced instead)
        /// </summary>
        internal void DrawAuthoring(ref Rect position, bool meshes)
        {
            VRCAvatarDescriptor source = SourceAvatar;

            using (var c = new EditorGUI.ChangeCheckScope())
            {
                VRCAvatarDescriptor selected = EditorGUI.ObjectField(FaceTrackingDrawerUtility.TakeLine(ref position),
                    new GUIContent("Source Avatar",
                        "Optional source for mesh pickers and auto-fill. Names remain portable when this reference is unavailable."),
                    source, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
                if (c.changed) SourceAvatar = selected;
            }
            
            // Only draw mesh list when meshes are requested and no source avatar is set
            if (!meshes || SourceAvatar != null) return;
            
            _showMeshes = EditorGUI.Foldout(FaceTrackingDrawerUtility.TakeLine(ref position), _showMeshes, "Authoring Meshes", true);
            if (!_showMeshes) return;
            
            for (int i = 0; i < _manualMeshes.Count; i++)
            {
                Rect row = FaceTrackingDrawerUtility.TakeLine(ref position);
                row.width -= 28f;
                using (var c = new EditorGUI.ChangeCheckScope())
                {
                    _manualMeshes[i] = EditorGUI.ObjectField(row, _manualMeshes[i], typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
                    if (c.changed) Store();
                }
                
                if (GUI.Button(new Rect(row.xMax + 3f, row.y, 25f, row.height), "-"))
                {
                    _manualMeshes.RemoveAt(i);
                    Store();
                    break;
                }
            }
            
            Rect addMeshButton = FaceTrackingDrawerUtility.TakeLine(ref position);
            addMeshButton.xMin = Mathf.Max(addMeshButton.x, addMeshButton.xMax - 90f);
            using (new EditorGUI.DisabledScope(_manualMeshes.Count >= 16))
            {
                if (GUI.Button(addMeshButton, "Add Mesh"))
                {
                    _manualMeshes.Add(null);
                    Store();
                }
            }
        }

        /// <summary>
        /// Stores all the relevant mesh data required for Face Tracking authoring.
        /// Mainly initialized from the provided avatar descriptor
        /// </summary>
        internal sealed class MeshCatalog
        {
            int _tick = -1;
            int _revision = -1;
            int _assetsRevision = -1;
            readonly List<SkinnedMeshRenderer> _sources = new List<SkinnedMeshRenderer>();
            readonly List<Mesh> _sharedMeshes = new List<Mesh>();
            readonly List<int> _blendshapeCounts = new List<int>();
            string[] _blendshapeOptions;
            HashSet<string> _blendshapeNames;
            readonly List<string> _setSnapshot = new List<string>();
            readonly HashSet<string> _sets = new HashSet<string>(StringComparer.Ordinal);
            public readonly List<SkinnedMeshRenderer> Meshes = new List<SkinnedMeshRenderer>();
            public int Version { get; private set; }
            public int MappingsVersion { get; private set; }
            public bool UseAllKeys { get; private set; }
            public bool VisemesAvailable { get; private set; }
            public bool GazeDisabled { get; private set; }
            public bool BlinkDisabled { get; private set; }

            public MeshCatalog() { }

            public void Refresh(List<SkinnedMeshRenderer> meshes, VRCAvatarDescriptor descriptor)
            {
                if (_tick == Tick && _revision == Revision)
                    return;
                _tick = Tick;
                bool changed = _assetsRevision != AssetsRevision;
                _assetsRevision = AssetsRevision;
                _revision = Revision;
                bool useAllTrackingKeys = descriptor == null || descriptor.useAllTrackingKeys;
                bool visemesAvailable = descriptor == null || descriptor.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShapeFaceTracking;
                bool gazeDisabled = descriptor != null && descriptor.enableEyeLook && descriptor.customEyeLookSettings.eyeGazeMode == VRCAvatarDescriptor.CustomEyeLookSettings.EyeGazeMode.BoneRotation;
                bool blinkDisabled = descriptor != null && descriptor.enableEyeLook && descriptor.customEyeLookSettings.eyelidType != VRCAvatarDescriptor.EyelidType.None && descriptor.customEyeLookSettings.eyelidType != VRCAvatarDescriptor.EyelidType.FaceTracking;
                if (UseAllKeys != useAllTrackingKeys || VisemesAvailable != visemesAvailable || GazeDisabled != gazeDisabled || BlinkDisabled != blinkDisabled)
                {
                    Version++;
                    MappingsVersion++;
                }
                UseAllKeys = useAllTrackingKeys;
                VisemesAvailable = visemesAvailable;
                GazeDisabled = gazeDisabled;
                BlinkDisabled = blinkDisabled;
                var sets = descriptor != null ? descriptor.faceTrackingBlendshapesSetList : null;
                bool setsChanged = _setSnapshot.Count != (sets?.Count ?? 0);
                if (!setsChanged && sets != null)
                {
                    for (int i = 0; i < sets.Count; i++)
                        setsChanged |= _setSnapshot[i] != sets[i]?.setName;
                }
                if (setsChanged)
                {
                    _sets.Clear();
                    _setSnapshot.Clear();
                    if (sets != null)
                    {
                        foreach (VRCAvatarDescriptor.FaceTrackingBlendshapesSet set in sets)
                        {
                            _setSnapshot.Add(set?.setName);
                            if (!string.IsNullOrEmpty(set?.setName))
                                _sets.Add(set.setName);
                        }
                    }
                    Version++;
                }
                int count = Math.Min(meshes?.Count ?? 0, 16);
                changed |= _sources.Count != count;
                for (int i = 0; i < count; i++)
                {
                    SkinnedMeshRenderer renderer = meshes[i];
                    Mesh sharedMesh = renderer != null ? renderer.sharedMesh : null;
                    int blendshapeCount = sharedMesh != null ? sharedMesh.blendShapeCount : 0;
                    if (i >= _sources.Count)
                    {
                        _sources.Add(renderer);
                        _sharedMeshes.Add(sharedMesh);
                        _blendshapeCounts.Add(blendshapeCount);
                    }
                    else
                    {
                        changed |= _sources[i] != renderer || _sharedMeshes[i] != sharedMesh || _blendshapeCounts[i] != blendshapeCount;
                        _sources[i] = renderer;
                        _sharedMeshes[i] = sharedMesh;
                        _blendshapeCounts[i] = blendshapeCount;
                    }
                }
                if (_sources.Count > count)
                {
                    _sources.RemoveRange(count, _sources.Count - count);
                    _sharedMeshes.RemoveRange(count, _sharedMeshes.Count - count);
                    _blendshapeCounts.RemoveRange(count, _blendshapeCounts.Count - count);
                }
                if (!changed)
                    return;
                Version++;
                MappingsVersion++;
                Meshes.Clear();
                _blendshapeOptions = null;
                _blendshapeNames = null;
                for (int i = 0; i < count; i++)
                {
                    if (_sources[i] == null || _sharedMeshes[i] == null)
                        continue;
                    Meshes.Add(_sources[i]);
                }
            }

            public bool HasSet(string name) => !string.IsNullOrEmpty(name) && _sets.Contains(name);

            public bool HasBlendshape(string name)
            {
                GetBlendshapeNames();
                return _blendshapeNames.Contains(name);
            }

            public string[] GetBlendshapeNames()
            {
                if (_blendshapeOptions != null)
                    return _blendshapeOptions;
                List<string> names = new List<string>();
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < Meshes.Count; i++)
                {
                    Mesh sharedMesh = Meshes[i].sharedMesh;
                    for (int j = 0; j < sharedMesh.blendShapeCount; j++)
                    {
                        string name = sharedMesh.GetBlendShapeName(j);
                        if (seen.Add(name))
                            names.Add(name);
                    }
                }
                _blendshapeOptions = names.ToArray();
                _blendshapeNames = seen;
                return _blendshapeOptions;
            }
        }
    }
}
#endif

using System.Collections.Generic;
using System.IO;
using System.Linq;
using net.satania_shopping.tabakosystem.runtime;
using net.satania_shopping.tabakosystem.utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace net.satania_shopping.tabakosystem.editor
{
    internal sealed class UnsupportedAvatarWizardWindow : EditorWindow
    {
        private enum Step
        {
            Setup,
            Contacts,
            Case,
            Cigarette,
            Lighter,
            Save,
        }

        private struct HandleEntry
        {
            public string label;
            public Transform transform;
            public ContactBase contact;
            public ContactBase syncReceiver;

            public Vector3 WorldPosition => contact != null ? ContactSpace.LocalToWorld(contact.transform, contact.position) : transform.position;
            public Quaternion WorldRotation => contact != null ? contact.transform.rotation * contact.rotation : transform.rotation;
            public bool IsValid => contact != null || transform != null;
        }

        private const string k_docUrl = "https://saturnianjp.github.io/satania_shopping_document/docs/SataniaTabako/Install/for_unsupported_avatar";
        private const string k_keyStep = "Satabako_Wizard_Step";
        private const string k_keyTarget = "Satabako_Wizard_Target";
        private const float k_panelWidth = 400f;
        private const float k_gap = 4f;
        private const double k_repaintInterval = 1.0 / 30.0;
        private const float k_minScale = 0.2f;
        private const float k_maxScale = 2.0f;

        private static readonly string[] k_stepKeys =
        {
            "t_Wiz_Step_Setup", "t_Wiz_Step_Contacts", "t_Wiz_Step_Case", "t_Wiz_Step_Cigarette", "t_Wiz_Step_Lighter", "t_Wiz_Step_Save",
        };

        private static readonly string[] k_helpKeys =
        {
            "t_Wiz_Help_Orbit", "t_Wiz_Help_Pan", "t_Wiz_Help_Zoom", "t_Wiz_Help_Frame", "t_Wiz_Help_Tool",
        };
        private static readonly string[] k_helpMeshKeys =
        {
            "t_Wiz_Help_MeshSelect", "t_Wiz_Help_MeshHide", "t_Wiz_Help_MeshShowAll",
        };

        private static readonly string[] k_stepDescriptionKeys =
        {
            "t_Wiz_Desc_Setup", "t_Wiz_Desc_Contacts", "t_Wiz_Desc_Case", "t_Wiz_Desc_Cigarette", "t_Wiz_Desc_Lighter", "t_Wiz_Desc_Save",
        };

        [SerializeField] private GameObject m_target;
        [SerializeField] private PreviewViewport.CameraState m_camera = PreviewViewport.CameraState.Default;
        [SerializeField] private bool m_showGizmos;
        [SerializeField] private bool m_resetByReload;

        private PreviewViewport _viewport;
        private UnityEngine.UIElements.IMGUIContainer _viewportContainer;
        private WizardPreviewSession _session;
        private GUIStyle _helpStyle;
        private GUIStyle _pacingHintStyle;
        private Step _step;
        private Vector2 _scroll;
        private double _lastRepaint;
        private string _notice;
        private bool _completed;
        private Object _lastResult;

        private PreviewViewport.Tool _tool = PreviewViewport.Tool.Move;
        private int _handleIndex;
        private readonly List<HandleEntry> _handleEntries = new List<HandleEntry>();
        private Vector2 _lastMouseMoveLocal;
        private Vector2 _handleCenterLocal;
        private Vector2 _handleXArrowLocal;
        private Vector2 _caseMeshGuiLocal;
        private bool _handleGuiValid;

        private bool _showGesture = true;
        private bool _hideOthers = true;
        private bool _mirrorBoth = true;
        private const string k_prefMirrorBoth = "SatabakoWizard.MirrorBothHands";
        private bool _mirrorContacts = true;
        private const string k_prefMirrorContacts = "SatabakoWizard.MirrorBothContacts";
        private bool _advancedMode;
        private const string k_prefAdvancedMode = "SatabakoWizard.AdvancedMode";
        [System.NonSerialized] private TabakoScriptBehaviour _copySource;
        [System.NonSerialized] private bool _copyTargets = true;
        [System.NonSerialized] private bool _copyContacts = true;
        [System.NonSerialized] private bool _copyScales = true;
        [System.NonSerialized] private bool _copyGestures = true;
        [System.NonSerialized] private bool _copySearcherInfo;
        private bool _mirrorGesture = true;
        private const string k_prefMirrorGesture = "SatabakoWizard.MirrorGesture";
        private bool _syncReceivers = true;
        private float _holdOffsetMm;

        private FingerPosePreview _preview;
        private GestureKind _previewKind;
        private bool _previewWithGesture;
        private float[] _previewMuscles;

        private readonly Dictionary<GestureKind, float[]> _gestureEdits = new Dictionary<GestureKind, float[]>();
        private readonly Dictionary<GestureKind, float[]> _gestureOriginals = new Dictionary<GestureKind, float[]>();
        private bool _editGesture;
        private HumanBodyBones _selectedJoint = HumanBodyBones.LastBone;
        private readonly Quaternion[] _jointDiscRotation = { Quaternion.identity, Quaternion.identity, Quaternion.identity };
        private readonly Vector3[] _jointDofAxis = new Vector3[3];
        private readonly float[] _jointDofDegPerUnit = new float[3];
        private readonly bool[] _jointDofValid = new bool[3];
        internal static string GestureFallbackFolder = "Assets/SataniaTabakoGestures";
        private bool _embedGestureClips = true;
        private const string k_prefEmbedGestureClips = "SatabakoWizard.EmbedGestureClips";
        private string _previewMessage;
        private TabakoWeightScope _weightScope;
        private readonly List<(VRCParentConstraint constraint, bool wasActive, bool hadSourcesOverride)> _previewConstraints = new List<(VRCParentConstraint, bool, bool)>();
        private readonly List<VRCParentConstraint> _pendingSourceReverts = new List<VRCParentConstraint>();
        private readonly List<IFollowSatabakoObject> _previewFollowers = new List<IFollowSatabakoObject>();
        private readonly List<GameObject> _hidden = new List<GameObject>();

        private string _prefabName;
        private string _avatarNames;
        private string _shopName;
        private string _avatarUrl;
        private bool _saveFieldsInitialized;
        private string _saveMessage;

        private TabakoTemplateRefs Refs => _session != null && _session.IsValid ? _session.Refs : null;

        #region Menu
        [MenuItem(MenuItemDictionary.p_UnsupportedAvatarWizard, priority = 35)]
        private static void OpenFromMenu()
        {
            Open().TrySetTargetFromSelection();
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedAvatarWizardGameObject, false, 21)]
        private static void OpenFromHierarchy()
        {
            Open().TrySetTargetFromSelection();
        }

        [MenuItem(MenuItemDictionary.p_UnsupportedAvatarWizardGameObject, true, 21)]
        private static bool OpenFromHierarchyValidate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && Selection.activeGameObject != null
                && Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>() != null;
        }

        private static UnsupportedAvatarWizardWindow Open()
        {
            var window = GetWindow<UnsupportedAvatarWizardWindow>();
            window.titleContent = new GUIContent(L("t_Wiz_Title"));
            window.minSize = new Vector2(960, 560);
            window.Show();
            return window;
        }

        internal static void OpenFor(GameObject target)
        {
            Open().SetTargetUnlessEditing(target);
        }
        #endregion

        private static string L(string id) => LanguageManager.GetText(id);

        #region Domain reload / play mode
        [InitializeOnLoadMethod]
        private static void Init()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<UnsupportedAvatarWizardWindow>())
            {
                if (window._session == null && (window._viewport == null || !window._viewport.IsCreated))
                    continue;
                window.EndSession();
                window._viewport?.Dispose();
                window.m_resetByReload = true;
            }
        }
        #endregion

        #region Test hooks
        internal WizardPreviewSession SessionForTest => _session;
        internal void SetTargetForTest(GameObject target)
        {
            SetTarget(target);
            EnsureSession();
        }
        internal void SetStepForTest(int step) => SetStep((Step)Mathf.Clamp(step, 0, k_stepKeys.Length - 1));
        internal bool HasPreviewForTest => _preview != null && FingerPosePreview.Active == _preview;
        internal int HiddenCountForTest => _hidden.Count;
        internal Object SaveForTest(string assetPathOrNull)
        {
            Save(assetPathOrNull, false);
            return _lastResult;
        }
        internal void SetToolForTest(PreviewViewport.Tool tool) => _tool = tool;
        internal void SetHandleTargetForTest(int index) => SetHandleIndex(index, false);
        internal bool MirrorBothForTest
        {
            get => _mirrorBoth;
            set => _mirrorBoth = value;
        }
        internal bool MirrorContactsForTest
        {
            get => _mirrorContacts;
            set => _mirrorContacts = value;
        }
        internal bool AdvancedModeForTest
        {
            get => _advancedMode;
            set => _advancedMode = value;
        }
        internal TabakoScriptBehaviour CopySourceForTest
        {
            get => _copySource;
            set => _copySource = value;
        }
        internal TabakoValueCopier.Result CopyValuesForTest(TabakoScriptBehaviour source, bool targets, bool contacts, bool scales, bool gestures)
        {
            _copySource = source;
            _copyTargets = targets;
            _copyContacts = contacts;
            _copyScales = scales;
            _copyGestures = gestures;
            return CopyValuesFromSource(false);
        }
        internal void ApplyHandleForTest(Vector3 position, Quaternion rotation) => ApplyHandle(CurrentHandleEntry(), position, rotation);
        internal Transform HandleTargetTransformForTest => CurrentHandleEntry().transform;
        internal Vector2 LastMouseMoveLocalForTest => _lastMouseMoveLocal;
        internal int StepForTest => (int)_step;
        internal int HandleIndexForTest => _handleIndex;
        internal Vector2 CaseMeshGuiForTest => _caseMeshGuiLocal;
        internal Vector2 RingXGuiForTest => _ringXGuiLocal;
        internal Vector2 RingXTangentGuiForTest => _ringXTangentGuiLocal;
        internal Vector2 ViewportHostOriginForTest => _viewportContainer != null ? _viewportContainer.worldBound.position : ViewportRect.position;
        internal Vector2 PanelHostOriginForTest => rootVisualElement != null ? rootVisualElement.worldBound.position : Vector2.zero;
        internal Rect StepBarHostRectForTest => new Rect(_stepBarRectForTest.position + PanelHostOriginForTest, _stepBarRectForTest.size);
        internal bool TryGetFieldHostRectForTest(string name, out Rect rect)
        {
            rect = default;
            if (_fieldRectsForTest == null || !_fieldRectsForTest.TryGetValue(name, out Rect local))
                return false;
            rect = new Rect(local.position + PanelHostOriginForTest, local.size);
            return true;
        }
        internal int StepCountForTest => k_stepKeys.Length;
        internal int PanelKeyboardControlForTest => _panelKeyboardControlForTest;
        internal bool PanelEditingTextFieldForTest => _panelEditingTextFieldForTest;
        internal string PanelFocusedControlForTest => _panelFocusedControlForTest;
        internal bool TransformsFoldoutForTest
        {
            get => _transformsFoldout;
            set => _transformsFoldout = value;
        }
        internal HumanBodyBones SelectedJointForTest => _selectedJoint;
        internal Vector2 JointGuiForTest => _jointGuiLocal;
        internal Vector2 StretchRingGuiForTest => _stretchRingGuiLocal;
        internal Vector2 StretchRingTangentGuiForTest => _stretchRingTangentLocal;
        private Vector2 _jointGuiLocal;
        private Vector2 _stretchRingGuiLocal;
        private Vector2 _stretchRingTangentLocal;
        internal void SetGestureEditingForTest(bool on)
        {
            if (CurrentGestureKind.HasValue)
                SetGestureEditing(CurrentGestureKind.Value, on);
        }
        internal float[] GestureEditForTest(GestureKind kind) => _gestureEdits.TryGetValue(kind, out var m) ? m : null;
        internal float GestureMuscleForTest(int muscle) => CurrentGestureKind.HasValue && _gestureEdits.TryGetValue(CurrentGestureKind.Value, out var m) ? m[muscle] : float.NaN;
        internal void SetGestureMuscleForTest(int muscle, float value) => SetGestureMuscle(muscle, value);
        internal bool MirrorGestureForTest
        {
            get => _mirrorGesture;
            set => _mirrorGesture = value;
        }
        internal bool EmbedGestureClipsForTest
        {
            get => _embedGestureClips;
            set => _embedGestureClips = value;
        }
        internal void SelectJointForTest(HumanBodyBones bone)
        {
            _selectedJoint = bone;
            for (int d = 0; d < _jointDiscRotation.Length; d++)
                _jointDiscRotation[d] = Quaternion.identity;
            Repaint();
        }
        private Vector2 _ringXGuiLocal;
        private Vector2 _ringXTangentGuiLocal;
        internal string LastPickedNameForTest => _lastPickedName;
        internal System.Action<Rect> ViewportProbeForTest;
        internal void RequestPickForTest(Vector2 local) => _testPickRequest = local;
        internal Renderer HoverMeshForTest => _hoverMesh;
        internal void SetHoverMeshForTest(Renderer renderer)
        {
            _hoverMesh = renderer;
            Repaint();
        }
        internal void SetSelectedMeshForTest(Renderer renderer)
        {
            _selectedMesh = renderer;
            Repaint();
        }
        internal void HideMeshForTest(Renderer renderer) => HideMesh(renderer);
        internal void ShowAllMeshesForTest() => UnhideAllMeshes();
        internal void ApplyViewPresetForTest(int index) => ApplyViewPreset(index);
        internal void SetOrthographicForTest(bool on)
        {
            m_camera.orthographic = on;
            Repaint();
        }
        internal IEnumerable<string> FieldRectNamesForTest => _fieldRectsForTest != null ? _fieldRectsForTest.Keys : Enumerable.Empty<string>();
        internal Rect ViewportToolbarHostRectForTest
        {
            get
            {
                var local = ViewportToolbarRect();
                return new Rect(local.position + ViewportHostOriginForTest, local.size);
            }
        }
        internal Rect? OrientationGizmoHostRectForTest
        {
            get
            {
                Vector2 size = _viewportContainer != null ? _viewportContainer.layout.size : ViewportRect.size;
                var local = OrientationGizmoRect(new Rect(0f, 0f, size.x, size.y));
                return local.HasValue ? new Rect(local.Value.position + ViewportHostOriginForTest, local.Value.size) : (Rect?)null;
            }
        }
        internal string OpLogForTest => string.Join("\n", _opLog);
        internal int HoverContactForTest => _hoverContact;
        internal void EstimateEverythingForTest() => EstimateEverything();
        internal void RequestGuiWorldPointForTest(Vector3 world)
        {
            _guiWorldPointRequestForTest = world;
            _guiPointForTest = null;
            Repaint();
        }
        [System.NonSerialized] private Vector3? _guiWorldPointRequestForTest;
        internal Vector3 CameraPivotForTest => m_camera.pivot;
        internal bool MeshSelectEnabledForTest
        {
            get => _meshSelect;
            set => SetMeshSelect(value);
        }
        internal Rect MeshSelectButtonHostRectForTest => new Rect(_meshSelectButtonRectForTest.position + ViewportHostOriginForTest, _meshSelectButtonRectForTest.size);
        internal bool IsItemMeshForTest(Renderer renderer) => IsItemRenderer(Refs, renderer);
        internal Renderer SelectedMeshForTest => _selectedMesh;
        internal int HiddenMeshCountForTest => _hiddenMeshes.Count;
        internal IReadOnlyList<Renderer> AvatarRenderersForTest => AvatarRenderers(Refs);
        internal void RequestGuiPointForTest(Transform target)
        {
            _guiPointRequestForTest = target;
            _guiPointForTest = null;
            Repaint();
        }
        internal Vector2? GuiPointForTest => _guiPointForTest;
        internal void EndSessionForTest() => EndSession();
        private Vector2? _testPickRequest;
        private string _lastPickedName;
        internal void RequestViewportCaptureForTest(string path)
        {
            _viewport?.RequestCapture(path);
            Repaint();
        }
        internal bool TryGetHandleGuiForTest(out Vector2 centerLocal, out Vector2 xArrowLocal, out Rect viewport)
        {
            centerLocal = _handleCenterLocal;
            xArrowLocal = _handleXArrowLocal;
            viewport = ViewportRect;
            return _handleGuiValid && _viewport != null && _viewport.IsCreated;
        }
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
            wantsMouseMove = true;
            _embedGestureClips = EditorPrefs.GetBool(k_prefEmbedGestureClips, true);
            _mirrorBoth = EditorPrefs.GetBool(k_prefMirrorBoth, true);
            _mirrorContacts = EditorPrefs.GetBool(k_prefMirrorContacts, true);
            _advancedMode = EditorPrefs.GetBool(k_prefAdvancedMode, false);
            _mirrorGesture = EditorPrefs.GetBool(k_prefMirrorGesture, true);
            _transformsFoldout = EditorPrefs.GetBool(k_prefTransformsFoldout, true);
            _meshSelect = EditorPrefs.GetBool(k_prefMeshSelect, true);

            FingerPosePreview.Trace = Op;
            _viewport = new PreviewViewport
            {
                HandlesGUI = DrawViewportHandles,
                KeyDown = OnViewportKey,
                FrameTarget = FrameTargetBounds,
            };

            _viewportContainer = new UnityEngine.UIElements.IMGUIContainer(OnViewportGUI)
            {
                name = "satabako-wizard-viewport",
                pickingMode = UnityEngine.UIElements.PickingMode.Position,
                focusable = true,
            };
            _viewportContainer.style.position = UnityEngine.UIElements.Position.Absolute;
            _viewportContainer.style.left = k_panelWidth + k_gap;
            _viewportContainer.style.top = 0f;
            _viewportContainer.style.right = 0f;
            _viewportContainer.style.bottom = 0f;
            _viewportContainer.style.overflow = UnityEngine.UIElements.Overflow.Hidden;
            rootVisualElement.Add(_viewportContainer);

            _step = (Step)Mathf.Clamp(SessionState.GetInt(k_keyStep, 0), 0, k_stepKeys.Length - 1);
            if (m_target == null)
            {
                string id = SessionState.GetString(k_keyTarget, "");
                if (!string.IsNullOrEmpty(id) && GlobalObjectId.TryParse(id, out var globalId))
                    m_target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
            }

            if (m_resetByReload)
            {
                m_resetByReload = false;
                _step = Step.Setup;
                SessionState.SetInt(k_keyStep, 0);
                _notice = L("t_Wiz_MsgPreviewReset");
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnUpdate;

            _viewportContainer?.RemoveFromHierarchy();
            _viewportContainer = null;
            if (FingerPosePreview.Trace == (System.Action<string>)Op)
                FingerPosePreview.Trace = null;
            EndSession();
            ReleaseHighlightResources();
            _viewport?.Dispose();
            _viewport = null;
        }

        private void OnUpdate()
        {
            if (_session == null || !_session.IsValid || _viewport == null)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRepaint < k_repaintInterval)
                return;
            _lastRepaint = now;
            DriveConstraints();
            _viewport.Tick();
            Repaint();
        }

        private void OnUndoRedo()
        {
            Op($"undo/redo performed (group '{Undo.GetCurrentGroupName()}')");
            if (_session != null && _session.IsValid)
            {
                if (_undoState != null)
                {
                    _undoState.Apply();
                    _preview?.RefreshAttachments();
                }
                ReassertConstraintPreview();
                DriveConstraints();
            }
            UnsupportedAvatarOps.RefreshScene();
            Repaint();
        }

        private void OnHierarchyChanged()
        {
            _avatarRenderers = null;
            _itemRenderers = null;
            if (_session != null && !_session.IsValid)
            {
                _session.ResolveRefs();
                if (!_session.IsValid)
                {
                    EndSession();
                    _notice = L("t_Wiz_MsgPreviewReset");
                    SetStep(Step.Setup);
                }
            }
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            if (_session != null)
            {
                EndSession();
                _notice = L("t_Wiz_MsgPreviewReset");
                SetStep(Step.Setup);
            }
            _viewport?.Dispose();
        }
        #endregion

        #region Target / session
        private string RomajiSeed()
        {
            foreach (var name in ParseAvatarNames())
            {
                string normalized = KanaRomajiConverter.Normalize(name);
                if (normalized.Length > 0 && !KanaRomajiConverter.ContainsNonKana(normalized) && normalized.ToCharArray().Any(KanaRomajiConverter.IsKana))
                    return normalized;
            }
            return m_target != null ? m_target.name : _prefabName;
        }

        private void AppendAvatarName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            var names = new List<string>(ParseAvatarNames());
            if (names.Contains(name))
                return;

            names.Add(name);
            _avatarNames = string.Join(", ", names);
            Repaint();
        }

        private void DrawMergeArmatureWarning(TabakoTemplateRefs refs)
        {
            if (refs == null || refs.AvatarRoot == null)
                return;

            var merges = refs.AvatarRoot.GetComponentsInChildren<nadena.dev.modular_avatar.core.ModularAvatarMergeArmature>(true);
            if (merges.Length == 0)
                return;

            if (MergeArmatureLockUtil.AnyUnlocked(merges))
            {
                if (EditorGUIUtils.HelpBoxWithButton(L("t_Wiz_MsgMergeArmatureNotLocked"), L("t_Wiz_SyncMergeArmaturePreview"), EditorGUIUtility.IconContent("console.warnicon")))
                {
                    MergeArmatureLockUtil.LockAll(merges);
                    Repaint();
                }
            }

            if (!MergeArmatureLockUtil.IsGlobalBoneSyncEnabled)
            {
                if (EditorGUIUtils.HelpBoxWithButton(L("t_Wiz_MsgBoneSyncOff"), L("t_Wiz_EnableBoneSync"), EditorGUIUtility.IconContent("console.warnicon")))
                {
                    MergeArmatureLockUtil.ToggleGlobalBoneSync();
                    Repaint();
                }
            }
        }

        private void TrySetTargetFromSelection()
        {
            var go = Selection.activeGameObject;
            var descriptor = go != null ? go.GetComponentInParent<VRCAvatarDescriptor>() : null;
            if (descriptor != null && descriptor.gameObject != m_target)
                SetTarget(descriptor.gameObject);
        }

        internal void SetTargetUnlessEditing(GameObject target)
        {
            var descriptor = target != null ? target.GetComponentInParent<VRCAvatarDescriptor>(true) : null;
            if (descriptor == null)
                return;

            GameObject avatar = descriptor.gameObject;
            bool editing = !_completed && _session != null && _session.IsValid && _session.Source == avatar;
            if (!editing)
                SetTarget(avatar);
        }

        private void SetTarget(GameObject target)
        {
            if (target != null)
            {
                var descriptor = target.GetComponentInParent<VRCAvatarDescriptor>();
                if (descriptor != null)
                    target = descriptor.gameObject;
            }

            if (target != m_target)
                m_hiddenMeshPaths.Clear();
            EndSession();
            m_target = target;
            _completed = false;
            _notice = null;
            _saveFieldsInitialized = false;
            _saveMessage = null;
            _lastResult = null;
            SessionState.SetString(k_keyTarget, target != null ? GlobalObjectId.GetGlobalObjectIdSlow(target).ToString() : "");
            SetStep(Step.Setup);
        }

        private bool TargetIsAcceptable => WizardPreviewSession.IsAcceptableTarget(m_target, out _);

        private void EnsureSession()
        {
            if (m_target == null || _completed || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (_session != null && _session.Source == m_target)
                return;
            if (!TargetIsAcceptable)
                return;

            EndSession();
            _viewport.EnsureCreated();
            _session = WizardPreviewSession.Create(m_target, _viewport.Scene);
            Op($"session start ({m_target.name})");
            BeginConstraintPreview();
            BeginUndoState();
            ApplyHiddenMeshPaths();

            m_camera = PreviewViewport.CameraState.Default;
            ApplyStepState();
            ApplyViewPresetForStep();
            Repaint();
        }

        private void EndSession()
        {
            if (_session != null)
                Op("session end");
            EndTransientState();
            RestoreConstraintPreview();
            _gestureEdits.Clear();
            _gestureOriginals.Clear();
            _editGesture = false;
            _selectedJoint = HumanBodyBones.LastBone;
            ReleaseHiddenMeshes();
            EndUndoState();
            if (_session != null)
            {
                if (_viewport != null && _viewport.IsCreated)
                    UnsupportedAvatarOps.ClearSelectionInScene(_viewport.Scene);
                _session.Dispose();
                _session = null;
            }
            _handleEntries.Clear();
            _handleGuiValid = false;
            ReleaseHighlightResources();
        }

        private bool CanLeaveSetup => Refs != null && Refs.IsComplete && Refs.IsHumanoid && !EditorApplication.isPlayingOrWillChangePlaymode;

        private void ReplaceTemplate(Object picked, bool showDialog)
        {
            if (_session == null || !_session.IsValid || picked == null)
                return;

            GameObject asset = picked as GameObject;
            if (asset == null && picked is Component component)
                asset = component.gameObject;
            if (asset != null)
                asset = asset.transform.root.gameObject;

            if (asset == null || !EditorUtility.IsPersistent(asset) || UnsupportedAvatarOps.FindScripts(asset.transform).Length != 1)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog(L("t_Wiz_Title"), L("t_Wiz_MsgTemplateInvalid"), "OK");
                return;
            }

            if (!string.IsNullOrEmpty(_session.SaveModeAssetPath) && _session.SaveModeAssetPath == AssetDatabase.GetAssetPath(asset))
                return;

            Op($"replace template -> {asset.name}");
            EndPanelTextEditing();
            EndTransientState();
            RestoreConstraintPreview();
            EndUndoState();
            _gestureEdits.Clear();
            _gestureOriginals.Clear();
            _editGesture = false;
            _selectedJoint = HumanBodyBones.LastBone;
            _handleEntries.Clear();
            _handleGuiValid = false;
            _handleIndex = 0;
            _selectedMesh = null;
            _hoverMesh = null;
            _hoverContact = -1;
            _saveFieldsInitialized = false;
            _saveMessage = null;

            if (!_session.ReplaceTemplate(asset, out string error))
            {
                if (showDialog)
                    EditorUtility.DisplayDialog(L("t_Wiz_Title"), string.Format(L("t_Wiz_MsgTemplateReplaceFailed"), error), "OK");
                if (!_session.IsValid)
                {
                    EndSession();
                    _notice = L("t_Wiz_MsgPreviewReset");
                }
                Repaint();
                return;
            }

            BeginConstraintPreview();
            BeginUndoState();
            ApplyStepState();
            Repaint();
            if (showDialog)
                EditorUtility.DisplayDialog(L("t_Wiz_Title"), string.Format(L("t_Wiz_MsgTemplateReplaced"), asset.name), "OK");
        }
        internal void ReplaceTemplateForTest(GameObject asset) => ReplaceTemplate(asset, false);
        #endregion

        #region Constraint preview
        private void BeginConstraintPreview()
        {
            RestoreConstraintPreview();
            var refs = Refs;
            if (refs == null)
                return;

            var constraints = new List<VRCParentConstraint> { refs.PC_Case, refs.PC_Tabako, refs.PC_Lighter };
            if (refs.CT_Mouth != null)
                constraints.Add(refs.CT_Mouth.GetComponent<VRCParentConstraint>());
            foreach (var c in constraints)
            {
                if (c == null)
                    continue;
                _previewConstraints.Add((c, c.IsActive, UnsupportedAvatarOps.HasSourcesOverride(c)));
                c.IsActive = false;
                c.ApplyConfigurationChanges();
            }
            _previewFollowers.Clear();
            if (refs.AvatarRoot != null)
                _previewFollowers.AddRange(refs.AvatarRoot.GetComponentsInChildren<IFollowSatabakoObject>(true));
            DriveConstraints();
        }

        private void RestoreConstraintPreview()
        {
            _pendingSourceReverts.Clear();
            foreach (var entry in _previewConstraints)
            {
                if (entry.constraint == null)
                    continue;
                entry.constraint.IsActive = entry.wasActive;
                entry.constraint.ApplyConfigurationChanges();
                if (!entry.hadSourcesOverride)
                    _pendingSourceReverts.Add(entry.constraint);
            }
            _previewConstraints.Clear();
            _previewFollowers.Clear();
        }

        private void RevertTemporarySourceOverrides()
        {
            foreach (var constraint in _pendingSourceReverts)
                UnsupportedAvatarOps.RevertSourcesOverride(constraint);
            _pendingSourceReverts.Clear();
        }

        private void ReassertConstraintPreview()
        {
            foreach (var entry in _previewConstraints)
            {
                if (entry.constraint != null && entry.constraint.IsActive)
                {
                    entry.constraint.IsActive = false;
                    entry.constraint.ApplyConfigurationChanges();
                }
            }
            _weightScope?.Reapply();
        }

        #region Undo (ボーン相対)
        [System.NonSerialized] private WizardUndoState _undoState;

        private void BeginUndoState()
        {
            EndUndoState();
            var refs = Refs;
            if (refs == null)
                return;
            _undoState = ScriptableObject.CreateInstance<WizardUndoState>();
            _undoState.hideFlags = HideFlags.HideAndDontSave;
            _undoState.Initialize(refs.BoneProxyAttachments(false), refs.AvatarRoot);
            UnsupportedAvatarOps.RecordTransform = RecordTargetTransform;
            UnsupportedAvatarOps.TransformMoved = OnTargetTransformMoved;
        }

        private void EndUndoState()
        {
            UnsupportedAvatarOps.RecordTransform = UnsupportedAvatarOps.DefaultRecordTransform;
            UnsupportedAvatarOps.TransformMoved = null;
            if (_undoState != null)
            {
                Undo.ClearUndo(_undoState);
                DestroyImmediate(_undoState);
                _undoState = null;
            }
        }

        private void RecordTargetTransform(Transform target, string undoName)
        {
            if (_undoState != null && _undoState.Contains(target))
                Undo.RecordObject(_undoState, undoName);
            else
                Undo.RecordObject(target, undoName);
        }

        private void OnTargetTransformMoved(Transform target) => _undoState?.Capture(target);
        #endregion

        private void DriveConstraints()
        {
            VerifyTargets();
            foreach (var entry in _previewConstraints)
                UnsupportedAvatarOps.DriveParentConstraint(entry.constraint);
            foreach (var follower in _previewFollowers)
                UnsupportedAvatarOps.DriveFollower(follower);
        }

        #region Operation log / watchdog
        [System.NonSerialized] private readonly List<string> _opLog = new List<string>();
        [System.NonSerialized] private double _lastTargetWarning;
        private const int k_opLogLength = 40;

        private void Op(string text)
        {
            if (_opLog.Count >= k_opLogLength)
                _opLog.RemoveAt(0);
            _opLog.Add($"{EditorApplication.timeSinceStartup:F1}s [{_step}] {text}");
        }

        private void VerifyTargets()
        {
            if (_undoState == null || _session == null || !_session.IsValid)
                return;
            var drifted = _undoState.FindDrifted(0.005f);
            if (drifted.Count == 0)
                return;

            _undoState.Apply();
            _preview?.RefreshAttachments();
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastTargetWarning > 1.0)
            {
                _lastTargetWarning = now;
                Debug.LogWarning($"[SatabakoWizard] 追従先が想定外の経路で動いたので、ボーン相対の記録へ戻しました: {string.Join(", ", drifted)}\n直前の操作:\n{string.Join("\n", _opLog)}");
            }
            Op($"watchdog: {drifted.Count} target(s) restored: {string.Join(", ", drifted)}");
        }
        #endregion
        internal void DriveConstraintsForTest() => DriveConstraints();
        internal float FollowerErrorMmForTest()
        {
            float max = -1f;
            foreach (var follower in _previewFollowers)
            {
                if (follower == null || follower.target == null)
                    continue;
                max = Mathf.Max(max, Vector3.Distance(follower.transform.position, follower.target.position) * 1000f);
            }
            return max;
        }
        #endregion

        #region Transient state (gesture preview / weights / visibility)
        private void EndTransientState()
        {
            StopPreview();
            EndWeightScope();
            RestoreVisibility();
        }

        private void StopPreview()
        {
            if (_preview != null)
            {
                Op($"preview stop {_previewKind} alreadyStopped={_preview.IsStopped} active={FingerPosePreview.Active == _preview}");
                _preview.Stop();
                _preview = null;
            }
            _previewMessage = null;
        }

        private void StartPreview(GestureKind kind)
        {
            var refs = Refs;
            if (refs == null || !refs.IsHumanoid)
                return;
            float[] muscles = _showGesture && _gestureEdits.TryGetValue(kind, out var edited) ? edited : null;
            if (_preview != null && _previewKind == kind && _previewWithGesture == _showGesture && _previewMuscles == muscles && FingerPosePreview.Active == _preview)
                return;

            StopPreview();

            if (!FingerPosePreview.CanStart(refs.Animator, out string reason))
            {
                _previewMessage = reason;
                return;
            }

            AnimationClip clip = null;
            if (_showGesture)
            {
                clip = UnsupportedAvatarOps.ResolveGestureClip(refs.AvatarRoot, kind, out _, out Motion unsupportedOverride);
                if (clip == null)
                    _previewMessage = "ジェスチャーのアニメーションが見つかりません。";
                else if (unsupportedOverride != null)
                    _previewMessage = $"IOverrideHandGesture の {unsupportedOverride.name} は AnimationClip ではないため、既定のジェスチャーで表示しています。";
            }

            Op($"preview start {kind} gesture={_showGesture}");
            _preview = FingerPosePreview.Start(refs.Animator, clip, true, refs.BoneProxyAttachments(), muscles, stopOnSceneSaving: false);
            _previewKind = kind;
            _previewWithGesture = _showGesture;
            _previewMuscles = muscles;
        }

        private void RestartPreview()
        {
            StopPreview();
            UpdatePreviewForStep();
        }

        private GestureKind? CurrentGestureKind
        {
            get
            {
                switch (_step)
                {
                    case Step.Case: return GestureKind.Case;
                    case Step.Cigarette: return GestureKind.Tabako;
                    case Step.Lighter: return GestureKind.Lighter;
                    default: return null;
                }
            }
        }

        private void UpdatePreviewForStep()
        {
            GestureKind? kind = CurrentGestureKind;
            if (kind.HasValue)
                StartPreview(kind.Value);
            else
                StopPreview();
        }

        private void UpdateWeightsForSelection()
        {
            var refs = Refs;
            VRCParentConstraint constraint = null;
            if (refs != null)
            {
                switch (_step)
                {
                    case Step.Case: constraint = refs.PC_Case; break;
                    case Step.Cigarette: constraint = refs.PC_Tabako; break;
                    case Step.Lighter: constraint = refs.PC_Lighter; break;
                }
            }

            var entry = CurrentHandleEntry();
            int source = constraint != null && entry.IsValid ? IndexOfSource(constraint, entry.transform) : -1;
            if (constraint == null || source < 0)
            {
                EndWeightScope();
                return;
            }

            if (_weightScope != null && _weightScope.Constraint == constraint && _weightScope.ActiveSource == source)
                return;

            EndWeightScope();
            _weightScope = new TabakoWeightScope(constraint, source);
        }

        private static int IndexOfSource(VRCParentConstraint constraint, Transform target)
        {
            if (constraint == null || target == null)
                return -1;
            for (int i = 0; i < constraint.Sources.Count; i++)
            {
                if (TabakoTemplateRefs.GetSource(constraint, i) == target)
                    return i;
            }
            return -1;
        }

        private void EndWeightScope()
        {
            if (_weightScope != null)
            {
                _weightScope.Dispose();
                _weightScope = null;
            }
        }

        private static void EndPanelTextEditing()
        {
            if (DisableFocusResetForTest)
                return;
            if (s_endEditingActiveTextField == null)
                s_endEditingActiveTextField = typeof(EditorGUI).GetMethod("EndEditingActiveTextField", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (s_endEditingActiveTextField != null)
                s_endEditingActiveTextField.Invoke(null, null);
            EditorGUIUtility.editingTextField = false;
            GUIUtility.keyboardControl = 0;
        }
        private static System.Reflection.MethodInfo s_endEditingActiveTextField;
        internal static bool DisableFocusResetForTest;

        private void SetHandleIndex(int index, bool moveCamera)
        {
            if (index != _handleIndex)
            {
                EndPanelTextEditing();
                _selectedMesh = null;
            }
            _handleIndex = index;
            UpdateWeightsForSelection();
            if (moveCamera)
                ApplyViewForHandle();
            Repaint();
        }

        private void UpdateVisibilityForStep()
        {
            RestoreVisibility();
            var refs = Refs;
            if (refs == null || !_hideOthers)
                return;

            switch (_step)
            {
                case Step.Case:
                    Hide(refs.TabakoMesh);
                    Hide(refs.LighterMesh);
                    break;
                case Step.Cigarette:
                    Hide(refs.CaseMesh);
                    Hide(refs.LighterMesh);
                    break;
                case Step.Lighter:
                    Hide(refs.CaseMesh);
                    Hide(refs.TabakoMesh);
                    break;
            }
        }

        private void Hide(GameObject go)
        {
            if (go == null)
                return;

            UnsupportedAvatarOps.SetRenderersVisible(go, false);
            _hidden.Add(go);
        }

        private void RestoreVisibility()
        {
            foreach (var go in _hidden)
                UnsupportedAvatarOps.SetRenderersVisible(go, true);
            _hidden.Clear();
        }

        private void ApplyStepState()
        {
            UpdateVisibilityForStep();
            UpdatePreviewForStep();
            UpdateWeightsForSelection();
        }

        private void SetStep(Step step, bool applyViewPreset = true)
        {
            bool changed = step != _step;
            Op($"step {_step} -> {step}");
            EndPanelTextEditing();
            _selectedMesh = null;
            _hoverMesh = null;
            _hoverContact = -1;
            EndTransientState();
            _step = step;
            SessionState.SetInt(k_keyStep, (int)step);
            _scroll = Vector2.zero;
            _handleIndex = 0;
            _editGesture = false;
            _selectedJoint = HumanBodyBones.LastBone;
            ApplyStepState();
            if (changed && applyViewPreset)
                ApplyViewPresetForStep();
            Repaint();
        }
        #endregion

        #region GUI
        private Rect ViewportRect => new Rect(k_panelWidth + k_gap, 0f, Mathf.Max(10f, position.width - k_panelWidth - k_gap), position.height);

        private void OnGUI()
        {
            var panel = new Rect(0f, 0f, k_panelWidth, position.height);

            if (Event.current.type == EventType.Layout)
                EnsureSession();
            if (Event.current.type == EventType.Repaint)
                _panelScreenOriginForTest = GUIUtility.GUIToScreenPoint(Vector2.zero);
            PanelGuiHookForTest?.Invoke();
            if (Event.current.type == EventType.MouseMove && _hoverMesh != null)
            {
                _hoverMesh = null;
                Repaint();
            }

            GUILayout.BeginArea(panel);
            DrawPanel();
            GUILayout.EndArea();

            if (Event.current.type == EventType.Repaint)
            {
                _panelKeyboardControlForTest = GUIUtility.keyboardControl;
                _panelEditingTextFieldForTest = EditorGUIUtility.editingTextField;
                _panelFocusedControlForTest = GUI.GetNameOfFocusedControl();
            }
        }

        [System.NonSerialized] private Vector2 _panelScreenOriginForTest;
        [System.NonSerialized] private Rect _stepBarRectForTest;
        [System.NonSerialized] private int _panelKeyboardControlForTest;
        [System.NonSerialized] private bool _panelEditingTextFieldForTest;
        [System.NonSerialized] private string _panelFocusedControlForTest;
        [System.NonSerialized] internal System.Action PanelGuiHookForTest;

        private void OnViewportGUI()
        {
            Vector2 size = _viewportContainer != null ? _viewportContainer.layout.size : ViewportRect.size;
            if (float.IsNaN(size.x) || float.IsNaN(size.y) || size.x < 1f || size.y < 1f)
                size = ViewportRect.size;
            DrawViewport(new Rect(0f, 0f, size.x, size.y));
        }

        private void DrawPanel()
        {
            EditorGUILayout.Space(4f);
            EditorGUI.BeginChangeCheck();
            var target = (GameObject)EditorGUILayout.ObjectField(L("t_Wiz_Avatar"), m_target, typeof(GameObject), true);
            MarkForTest("AvatarField");
            if (EditorGUI.EndChangeCheck())
                SetTarget(target);

            if (!string.IsNullOrEmpty(_notice))
                EditorGUILayout.HelpBox(_notice, MessageType.Info);

            if (m_target == null)
            {
                EditorGUILayout.HelpBox(L("t_Wiz_MsgNoAvatar"), MessageType.Info);
                return;
            }
            if (!WizardPreviewSession.IsAcceptableTarget(m_target, out string reason))
            {
                EditorGUILayout.HelpBox(L(reason), MessageType.Error);
                return;
            }

            DrawStepBar();
            EditorGUILayout.LabelField(L(k_stepDescriptionKeys[(int)_step]), EditorStyles.wordWrappedLabel);
            SatabakoEditorUtils.DrawWebButton(L("t_Wiz_OpenDoc"), k_docUrl);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_step)
            {
                case Step.Setup: DrawSetup(); break;
                case Step.Contacts: DrawContacts(); break;
                case Step.Case: DrawCase(); break;
                case Step.Cigarette: DrawCigarette(); break;
                case Step.Lighter: DrawLighter(); break;
                case Step.Save: DrawSave(); break;
            }
            EditorGUILayout.EndScrollView();

            DrawNavigation();
        }

        private void DrawStepBar()
        {
            var names = k_stepKeys.Select(L).ToArray();
            EditorGUI.BeginDisabledGroup(!CanLeaveSetup);
            int selected = GUILayout.Toolbar((int)_step, names);
            if (Event.current.type == EventType.Repaint)
            {
                Rect bar = GUILayoutUtility.GetLastRect();
                _stepBarRectForTest = new Rect(GUIUtility.GUIToScreenPoint(bar.position) - _panelScreenOriginForTest, bar.size);
            }
            EditorGUI.EndDisabledGroup();
            if (selected != (int)_step && (selected == 0 || CanLeaveSetup))
                SetStep((Step)selected);
        }

        private void DrawNavigation()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(_step == Step.Setup);
                if (GUILayout.Button(L("t_Wiz_Back")))
                    SetStep(_step - 1);
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(_step == Step.Save || !CanLeaveSetup);
                if (GUILayout.Button(L("t_Wiz_Next")))
                    SetStep(_step + 1);
                EditorGUI.EndDisabledGroup();
            }
        }

        private static void DrawSectionTitle(string title)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private void DrawGestureToggles()
        {
            EditorGUI.BeginChangeCheck();
            _showGesture = EditorGUILayout.ToggleLeft(L("t_Wiz_ShowGesture"), _showGesture);
            MarkForTest("ShowGesture");
            if (EditorGUI.EndChangeCheck())
            {
                if (!_showGesture)
                    _editGesture = false;
                UpdatePreviewForStep();
            }

            if (!string.IsNullOrEmpty(_previewMessage))
                EditorGUILayout.HelpBox(_previewMessage, MessageType.Warning);

            EditorGUI.BeginChangeCheck();
            _hideOthers = EditorGUILayout.ToggleLeft(L("t_Wiz_HideOthers"), _hideOthers);
            MarkForTest("HideOthers");
            if (EditorGUI.EndChangeCheck())
                UpdateVisibilityForStep();

            EditorGUI.BeginChangeCheck();
            _mirrorBoth = EditorGUILayout.ToggleLeft(new GUIContent(L("t_Wiz_MirrorBoth"), L("t_Wiz_MirrorBothTip")), _mirrorBoth);
            MarkForTest("MirrorBoth");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_prefMirrorBoth, _mirrorBoth);

            if (HandPairForStep(out Transform rightHand, out Transform leftHand))
            {
                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(rightHand == null || leftHand == null))
                {
                    if (GUILayout.Button(new GUIContent(L("t_Wiz_CopyRightToLeft"), L("t_Wiz_CopyHandTip"))))
                        CopyHandPose(true);
                    MarkForTest("CopyRightToLeft");
                    if (GUILayout.Button(new GUIContent(L("t_Wiz_CopyLeftToRight"), L("t_Wiz_CopyHandTip"))))
                        CopyHandPose(false);
                    MarkForTest("CopyLeftToRight");
                }
            }

            DrawGestureField();
        }

        #region Gesture override / edit
        private void DrawGestureField()
        {
            var kind = CurrentGestureKind;
            var refs = Refs;
            if (!kind.HasValue || refs == null)
                return;

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(L("t_Wiz_Gesture"), EditorStyles.boldLabel);

            var over = UnsupportedAvatarOps.FindGestureOverride(refs.Root, kind.Value);
            bool edited = _gestureEdits.ContainsKey(kind.Value);
            if (edited)
            {
                EditorGUILayout.LabelField(L("t_Wiz_GestureEdited"), EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                var current = over != null ? over.CustomMotion as AnimationClip : null;
                var fallback = UnsupportedAvatarOps.DefaultGestureClip(kind.Value);
                EditorGUI.BeginChangeCheck();
                var picked = (AnimationClip)EditorGUILayout.ObjectField(L("t_Wiz_GestureClip"), current != null ? current : fallback, typeof(AnimationClip), false);
                MarkForTest("GestureClip");
                if (EditorGUI.EndChangeCheck())
                    SetGestureOverride(kind.Value, picked == fallback ? null : picked);
                if (current == null)
                    EditorGUILayout.LabelField(L("t_Wiz_GestureDefaultNote"), EditorStyles.miniLabel);
            }

            if (UnsupportedAvatarOps.HasGestureOverrideOutside(refs.AvatarRoot, refs.Root, kind.Value))
                EditorGUILayout.HelpBox(L("t_Wiz_MsgGestureOutside"), MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                bool edit = EditorGUILayout.ToggleLeft(L("t_Wiz_GestureEdit"), _editGesture);
                MarkForTest("GestureEdit");
                if (EditorGUI.EndChangeCheck())
                    SetGestureEditing(kind.Value, edit);

                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(over == null && !edited);
                if (GUILayout.Button(L("t_Wiz_GestureReset")))
                    SetGestureOverride(kind.Value, null);
                MarkForTest("GestureReset");
                EditorGUI.EndDisabledGroup();
            }

            if (_editGesture)
                DrawJointEditor(kind.Value);
        }

        private void DrawJointEditor(GestureKind kind)
        {
            EditorGUILayout.LabelField(L("t_Wiz_GestureHint"), EditorStyles.wordWrappedMiniLabel);

            EditorGUI.BeginChangeCheck();
            _mirrorGesture = EditorGUILayout.ToggleLeft(new GUIContent(L("t_Wiz_MirrorGesture"), L("t_Wiz_MirrorGestureTip")), _mirrorGesture);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_prefMirrorGesture, _mirrorGesture);

            if (_preview == null || !_gestureEdits.TryGetValue(kind, out var muscles) || _selectedJoint == HumanBodyBones.LastBone)
                return;

            EditorGUILayout.LabelField(L("t_Wiz_GestureJoint"), FingerMuscles.JointLabel(_selectedJoint));
            DrawMuscleSlider(kind, L("t_Wiz_GestureStretch"), FingerMuscles.Stretched(_selectedJoint), muscles);
            DrawMuscleSlider(kind, L("t_Wiz_GestureSpread"), FingerMuscles.Spread(_selectedJoint), muscles);
        }

        private void DrawMuscleSlider(GestureKind kind, string label, int muscle, float[] muscles)
        {
            if (muscle < 0)
                return;
            GestureRange(kind, muscle, out float min, out float max);
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider(label, muscles[muscle], min, max);
            if (EditorGUI.EndChangeCheck())
                SetGestureMuscle(muscle, value);
        }

        private void SetGestureEditing(GestureKind kind, bool on)
        {
            _editGesture = on;
            _selectedJoint = HumanBodyBones.LastBone;
            if (on)
            {
                EnsureGestureEdit(kind);
                _showGesture = true;
            }
            RestartPreview();
            Repaint();
        }

        private float[] EnsureGestureEdit(GestureKind kind)
        {
            if (_gestureEdits.TryGetValue(kind, out var muscles))
                return muscles;

            var refs = Refs;
            var clip = UnsupportedAvatarOps.ResolveGestureClip(refs.AvatarRoot, kind, out _, out _);
            muscles = FingerMuscles.FromClip(clip);
            _gestureEdits[kind] = muscles;
            _gestureOriginals[kind] = (float[])muscles.Clone();
            return muscles;
        }

        private void GestureRange(GestureKind kind, int muscle, out float min, out float max)
        {
            min = -1f;
            max = 1f;
            if (_gestureOriginals.TryGetValue(kind, out var original) && muscle < original.Length && !float.IsNaN(original[muscle]))
            {
                min = Mathf.Min(min, original[muscle]);
                max = Mathf.Max(max, original[muscle]);
            }
        }

        private void SetGestureMuscle(int muscle, float value)
        {
            var kind = CurrentGestureKind;
            if (!kind.HasValue || muscle < 0 || !_gestureEdits.TryGetValue(kind.Value, out var muscles) || muscle >= muscles.Length)
                return;

            GestureRange(kind.Value, muscle, out float min, out float max);
            muscles[muscle] = Mathf.Clamp(value, min, max);

            int mirror = _mirrorGesture ? FingerMuscles.Mirror(muscle) : -1;
            if (mirror >= 0 && mirror < muscles.Length)
            {
                GestureRange(kind.Value, mirror, out float mirrorMin, out float mirrorMax);
                muscles[mirror] = Mathf.Clamp(value, mirrorMin, mirrorMax);
            }

            if (_preview != null && _preview.FingerMuscleValues == muscles && FingerPosePreview.Active == _preview)
            {
                _preview.SetFingerMuscle(muscle, muscles[muscle]);
                if (mirror >= 0 && mirror < muscles.Length)
                    _preview.SetFingerMuscle(mirror, muscles[mirror]);
            }
            else
                RestartPreview();
            Repaint();
        }

        private void SetGestureOverride(GestureKind kind, AnimationClip clip)
        {
            UnsupportedAvatarOps.SetGestureOverride(Refs, kind, clip);
            _gestureEdits.Remove(kind);
            _gestureOriginals.Remove(kind);
            _editGesture = false;
            _selectedJoint = HumanBodyBones.LastBone;
            RestartPreview();
            Repaint();
        }

        private static readonly Color k_jointColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color k_jointSelectedColor = new Color(1f, 0.9f, 0.2f, 1f);
        private static readonly Color k_stretchRingColor = new Color(0.35f, 0.65f, 1f, 0.95f);
        private static readonly Color k_spreadRingColor = new Color(0.45f, 1f, 0.45f, 0.95f);

        private void DrawFingerJointGizmos(GestureKind kind)
        {
            var refs = Refs;
            if (refs == null || refs.Animator == null || _preview == null || !_gestureEdits.TryGetValue(kind, out var muscles))
                return;

            var e = Event.current;
            var prev = Handles.color;
            for (int i = (int)HumanBodyBones.LeftThumbProximal; i <= (int)HumanBodyBones.RightLittleDistal; i++)
            {
                var bone = (HumanBodyBones)i;
                var t = HumanoidBones.Get(refs.Animator, bone);
                if (t == null)
                    continue;

                float size = HandleUtility.GetHandleSize(t.position) * 0.06f;
                Handles.color = bone == _selectedJoint ? k_jointSelectedColor : k_jointColor;
                if (Handles.Button(t.position, Quaternion.identity, size, size * 1.5f, Handles.SphereHandleCap))
                {
                    _selectedJoint = bone;
                    for (int d = 0; d < _jointDiscRotation.Length; d++)
                        _jointDiscRotation[d] = Quaternion.identity;
                    Repaint();
                }
                if (e.type == EventType.Layout && bone == HumanBodyBones.RightIndexProximal)
                    _jointGuiLocal = HandleUtility.WorldToGUIPoint(t.position);
            }
            Handles.color = prev;

            if (_selectedJoint != HumanBodyBones.LastBone)
                DrawJointDiscs(refs, muscles);
        }

        private void DrawJointDiscs(TabakoTemplateRefs refs, float[] muscles)
        {
            var joint = HumanoidBones.Get(refs.Animator, _selectedJoint);
            if (joint == null)
                return;

            var e = Event.current;
            Vector3 position = joint.position;
            float size = HandleUtility.GetHandleSize(position) * 0.45f;
            var prev = Handles.color;
            for (int dof = 1; dof <= 2; dof++)
            {
                int muscle = HumanTrait.MuscleFromBone((int)_selectedJoint, dof);
                if (muscle < 0)
                    continue;

                if (e.type == EventType.Layout)
                    _jointDofValid[dof] = _preview.TryGetMuscleAxis(muscle, joint, out _jointDofAxis[dof], out _jointDofDegPerUnit[dof]);
                if (!_jointDofValid[dof])
                    continue;

                Vector3 axis = _jointDofAxis[dof];
                float ringSize = dof == 2 ? size : size * 0.7f;
                if (e.type == EventType.Layout && dof == 2 && Camera.current != null)
                {
                    Vector3 from = Vector3.Cross(axis, Camera.current.transform.forward).normalized;
                    Vector3 onRing = position + Quaternion.AngleAxis(30f, axis) * from * ringSize;
                    Vector3 ahead = position + Quaternion.AngleAxis(35f, axis) * from * ringSize;
                    _stretchRingGuiLocal = HandleUtility.WorldToGUIPoint(onRing);
                    _stretchRingTangentLocal = (HandleUtility.WorldToGUIPoint(ahead) - _stretchRingGuiLocal).normalized;
                }

                Handles.color = dof == 2 ? k_stretchRingColor : k_spreadRingColor;
                Quaternion before = _jointDiscRotation[dof];
                Quaternion after = Handles.Disc(before, position, axis, ringSize, false, 0f);
                if (after != before)
                {
                    _jointDiscRotation[dof] = after;
                    (after * Quaternion.Inverse(before)).ToAngleAxis(out float angle, out Vector3 turned);
                    if (angle > 180f)
                        angle -= 360f;
                    float signed = angle * Mathf.Sign(Vector3.Dot(turned, axis));
                    if (_jointDofDegPerUnit[dof] > 0.01f)
                        SetGestureMuscle(muscle, muscles[muscle] + signed / _jointDofDegPerUnit[dof]);
                }
            }
            Handles.color = prev;
        }

        private List<string> ApplyGestureEdits(TabakoTemplateRefs refs, string folder, string baseName, string embeddedAssetPath)
        {
            var saved = new List<string>();
            if (refs == null || string.IsNullOrEmpty(folder))
                return saved;

            foreach (var pair in _gestureEdits)
            {
                string token = GestureToken(pair.Key);
                string path = $"{folder.Replace('\\', '/').TrimEnd('/')}/hands_{token}_{SanitizeFileName(baseName)}.anim";
                var clip = FingerMuscles.SaveClipAsset(pair.Value, path);
                if (clip == null)
                    continue;
                UnsupportedAvatarOps.SetGestureOverride(refs, pair.Key, clip);
                if (!string.IsNullOrEmpty(embeddedAssetPath))
                    FingerMuscles.RemoveEmbeddedClip(embeddedAssetPath, "hands_" + token);
                saved.Add(path);
            }
            return saved;
        }

        private List<string> EmbedGestureEdits(TabakoTemplateRefs refs, string assetPath)
        {
            var saved = new List<string>();
            if (refs == null || string.IsNullOrEmpty(assetPath) || AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
                return saved;

            foreach (var pair in _gestureEdits)
            {
                string name = "hands_" + GestureToken(pair.Key);
                var clip = FingerMuscles.EmbedClipInAsset(pair.Value, assetPath, name);
                if (clip == null)
                    continue;
                UnsupportedAvatarOps.SetGestureOverride(refs, pair.Key, clip);
                saved.Add($"{assetPath} ({name})");
            }
            return saved;
        }

        private static string GestureToken(GestureKind kind) => kind == GestureKind.Case ? "case" : kind == GestureKind.Tabako ? "tabacco" : "lighter";

        private static void CopyGestureOverrides(TabakoTemplateRefs from, TabakoTemplateRefs to)
        {
            if (from == null || to == null)
                return;
            foreach (GestureKind kind in System.Enum.GetValues(typeof(GestureKind)))
            {
                var o = UnsupportedAvatarOps.FindGestureOverride(from.Root, kind);
                if (o != null && o.CustomMotion != null && EditorUtility.IsPersistent(o.CustomMotion) && UnsupportedAvatarOps.FindGestureOverride(to.Root, kind) == null)
                    UnsupportedAvatarOps.SetGestureOverride(to, kind, o.CustomMotion);
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "prefab";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
        #endregion

        private PalmFrame BuildRightPalm() => PalmFrame.Build(Refs.Animator, false);
        #endregion

        #region Handle targets
        private List<HandleEntry> CurrentHandleEntries()
        {
            _handleEntries.Clear();
            var refs = Refs;
            if (refs == null)
                return _handleEntries;

            switch (_step)
            {
                case Step.Contacts:
                    AddContactEntry(refs.PocketR, null, false);
                    AddContactEntry(refs.PocketL, null, false);
                    AddContactEntry(refs.SenderR, _syncReceivers ? refs.CaseReceiverR : null, true);
                    AddContactEntry(refs.SenderL, _syncReceivers ? refs.CaseReceiverL : null, true);
                    AddTransformEntry(refs.MouthTarget);
                    break;
                case Step.Case:
                    AddTransformEntry(refs.CaseR);
                    AddTransformEntry(refs.CaseL);
                    break;
                case Step.Cigarette:
                    AddTransformEntry(refs.TabakoR);
                    AddTransformEntry(refs.TabakoL);
                    AddTransformEntry(refs.TabakoHead);
                    break;
                case Step.Lighter:
                    AddTransformEntry(refs.LighterR);
                    AddTransformEntry(refs.LighterL);
                    break;
            }
            return _handleEntries;
        }

        private void AddTransformEntry(Transform t)
        {
            if (t != null)
                _handleEntries.Add(new HandleEntry { label = t.name, transform = t });
        }

        private void AddContactEntry(ContactBase contact, ContactBase sync, bool shape)
        {
            if (contact == null)
                return;
            if (shape)
                _handleEntries.Add(new HandleEntry { label = contact.gameObject.name, contact = contact, syncReceiver = sync });
            else
                _handleEntries.Add(new HandleEntry { label = contact.gameObject.name, transform = contact.transform });
        }

        private HandleEntry CurrentHandleEntry()
        {
            var entries = CurrentHandleEntries();
            if (entries.Count == 0)
                return default;
            _handleIndex = Mathf.Clamp(_handleIndex, 0, entries.Count - 1);
            return entries[_handleIndex];
        }

        private void DrawHandleTargetBar()
        {
            var entries = CurrentHandleEntries();
            if (entries.Count == 0)
                return;

            EditorGUILayout.LabelField(L("t_Wiz_HandleTarget"));
            var labels = entries.Select(e => e.label).ToArray();
            _handleIndex = Mathf.Clamp(_handleIndex, 0, entries.Count - 1);
            int selected = GUILayout.SelectionGrid(_handleIndex, labels, Mathf.Min(labels.Length, 3));
            MarkForTest("HandleTargets");
            if (selected != _handleIndex)
                SetHandleIndex(selected, true);
        }
        #endregion

        #region Viewport
        private void DrawViewport(Rect vp)
        {
            if (_viewport == null)
            {
                EditorGUI.DrawRect(vp, new Color(0.22f, 0.22f, 0.22f));
                return;
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                _viewport.EnsureCreated();
            _viewport.DrawGizmos = m_showGizmos;
            _viewport.IgnoreMouseRects.Clear();
            _viewport.IgnoreMouseRects.Add(ViewportToolbarRect());
            var orientation = OrientationGizmoRect(vp);
            if (orientation.HasValue)
                _viewport.IgnoreMouseRects.Add(orientation.Value);
            HandleContactLabelClicks(vp);
            _viewport.OnGUI(vp, ref m_camera);
            DrawContactLabels(vp);
            DrawViewportToolbar(vp);
            DrawViewportPacingHint(vp);
            DrawOrientationGizmo(vp);
            DrawViewportHelpOverlay(vp);

            if (_session == null || !_session.IsValid)
            {
                string message = m_target == null ? L("t_Wiz_MsgNoAvatar") : (_completed ? L("t_Wiz_MsgSaved") : "");
                DrawViewportMessage(vp, message);
            }
        }

        private void DrawViewportMessage(Rect vp, string message)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(message))
                return;

            var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = 13 };
            style.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
            float width = Mathf.Min(vp.width - 32f, Mathf.Max(220f, style.CalcSize(new GUIContent(message)).x + 40f));
            float height = style.CalcHeight(new GUIContent(message), width - 24f) + 24f;
            var box = new Rect(vp.center.x - width * 0.5f, vp.center.y - height * 0.5f, width, height);
            EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(new Rect(box.x + 12f, box.y + 12f, box.width - 24f, box.height - 24f), message, style);
        }

        private const float k_toolButtonSize = 32f;
        private const float k_overlayMargin = 8f;
        private const float k_overlayPad = 4f;
        private const float k_toolbarGap = 10f;
        private static readonly string[] k_viewKeys = { "t_Wiz_View_Fit", "t_Wiz_View_Front", "t_Wiz_View_Hips", "t_Wiz_View_RightHand", "t_Wiz_View_LeftHand", "t_Wiz_View_Mouth" };
        [System.NonSerialized] private GUIContent[] _toolContents;
        [System.NonSerialized] private GUIContent _gizmosContent;
        [System.NonSerialized] private GUIContent _meshSelectContent;
        [System.NonSerialized] private Texture2D _meshSelectIcon;
        [System.NonSerialized] private bool _meshSelectIconLoaded;
        [System.NonSerialized] private Rect _meshSelectButtonRectForTest;
        private const string k_meshSelectIconGuid = "467e5b688a7540c38ec2c5cab7bd2dcc";
        private const string k_meshSelectIcon2xGuid = "3efae9ee452f44cc8c09739fcbb399fb";
        private const string k_meshSelectIconDarkGuid = "0f4594c490af44cf90db02f26ed1225a";
        private const string k_meshSelectIconDark2xGuid = "26fc10dbbf8147a9a7b6e2ee934f4354";
        [System.NonSerialized] private GUIContent[] _viewContents;
        [System.NonSerialized] private float[] _viewWidths;
        [System.NonSerialized] private GUIStyle[] _viewStyles;
        [System.NonSerialized] private float _toolbarButtonHeight;

        private void EnsureToolbarContents()
        {
            if (_toolContents == null)
            {
                _toolContents = new[]
                {
                    new GUIContent(EditorGUIUtility.IconContent("MoveTool").image, L("t_Wiz_Tool_Move") + " (W)"),
                    new GUIContent(EditorGUIUtility.IconContent("RotateTool").image, L("t_Wiz_Tool_Rotate") + " (E)"),
                };
            }
            if (_gizmosContent == null)
            {
                var icon = EditorGUIUtility.IconContent("GizmosToggle");
                _gizmosContent = icon != null && icon.image != null
                    ? new GUIContent(icon.image, L("t_Wiz_ShowGizmos"))
                    : new GUIContent("Gizmos", L("t_Wiz_ShowGizmos"));
            }
            if (_meshSelectContent == null || !_meshSelectIconLoaded)
            {
                _meshSelectIconLoaded = true;
                bool dark = EditorGUIUtility.isProSkin;
                bool hiDpi = EditorGUIUtility.pixelsPerPoint > 1.5f;
                string guid = dark ? (hiDpi ? k_meshSelectIconDark2xGuid : k_meshSelectIconDarkGuid) : (hiDpi ? k_meshSelectIcon2xGuid : k_meshSelectIconGuid);
                _meshSelectIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                _meshSelectContent = _meshSelectIcon != null
                    ? new GUIContent(string.Empty, L("t_Wiz_MeshSelectToggle"))
                    : new GUIContent("Mesh", L("t_Wiz_MeshSelectToggle"));
            }
            if (_viewContents == null || _viewStyles == null || _viewWidths == null || _toolbarButtonHeight <= 0f)
            {
                var command = GUI.skin.FindStyle("Command") ?? EditorStyles.miniButton;
                _toolbarButtonHeight = command.fixedHeight > 0f ? command.fixedHeight : k_toolButtonSize;
                var left = GUI.skin.FindStyle("CommandLeft") ?? command;
                var mid = GUI.skin.FindStyle("CommandMid") ?? command;
                var right = GUI.skin.FindStyle("CommandRight") ?? command;
                _viewContents = new GUIContent[k_viewKeys.Length];
                _viewWidths = new float[k_viewKeys.Length];
                _viewStyles = new GUIStyle[k_viewKeys.Length];
                for (int i = 0; i < k_viewKeys.Length; i++)
                {
                    var source = i == 0 ? left : i == k_viewKeys.Length - 1 ? right : mid;
                    _viewStyles[i] = new GUIStyle(source)
                    {
                        fixedWidth = 0f,
                        fixedHeight = _toolbarButtonHeight,
                        alignment = TextAnchor.MiddleCenter,
                        imagePosition = ImagePosition.TextOnly,
                        wordWrap = false,
                        clipping = TextClipping.Overflow,
                    };
                    if (_viewStyles[i].font == null)
                        _viewStyles[i].font = EditorStyles.miniButton.font;
                    if (_viewStyles[i].fontSize == 0)
                        _viewStyles[i].fontSize = EditorStyles.miniButton.fontSize;
                    _viewStyles[i].normal.textColor = EditorStyles.miniButton.normal.textColor;
                    _viewStyles[i].hover.textColor = EditorStyles.miniButton.normal.textColor;
                    _viewStyles[i].active.textColor = EditorStyles.miniButton.active.textColor;
                    _viewContents[i] = new GUIContent(L(k_viewKeys[i]));
                    _viewWidths[i] = Mathf.Ceil(_viewStyles[i].CalcSize(_viewContents[i]).x) + 8f;
                }
            }
        }

        private float GizmosButtonWidth => _gizmosContent != null && _gizmosContent.image != null ? k_toolButtonSize : 56f;
        private float MeshSelectButtonWidth => _meshSelectIcon != null ? k_toolButtonSize : 48f;

        private Rect ViewportToolbarRect()
        {
            EnsureToolbarContents();
            float views = 0f;
            foreach (float width in _viewWidths)
                views += width;
            float total = k_overlayPad + k_toolButtonSize * 2f + k_toolbarGap + GizmosButtonWidth + k_toolbarGap + MeshSelectButtonWidth + k_toolbarGap + views + k_overlayPad;
            return new Rect(k_overlayMargin, k_overlayMargin, total, _toolbarButtonHeight + k_overlayPad * 2f);
        }

        private void DrawViewportToolbar(Rect vp)
        {
            EnsureToolbarContents();
            var local = ViewportToolbarRect();
            var box = new Rect(vp.x + local.x, vp.y + local.y, local.width, local.height);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.45f));

            var command = GUI.skin.FindStyle("Command") ?? EditorStyles.miniButton;
            float x = box.x + k_overlayPad;
            float y = box.y + k_overlayPad;

            float h = _toolbarButtonHeight;

            var tools = new Rect(x, y, k_toolButtonSize * 2f, h);
            int tool = GUI.Toolbar(tools, (int)_tool, _toolContents, command);
            if (tool != (int)_tool)
            {
                _tool = (PreviewViewport.Tool)tool;
                Repaint();
            }
            x += tools.width + k_toolbarGap;

            var gizmoRect = new Rect(x, y, GizmosButtonWidth, h);
            bool gizmos = GUI.Toggle(gizmoRect, m_showGizmos, _gizmosContent, command);
            if (gizmos != m_showGizmos)
            {
                m_showGizmos = gizmos;
                Repaint();
            }
            x += gizmoRect.width + k_toolbarGap;

            var meshRect = new Rect(x, y, MeshSelectButtonWidth, h);
            _meshSelectButtonRectForTest = new Rect(meshRect.x - vp.x, meshRect.y - vp.y, meshRect.width, meshRect.height);
            bool meshSelect = GUI.Toggle(meshRect, _meshSelect, _meshSelectContent, _meshSelectIcon != null ? command : EditorStyles.miniButton);
            if (_meshSelectIcon != null && Event.current.type == EventType.Repaint)
            {
                const float iconSize = 16f;
                var iconRect = new Rect(meshRect.center.x - iconSize / 2f, meshRect.center.y - iconSize / 2f, iconSize, iconSize);
                GUI.DrawTexture(iconRect, _meshSelectIcon, ScaleMode.ScaleToFit, true);
            }
            if (meshSelect != _meshSelect)
                SetMeshSelect(meshSelect);
            x += meshRect.width + k_toolbarGap;

            for (int i = 0; i < _viewContents.Length; i++)
            {
                if (GUI.Button(new Rect(x, y, _viewWidths[i], h), _viewContents[i], _viewStyles[i]))
                    ApplyViewPreset(i);
                x += _viewWidths[i];
            }
        }

        private void ApplyViewPreset(int index)
        {
            _hoverMesh = null;
            switch (index)
            {
                case 0: ViewFit(); break;
                case 1: ViewFront(); break;
                case 2: ViewHips(180f); break;
                case 3: ViewRightHand(); break;
                case 4: ViewLeftHand(); break;
                default: ViewMouth(); break;
            }
            Repaint();
        }

        private void DrawViewportHelpOverlay(Rect vp)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (_helpStyle == null)
            {
                _helpStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = false,
                    richText = false,
                };
                _helpStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
            }

            const float pad = 8f;
            const float margin = 8f;
            float lineHeight = _helpStyle.lineHeight + 2f;
            float width = 0f;
            var lines = new List<string>(k_helpKeys.Length + 1);
            foreach (var key in k_helpKeys)
                lines.Add(L(key));
            if (_meshSelect)
            {
                foreach (var key in k_helpMeshKeys)
                    lines.Add(L(key));
            }
            if (_hiddenMeshes.Count > 0)
                lines.Add(string.Format(L("t_Wiz_Help_MeshHidden"), _hiddenMeshes.Count));
            foreach (var text in lines)
                width = Mathf.Max(width, _helpStyle.CalcSize(new GUIContent(text)).x);

            float top = vp.y + margin;
            var orientation = OrientationGizmoRect(vp);
            if (orientation.HasValue)
                top = orientation.Value.yMax + margin;
            var box = new Rect(vp.xMax - width - pad * 2f - margin, top, width + pad * 2f, lineHeight * lines.Count + pad * 2f);
            if (box.width > vp.width - margin * 2f || box.yMax > vp.yMax - margin)
                return;
            if (box.x < vp.x + ViewportToolbarRect().xMax + margin)
                return;

            EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.45f));
            var border = new Color(1f, 1f, 1f, 0.22f);
            EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, 1f), border);
            EditorGUI.DrawRect(new Rect(box.x, box.yMax - 1f, box.width, 1f), border);
            EditorGUI.DrawRect(new Rect(box.x, box.y, 1f, box.height), border);
            EditorGUI.DrawRect(new Rect(box.xMax - 1f, box.y, 1f, box.height), border);

            var line = new Rect(box.x + pad, box.y + pad, width, lineHeight);
            foreach (var text in lines)
            {
                GUI.Label(line, text, _helpStyle);
                line.y += lineHeight;
            }
        }

        private void DrawViewportPacingHint(Rect vp)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (_pacingHintStyle == null)
            {
                _pacingHintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    richText = false,
                };
                _pacingHintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
            }

            const float pad = 6f;
            float left = vp.x + ViewportToolbarRect().xMax + k_overlayMargin;
            var orientation = OrientationGizmoRect(vp);
            float right = (orientation.HasValue ? orientation.Value.x : vp.xMax) - k_overlayMargin;
            float available = right - left;
            if (available < 120f)
                return;

            var content = new GUIContent(L("t_Wiz_PacingHint"));
            float width = Mathf.Min(available, _pacingHintStyle.CalcSize(content).x + pad * 2f);
            float height = _pacingHintStyle.CalcHeight(content, width - pad * 2f) + pad * 2f;
            var box = new Rect(left + (available - width) * 0.5f, vp.y + k_overlayMargin, width, height);
            if (box.yMax > vp.yMax - k_overlayMargin)
                return;

            EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.45f));
            var border = new Color(1f, 1f, 1f, 0.22f);
            EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, 1f), border);
            EditorGUI.DrawRect(new Rect(box.x, box.yMax - 1f, box.width, 1f), border);
            EditorGUI.DrawRect(new Rect(box.x, box.y, 1f, box.height), border);
            EditorGUI.DrawRect(new Rect(box.xMax - 1f, box.y, 1f, box.height), border);
            GUI.Label(new Rect(box.x + pad, box.y + pad, box.width - pad * 2f, box.height - pad * 2f), content, _pacingHintStyle);
        }

        private const float k_orientationSize = 84f;
        private const float k_orientationArm = 26f;
        private const float k_orientationTipRadius = 7f;
        private const float k_orientationCenterRadius = 5.5f;
        private const float k_orientationLabelHeight = 14f;
        private static readonly Vector3[] k_orientationAxes = { Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back };
        private static readonly Vector2[] k_orientationOrbits = { new Vector2(0f, 270f), new Vector2(90f, 0f), new Vector2(0f, 180f), new Vector2(0f, 90f), new Vector2(-90f, 0f), new Vector2(0f, 0f) };
        private static readonly string[] k_orientationNames = { "Right", "Top", "Back", "Left", "Bottom", "Front" };
        private static readonly string[] k_orientationLetters = { "X", "Y", "Z" };
        [System.NonSerialized] private int _orientationHover = -1;
        [System.NonSerialized] private GUIStyle _orientationLetterStyle;
        [System.NonSerialized] private GUIStyle _orientationLabelStyle;

        private Rect? OrientationGizmoRect(Rect vp)
        {
            float height = k_orientationSize + k_orientationLabelHeight;
            var rect = new Rect(vp.xMax - k_overlayMargin - k_orientationSize, vp.y + k_overlayMargin, k_orientationSize, height);
            if (rect.x < vp.x + ViewportToolbarRect().xMax + k_overlayMargin || rect.yMax > vp.yMax - k_overlayMargin)
                return null;
            return rect;
        }

        private Vector2 OrientationTip(Vector2 center, int axis)
        {
            Vector3 view = Quaternion.Inverse(m_camera.Rotation) * k_orientationAxes[axis];
            return center + new Vector2(view.x, -view.y) * k_orientationArm;
        }

        private int OrientationHit(Rect rect, Vector2 mouse)
        {
            Vector2 center = new Vector2(rect.center.x, rect.y + k_orientationSize * 0.5f);
            if (Vector2.Distance(center, mouse) <= k_orientationCenterRadius + 2f)
                return 6;
            int best = -1;
            float bestDepth = float.MaxValue;
            for (int i = 0; i < 6; i++)
            {
                if (Vector2.Distance(OrientationTip(center, i), mouse) > k_orientationTipRadius + 2f)
                    continue;
                float depth = (Quaternion.Inverse(m_camera.Rotation) * k_orientationAxes[i]).z;
                if (depth < bestDepth)
                {
                    bestDepth = depth;
                    best = i;
                }
            }
            return best;
        }

        private string OrientationViewName()
        {
            for (int i = 0; i < 6; i++)
            {
                if (Quaternion.Angle(m_camera.Rotation, Quaternion.Euler(k_orientationOrbits[i].x, k_orientationOrbits[i].y, 0f)) < 0.5f)
                    return k_orientationNames[i];
            }
            return m_camera.orthographic ? "Iso" : "Persp";
        }

        private void SnapOrientation(int axis)
        {
            m_camera.orbit = k_orientationOrbits[axis];
            _hoverMesh = null;
            Op($"orientation {k_orientationNames[axis]}");
            Repaint();
        }

        private void DrawOrientationGizmo(Rect vp)
        {
            var rectOrNull = OrientationGizmoRect(vp);
            if (!rectOrNull.HasValue)
                return;
            var rect = rectOrNull.Value;
            var e = Event.current;
            Vector2 center = new Vector2(rect.center.x, rect.y + k_orientationSize * 0.5f);

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                int hover = rect.Contains(e.mousePosition) ? OrientationHit(rect, e.mousePosition) : -1;
                if (hover != _orientationHover)
                {
                    _orientationHover = hover;
                    Repaint();
                }
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && rect.Contains(e.mousePosition))
            {
                int hit = OrientationHit(rect, e.mousePosition);
                if (hit >= 0 && hit < 6)
                    SnapOrientation(hit);
                else if (hit == 6)
                {
                    m_camera.orthographic = !m_camera.orthographic;
                    Op($"orientation {(m_camera.orthographic ? "iso" : "persp")}");
                    Repaint();
                }
                e.Use();
            }
            else if ((e.type == EventType.MouseUp || e.type == EventType.ScrollWheel) && rect.Contains(e.mousePosition))
                e.Use();

            if (e.type != EventType.Repaint)
                return;

            if (_orientationLetterStyle == null)
            {
                _orientationLetterStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
                _orientationLetterStyle.normal.textColor = Color.white;
                _orientationLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                _orientationLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
            }

            var prevColor = Handles.color;
            Handles.color = new Color(0f, 0f, 0f, 0.35f);
            Handles.DrawSolidDisc(new Vector3(center.x, center.y, 0f), Vector3.forward, k_orientationSize * 0.5f - 2f);

            var order = new int[6];
            var depths = new float[6];
            Quaternion inverse = Quaternion.Inverse(m_camera.Rotation);
            for (int i = 0; i < 6; i++)
            {
                order[i] = i;
                depths[i] = (inverse * k_orientationAxes[i]).z;
            }
            System.Array.Sort(depths, order);
            System.Array.Reverse(order);

            bool centerDrawn = false;
            for (int n = 0; n < 6; n++)
            {
                int i = order[n];
                float depth = (inverse * k_orientationAxes[i]).z;
                if (!centerDrawn && depth < 0f)
                {
                    DrawOrientationCenter(center);
                    centerDrawn = true;
                }
                Color axisColor = i % 3 == 0 ? Handles.xAxisColor : i % 3 == 1 ? Handles.yAxisColor : Handles.zAxisColor;
                float alpha = depth > 0f ? 0.65f : 1f;
                Vector2 tip = OrientationTip(center, i);
                var tip3 = new Vector3(tip.x, tip.y, 0f);
                if (i < 3)
                {
                    Handles.color = new Color(axisColor.r, axisColor.g, axisColor.b, alpha);
                    Handles.DrawAAPolyLine(2.5f, new Vector3(center.x, center.y, 0f), tip3);
                    Handles.DrawSolidDisc(tip3, Vector3.forward, k_orientationTipRadius);
                    GUI.Label(new Rect(tip.x - k_orientationTipRadius, tip.y - k_orientationTipRadius - 1f, k_orientationTipRadius * 2f, k_orientationTipRadius * 2f), k_orientationLetters[i], _orientationLetterStyle);
                }
                else
                {
                    Handles.color = new Color(axisColor.r, axisColor.g, axisColor.b, 0.35f * alpha);
                    Handles.DrawSolidDisc(tip3, Vector3.forward, k_orientationTipRadius - 1f);
                    Handles.color = new Color(axisColor.r, axisColor.g, axisColor.b, alpha);
                    Handles.DrawWireDisc(tip3, Vector3.forward, k_orientationTipRadius - 1f);
                }
                if (_orientationHover == i)
                {
                    Handles.color = Color.white;
                    Handles.DrawWireDisc(tip3, Vector3.forward, k_orientationTipRadius + 1.5f);
                }
            }
            if (!centerDrawn)
                DrawOrientationCenter(center);

            Handles.color = prevColor;
            GUI.Label(new Rect(rect.x, rect.yMax - k_orientationLabelHeight, rect.width, k_orientationLabelHeight), OrientationViewName(), _orientationLabelStyle);
        }

        private void DrawOrientationCenter(Vector2 center)
        {
            var center3 = new Vector3(center.x, center.y, 0f);
            Handles.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            Handles.DrawSolidDisc(center3, Vector3.forward, k_orientationCenterRadius);
            if (_orientationHover == 6)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(center3, Vector3.forward, k_orientationCenterRadius + 2f);
            }
        }

        internal Vector2 OrientationGizmoPointForTest(int index)
        {
            var vp = new Rect(0f, 0f, _viewport != null ? _viewport.LastViewportRect.width : ViewportRect.width, _viewport != null ? _viewport.LastViewportRect.height : ViewportRect.height);
            var rect = OrientationGizmoRect(vp);
            if (!rect.HasValue)
                return Vector2.zero;
            Vector2 center = new Vector2(rect.Value.center.x, rect.Value.y + k_orientationSize * 0.5f);
            return index >= 0 && index < 6 ? OrientationTip(center, index) : center;
        }
        internal Vector2 CameraOrbitForTest => m_camera.orbit;
        internal bool CameraOrthographicForTest => m_camera.orthographic;
        internal string OrientationViewNameForTest => OrientationViewName();

        private bool OnViewportKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.W: _tool = PreviewViewport.Tool.Move; Repaint(); return true;
                case KeyCode.E: _tool = PreviewViewport.Tool.Rotate; Repaint(); return true;
                case KeyCode.H:
                    if (Event.current != null && Event.current.alt)
                        UnhideAllMeshes();
                    else
                        HideMesh(_selectedMesh != null ? _selectedMesh : _hoverMesh);
                    return true;
                case KeyCode.Escape:
                    if (_selectedMesh == null)
                        return false;
                    _selectedMesh = null;
                    Repaint();
                    return true;
            }
            return false;
        }

        private Bounds? FrameTargetBounds()
        {
            var entry = CurrentHandleEntry();
            if (entry.IsValid)
                return new Bounds(entry.WorldPosition, Vector3.one * 0.3f);
            return _session?.CloneBounds();
        }

        private void ApplyViewPresetForStep()
        {
            switch (_step)
            {
                case Step.Case:
                case Step.Cigarette:
                case Step.Lighter:
                    ApplyViewForHandle();
                    break;
                default:
                    ViewFit();
                    break;
            }
        }

        private void ApplyViewForHandle()
        {
            var entry = CurrentHandleEntry();
            var refs = Refs;
            Transform t = entry.IsValid ? (entry.transform != null ? entry.transform : entry.contact != null ? entry.contact.transform : null) : null;
            switch (t != null ? TabakoTemplateRefs.BoneOf(t) : HumanBodyBones.RightHand)
            {
                case HumanBodyBones.LeftHand: ViewLeftHand(); break;
                case HumanBodyBones.Head: ViewMouth(); break;
                case HumanBodyBones.Hips:
                    bool left = refs != null && refs.PocketL != null && t == refs.PocketL.transform;
                    ViewHips(left ? 135f : 225f);
                    break;
                default: ViewRightHand(); break;
            }
        }

        private void ViewHips(float yaw)
        {
            var refs = Refs;
            var hips = refs != null ? refs.BoneTransform(HumanBodyBones.Hips) : null;
            if (hips == null)
            {
                ViewFit();
                return;
            }
            PreviewViewport.LookAt(ref m_camera, hips.position, 0.8f, new Vector2(15f, yaw));
        }

        private void ViewFit()
        {
            var bounds = _session?.CloneBounds();
            if (bounds.HasValue)
                PreviewViewport.Frame(ref m_camera, bounds.Value);
        }

        private void ViewFront()
        {
            var bounds = _session?.CloneBounds();
            if (!bounds.HasValue)
                return;
            PreviewViewport.LookAt(ref m_camera, bounds.Value.center, Mathf.Max(0.5f, bounds.Value.extents.magnitude * 2.2f), new Vector2(0f, 180f));
        }

        private void ViewRightHand()
        {
            var refs = Refs;
            var hand = refs != null ? refs.BoneTransform(HumanBodyBones.RightHand) : null;
            if (hand == null)
            {
                ViewFit();
                return;
            }
            PreviewViewport.LookAt(ref m_camera, hand.position, 0.55f, new Vector2(25f, 210f));
        }

        private void ViewLeftHand()
        {
            var refs = Refs;
            var hand = refs != null ? refs.BoneTransform(HumanBodyBones.LeftHand) : null;
            if (hand == null)
            {
                ViewFit();
                return;
            }
            PreviewViewport.LookAt(ref m_camera, hand.position, 0.55f, new Vector2(25f, 150f));
        }

        private void ViewMouth()
        {
            var refs = Refs;
            Transform head = refs != null ? refs.BoneTransform(HumanBodyBones.Head) : null;
            Vector3 pivot = refs != null && refs.MouthTarget != null ? refs.MouthTarget.position : (head != null ? head.position : Vector3.zero);
            PreviewViewport.LookAt(ref m_camera, pivot, 0.45f, new Vector2(5f, 200f));
        }

        private void DrawViewportHandles(Rect local)
        {
            var refs = Refs;
            if (refs == null)
                return;

            var e = Event.current;
            if (e.type == EventType.MouseMove)
                _lastMouseMoveLocal = e.mousePosition;
            if (e.type == EventType.Layout)
                DriveConstraints();
            if (e.type == EventType.Layout && _guiPointRequestForTest != null)
            {
                _guiPointForTest = HandleUtility.WorldToGUIPoint(_guiPointRequestForTest.position);
                _guiPointRequestForTest = null;
            }
            if (e.type == EventType.Layout && _guiWorldPointRequestForTest.HasValue)
            {
                _guiPointForTest = HandleUtility.WorldToGUIPoint(_guiWorldPointRequestForTest.Value);
                _guiWorldPointRequestForTest = null;
            }

            UpdateHoverMesh(refs, local, e);
            DrawMeshHighlights(refs);

            if (_step == Step.Contacts)
                DrawContactShapes(refs);

            if (_editGesture && CurrentGestureKind.HasValue)
                DrawFingerJointGizmos(CurrentGestureKind.Value);
            else
                DrawSelectedHandle(local);

            if (e.type == EventType.Layout && refs.CaseMesh != null)
                _caseMeshGuiLocal = HandleUtility.WorldToGUIPoint(refs.CaseMesh.transform.position);

            if (e.type == EventType.Layout && ViewportProbeForTest != null)
            {
                var probe = ViewportProbeForTest;
                ViewportProbeForTest = null;
                probe(local);
            }

            HandleViewportClick(local, e);

            if (_testPickRequest.HasValue && e.type == EventType.Layout)
            {
                var picked = PickAt(_testPickRequest.Value);
                _lastPickedName = picked != null ? picked.name : "(none)";
                if (picked != null)
                    TrySwitchToObject(refs, picked);
                _testPickRequest = null;
            }
        }

        private void DrawSelectedHandle(Rect local)
        {
            var entry = CurrentHandleEntry();
            if (!entry.IsValid)
            {
                _handleGuiValid = false;
                return;
            }

            var e = Event.current;
            Vector3 position = entry.WorldPosition;
            Quaternion rotation = entry.WorldRotation;

            if (e.type == EventType.Layout)
            {
                float size = HandleUtility.GetHandleSize(position);
                _handleCenterLocal = HandleUtility.WorldToGUIPoint(position);
                _handleXArrowLocal = HandleUtility.WorldToGUIPoint(position + rotation * Vector3.right * size * 0.8f);
                _handleGuiValid = local.Contains(_handleCenterLocal) && local.Contains(_handleXArrowLocal);

                if (Camera.current != null)
                {
                    Vector3 axis = rotation * Vector3.right;
                    Vector3 from = Vector3.Cross(axis, Camera.current.transform.forward).normalized;
                    Vector3 onRing = position + Quaternion.AngleAxis(30f, axis) * from * size;
                    Vector3 ahead = position + Quaternion.AngleAxis(35f, axis) * from * size;
                    _ringXGuiLocal = HandleUtility.WorldToGUIPoint(onRing);
                    _ringXTangentGuiLocal = (HandleUtility.WorldToGUIPoint(ahead) - _ringXGuiLocal).normalized;
                }
            }

            Handles.color = Color.white;
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = position;
            Quaternion newRotation = rotation;
            if (_tool == PreviewViewport.Tool.Rotate)
                newRotation = Handles.RotationHandle(rotation, position);
            else
                newPosition = Handles.PositionHandle(position, rotation);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyHandle(entry, newPosition, newRotation);
                Repaint();
            }
        }

        private void HandleViewportClick(Rect local, Event e)
        {
            if (_editGesture)
                return;
            if (e.type != EventType.MouseDown || e.button != 0 || e.alt || GUIUtility.hotControl != 0 || !local.Contains(e.mousePosition))
                return;

            var refs = Refs;
            if (refs == null)
                return;

            var mesh = PickMeshAt(refs, e.mousePosition);
            _lastPickedName = mesh != null ? mesh.gameObject.name : "(none)";
            if (mesh != null && IsItemRenderer(refs, mesh))
            {
                TrySwitchToObject(refs, mesh.gameObject);
                if (_meshSelect)
                    _selectedMesh = mesh;
                e.Use();
                Repaint();
                return;
            }
            int contactHit = HitTestContact(refs, e.mousePosition);
            if (contactHit >= 0)
            {
                SelectContact(contactHit);
                e.Use();
                return;
            }

            if (!_meshSelect)
                return;

            if (mesh != _selectedMesh)
            {
                _selectedMesh = mesh;
                Repaint();
            }
            if (mesh != null)
                e.Use();
        }

        private GameObject PickAt(Vector2 local)
        {
            var refs = Refs;
            if (refs == null)
                return null;

            var filter = new List<GameObject>();
            AddRendererObjects(refs.CaseMesh, filter);
            AddRendererObjects(refs.TabakoMesh, filter);
            AddRendererObjects(refs.LighterMesh, filter);
            if (filter.Count == 0)
                return null;

            return HandleUtility.PickGameObject(local, false, null, filter.ToArray(), out _);
        }

        private static void AddRendererObjects(GameObject root, List<GameObject> list)
        {
            if (root == null)
                return;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    list.Add(r.gameObject);
            }
        }

        private bool TrySwitchToObject(TabakoTemplateRefs refs, GameObject picked)
        {
            Transform t = picked.transform;
            if (refs.CaseMesh != null && t.IsChildOf(refs.CaseMesh.transform))
            {
                GoToStepByClick(Step.Case, NearerHandIndex(refs.CaseMesh, refs.CaseR, refs.CaseL, 0));
                return true;
            }
            if (refs.TabakoMesh != null && t.IsChildOf(refs.TabakoMesh.transform))
            {
                GoToStepByClick(Step.Cigarette, NearerHandIndex(refs.TabakoMesh, refs.TabakoR, refs.TabakoL, 0));
                return true;
            }
            if (refs.LighterMesh != null && t.IsChildOf(refs.LighterMesh.transform))
            {
                GoToStepByClick(Step.Lighter, NearerHandIndex(refs.LighterMesh, refs.LighterR, refs.LighterL, 0));
                return true;
            }
            return false;
        }

        private static int NearerHandIndex(GameObject mesh, Transform right, Transform left, int firstIndex)
        {
            if (mesh == null || right == null || left == null)
                return firstIndex;

            Vector3 p = mesh.transform.position;
            return Vector3.Distance(p, right.position) <= Vector3.Distance(p, left.position) ? firstIndex : firstIndex + 1;
        }

        private void GoToStepByClick(Step step, int handleIndex)
        {
            bool changed = step != _step || handleIndex != _handleIndex;
            if (step != _step)
                SetStep(step, false);
            SetHandleIndex(handleIndex, changed);
        }

        private void ApplyHandle(HandleEntry entry, Vector3 position, Quaternion rotation)
        {
            _selectedMesh = null;
            if (entry.contact != null)
            {
                Transform bone = entry.contact.transform;
                Vector3 localPosition = ContactSpace.WorldToLocal(bone, position);
                Quaternion localRotation = Quaternion.Inverse(bone.rotation) * rotation;
                UnsupportedAvatarOps.SetContactShape(entry.contact, localPosition, localRotation, entry.contact.radius, entry.contact.height);
                if (entry.syncReceiver != null)
                    UnsupportedAvatarOps.CopyContactShape(entry.contact, entry.syncReceiver);
                if (_mirrorContacts)
                    MirrorContactToOtherHand(entry.contact);
                return;
            }

            if (entry.transform == null)
                return;

            UnsupportedAvatarOps.RecordTransform(entry.transform, "Move");
            Vector3 previous = entry.transform.position;
            entry.transform.SetPositionAndRotation(position, rotation);
            UnsupportedAvatarOps.CommitTransform(entry.transform);
            UnsupportedAvatarOps.TransformMoved?.Invoke(entry.transform);
            Op($"move {entry.transform.name} {(position - previous).magnitude * 1000f:F0}mm");

            if (_mirrorBoth)
            {
                var other = MirrorCounterpart(entry.transform);
                if (other != null)
                    UnsupportedAvatarOps.MirrorToLeft(entry.transform, other, Refs.Root);
            }
            if (_mirrorContacts && _step == Step.Contacts)
            {
                var otherPocket = PocketCounterpart(entry.transform);
                if (otherPocket != null)
                    UnsupportedAvatarOps.MirrorToLeft(entry.transform, otherPocket, Refs.Root);
            }
            UnsupportedAvatarOps.RefreshScene();
        }

        private Transform PocketCounterpart(Transform target)
        {
            var refs = Refs;
            if (refs == null || target == null || refs.PocketR == null || refs.PocketL == null)
                return null;
            if (target == refs.PocketR.transform) return refs.PocketL.transform;
            if (target == refs.PocketL.transform) return refs.PocketR.transform;
            return null;
        }

        private void MirrorContactToOtherHand(ContactBase contact)
        {
            var refs = Refs;
            if (refs == null || contact == null)
                return;
            bool fromLeft;
            ContactBase other;
            ContactBase otherReceiver;
            if (contact == refs.SenderR)
            {
                fromLeft = false;
                other = refs.SenderL;
                otherReceiver = refs.CaseReceiverL;
            }
            else if (contact == refs.SenderL)
            {
                fromLeft = true;
                other = refs.SenderR;
                otherReceiver = refs.CaseReceiverR;
            }
            else
                return;
            if (other == null)
                return;

            var from = PalmFrame.Build(refs.Animator, fromLeft);
            var to = PalmFrame.Build(refs.Animator, !fromLeft);
            if (from.IsValid && to.IsValid)
            {
                Vector3 worldPosition = ContactSpace.LocalToWorld(contact.transform, contact.position);
                Quaternion worldRotation = contact.transform.rotation * contact.rotation;
                PlacementEstimator.MirrorPalmPose(from, to, worldPosition, worldRotation, out Vector3 mirroredPosition, out Quaternion mirroredRotation);
                UnsupportedAvatarOps.SetContactShape(other, ContactSpace.WorldToLocal(other.transform, mirroredPosition), Quaternion.Inverse(other.transform.rotation) * mirroredRotation, contact.radius, contact.height, contact.shapeType);
            }
            else
                UnsupportedAvatarOps.CopyContactShape(contact, other);
            if (_syncReceivers)
                UnsupportedAvatarOps.CopyContactShape(other, otherReceiver);
            Op($"mirror contact {contact.name} -> {other.name}");
        }

        private bool HandPairForStep(out Transform right, out Transform left)
        {
            right = null;
            left = null;
            var refs = Refs;
            if (refs == null)
                return false;
            switch (_step)
            {
                case Step.Case: right = refs.CaseR; left = refs.CaseL; return true;
                case Step.Cigarette: right = refs.TabakoR; left = refs.TabakoL; return true;
                case Step.Lighter: right = refs.LighterR; left = refs.LighterL; return true;
            }
            return false;
        }

        private void CopyHandPose(bool rightToLeft)
        {
            var refs = Refs;
            if (refs == null || !HandPairForStep(out Transform right, out Transform left) || right == null || left == null)
                return;
            Transform from = rightToLeft ? right : left;
            Transform to = rightToLeft ? left : right;

            EndPanelTextEditing();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            UnsupportedAvatarOps.MirrorToLeft(from, to, refs.Root);
            Undo.FlushUndoRecordObjects();
            Undo.SetCurrentGroupName(rightToLeft ? "Copy right hand to left" : "Copy left hand to right");
            Undo.CollapseUndoOperations(group);
            Op($"copy {from.name} -> {to.name}");
            _selectedMesh = null;
            Repaint();
        }
        internal void CopyHandPoseForTest(bool rightToLeft) => CopyHandPose(rightToLeft);

        private Transform MirrorCounterpart(Transform target)
        {
            var refs = Refs;
            if (refs == null || target == null)
                return null;
            if (target == refs.CaseR) return refs.CaseL;
            if (target == refs.CaseL) return refs.CaseR;
            if (target == refs.TabakoR) return refs.TabakoL;
            if (target == refs.TabakoL) return refs.TabakoR;
            if (target == refs.LighterR) return refs.LighterL;
            if (target == refs.LighterL) return refs.LighterR;
            return null;
        }

        private static readonly Color k_senderRColor = new Color(0.30f, 0.62f, 1f);
        private static readonly Color k_senderLColor = new Color(0.35f, 0.90f, 0.85f);
        private static readonly Color k_pocketRColor = new Color(1f, 0.58f, 0.20f);
        private static readonly Color k_pocketLColor = new Color(1f, 0.85f, 0.30f);
        private static readonly Color k_mouthColor = new Color(1f, 0.45f, 0.65f);

        private struct ContactLabel
        {
            public string text;
            public Vector2 edge;
            public Vector2 anchor;
            public Color color;
            public bool selected;
            public ContactBase contact;
            public Transform target;
            public Rect box;
        }

        private readonly List<ContactLabel> _contactLabels = new List<ContactLabel>();
        private GUIStyle _contactLabelStyle;
        private GUIStyle _contactLabelSelectedStyle;

        private void DrawContactShapes(TabakoTemplateRefs refs)
        {
            if (Event.current.type != EventType.Repaint || Camera.current == null)
                return;

            var entry = CurrentHandleEntry();
            var entries = CurrentHandleEntries();
            var prevColor = Handles.color;
            var prevZTest = Handles.zTest;

            bool frame = _selectedMesh == null;
            int hover = _hoverContact;

            var previous = _contactLabels.Count > 0 ? _contactLabels.ToArray() : null;
            _contactLabels.Clear();
            DrawCapsule(refs.SenderR, k_senderRColor, frame && IsSelected(entry, refs.SenderR), hover >= 0 && hover == IndexOfContactEntry(entries, refs.SenderR), true);
            DrawCapsule(refs.SenderL, k_senderLColor, frame && IsSelected(entry, refs.SenderL), hover >= 0 && hover == IndexOfContactEntry(entries, refs.SenderL), false);
            DrawSphere(refs.PocketR, k_pocketRColor, frame && IsSelected(entry, refs.PocketR), hover >= 0 && hover == IndexOfContactEntry(entries, refs.PocketR), true);
            DrawSphere(refs.PocketL, k_pocketLColor, frame && IsSelected(entry, refs.PocketL), hover >= 0 && hover == IndexOfContactEntry(entries, refs.PocketL), false);
            DrawMouthMark(refs, entry, frame, hover >= 0 && hover == IndexOfTransformEntry(entries, refs.MouthTarget));

            if (previous != null)
            {
                for (int i = 0; i < _contactLabels.Count; i++)
                {
                    foreach (var old in previous)
                    {
                        if (old.contact == _contactLabels[i].contact && old.target == _contactLabels[i].target)
                        {
                            var l = _contactLabels[i];
                            l.box = old.box;
                            _contactLabels[i] = l;
                            break;
                        }
                    }
                }
            }

            Handles.zTest = prevZTest;
            Handles.color = prevColor;
        }

        private static int IndexOfContactEntry(List<HandleEntry> entries, ContactBase contact)
        {
            if (contact == null)
                return -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].contact == contact || (entries[i].contact == null && entries[i].transform == contact.transform))
                    return i;
            }
            return -1;
        }

        private static int IndexOfTransformEntry(List<HandleEntry> entries, Transform target)
        {
            if (target == null)
                return -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].contact == null && entries[i].transform == target)
                    return i;
            }
            return -1;
        }

        private static Color Emphasize(Color color, bool hovered) => hovered ? Color.Lerp(color, Color.white, 0.4f) : color;

        private static bool IsSelected(HandleEntry entry, ContactBase contact)
        {
            if (contact == null || !entry.IsValid)
                return false;
            return entry.contact == contact || (entry.contact == null && entry.transform == contact.transform);
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, Mathf.Clamp01(a));

        private void DrawSphere(ContactBase contact, Color color, bool selected, bool hovered, bool rightSide)
        {
            if (contact == null)
                return;

            Vector3 center = ContactSpace.LocalToWorld(contact.transform, contact.position);
            float r = contact.radius;
            Vector3 camForward = Camera.current.transform.forward;
            float thickness = selected ? 3f : hovered ? 2.5f : 1.5f;
            float fill = selected ? 0.22f : hovered ? 0.2f : 0.12f;
            float line = selected || hovered ? 1f : 0.7f;
            Color labelColor = color;
            color = Emphasize(color, hovered);

            for (int pass = 0; pass < 2; pass++)
            {
                bool hidden = pass == 1;
                Handles.zTest = hidden ? CompareFunction.Greater : CompareFunction.LessEqual;
                float k = hidden ? 0.3f : 1f;
                Handles.color = WithAlpha(color, fill * k);
                Handles.DrawSolidDisc(center, camForward, r);
                Handles.color = WithAlpha(color, line * k);
                Handles.DrawWireDisc(center, camForward, r, thickness);
                Handles.color = WithAlpha(color, line * 0.5f * k);
                Handles.DrawWireDisc(center, Vector3.up, r, 1f);
            }
            Handles.zTest = CompareFunction.Always;

            AddContactLabel($"{contact.gameObject.name}  r {r * 100f:F1} cm", center, r, labelColor, selected, rightSide, contact, null);
        }

        private void DrawCapsule(ContactBase contact, Color color, bool selected, bool hovered, bool rightSide)
        {
            if (contact == null)
                return;
            Color labelColor = color;
            color = Emphasize(color, hovered);

            Vector3 center = ContactSpace.LocalToWorld(contact.transform, contact.position);
            Quaternion rotation = contact.transform.rotation * contact.rotation;
            Vector3 axis = rotation * Vector3.up;
            float r = contact.radius;
            float half = Mathf.Max(0f, contact.height * 0.5f - r);
            Vector3 a = center - axis * half;
            Vector3 b = center + axis * half;

            var cam = Camera.current.transform;
            Vector3 side = Vector3.Cross(axis, cam.forward);
            if (side.sqrMagnitude < 1e-6f)
                side = Vector3.Cross(axis, cam.up);
            side = side.normalized * r;
            var quad = new[] { a - side, a + side, b + side, b - side };

            float thickness = selected ? 3f : hovered ? 2.5f : 1.5f;
            float fill = selected ? 0.22f : hovered ? 0.2f : 0.12f;
            float line = selected || hovered ? 1f : 0.7f;

            for (int pass = 0; pass < 2; pass++)
            {
                bool hidden = pass == 1;
                Handles.zTest = hidden ? CompareFunction.Greater : CompareFunction.LessEqual;
                float k = hidden ? 0.3f : 1f;

                Handles.color = WithAlpha(color, fill * k);
                Handles.DrawSolidDisc(a, cam.forward, r);
                Handles.DrawSolidDisc(b, cam.forward, r);
                if (half > 0f)
                    Handles.DrawSolidRectangleWithOutline(quad, WithAlpha(color, fill * k), Color.clear);

                Handles.color = WithAlpha(color, line * k);
                Handles.DrawWireDisc(a, cam.forward, r, thickness);
                Handles.DrawWireDisc(b, cam.forward, r, thickness);
                if (half > 0f)
                {
                    Handles.DrawLine(quad[0], quad[3], thickness);
                    Handles.DrawLine(quad[1], quad[2], thickness);
                }

                Handles.color = WithAlpha(color, line * 0.5f * k);
                Handles.DrawWireDisc(a, axis, r, 1f);
                Handles.DrawWireDisc(b, axis, r, 1f);
            }
            Handles.zTest = CompareFunction.Always;

            AddContactLabel($"{contact.gameObject.name}  r {r * 100f:F1} cm  h {contact.height * 100f:F1} cm", center, half + r, labelColor, selected, rightSide, contact, null);
        }

        private void DrawMouthMark(TabakoTemplateRefs refs, HandleEntry entry, bool frame, bool hovered)
        {
            var mouth = refs.MouthTarget;
            if (mouth == null)
                return;

            bool selected = frame && entry.IsValid && entry.transform == mouth;
            var cam = Camera.current.transform;
            const float r = 0.012f;
            Color color = Emphasize(k_mouthColor, hovered);
            Handles.zTest = CompareFunction.Always;
            Handles.color = WithAlpha(color, selected ? 0.35f : hovered ? 0.3f : 0.2f);
            Handles.DrawSolidDisc(mouth.position, cam.forward, r);
            Handles.color = WithAlpha(color, selected || hovered ? 1f : 0.8f);
            Handles.DrawWireDisc(mouth.position, cam.forward, r, selected ? 3f : hovered ? 2.5f : 1.5f);
            AddContactLabel(mouth.name, mouth.position, r, k_mouthColor, selected, true, null, mouth);
        }

        private void AddContactLabel(string text, Vector3 center, float worldRadius, Color color, bool selected, bool rightSide, ContactBase contact, Transform target)
        {
            var cam = Camera.current.transform;
            Vector2 c = HandleUtility.WorldToGUIPoint(center);
            float screenRadius = (HandleUtility.WorldToGUIPoint(center + cam.right * worldRadius) - c).magnitude;
            Vector2 dir = new Vector2(rightSide ? 1f : -1f, -1f).normalized;
            Vector2 edge = c + dir * screenRadius;
            _contactLabels.Add(new ContactLabel
            {
                text = text,
                edge = edge,
                anchor = edge + dir * 22f,
                color = color,
                selected = selected,
                contact = contact,
                target = target,
            });
        }

        private void DrawContactLabels(Rect vp)
        {
            if (Event.current.type != EventType.Repaint || _step != Step.Contacts || _contactLabels.Count == 0)
                return;

            if (_contactLabelStyle == null)
            {
                _contactLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
                _contactLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
                _contactLabelSelectedStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleLeft };
                _contactLabelSelectedStyle.normal.textColor = Color.white;
            }

            Vector2 mouse = Event.current.mousePosition - vp.position;
            var entries = CurrentHandleEntries();
            var prevHandles = Handles.color;
            var prevGui = GUI.color;
            for (int i = 0; i < _contactLabels.Count; i++)
            {
                var label = _contactLabels[i];
                int entryIndex = label.contact != null ? IndexOfContactEntry(entries, label.contact) : IndexOfTransformEntry(entries, label.target);
                var style = label.selected ? _contactLabelSelectedStyle : _contactLabelStyle;
                var content = new GUIContent(label.text);
                Vector2 size = style.CalcSize(content) + new Vector2(14f, 4f);
                bool right = label.anchor.x >= label.edge.x;
                var box = new Rect(right ? label.anchor.x : label.anchor.x - size.x, label.anchor.y - size.y, size.x, size.y);
                box.x = Mathf.Clamp(box.x, 2f, Mathf.Max(2f, vp.width - size.x - 2f));
                box.y = Mathf.Clamp(box.y, 2f, Mathf.Max(2f, vp.height - size.y - 2f));
                label.box = box;
                _contactLabels[i] = label;

                bool hovered = box.Contains(mouse) || (_hoverContact >= 0 && _hoverContact == entryIndex);
                float fade = hovered ? 1f : 0.5f;

                Handles.color = WithAlpha(label.color, (label.selected ? 1f : 0.7f) * fade);
                Vector2 from = vp.position + label.edge;
                Vector2 to = vp.position + new Vector2(right ? box.x : box.xMax, box.yMax);
                Handles.DrawAAPolyLine(label.selected ? 2.5f : 1.5f, new Vector3(from.x, from.y, 0f), new Vector3(to.x, to.y, 0f));

                var drawBox = new Rect(vp.position + box.position, box.size);
                GUI.color = new Color(1f, 1f, 1f, fade);
                EditorGUI.DrawRect(drawBox, new Color(0f, 0f, 0f, label.selected ? 0.7f : 0.5f));
                EditorGUI.DrawRect(new Rect(drawBox.x, drawBox.y, 3f, drawBox.height), label.color);
                GUI.Label(new Rect(drawBox.x + 7f, drawBox.y + 2f, size.x - 9f, size.y - 4f), content, style);
                GUI.color = prevGui;
                EditorGUIUtility.AddCursorRect(drawBox, MouseCursor.Link);
            }
            Handles.color = prevHandles;
        }

        private void HandleContactLabelClicks(Rect vp)
        {
            var e = Event.current;
            if (_step != Step.Contacts || e.type != EventType.MouseDown || e.button != 0 || _contactLabels.Count == 0)
                return;

            Vector2 mouse = e.mousePosition - vp.position;
            foreach (var label in _contactLabels)
            {
                if (label.box.width <= 0f || !label.box.Contains(mouse))
                    continue;

                var entries = CurrentHandleEntries();
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    bool match = label.contact != null
                        ? entry.contact == label.contact || (entry.contact == null && entry.transform == label.contact.transform)
                        : label.target != null && entry.transform == label.target;
                    if (!match)
                        continue;
                    SelectContact(i);
                    break;
                }
                e.Use();
                Repaint();
                return;
            }
        }

        private static void DrawLegendRow(Color right, Color left, string text)
        {
            var rect = EditorGUILayout.GetControlRect(false, 14f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 2f, 10f, 10f), right);
            EditorGUI.DrawRect(new Rect(rect.x + 12f, rect.y + 2f, 10f, 10f), left);
            GUI.Label(new Rect(rect.x + 28f, rect.y, rect.width - 28f, rect.height), text, EditorStyles.miniLabel);
        }
        #endregion

        #region Step: Setup
        private void DrawSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorGUILayout.HelpBox(L("t_Wiz_MsgPlayMode"), MessageType.Error);

            EditorGUILayout.HelpBox(L("t_Wiz_MsgPreviewNote"), MessageType.None);

            if (_session == null)
                return;
            if (!string.IsNullOrEmpty(_session.Error))
            {
                EditorGUILayout.HelpBox(L("t_Wiz_MsgSessionError") + "\n" + _session.Error, MessageType.Error);
                return;
            }

            if (_session.ScriptCount > 1)
            {
                EditorGUILayout.HelpBox(L("t_Wiz_MsgMultipleTemplates"), MessageType.Error);
                return;
            }

            var refs = Refs;
            if (refs == null)
                return;

            if (!refs.IsHumanoid)
                EditorGUILayout.HelpBox(L("t_Wiz_MsgNotHumanoid"), MessageType.Error);

            EditorGUI.BeginChangeCheck();
            var pickedTemplate = EditorGUILayout.ObjectField(L("t_Wiz_TemplateField"), refs.Script, typeof(TabakoScriptBehaviour), false);
            MarkForTest("TemplateField");
            if (EditorGUI.EndChangeCheck() && pickedTemplate != null && pickedTemplate != refs.Script)
                ReplaceTemplate(pickedTemplate, true);
            if (!refs.IsComplete)
                EditorGUILayout.HelpBox("テンプレートの参照が壊れています。unitypackage を再インポートしてプレハブを入れ直してください。", MessageType.Error);

            if (_session.SourceTemplateRoot == null && !_session.SourceIsAsset)
                EditorGUILayout.HelpBox(L("t_Wiz_MsgTemplateAdded"), MessageType.Info);

            switch (_session.SaveMode)
            {
                case PrefabSaveMode.NewVariant:
                    EditorGUILayout.LabelField(L("t_Wiz_MsgNewVariant"), EditorStyles.wordWrappedMiniLabel);
                    break;
                case PrefabSaveMode.UpdateUserVariant:
                    EditorGUILayout.LabelField(L("t_Wiz_MsgUpdateVariant"), EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField("Prefab", _session.SaveModeAssetPath);
                    break;
                case PrefabSaveMode.UpdateVendorVariant:
                    EditorGUILayout.HelpBox(L("t_Wiz_MsgVendorVariant") + "\n" + _session.SaveModeAssetPath, MessageType.Warning);
                    break;
            }
            if (_session.SourceIsAsset)
                EditorGUILayout.HelpBox(L("t_Wiz_MsgAssetTarget"), MessageType.Info);

            if (refs.IsHumanoid)
            {
                var right = PalmFrame.Build(refs.Animator, false);
                var left = PalmFrame.Build(refs.Animator, true);
                if (right.FingerBoneCount < 15 || left.FingerBoneCount < 15)
                    EditorGUILayout.HelpBox(L("t_Wiz_MsgFewFingerBones"), MessageType.Warning);
            }

            if (!UnsupportedAvatarOps.IsConstraintEditModeEnabled)
                EditorGUILayout.HelpBox(L("t_Wiz_MsgConstraintsOff"), MessageType.Warning);

            DrawMergeArmatureWarning(refs);

            if (CanLeaveSetup)
            {
                if (_advancedMode)
                    DrawCopyValuesSection(refs);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(L("t_Wiz_EstimateEverythingNote"), EditorStyles.wordWrappedMiniLabel);
                var everything = new GUIContent(L("t_Wiz_EstimateEverything"));
                if (GUILayout.Button(everything, GUILayout.Height(GUI.skin.button.CalcSize(everything).y + 10f)))
                    EstimateEverything();
                MarkForTest("EstimateEverything");
            }

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            _advancedMode = EditorGUILayout.ToggleLeft(new GUIContent(L("t_Wiz_AdvancedMode"), L("t_Wiz_AdvancedModeTip")), _advancedMode);
            MarkForTest("AdvancedMode");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_prefAdvancedMode, _advancedMode);
        }

        private void DrawCopyValuesSection(TabakoTemplateRefs refs)
        {
            EditorGUILayout.Space();
            DrawSectionTitle(L("t_Wiz_CopyValues"));
            EditorGUILayout.LabelField(L("t_Wiz_CopyValuesNote"), EditorStyles.wordWrappedMiniLabel);
            _copySource = (TabakoScriptBehaviour)EditorGUILayout.ObjectField(L("t_Wiz_CopyValuesSource"), _copySource, typeof(TabakoScriptBehaviour), true);
            MarkForTest("CopySource");
            _copyTargets = EditorGUILayout.ToggleLeft(L("t_Wiz_CopyTargets"), _copyTargets);
            _copyContacts = EditorGUILayout.ToggleLeft(L("t_Wiz_CopyContacts"), _copyContacts);
            _copyScales = EditorGUILayout.ToggleLeft(L("t_Wiz_CopyScales"), _copyScales);
            _copyGestures = EditorGUILayout.ToggleLeft(L("t_Wiz_CopyGestures"), _copyGestures);
            _copySearcherInfo = EditorGUILayout.ToggleLeft(L("t_Wiz_CopySearcherInfo"), _copySearcherInfo);
            using (new EditorGUI.DisabledScope(_copySource == null || _copySource == refs.Script))
            {
                if (GUILayout.Button(L("t_Wiz_CopyValuesButton")))
                    CopyValuesFromSource(true);
                MarkForTest("CopyValuesButton");
            }
        }

        private TabakoValueCopier.Result CopyValuesFromSource(bool showDialog)
        {
            var refs = Refs;
            var result = new TabakoValueCopier.Result();
            if (refs == null || _copySource == null)
                return result;
            if (_copySource == refs.Script)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog(L("t_Wiz_Title"), L("t_Wiz_MsgCopyValuesSame"), "OK");
                return result;
            }
            var from = TabakoTemplateRefs.Resolve(_copySource);
            if (from == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog(L("t_Wiz_Title"), L("t_Wiz_MsgCopyValuesInvalid"), "OK");
                return result;
            }

            EndPanelTextEditing();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            result = TabakoValueCopier.Copy(from, refs, new TabakoValueCopier.Options { Targets = _copyTargets, Contacts = _copyContacts, Scales = _copyScales, Gestures = _copyGestures });
            if (_copySearcherInfo)
            {
                UnsupportedAvatarOps.SetSearcherInfo(refs.Script, from.Script.AvatarNamesForSearcher, from.Script.ShopName, from.Script.AvatarURL);
                _saveFieldsInitialized = false;
            }
            Undo.FlushUndoRecordObjects();
            Undo.SetCurrentGroupName("Copy values from prefab");
            Undo.CollapseUndoOperations(group);

            _undoState?.CaptureAll();
            _selectedMesh = null;
            Op($"copy values from {_copySource.name}: targets {result.Targets} contacts {result.Contacts} scales {result.Scales} gestures {result.Gestures} rootSpace={result.UsedRootSpace}");
            DriveConstraints();
            UnsupportedAvatarOps.RefreshScene();
            Repaint();
            if (showDialog)
            {
                string message = string.Format(L("t_Wiz_MsgCopyValuesDone"), _copySource.name, result.Targets, result.Contacts, result.Scales, result.Gestures);
                if (result.UsedRootSpace)
                    message += "\n" + L("t_Wiz_MsgCopyValuesRootSpace");
                EditorUtility.DisplayDialog(L("t_Wiz_Title"), message, "OK");
            }
            return result;
        }
        #endregion

        #region Step: Contacts
        private void DrawContacts()
        {
            var refs = Refs;
            if (refs == null)
                return;

            DrawEstimateAllButton();

            DrawLegendRow(k_senderRColor, k_senderLColor, L("t_Wiz_LegendSender"));
            DrawLegendRow(k_pocketRColor, k_pocketLColor, L("t_Wiz_LegendPocket"));
            DrawLegendRow(k_mouthColor, k_mouthColor, L("t_Wiz_LegendMouth"));
            EditorGUILayout.Space();

            DrawHandleTargetBar();

            EditorGUI.BeginChangeCheck();
            _mirrorContacts = EditorGUILayout.ToggleLeft(new GUIContent(L("t_Wiz_MirrorBoth"), L("t_Wiz_MirrorBothContactsTip")), _mirrorContacts);
            MarkForTest("MirrorContacts");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_prefMirrorContacts, _mirrorContacts);

            DrawSectionTitle(L("t_Wiz_Pocket"));
            DrawRadiusSlider(L("t_Wiz_Radius"), refs.PocketR, refs.PocketL, 0.03f, 0.3f);
            MarkForTest("PocketRadius");

            DrawSectionTitle(L("t_Wiz_HandContacts"));
            _syncReceivers = EditorGUILayout.ToggleLeft(L("t_Wiz_SyncReceivers"), _syncReceivers);
            MarkForTest("SyncReceivers");
            DrawRadiusSlider(L("t_Wiz_Radius"), refs.SenderR, refs.SenderL, 0.01f, 0.2f);
            MarkForTest("SenderRadius");
            DrawHeightSlider(L("t_Wiz_Height"), refs.SenderR, refs.SenderL, 0.02f, 0.5f);
            MarkForTest("SenderHeight");
            if (GUILayout.Button(L("t_Wiz_CopyToReceivers")))
                CopySendersToReceivers();
            MarkForTest("CopyToReceivers");
        }

        private void EstimatePockets()
        {
            var refs = Refs;
            float caseScale = refs.CT_Case != null ? refs.CT_Case.localScale.x : 0.73f;
            EstimatePocket(refs.PocketR, false, caseScale);
            EstimatePocket(refs.PocketL, true, caseScale);
        }

        private void EstimatePocket(ContactBase pocket, bool left, float caseScale)
        {
            var refs = Refs;
            if (pocket == null)
                return;
            if (!PlacementEstimator.TryEstimatePocket(refs.Animator, refs.AvatarRoot, caseScale, left, out Vector3 local))
                return;

            UnsupportedAvatarOps.ApplyPosition(pocket.transform, refs.AvatarRoot.TransformPoint(local), "Estimate pocket");
            UnsupportedAvatarOps.SetContactShape(pocket, pocket.position, pocket.rotation, PlacementEstimator.PocketRadius, pocket.height);
        }

        private void EstimateHandContacts()
        {
            var refs = Refs;
            EstimateHandContact(refs.SenderR, refs.CaseReceiverR, false);
            EstimateHandContact(refs.SenderL, refs.CaseReceiverL, true);
        }

        private void EstimateHandContact(ContactBase sender, ContactBase receiver, bool left)
        {
            if (sender == null)
                return;

            var frame = PalmFrame.Build(Refs.Animator, left);
            if (!frame.IsValid)
                return;

            PlacementEstimator.EstimateHandContact(frame, out Vector3 position, out Quaternion rotation, out float radius, out float height);
            UnsupportedAvatarOps.SetContactShape(sender, position, rotation, radius, height, ContactBase.ShapeType.Capsule);
            if (_syncReceivers)
                UnsupportedAvatarOps.CopyContactShape(sender, receiver);
        }

        private void CopySendersToReceivers()
        {
            var refs = Refs;
            UnsupportedAvatarOps.CopyContactShape(refs.SenderR, refs.CaseReceiverR);
            UnsupportedAvatarOps.CopyContactShape(refs.SenderL, refs.CaseReceiverL);
        }

        private void EstimateMouth()
        {
            var refs = Refs;
            if (refs.MouthTarget == null)
                return;
            if (!PlacementEstimator.TryEstimateMouth(refs.Descriptor, refs.Animator, refs.AvatarRoot, out Vector3 local, out _))
                return;

            UnsupportedAvatarOps.ApplyPosition(refs.MouthTarget, refs.AvatarRoot.TransformPoint(local), "Estimate mouth");
        }

        private void DrawEstimateAllButton()
        {
            var estimateAll = new GUIContent(L("t_Wiz_EstimateAll"));
            if (GUILayout.Button(estimateAll, GUILayout.Height(GUI.skin.button.CalcSize(estimateAll).y + 10f)))
                EstimateAllForStep();
            MarkForTest("EstimateAll");
            EditorGUILayout.Space();
        }

        private void EstimateAllForStep()
        {
            Op($"estimate all ({_step})");
            switch (_step)
            {
                case Step.Contacts:
                    EstimatePockets();
                    EstimateHandContacts();
                    EstimateMouth();
                    break;
                case Step.Case:
                    EstimateCase();
                    break;
                case Step.Cigarette:
                    EstimateTabakoHead();
                    EstimateTabakoHand();
                    break;
                case Step.Lighter:
                    EstimateLighter();
                    break;
            }
            Repaint();
        }

        private void EstimateEverything()
        {
            Step current = _step;
            foreach (var step in new[] { Step.Case, Step.Contacts, Step.Cigarette, Step.Lighter })
            {
                SetStep(step, false);
                EstimateAllForStep();
            }
            SetStep(current, false);
        }

        private bool _transformsFoldout = true;
        private const string k_prefTransformsFoldout = "SatabakoWizard.TransformsFoldout";

        private void DrawTransformsFoldout()
        {
            var refs = Refs;
            var entry = CurrentHandleEntry();
            if (refs == null || !entry.IsValid || entry.transform == null)
                return;

            EditorGUI.BeginChangeCheck();
            _transformsFoldout = EditorGUILayout.Foldout(_transformsFoldout, "Transforms", true);
            MarkForTest("TransformsFoldout");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_prefTransformsFoldout, _transformsFoldout);
            if (!_transformsFoldout)
                return;

            Transform bone = refs.BoneTransformOf(entry.transform);
            if (bone == null)
                bone = refs.AvatarRoot;
            if (bone == null)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField(string.Format(L("t_Wiz_TransformsNote"), bone.name), EditorStyles.wordWrappedMiniLabel);

                Vector3 position = bone.InverseTransformPoint(entry.transform.position);
                Vector3 rotation = WrapEuler((Quaternion.Inverse(bone.rotation) * entry.transform.rotation).eulerAngles);

                EditorGUI.BeginChangeCheck();
                position = Vector3Row("Position", k_transformsPositionControl, position);
                rotation = Vector3Row("Rotation", k_transformsRotationControl, rotation);
                if (EditorGUI.EndChangeCheck())
                    ApplyHandle(entry, bone.TransformPoint(position), bone.rotation * Quaternion.Euler(rotation));
            }
        }

        internal const string k_transformsPositionControl = "SatabakoWizard.Transforms.Position";
        internal const string k_transformsRotationControl = "SatabakoWizard.Transforms.Rotation";
        private static readonly string[] k_xyz = { "X", "Y", "Z" };
        private static readonly GUIContent[] k_xyzLabels = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };
        [System.NonSerialized] private Dictionary<string, Rect> _fieldRectsForTest;

        private Vector3 Vector3Row(string label, string controlName, Vector3 value)
        {
            Rect row = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            int id = GUIUtility.GetControlID(FocusType.Passive, row);
            Rect field = EditorGUI.PrefixLabel(row, id, EditorGUIUtility.TrTempContent(label));

            const float spacing = 4f;
            float width = (field.width - spacing * 2f) / 3f;
            float labelWidth = EditorGUIUtility.labelWidth;
            int indent = EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth = 13f;
            EditorGUI.indentLevel = 0;
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(field.x + i * (width + spacing), field.y, width, field.height);
                string name = controlName + "." + k_xyz[i];
                GUI.SetNextControlName(name);
                value[i] = EditorGUI.FloatField(r, k_xyzLabels[i], value[i]);
                if (Event.current.type == EventType.Repaint)
                    RecordFieldRectForTest(name, r);
            }
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indent;
            return value;
        }

        private void RecordFieldRectForTest(string name, Rect gui)
        {
            if (_fieldRectsForTest == null)
                _fieldRectsForTest = new Dictionary<string, Rect>();
            Vector2 screen = GUIUtility.GUIToScreenPoint(gui.position);
            _fieldRectsForTest[name] = new Rect(screen - _panelScreenOriginForTest, gui.size);
        }

        private void MarkForTest(string name)
        {
            if (Event.current.type == EventType.Repaint)
                RecordFieldRectForTest(name, GUILayoutUtility.GetLastRect());
        }

        private static Vector3 WrapEuler(Vector3 euler)
        {
            for (int i = 0; i < 3; i++)
            {
                float v = Mathf.Repeat(euler[i], 360f);
                euler[i] = Mathf.Round((v > 180f ? v - 360f : v) * 1000f) / 1000f;
            }
            return euler;
        }

        private void DrawRadiusSlider(string label, ContactBase right, ContactBase left, float min, float max)
        {
            if (right == null)
                return;

            EditorGUI.BeginChangeCheck();
            float radius = EditorGUILayout.Slider(label, right.radius, min, max);
            if (EditorGUI.EndChangeCheck())
            {
                UnsupportedAvatarOps.SetContactShape(right, right.position, right.rotation, radius, right.height);
                if (left != null)
                    UnsupportedAvatarOps.SetContactShape(left, left.position, left.rotation, radius, left.height);
                SyncReceiversIfNeeded();
            }
        }

        private void DrawHeightSlider(string label, ContactBase right, ContactBase left, float min, float max)
        {
            if (right == null)
                return;

            EditorGUI.BeginChangeCheck();
            float height = EditorGUILayout.Slider(label, right.height, min, max);
            if (EditorGUI.EndChangeCheck())
            {
                UnsupportedAvatarOps.SetContactShape(right, right.position, right.rotation, right.radius, height);
                if (left != null)
                    UnsupportedAvatarOps.SetContactShape(left, left.position, left.rotation, left.radius, height);
                SyncReceiversIfNeeded();
            }
        }

        private void SyncReceiversIfNeeded()
        {
            if (_syncReceivers)
                CopySendersToReceivers();
        }
        #endregion

        #region Step: Case
        private void DrawCase()
        {
            var refs = Refs;
            if (refs == null)
                return;

            DrawEstimateAllButton();
            DrawGestureToggles();
            EditorGUILayout.Space();
            DrawHandleTargetBar();
            DrawTransformsFoldout();
            EditorGUILayout.Space();

            DrawScaleSlider(refs.CT_Case, scale => UnsupportedAvatarOps.SetCaseScale(refs, scale));
        }

        private void EstimateCase()
        {
            var refs = Refs;
            var frame = BuildRightPalm();
            if (refs == null || !frame.IsValid)
                return;
            UnsupportedAvatarOps.ApplyPose(refs.CaseR, PlacementEstimator.EstimateCase(frame), "Estimate case");
            UnsupportedAvatarOps.SetCaseScale(refs, PlacementEstimator.EstimateCaseScale(frame.HandLength));
            UnsupportedAvatarOps.MirrorToLeft(refs.CaseR, refs.CaseL, refs.Root);
        }

        private void DrawScaleSlider(Transform target, System.Action<float> apply)
        {
            if (target == null)
                return;

            EditorGUI.BeginChangeCheck();
            float scale = EditorGUILayout.Slider(L("t_Wiz_Scale"), target.localScale.x, k_minScale, k_maxScale);
            MarkForTest("Scale");
            if (EditorGUI.EndChangeCheck())
                apply(scale);
        }
        #endregion

        #region Step: Cigarette
        private void DrawCigarette()
        {
            var refs = Refs;
            if (refs == null)
                return;

            DrawEstimateAllButton();
            DrawGestureToggles();
            EditorGUILayout.Space();
            DrawHandleTargetBar();
            DrawTransformsFoldout();

            DrawSectionTitle(L("t_Wiz_HandPosition"));
            _holdOffsetMm = EditorGUILayout.Slider(L("t_Wiz_HoldOffset"), _holdOffsetMm, -20f, 40f);
            MarkForTest("HoldOffset");

            DrawScaleSlider(refs.CT_Tabako, scale => UnsupportedAvatarOps.SetCaseScale(refs, scale));
        }

        private void EstimateTabakoHand()
        {
            var refs = Refs;
            var palm = BuildRightPalm();
            if (!palm.IsValid || refs.TabakoR == null)
                return;

            var fingers = PlacementEstimator.BuildRightFingerFrame(refs.Animator);
            bool gesturePose = _preview != null && _previewKind == GestureKind.Tabako && FingerPosePreview.Active == _preview;

            PlacementEstimator.Pose pose;
            if (fingers.IsValid && PlacementEstimator.TryGetTabakoPivot(refs, out Vector3 pivotLocal, out Vector3 tipDirLocal, out _, out _))
                pose = PlacementEstimator.EstimateTabakoByFingers(fingers, palm, pivotLocal, tipDirLocal, gesturePose, _holdOffsetMm * 0.001f);
            else
                pose = PlacementEstimator.EstimateTabako(palm);

            UnsupportedAvatarOps.ApplyPose(refs.TabakoR, pose, "Estimate cigarette");
            UnsupportedAvatarOps.MirrorToLeft(refs.TabakoR, refs.TabakoL, refs.Root);
        }

        private void EstimateTabakoHead()
        {
            var refs = Refs;
            if (refs.TabakoHead == null || refs.MouthTarget == null)
                return;

            Vector3 mouthLocal = refs.AvatarRoot.InverseTransformPoint(refs.MouthTarget.position);
            Vector3 headLocal = PlacementEstimator.EstimateTabakoHead(mouthLocal);
            UnsupportedAvatarOps.ApplyPosition(refs.TabakoHead, refs.AvatarRoot.TransformPoint(headLocal), "Estimate cigarette (mouth)");
        }
        #endregion

        #region Step: Lighter
        private void DrawLighter()
        {
            var refs = Refs;
            if (refs == null)
                return;

            DrawEstimateAllButton();
            DrawGestureToggles();
            EditorGUILayout.Space();
            DrawHandleTargetBar();
            DrawTransformsFoldout();
            EditorGUILayout.Space();

            DrawScaleSlider(refs.CT_Lighter, scale => UnsupportedAvatarOps.SetLighterScale(refs, scale));
        }

        private void EstimateLighter()
        {
            var refs = Refs;
            var frame = BuildRightPalm();
            if (refs == null || !frame.IsValid)
                return;
            UnsupportedAvatarOps.ApplyPose(refs.LighterR, PlacementEstimator.EstimateLighter(frame), "Estimate lighter");
            UnsupportedAvatarOps.SetLighterScale(refs, PlacementEstimator.EstimateLighterScale(frame.HandLength));
            UnsupportedAvatarOps.MirrorToLeft(refs.LighterR, refs.LighterL, refs.Root);
        }
        #endregion

        #region Avatar mesh selection (hover / click / H)
        private bool _meshSelect = true;
        private const string k_prefMeshSelect = "SatabakoWizard.MeshSelect";
        [System.NonSerialized] private Renderer _hoverMesh;
        [System.NonSerialized] private Renderer _selectedMesh;
        [System.NonSerialized] private int _hoverContact = -1;
        [System.NonSerialized] private readonly List<Renderer> _hiddenMeshes = new List<Renderer>();
        [SerializeField] private List<string> m_hiddenMeshPaths = new List<string>();
        [System.NonSerialized] private List<Renderer> _avatarRenderers;
        [System.NonSerialized] private double _lastHoverPick;
        [System.NonSerialized] private Transform _guiPointRequestForTest;
        [System.NonSerialized] private Vector2? _guiPointForTest;
        private const double k_hoverPickInterval = 0.03;
        private static readonly Color k_hoverOutline = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color k_selectedOutline = new Color(1f, 1f, 1f, 1f);
        private const float k_hoverFill = 0.15f;
        private const float k_selectedFill = 0.3f;

        private void SetMeshSelect(bool on)
        {
            _meshSelect = on;
            EditorPrefs.SetBool(k_prefMeshSelect, on);
            _hoverMesh = null;
            _selectedMesh = null;
            Repaint();
        }

        private List<Renderer> AvatarRenderers(TabakoTemplateRefs refs)
        {
            if (_avatarRenderers != null && _avatarRenderers.All(r => r != null))
                return _avatarRenderers;

            _avatarRenderers = new List<Renderer>();
            if (refs == null || refs.AvatarRoot == null)
                return _avatarRenderers;

            Transform template = refs.Root;
            foreach (var renderer in refs.AvatarRoot.GetComponentsInChildren<Renderer>(false))
            {
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                    continue;
                if (template != null && renderer.transform.IsChildOf(template))
                    continue;
                _avatarRenderers.Add(renderer);
            }
            return _avatarRenderers;
        }

        [System.NonSerialized] private List<Renderer> _itemRenderers;
        private List<Renderer> ItemRenderers(TabakoTemplateRefs refs)
        {
            if (_itemRenderers != null && _itemRenderers.All(r => r != null))
                return _itemRenderers;

            _itemRenderers = new List<Renderer>();
            if (refs == null)
                return _itemRenderers;
            foreach (var root in new[] { refs.CaseMesh, refs.TabakoMesh, refs.LighterMesh })
            {
                if (root == null)
                    continue;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                        _itemRenderers.Add(renderer);
                }
            }
            return _itemRenderers;
        }

        private bool IsItemRenderer(TabakoTemplateRefs refs, Renderer renderer) => renderer != null && ItemRenderers(refs).Contains(renderer);

        private Renderer PickMeshAt(TabakoTemplateRefs refs, Vector2 local)
        {
            var avatar = AvatarRenderers(refs);
            var items = ItemRenderers(refs);
            if (avatar.Count + items.Count == 0)
                return null;
            var filter = new GameObject[avatar.Count + items.Count];
            for (int i = 0; i < avatar.Count; i++)
                filter[i] = avatar[i].gameObject;
            for (int i = 0; i < items.Count; i++)
                filter[avatar.Count + i] = items[i].gameObject;
            var picked = HandleUtility.PickGameObject(local, false, null, filter, out _);
            return picked != null ? picked.GetComponent<Renderer>() : null;
        }

        private void UpdateHoverMesh(TabakoTemplateRefs refs, Rect local, Event e)
        {
            if (e.type != EventType.MouseMove)
                return;

            bool over = !_editGesture && GUIUtility.hotControl == 0 && local.Contains(e.mousePosition);

            Renderer hovered = null;
            int contact = -1;
            if (over)
            {
                if (_meshSelect)
                {
                    double now = EditorApplication.timeSinceStartup;
                    if (now - _lastHoverPick < k_hoverPickInterval)
                        return;
                    _lastHoverPick = now;
                    hovered = PickMeshAt(refs, e.mousePosition);
                }
                if (hovered == null || !IsItemRenderer(refs, hovered))
                {
                    contact = HitTestContact(refs, e.mousePosition);
                    if (contact >= 0)
                        hovered = null;
                }
            }
            if (contact != _hoverContact || hovered != _hoverMesh)
            {
                _hoverContact = contact;
                _hoverMesh = hovered;
                Repaint();
            }
        }

        private int HitTestContact(TabakoTemplateRefs refs, Vector2 local)
        {
            if (_step != Step.Contacts || refs == null || Camera.current == null)
                return -1;

            var entries = CurrentHandleEntries();
            Ray ray = HandleUtility.GUIPointToWorldRay(local);
            int best = -1;
            float bestT = float.MaxValue;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                bool hit;
                float t = 0f;
                if (entry.contact != null)
                {
                    hit = RayHitsCapsule(ray, entry.contact, out t);
                }
                else if (entry.transform != null && entry.transform == refs.MouthTarget)
                {
                    hit = RayHitsSphere(ray, entry.transform.position, 0.02f, out t);
                }
                else
                {
                    ContactBase pocket = refs.PocketR != null && entry.transform == refs.PocketR.transform ? refs.PocketR
                        : refs.PocketL != null && entry.transform == refs.PocketL.transform ? refs.PocketL : null;
                    hit = pocket != null && RayHitsSphere(ray, ContactSpace.LocalToWorld(pocket.transform, pocket.position), pocket.radius, out t);
                }
                if (hit && t < bestT)
                {
                    bestT = t;
                    best = i;
                }
            }
            return best;
        }

        private static bool RayHitsSphere(Ray ray, Vector3 center, float radius, out float t)
        {
            Vector3 oc = ray.origin - center;
            float b = Vector3.Dot(oc, ray.direction);
            float c = oc.sqrMagnitude - radius * radius;
            float disc = b * b - c;
            t = 0f;
            if (disc < 0f)
                return false;
            float root = Mathf.Sqrt(disc);
            t = -b - root;
            if (t < 0f)
                t = -b + root;
            return t >= 0f;
        }

        private static bool RayHitsCapsule(Ray ray, ContactBase contact, out float t)
        {
            Vector3 center = ContactSpace.LocalToWorld(contact.transform, contact.position);
            Vector3 axis = contact.transform.rotation * contact.rotation * Vector3.up;
            float r = contact.radius;
            float half = Mathf.Max(0f, contact.height * 0.5f - r);
            Vector3 a = center - axis * half;
            Vector3 b = center + axis * half;

            Vector3 d1 = ray.direction;
            Vector3 d2 = b - a;
            Vector3 rel = ray.origin - a;
            float a11 = Vector3.Dot(d1, d1), a22 = Vector3.Dot(d2, d2), a12 = Vector3.Dot(d1, d2);
            float b1 = Vector3.Dot(d1, rel), b2 = Vector3.Dot(d2, rel);
            float s;
            if (a22 < 1e-8f)
            {
                s = 0f;
            }
            else
            {
                float denom = a11 * a22 - a12 * a12;
                s = denom > 1e-8f ? Mathf.Clamp01((b2 * a11 - b1 * a12) / denom) : 0f;
            }
            t = (s * a12 - b1) / a11;
            if (t < 0f)
            {
                t = 0f;
                s = a22 > 1e-8f ? Mathf.Clamp01(b2 / a22) : 0f;
            }
            Vector3 p = ray.origin + d1 * t;
            Vector3 q = a + d2 * s;
            return (p - q).sqrMagnitude <= r * r;
        }

        private void SelectContact(int index)
        {
            SetHandleIndex(index, false);
            _selectedMesh = null;
            Repaint();
        }

        private void DrawMeshHighlights(TabakoTemplateRefs refs)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            if (_selectedMesh != null)
                DrawHighlight(_selectedMesh, k_selectedOutline, IsItemRenderer(refs, _selectedMesh) ? 0f : k_selectedFill);
            if (_hoverMesh != null && _hoverMesh != _selectedMesh)
                DrawHighlight(_hoverMesh, k_hoverOutline, IsItemRenderer(refs, _hoverMesh) ? 0f : k_hoverFill);
        }

        private const string k_highlightShader = "Hidden/SataniaTabako/WizardHighlight";
        private const float k_highlightWidth = 2f;
        [System.NonSerialized] private Material _highlightMaterial;
        [System.NonSerialized] private bool _highlightShaderMissing;
        [System.NonSerialized] private readonly Dictionary<Renderer, Mesh> _highlightMeshes = new Dictionary<Renderer, Mesh>();

        private Material HighlightMaterial
        {
            get
            {
                if (_highlightMaterial != null || _highlightShaderMissing)
                    return _highlightMaterial;
                var shader = Shader.Find(k_highlightShader);
                if (shader == null)
                {
                    _highlightShaderMissing = true;
                    return null;
                }
                _highlightMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                return _highlightMaterial;
            }
        }

        private void DrawHighlight(Renderer renderer, Color color, float fillOpacity)
        {
            var material = HighlightMaterial;
            if (material == null)
            {
                Handles.DrawOutline(new[] { renderer }, color, fillOpacity);
                return;
            }
            Mesh mesh = HighlightMesh(renderer);
            if (mesh == null)
                return;
            Matrix4x4 matrix = renderer.transform.localToWorldMatrix;
            material.SetColor("_Color", color);
            material.SetFloat("_FillOpacity", fillOpacity);
            material.SetFloat("_Width", k_highlightWidth);
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                if (fillOpacity > 0f && material.SetPass(0))
                    Graphics.DrawMeshNow(mesh, matrix, sub);
                if (material.SetPass(1))
                    Graphics.DrawMeshNow(mesh, matrix, sub);
            }
        }

        private Mesh HighlightMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                if (skinned.sharedMesh == null)
                    return null;
                if (!_highlightMeshes.TryGetValue(renderer, out Mesh baked) || baked == null)
                {
                    baked = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                    _highlightMeshes[renderer] = baked;
                }
                skinned.BakeMesh(baked);
                return baked;
            }
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private void ReleaseHighlightResources()
        {
            foreach (var mesh in _highlightMeshes.Values)
            {
                if (mesh != null)
                    DestroyImmediate(mesh);
            }
            _highlightMeshes.Clear();
            if (_highlightMaterial != null)
            {
                DestroyImmediate(_highlightMaterial);
                _highlightMaterial = null;
            }
        }

        private void HideMesh(Renderer renderer)
        {
            if (renderer == null || _hiddenMeshes.Contains(renderer) || IsItemRenderer(Refs, renderer))
                return;

            renderer.forceRenderingOff = true;
            _hiddenMeshes.Add(renderer);
            string path = MeshPath(renderer);
            if (path != null && !m_hiddenMeshPaths.Contains(path))
                m_hiddenMeshPaths.Add(path);
            if (_selectedMesh == renderer)
                _selectedMesh = null;
            if (_hoverMesh == renderer)
                _hoverMesh = null;
            Repaint();
        }

        private void UnhideAllMeshes()
        {
            foreach (var renderer in _hiddenMeshes)
            {
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            }
            _hiddenMeshes.Clear();
            m_hiddenMeshPaths.Clear();
            Repaint();
        }

        private void ReleaseHiddenMeshes()
        {
            foreach (var renderer in _hiddenMeshes)
            {
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            }
            _hiddenMeshes.Clear();
            _hoverMesh = null;
            _selectedMesh = null;
            _avatarRenderers = null;
            _itemRenderers = null;
        }

        private void ApplyHiddenMeshPaths()
        {
            var refs = Refs;
            if (refs == null || refs.AvatarRoot == null)
                return;
            foreach (string path in m_hiddenMeshPaths)
            {
                Transform t = string.IsNullOrEmpty(path) ? refs.AvatarRoot : refs.AvatarRoot.Find(path);
                var renderer = t != null ? t.GetComponent<Renderer>() : null;
                if (renderer != null && !_hiddenMeshes.Contains(renderer))
                {
                    renderer.forceRenderingOff = true;
                    _hiddenMeshes.Add(renderer);
                }
            }
        }

        private string MeshPath(Renderer renderer)
        {
            var refs = Refs;
            if (refs == null || refs.AvatarRoot == null || renderer == null)
                return null;
            return AnimationUtility.CalculateTransformPath(renderer.transform, refs.AvatarRoot);
        }
        #endregion

        #region Step: Save
        private void DrawSave()
        {
            if (_completed)
            {
                if (!string.IsNullOrEmpty(_saveMessage))
                    EditorGUILayout.HelpBox(_saveMessage, MessageType.Info);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(L("t_Wiz_Result"), _lastResult, typeof(Object), true);
                if (_lastResult != null && GUILayout.Button(L("t_Wiz_Select")))
                    SelectAndPing(_lastResult);
                EditorGUILayout.Space();
                if (GUILayout.Button(L("t_Wiz_AdjustAgain")))
                {
                    _completed = false;
                    _saveMessage = null;
                    EnsureSession();
                    SetStep(Step.Setup);
                }
                return;
            }

            var refs = Refs;
            if (refs == null || _session == null)
                return;

            InitSaveFields(refs);

            float savedLabelWidth = EditorGUIUtility.labelWidth;
            if (_advancedMode)
                EditorGUIUtility.labelWidth = Mathf.Max(savedLabelWidth, EditorStyles.label.CalcSize(new GUIContent(L("t_Wiz_AvatarNames"))).x + 6f);
            _prefabName = EditorGUILayout.TextField(L("t_Wiz_PrefabName"), _prefabName);
            MarkForTest("PrefabName");
            if (_advancedMode)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _avatarNames = EditorGUILayout.TextField(L("t_Wiz_AvatarNames"), _avatarNames);
                    if (GUILayout.Button(L("t_Romaji_Open"), GUILayout.Width(70)))
                        RomajiReadingWindow.Open(RomajiSeed(), AppendAvatarName);
                }
                _shopName = EditorGUILayout.TextField(L("t_Wiz_ShopName"), _shopName);
                _avatarUrl = EditorGUILayout.TextField(L("t_Wiz_AvatarURL"), _avatarUrl);
                MarkForTest("AvatarUrl");
            }
            else
                EditorGUILayout.LabelField(L("t_Wiz_SaveFieldsHidden"), EditorStyles.wordWrappedMiniLabel);
            EditorGUIUtility.labelWidth = savedLabelWidth;

            EditorGUILayout.Space();

            switch (_session.SaveMode)
            {
                case PrefabSaveMode.NewVariant:
                    EditorGUILayout.LabelField(L("t_Wiz_MsgNewVariant"), EditorStyles.wordWrappedMiniLabel);
                    break;
                case PrefabSaveMode.UpdateVendorVariant:
                    EditorGUILayout.HelpBox(L("t_Wiz_MsgVendorVariant") + "\n" + _session.SaveModeAssetPath, MessageType.Warning);
                    break;
                case PrefabSaveMode.UpdateUserVariant:
                    EditorGUILayout.LabelField(L("t_Wiz_MsgUpdateVariant"), EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField("Prefab", _session.SaveModeAssetPath);
                    break;
                default:
                    EditorGUILayout.HelpBox($"{L("t_Wiz_MsgCannotSave")} ({_session.SaveMode})", MessageType.Error);
                    if (!string.IsNullOrEmpty(_saveMessage))
                        EditorGUILayout.HelpBox(_saveMessage, MessageType.Info);
                    return;
            }
            if (_session.SourceIsAsset)
                EditorGUILayout.HelpBox(L("t_Wiz_MsgAssetTarget"), MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _embedGestureClips = EditorGUILayout.ToggleLeft(L("t_Wiz_EmbedGestureClips"), _embedGestureClips);
            MarkForTest("EmbedGestureClips");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_prefEmbedGestureClips, _embedGestureClips);
            EditorGUILayout.LabelField(L(_embedGestureClips ? "t_Wiz_EmbedGestureClipsNote" : "t_Wiz_GestureFileNote"), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();

            string label = _session.SourceIsAsset ? L("t_Wiz_SaveOnly") : L("t_Wiz_SaveAndInsert");
            if (GUILayout.Button(label, GUILayout.Height(28f)))
                Save(null, true);
            MarkForTest("SaveButton");

            if (!string.IsNullOrEmpty(_saveMessage))
                EditorGUILayout.HelpBox(_saveMessage, MessageType.Info);
        }

        private void InitSaveFields(TabakoTemplateRefs refs)
        {
            if (_saveFieldsInitialized)
                return;

            _prefabName = refs.Root.name;
            if (_prefabName == UnsupportedAvatarUtils.TemplatePrefab?.name)
                _prefabName = m_target.name;
            _avatarNames = string.Join(", ", refs.Script.AvatarNamesForSearcher ?? new string[0]);
            if (string.IsNullOrEmpty(_avatarNames))
                _avatarNames = m_target.name;
            _shopName = refs.Script.ShopName ?? "";
            _avatarUrl = refs.Script.AvatarURL ?? "";
            _saveFieldsInitialized = true;
        }

        private string[] ParseAvatarNames()
        {
            return (_avatarNames ?? "")
                .Split(new[] { ',', '、', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        }

        private void PrepareForSave()
        {
            EndTransientState();

            var refs = Refs;
            GameObject root = refs.Root.gameObject;
            Undo.RecordObject(root, "Rename prefab root");
            if (!string.IsNullOrEmpty(_prefabName))
                root.name = _prefabName;

            UnsupportedAvatarOps.SetSearcherInfo(refs.Script, ParseAvatarNames(), _shopName, _avatarUrl);
        }

        private void Save(string assetPath, bool useDialog)
        {
            var refs = Refs;
            if (refs == null || _session == null || !_session.IsValid)
                return;

            InitSaveFields(refs);
            PrepareForSave();

            GameObject cloneRoot = refs.Root.gameObject;
            RestoreConstraintPreview();
            UnsupportedAvatarOps.RecordInstanceModifications(cloneRoot);
            RevertTemporarySourceOverrides();
            BoneReferenceBaker.Bake(refs);
            VersionStamper.Stamp(refs.Script);
            Transform avatarRoot = _session.SourceIsAsset ? null : _session.Source.transform;
            GameObject replace = _session.SourceTemplateRoot;
            Vector3 localPosition = cloneRoot.transform.localPosition;
            Quaternion localRotation = cloneRoot.transform.localRotation;
            Vector3 localScale = cloneRoot.transform.localScale;
            Object result = null;
            string message;
            List<string> savedClips = null;

            switch (_session.SaveMode)
            {
                case PrefabSaveMode.UpdateUserVariant:
                {
                    savedClips = _embedGestureClips
                        ? EmbedGestureEdits(refs, _session.SaveModeAssetPath)
                        : ApplyGestureEdits(refs, Path.GetDirectoryName(_session.SaveModeAssetPath), _prefabName, _session.SaveModeAssetPath);
                    UnsupportedAvatarOps.ApplyOverrides(cloneRoot);
                    if (avatarRoot != null && replace != null)
                    {
                        if (PrefabUtility.IsOutermostPrefabInstanceRoot(replace))
                            PrefabUtility.RevertPrefabInstance(replace, InteractionMode.AutomatedAction);
                        result = replace;
                    }
                    else
                        result = AssetDatabase.LoadAssetAtPath<GameObject>(_session.SaveModeAssetPath);
                    message = L("t_Wiz_MsgApplied") + "\n" + _session.SaveModeAssetPath;
                    break;
                }

                case PrefabSaveMode.NewVariant:
                case PrefabSaveMode.UpdateVendorVariant:
                {
                    if (useDialog)
                    {
                        string defaultName = string.IsNullOrEmpty(_prefabName) ? m_target.name : _prefabName;
                        assetPath = EditorUtility.SaveFilePanelInProject("Save Tabako Prefab", defaultName, "prefab", "");
                    }

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        _prefabName = Path.GetFileNameWithoutExtension(assetPath);
                        cloneRoot.name = _prefabName;
                        if (!_embedGestureClips)
                            savedClips = ApplyGestureEdits(refs, Path.GetDirectoryName(assetPath), _prefabName, null);
                        var asset = UnsupportedAvatarOps.SaveAsNewVariant(cloneRoot, assetPath, out bool success);
                        if (!success || asset == null)
                        {
                            _saveMessage = $"{L("t_Wiz_MsgCannotSave")}\n{assetPath}";
                            return;
                        }
                        if (_embedGestureClips && _gestureEdits.Count > 0)
                        {
                            savedClips = EmbedGestureEdits(refs, assetPath);
                            UnsupportedAvatarOps.ApplyOverrides(cloneRoot);
                        }

                        if (avatarRoot != null)
                        {
                            result = UnsupportedAvatarOps.InstantiateIntoAvatar(asset, avatarRoot, replace, localPosition, localRotation, localScale, asset.name, out bool replaced);
                            message = L("t_Wiz_MsgSavedAndInserted") + "\n" + assetPath + (replaced ? "\n" + L("t_Wiz_MsgReplacedOld") : "");
                        }
                        else
                        {
                            result = asset;
                            message = L("t_Wiz_MsgSaved") + "\n" + assetPath;
                        }
                    }
                    else
                    {
                        if (avatarRoot == null)
                        {
                            _saveMessage = L("t_Wiz_MsgNotSaved");
                            return;
                        }

                        var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(_session.SaveModeAssetPath);
                        if (baseAsset == null)
                        {
                            _saveMessage = L("t_Wiz_MsgNotSaved");
                            return;
                        }
                        var inserted = UnsupportedAvatarOps.InstantiateIntoAvatar(baseAsset, avatarRoot, replace, localPosition, localRotation, localScale, cloneRoot.name, out bool replaced);
                        if (inserted != null)
                        {
                            UnsupportedAvatarOps.TransferPrefabModifications(cloneRoot, inserted, _session.CloneRoot, avatarRoot);

                            var insertedScript = inserted.GetComponent<TabakoScriptBehaviour>();
                            var insertedRefs = insertedScript != null ? TabakoTemplateRefs.Resolve(insertedScript) : null;
                            if (insertedRefs != null)
                            {
                                savedClips = ApplyGestureEdits(insertedRefs, $"{GestureFallbackFolder}/{SanitizeFileName(avatarRoot.name)}", cloneRoot.name, null);
                                CopyGestureOverrides(refs, insertedRefs);
                            }
                        }
                        result = inserted;
                        message = L("t_Wiz_MsgInsertedNotSaved") + (replaced ? "\n" + L("t_Wiz_MsgReplacedOld") : "");
                    }
                    break;
                }

                default:
                    _saveMessage = $"{L("t_Wiz_MsgCannotSave")} ({_session.SaveMode})";
                    return;
            }

            if (savedClips != null && savedClips.Count > 0)
                message += "\n" + L("t_Wiz_MsgGestureSaved") + "\n" + string.Join("\n", savedClips);

            _lastResult = result;
            _saveMessage = message;
            _completed = true;
            EndSession();
            SelectAndPing(result);
            Repaint();
        }

        private static void SelectAndPing(Object target)
        {
            if (target == null)
                return;

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }
        #endregion
    }
}

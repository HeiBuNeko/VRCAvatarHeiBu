using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace net.satania_shopping.tabakosystem.editor
{
    internal sealed class FingerPosePreview
    {
        public readonly struct BoneAttachment
        {
            public readonly Transform Target;
            public readonly Transform Bone;

            public BoneAttachment(Transform target, Transform bone)
            {
                Target = target;
                Bone = bone;
            }
        }

        private struct AttachmentState
        {
            public Transform target;
            public Transform bone;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        public static FingerPosePreview Active { get; private set; }
        public static System.Action<string> Trace;
        public bool StopOnSceneSaving { get; set; } = true;
        public bool IsStopped => _stopped;

        public Animator Animator { get; }
        public AnimationClip Clip { get; }
        public bool BodyPose { get; }
        public int AppliedCurveCount { get; private set; }
        public float[] FingerMuscleValues { get; set; }

        private HumanPoseHandler _handler;
        private readonly HumanoidSnapshot _snapshot;
        private readonly HashSet<Transform> _fingerTransforms = new HashSet<Transform>();
        private readonly List<AttachmentState> _attachments = new List<AttachmentState>();
        private bool _stopped;

        private static readonly Regex k_stretched = new Regex(@"^(Left|Right)Hand\.(Thumb|Index|Middle|Ring|Little)\.(\d) Stretched$");
        private static readonly Regex k_spread = new Regex(@"^(Left|Right)Hand\.(Thumb|Index|Middle|Ring|Little)[. ]Spread$");

        private static Dictionary<string, int> s_muscleIndexByName;
        private static HashSet<int> s_fingerMuscles;

        private FingerPosePreview(Animator animator, AnimationClip clip, bool bodyPose, IReadOnlyList<BoneAttachment> attachments)
        {
            Animator = animator;
            Clip = clip;
            BodyPose = bodyPose;
            _snapshot = HumanoidSnapshot.Capture(animator);

            if (attachments != null)
            {
                foreach (var a in attachments)
                {
                    if (a.Target != null && a.Bone != null)
                        _attachments.Add(new AttachmentState { target = a.Target, bone = a.Bone });
                }
            }

            for (int i = (int)HumanBodyBones.LeftThumbProximal; i <= (int)HumanBodyBones.RightLittleDistal; i++)
            {
                var t = HumanoidBones.Get(animator, (HumanBodyBones)i);
                if (t != null)
                    _fingerTransforms.Add(t);
            }
        }

        public static bool CanStart(Animator animator, out string reason)
        {
            reason = null;
            if (!HumanoidBones.IsHumanoid(animator))
            {
                reason = "Humanoid の Animator が必要です。";
                return false;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reason = "プレイモード中は使用できません。";
                return false;
            }
            return true;
        }

        public static FingerPosePreview Start(Animator animator, AnimationClip clip)
        {
            return Start(animator, clip, false, null);
        }

        public static FingerPosePreview Start(Animator animator, AnimationClip clip, bool bodyPose, IReadOnlyList<BoneAttachment> attachments, float[] fingerMuscleValues = null, bool stopOnSceneSaving = true)
        {
            if (Active != null)
                Trace?.Invoke("preview: 前のプレビューが残っていたので止める (Start)");
            StopActive();

            if (!CanStart(animator, out _) || (clip == null && !bodyPose && fingerMuscleValues == null))
                return null;

            var preview = new FingerPosePreview(animator, clip, bodyPose, attachments) { FingerMuscleValues = fingerMuscleValues, StopOnSceneSaving = stopOnSceneSaving };
            preview.CaptureAttachments();
            preview.Apply();
            preview.PlaceAttachments();

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorApplication.quitting += OnQuitting;

            Active = preview;
            return preview;
        }

        private static void OnBeforeAssemblyReload()
        {
            Trace?.Invoke("preview: アセンブリリロードで停止");
            StopActive();
        }

        private static void OnQuitting()
        {
            StopActive();
        }

        public static void StopActive()
        {
            Active?.Stop();
        }

        public void Stop()
        {
            if (_stopped)
                return;
            _stopped = true;

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorApplication.quitting -= OnQuitting;

            CaptureAttachments();
            _snapshot.Restore();
            PlaceAttachments();

            _handler?.Dispose();
            _handler = null;

            if (Active == this)
                Active = null;

            RequestSceneUpdate();
        }

        public void RefreshAttachments()
        {
            if (!_stopped)
                CaptureAttachments();
        }

        public void SetFingerMuscle(int muscle, float value)
        {
            if (_stopped || FingerMuscleValues == null || muscle < 0 || muscle >= FingerMuscleValues.Length)
                return;
            FingerMuscleValues[muscle] = value;
            Apply();
        }

        public bool TryGetMuscleAxis(int muscle, Transform bone, out Vector3 worldAxis, out float degreesPerUnit)
        {
            worldAxis = Vector3.up;
            degreesPerUnit = 0f;
            if (_stopped || bone == null || muscle < 0 || muscle >= HumanTrait.MuscleCount)
                return false;

            const float delta = 0.1f;
            Transform root = Animator.transform;
            Vector3 axisInRoot;
            float angle;
            using (new RootScope(root))
            {
                var handler = Handler;
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);
                Quaternion rootRotation = root.rotation;
                Quaternion q0 = Quaternion.Inverse(rootRotation) * bone.rotation;

                float m = pose.muscles[muscle];
                float d = m + delta <= 1f ? delta : -delta;
                pose.muscles[muscle] = m + d;
                handler.SetHumanPose(ref pose);
                Quaternion q1 = Quaternion.Inverse(rootRotation) * bone.rotation;

                pose.muscles[muscle] = m;
                handler.SetHumanPose(ref pose);

                (q1 * Quaternion.Inverse(q0)).ToAngleAxis(out angle, out axisInRoot);
                if (angle > 180f)
                {
                    angle = 360f - angle;
                    axisInRoot = -axisInRoot;
                }
                if (d < 0f)
                    axisInRoot = -axisInRoot;
            }
            if (!BodyPose)
                _snapshot.RestoreExcept(_fingerTransforms);

            if (float.IsNaN(angle) || angle < 0.01f || axisInRoot.sqrMagnitude < 0.5f)
                return false;

            worldAxis = (root.rotation * axisInRoot).normalized;
            degreesPerUnit = angle / delta;
            return true;
        }

        private HumanPoseHandler Handler => _handler ?? (_handler = new HumanPoseHandler(Animator.avatar, Animator.transform));

        private readonly struct RootScope : IDisposable
        {
            private readonly Transform _root;
            private readonly Vector3 _position;
            private readonly Quaternion _rotation;
            private readonly Vector3 _scale;

            public RootScope(Transform root)
            {
                _root = root;
                _position = root.localPosition;
                _rotation = root.localRotation;
                _scale = root.localScale;
                root.localPosition = Vector3.zero;
                root.localRotation = Quaternion.identity;
                root.localScale = Vector3.one;
            }

            public void Dispose()
            {
                _root.localPosition = _position;
                _root.localRotation = _rotation;
                _root.localScale = _scale;
            }
        }

        private void CaptureAttachments()
        {
            for (int i = 0; i < _attachments.Count; i++)
            {
                var a = _attachments[i];
                if (a.target == null || a.bone == null)
                    continue;
                a.localPosition = a.bone.InverseTransformPoint(a.target.position);
                a.localRotation = Quaternion.Inverse(a.bone.rotation) * a.target.rotation;
                _attachments[i] = a;
            }
        }

        private void PlaceAttachments()
        {
            foreach (var a in _attachments)
            {
                if (a.target == null || a.bone == null)
                    continue;
                a.target.SetPositionAndRotation(a.bone.TransformPoint(a.localPosition), a.bone.rotation * a.localRotation);
                UnsupportedAvatarOps.CommitTransform(a.target);
            }
        }

        public void Reapply()
        {
            if (_stopped)
                return;
            Apply();
        }

        private void Apply()
        {
            EnsureMuscleTables();

            using (new RootScope(Animator.transform))
            {
                var handler = Handler;
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);

                if (BodyPose)
                {
                    for (int i = 0; i < pose.muscles.Length; i++)
                        pose.muscles[i] = 0f;
                }

                AppliedCurveCount = 0;
                if (Clip != null)
                {
                    foreach (var binding in AnimationUtility.GetCurveBindings(Clip))
                    {
                        if (binding.type != typeof(Animator) || !string.IsNullOrEmpty(binding.path))
                            continue;

                        int muscle = MuscleIndexFromCurveName(binding.propertyName);
                        if (muscle < 0 || !s_fingerMuscles.Contains(muscle))
                            continue;

                        var curve = AnimationUtility.GetEditorCurve(Clip, binding);
                        if (curve == null || curve.length == 0)
                            continue;

                        pose.muscles[muscle] = curve.Evaluate(0f);
                        AppliedCurveCount++;
                    }
                }

                if (FingerMuscleValues != null)
                {
                    int n = Mathf.Min(FingerMuscleValues.Length, pose.muscles.Length);
                    for (int i = 0; i < n; i++)
                    {
                        if (s_fingerMuscles.Contains(i) && !float.IsNaN(FingerMuscleValues[i]))
                            pose.muscles[i] = FingerMuscleValues[i];
                    }
                }

                handler.SetHumanPose(ref pose);
            }

            if (!BodyPose)
                _snapshot.RestoreExcept(_fingerTransforms);

            RequestSceneUpdate();
        }

        public void MeasureDrift(out float maxPosition, out float maxAngle, out Transform worst)
        {
            _snapshot.MeasureDrift(_fingerTransforms, out maxPosition, out maxAngle, out worst);
        }

        public float MaxFingerAngle()
        {
            var nonFinger = new HashSet<Transform>();
            foreach (var kv in _snapshot.Bones)
                if (!_fingerTransforms.Contains(kv.Key))
                    nonFinger.Add(kv.Key);

            _snapshot.MeasureDrift(nonFinger, out _, out float max, out _);
            return max;
        }

        private static void RequestSceneUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Trace?.Invoke("preview: プレイモード移行で停止");
                StopActive();
            }
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (Active != null && !Active.StopOnSceneSaving)
                return;
            Trace?.Invoke("preview: シーン保存で停止");
            StopActive();
        }

        private static void EnsureMuscleTables()
        {
            if (s_muscleIndexByName != null)
                return;

            s_muscleIndexByName = new Dictionary<string, int>();
            string[] names = HumanTrait.MuscleName;
            for (int i = 0; i < names.Length; i++)
                s_muscleIndexByName[names[i]] = i;

            s_fingerMuscles = new HashSet<int>();
            for (int bone = (int)HumanBodyBones.LeftThumbProximal; bone <= (int)HumanBodyBones.RightLittleDistal; bone++)
            {
                for (int dof = 0; dof < 3; dof++)
                {
                    int muscle = HumanTrait.MuscleFromBone(bone, dof);
                    if (muscle >= 0)
                        s_fingerMuscles.Add(muscle);
                }
            }
        }

        public static int MuscleIndexFromCurveName(string propertyName)
        {
            EnsureMuscleTables();

            if (string.IsNullOrEmpty(propertyName))
                return -1;

            string muscleName = null;
            var m = k_stretched.Match(propertyName);
            if (m.Success)
                muscleName = $"{m.Groups[1].Value} {m.Groups[2].Value} {m.Groups[3].Value} Stretched";
            else
            {
                m = k_spread.Match(propertyName);
                if (m.Success)
                    muscleName = $"{m.Groups[1].Value} {m.Groups[2].Value} Spread";
            }

            if (muscleName == null)
                muscleName = propertyName;

            return s_muscleIndexByName.TryGetValue(muscleName, out int index) ? index : -1;
        }
    }
}

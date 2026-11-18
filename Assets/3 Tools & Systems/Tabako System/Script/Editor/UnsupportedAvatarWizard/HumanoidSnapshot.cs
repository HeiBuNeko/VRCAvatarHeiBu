using System.Collections.Generic;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    internal sealed class HumanoidSnapshot
    {
        private struct Entry
        {
            public Transform transform;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Dictionary<Transform, HumanBodyBones> _bones = new Dictionary<Transform, HumanBodyBones>();

        public IReadOnlyDictionary<Transform, HumanBodyBones> Bones => _bones;

        public static HumanoidSnapshot Capture(Animator animator)
        {
            var snapshot = new HumanoidSnapshot();
            if (animator == null)
                return snapshot;

            snapshot.Add(animator.transform, HumanBodyBones.LastBone);

            if (!HumanoidBones.IsHumanoid(animator))
                return snapshot;

            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                var t = HumanoidBones.Get(animator, bone);
                if (t != null)
                    snapshot.Add(t, bone);
            }

            Transform hips = HumanoidBones.Get(animator, HumanBodyBones.Hips);
            for (Transform t = hips; t != null && t != animator.transform.parent; t = t.parent)
                snapshot.Add(t, HumanBodyBones.LastBone);

            var skeletonNames = new HashSet<string>();
            foreach (var skeletonBone in animator.avatar.humanDescription.skeleton)
                skeletonNames.Add(skeletonBone.name);

            if (skeletonNames.Count > 0)
            {
                foreach (var t in animator.GetComponentsInChildren<Transform>(true))
                {
                    if (skeletonNames.Contains(t.name))
                        snapshot.Add(t, HumanBodyBones.LastBone);
                }
            }

            return snapshot;
        }

        private void Add(Transform t, HumanBodyBones bone)
        {
            if (t == null || _bones.ContainsKey(t))
                return;

            _bones.Add(t, bone);
            _entries.Add(new Entry
            {
                transform = t,
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale,
            });
        }

        public void Restore()
        {
            RestoreExcept(null);
        }

        public void RestoreExcept(HashSet<Transform> keep)
        {
            foreach (var e in _entries)
            {
                if (e.transform == null)
                    continue;
                if (keep != null && keep.Contains(e.transform))
                    continue;

                e.transform.localPosition = e.localPosition;
                e.transform.localRotation = e.localRotation;
                e.transform.localScale = e.localScale;
            }
        }

        public void MeasureDrift(HashSet<Transform> exclude, out float maxPosition, out float maxAngle, out Transform worst)
        {
            maxPosition = 0f;
            maxAngle = 0f;
            worst = null;

            foreach (var e in _entries)
            {
                if (e.transform == null)
                    continue;
                if (exclude != null && exclude.Contains(e.transform))
                    continue;

                float p = Vector3.Distance(e.transform.localPosition, e.localPosition);
                float a = Quaternion.Angle(e.transform.localRotation, e.localRotation);
                if (p > maxPosition || a > maxAngle)
                    worst = e.transform;
                maxPosition = Mathf.Max(maxPosition, p);
                maxAngle = Mathf.Max(maxAngle, a);
            }
        }
    }
}

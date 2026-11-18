using System.Collections.Generic;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    internal sealed class WizardUndoState : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public Transform target;
            public Transform bone;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        [SerializeField] private List<Entry> m_entries = new List<Entry>();

        public int Count => m_entries.Count;

        public void Initialize(IReadOnlyList<FingerPosePreview.BoneAttachment> attachments, Transform fallbackBone)
        {
            m_entries.Clear();
            if (attachments == null)
                return;
            foreach (var a in attachments)
            {
                if (a.Target == null || IndexOf(a.Target) >= 0)
                    continue;
                m_entries.Add(new Entry { target = a.Target, bone = a.Bone != null ? a.Bone : fallbackBone });
            }
            CaptureAll();
        }

        public bool Contains(Transform target) => IndexOf(target) >= 0;

        private int IndexOf(Transform target)
        {
            if (target == null)
                return -1;
            for (int i = 0; i < m_entries.Count; i++)
            {
                if (m_entries[i].target == target)
                    return i;
            }
            return -1;
        }

        public void Capture(Transform target)
        {
            int i = IndexOf(target);
            if (i < 0)
                return;
            var e = m_entries[i];
            if (e.bone == null)
                return;
            e.localPosition = e.bone.InverseTransformPoint(target.position);
            e.localRotation = Quaternion.Inverse(e.bone.rotation) * target.rotation;
            m_entries[i] = e;
        }

        public void CaptureAll()
        {
            for (int i = 0; i < m_entries.Count; i++)
            {
                if (m_entries[i].target != null)
                    Capture(m_entries[i].target);
            }
        }

        public List<string> FindDrifted(float threshold)
        {
            var list = new List<string>();
            foreach (var e in m_entries)
            {
                if (e.target == null || e.bone == null)
                    continue;
                Vector3 current = e.bone.InverseTransformPoint(e.target.position);
                float d = (current - e.localPosition).magnitude;
                if (d > threshold)
                    list.Add($"{e.target.name} {d * 1000f:F1}mm (bone {e.bone.name})");
            }
            return list;
        }

        public void Apply()
        {
            foreach (var e in m_entries)
            {
                if (e.target == null || e.bone == null)
                    continue;
                e.target.SetPositionAndRotation(e.bone.TransformPoint(e.localPosition), e.bone.rotation * e.localRotation);
                UnsupportedAvatarOps.CommitTransform(e.target);
            }
        }
    }
}

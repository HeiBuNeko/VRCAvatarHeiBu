using System.Collections.Generic;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.runtime
{
    [AddComponentMenu("Satania Tabako/Satabako Bone Reference")]
    [DisallowMultipleComponent]
    public class SatabakoBoneReference : SatabakoBehaviour
    {
        [System.Serializable]
        public struct Entry
        {
            public HumanBodyBones bone;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        public List<Entry> bones = new List<Entry>();

        public bool TryGet(HumanBodyBones bone, out Entry entry)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].bone == bone)
                {
                    entry = bones[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }

        public void Set(Entry entry)
        {
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].bone == entry.bone)
                {
                    bones[i] = entry;
                    return;
                }
            }
            bones.Add(entry);
        }

        public static Entry Capture(HumanBodyBones bone, Transform boneTransform, Transform avatarRoot, Vector3 effectiveScale)
        {
            return new Entry
            {
                bone = bone,
                position = avatarRoot.InverseTransformPoint(boneTransform.position),
                rotation = Quaternion.Inverse(avatarRoot.rotation) * boneTransform.rotation,
                scale = Divide(effectiveScale, avatarRoot.lossyScale),
            };
        }

        public static Vector3 Ratio(Vector3 now, Vector3 reference) => Divide(now, reference);

        public static Vector3 Divide(Vector3 v, Vector3 s)
        {
            return new Vector3(Safe(v.x, s.x), Safe(v.y, s.y), Safe(v.z, s.z));
        }

        private static float Safe(float a, float b) => Mathf.Abs(b) > 1e-6f ? a / b : a;
    }
}

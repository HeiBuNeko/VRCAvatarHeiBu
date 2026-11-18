using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    internal struct PalmFrame
    {
        public bool IsValid;
        public bool IsLeft;
        public Transform Hand;
        public Vector3 Origin;
        public Quaternion Rotation;
        public float HandLength;
        public int FingerBoneCount;
        public bool HandLengthEstimated;

        public Vector3 FingerAxis => Rotation * Vector3.right;
        public Vector3 PalmNormal => Rotation * Vector3.up;

        public static PalmFrame Build(Animator animator, bool left)
        {
            var frame = new PalmFrame { IsLeft = left };
            if (!HumanoidBones.IsHumanoid(animator))
                return frame;

            Transform hand = HumanoidBones.Get(animator, left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            Transform indexProximal = HumanoidBones.Get(animator, left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
            Transform middleProximal = HumanoidBones.Get(animator, left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            Transform littleProximal = HumanoidBones.Get(animator, left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            Transform ringProximal = HumanoidBones.Get(animator, left ? HumanBodyBones.LeftRingProximal : HumanBodyBones.RightRingProximal);

            if (hand == null || middleProximal == null)
                return frame;

            if (littleProximal == null)
                littleProximal = ringProximal;
            if (indexProximal == null || littleProximal == null)
                return frame;

            Vector3 origin = hand.position;
            Vector3 x = middleProximal.position - origin;
            Vector3 g = indexProximal.position - littleProximal.position;
            if (x.sqrMagnitude < 1e-12f)
                return frame;
            x.Normalize();

            Vector3 c = Vector3.Cross(x, g);
            c -= Vector3.Dot(c, x) * x;
            if (c.sqrMagnitude < 1e-12f)
                return frame;

            Vector3 y = (left ? -1f : 1f) * c.normalized;
            Vector3 z = Vector3.Cross(x, y);

            frame.Hand = hand;
            frame.Origin = origin;
            frame.Rotation = Quaternion.LookRotation(z, y);

            Transform middleDistal = HumanoidBones.Get(animator, left ? HumanBodyBones.LeftMiddleDistal : HumanBodyBones.RightMiddleDistal);
            Transform middleIntermediate = HumanoidBones.Get(animator, left ? HumanBodyBones.LeftMiddleIntermediate : HumanBodyBones.RightMiddleIntermediate);
            if (middleDistal != null)
                frame.HandLength = Vector3.Distance(middleDistal.position, origin);
            else if (middleIntermediate != null)
            {
                frame.HandLength = Vector3.Distance(middleIntermediate.position, origin) / 0.8f;
                frame.HandLengthEstimated = true;
            }
            else
            {
                frame.HandLength = Vector3.Distance(middleProximal.position, origin) / 0.5f;
                frame.HandLengthEstimated = true;
            }

            int first = (int)(left ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal);
            for (int i = 0; i < 15; i++)
            {
                if (HumanoidBones.Get(animator, (HumanBodyBones)(first + i)) != null)
                    frame.FingerBoneCount++;
            }

            frame.IsValid = frame.HandLength > 1e-4f;
            return frame;
        }

        public Vector3 ToWorld(Vector3 palmLocal) => Origin + Rotation * (palmLocal * HandLength);
        public Vector3 ToPalm(Vector3 world) => Quaternion.Inverse(Rotation) * (world - Origin) / HandLength;
    }
}

using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class PlacementEstimator
    {
        public struct Pose
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        public static readonly Vector3 CaseOffset = new Vector3(0.566f, 0.238f, -0.111f);
        public static readonly Quaternion CaseRotation = Normalized(-0.642f, -0.210f, -0.270f, 0.686f);

        public static readonly Vector3 TabakoOffset = new Vector3(0.997f, 0.328f, 0.031f);
        public static readonly Quaternion TabakoRotation = Normalized(-0.274f, -0.090f, 0.692f, 0.662f);

        public static readonly Vector3 LighterOffset = new Vector3(0.466f, 0.180f, 0.171f);
        public static readonly Quaternion LighterRotation = Normalized(-0.696f, -0.301f, -0.041f, 0.651f);

        public const float CaseScalePerHandLength = 6.68f;
        public const float LighterScalePerHandLength = 5.98f;

        public static readonly Vector3 HandContactCenter = new Vector3(0.45f, 0.10f, 0.04f);
        public const float HandContactRadiusPerHandLength = 0.49f;
        public const float HandContactHeightPerHandLength = 1.5f;

        public const float PocketRadius = 0.11f;
        public const float PocketZ = 0.066f;
        public const float PocketYAboveHips = 0.02f;

        public static readonly Vector3 TabakoHeadFromMouth = new Vector3(0f, -0.0061f, 0.0238f);

        public const string TabakoPivotObjectName = "LipSync用";
        public static readonly Vector3 TabakoPivotOffset = new Vector3(0f, -0.0098f, 0f);

        public static readonly Vector3 TabakoPivotFromMid2_Gesture = new Vector3(0.016f, -0.008f, 0.003f);
        public static readonly Vector3 TabakoTipDirection_Gesture = new Vector3(0.675f, 0.664f, -0.323f).normalized;
        public static readonly Vector3 TabakoPivotFromMid2_Rest = new Vector3(0.004f, -0.028f, 0.007f);
        public static readonly Vector3 TabakoTipDirection_Rest = new Vector3(0.910f, -0.043f, -0.411f).normalized;

        public struct FingerFrame
        {
            public bool IsValid;
            public Vector3 Mid2;
            public Quaternion Rotation;
            public float Gap;
            public bool HasDistal;
        }

        public static FingerFrame BuildRightFingerFrame(Animator animator)
        {
            var frame = new FingerFrame();
            Transform i1 = HumanoidBones.Get(animator, HumanBodyBones.RightIndexProximal);
            Transform i2 = HumanoidBones.Get(animator, HumanBodyBones.RightIndexIntermediate);
            Transform i3 = HumanoidBones.Get(animator, HumanBodyBones.RightIndexDistal);
            Transform m1 = HumanoidBones.Get(animator, HumanBodyBones.RightMiddleProximal);
            Transform m2 = HumanoidBones.Get(animator, HumanBodyBones.RightMiddleIntermediate);
            Transform m3 = HumanoidBones.Get(animator, HumanBodyBones.RightMiddleDistal);
            if (i1 == null || i2 == null || m1 == null || m2 == null)
                return frame;

            frame.HasDistal = i3 != null && m3 != null;
            Vector3 pi3 = i3 != null ? i3.position : i2.position + (i2.position - i1.position);
            Vector3 pm3 = m3 != null ? m3.position : m2.position + (m2.position - m1.position);

            Vector3 f = (pi3 - i1.position) + (pm3 - m1.position);
            if (f.sqrMagnitude < 1e-12f)
                return frame;
            f.Normalize();

            Vector3 gap = i2.position - m2.position;
            Vector3 z = gap - Vector3.Dot(gap, f) * f;
            if (z.sqrMagnitude < 1e-12f)
                return frame;
            z.Normalize();

            Vector3 y = Vector3.Cross(z, f);
            frame.Rotation = Quaternion.LookRotation(z, y);
            frame.Mid2 = (i2.position + m2.position) * 0.5f;
            frame.Gap = gap.magnitude;
            frame.IsValid = true;
            return frame;
        }

        public static bool TryGetTabakoPivot(TabakoTemplateRefs refs, out Vector3 pivotLocal, out Vector3 tipDirLocal, out Vector3 upDirLocal, out string why)
        {
            pivotLocal = default;
            tipDirLocal = Vector3.down;
            upDirLocal = Vector3.forward;
            why = null;

            if (refs == null || refs.TabakoMesh == null || refs.CT_Tabako == null)
            {
                why = "タバコのメッシュが見つかりません";
                return false;
            }

            Transform lip = null;
            foreach (var t in refs.TabakoMesh.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == TabakoPivotObjectName)
                {
                    lip = t;
                    break;
                }
            }
            if (lip == null)
            {
                why = $"{TabakoPivotObjectName} が見つかりません";
                return false;
            }

            var chain = new System.Collections.Generic.List<Transform>();
            for (Transform t = lip; t != null && t != refs.TabakoMesh.transform; t = t.parent)
                chain.Add(t);
            chain.Reverse();

            Matrix4x4 m = Matrix4x4.TRS(refs.CT_Tabako.localPosition, refs.CT_Tabako.localRotation, refs.CT_Tabako.localScale);
            foreach (var t in chain)
                m = m * Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);

            pivotLocal = m.MultiplyPoint3x4(TabakoPivotOffset);
            tipDirLocal = refs.CT_Tabako.localRotation * Vector3.down;
            upDirLocal = refs.CT_Tabako.localRotation * Vector3.forward;
            return true;
        }

        public static Pose EstimateTabakoByFingers(in FingerFrame fingers, in PalmFrame palm, Vector3 pivotLocal, Vector3 tipDirLocal, bool gesturePose, float alongFingerOffset)
        {
            Vector3 pivotOffset = gesturePose ? TabakoPivotFromMid2_Gesture : TabakoPivotFromMid2_Rest;
            pivotOffset.x += alongFingerOffset;
            Vector3 tipLocal = gesturePose ? TabakoTipDirection_Gesture : TabakoTipDirection_Rest;

            Vector3 pivotWorld = fingers.Mid2 + fingers.Rotation * pivotOffset;
            Vector3 tipWorld = fingers.Rotation * tipLocal;

            Pose prior = EstimateTabako(palm);
            Quaternion rotation = Quaternion.FromToRotation(prior.rotation * tipDirLocal, tipWorld) * prior.rotation;

            return new Pose
            {
                position = pivotWorld - rotation * pivotLocal,
                rotation = rotation,
            };
        }

        public static Pose EstimateCase(in PalmFrame frame) => FromPrior(frame, CaseOffset, CaseRotation);
        public static Pose EstimateLighter(in PalmFrame frame) => FromPrior(frame, LighterOffset, LighterRotation);
        public static Pose EstimateTabako(in PalmFrame frame) => FromPrior(frame, TabakoOffset, TabakoRotation);

        private static Pose FromPrior(in PalmFrame frame, Vector3 offset, Quaternion rotation)
        {
            return new Pose
            {
                position = frame.ToWorld(offset),
                rotation = frame.Rotation * rotation,
            };
        }

        public static float EstimateCaseScale(float handLength) => CaseScalePerHandLength * handLength;
        public static float EstimateLighterScale(float handLength) => LighterScalePerHandLength * handLength;

        public static void MirrorPalmPose(in PalmFrame from, in PalmFrame to, Vector3 worldPosition, Quaternion worldRotation, out Vector3 mirroredPosition, out Quaternion mirroredRotation)
        {
            Vector3 palm = from.ToPalm(worldPosition);
            palm.z = -palm.z;
            mirroredPosition = to.ToWorld(palm);
            Quaternion local = Quaternion.Inverse(from.Rotation) * worldRotation;
            mirroredRotation = to.Rotation * new Quaternion(-local.x, -local.y, local.z, local.w);
        }

        public static void EstimateHandContact(in PalmFrame frame, out Vector3 localPosition, out Quaternion localRotation, out float radius, out float height)
        {
            Vector3 center = HandContactCenter;
            if (frame.IsLeft)
                center.z = -center.z;

            Vector3 worldCenter = frame.ToWorld(center);
            Quaternion worldRotation = Quaternion.FromToRotation(Vector3.up, frame.FingerAxis);

            localPosition = ContactSpace.WorldToLocal(frame.Hand, worldCenter);
            localRotation = Quaternion.Inverse(frame.Hand.rotation) * worldRotation;
            radius = HandContactRadiusPerHandLength * frame.HandLength;
            height = HandContactHeightPerHandLength * frame.HandLength;
        }

        public enum MouthMethod
        {
            None,
            Viseme,
            ViewPositionHead,
            ViewPosition,
        }

        public static bool TryEstimateMouth(VRCAvatarDescriptor descriptor, Animator animator, Transform avatarRoot, out Vector3 rootLocal, out MouthMethod method)
        {
            method = MouthMethod.None;
            if (VisemeMouthEstimator.TryEstimate(descriptor, avatarRoot, out rootLocal, out _))
            {
                method = MouthMethod.Viseme;
                return true;
            }

            if (TryEstimateMouthByViewPosition(descriptor, animator, avatarRoot, out rootLocal, out bool usedHead))
            {
                method = usedHead ? MouthMethod.ViewPositionHead : MouthMethod.ViewPosition;
                return true;
            }

            return false;
        }

        public static bool TryEstimateMouthByViewPosition(VRCAvatarDescriptor descriptor, Animator animator, Transform avatarRoot, out Vector3 rootLocal, out bool usedHead)
        {
            rootLocal = default;
            usedHead = false;
            if (descriptor == null || avatarRoot == null)
                return false;

            Vector3 vp = descriptor.ViewPosition;
            Transform head = HumanoidBones.Get(animator, HumanBodyBones.Head);

            if (head != null)
            {
                float h1 = vp.y - avatarRoot.InverseTransformPoint(head.position).y;
                if (h1 >= 0.02f && h1 <= 0.12f)
                {
                    rootLocal = new Vector3(vp.x, vp.y - (0.012f + 0.693f * h1), vp.z + 0.0056f + 0.034f * h1);
                    usedHead = true;
                    return true;
                }
            }

            rootLocal = vp + new Vector3(0f, -0.046f, 0.008f);
            return true;
        }

        public static Vector3 EstimateTabakoHead(Vector3 mouthRootLocal) => mouthRootLocal + TabakoHeadFromMouth;

        public static bool TryEstimatePocket(Animator animator, Transform avatarRoot, float caseScale, bool left, out Vector3 rootLocal)
        {
            rootLocal = default;
            Transform hips = HumanoidBones.Get(animator, HumanBodyBones.Hips);
            if (hips == null || avatarRoot == null)
                return false;

            float x = 0.032f + 0.105f * caseScale;
            float y = avatarRoot.InverseTransformPoint(hips.position).y + PocketYAboveHips;
            rootLocal = new Vector3(left ? -x : x, y, PocketZ);
            return true;
        }

        private static Quaternion Normalized(float x, float y, float z, float w)
        {
            return Quaternion.Normalize(new Quaternion(x, y, z, w));
        }
    }

    internal static class ContactSpace
    {
        public static Vector3 LocalToWorld(Transform bone, Vector3 local)
        {
            return bone.position + bone.rotation * local;
        }

        public static Vector3 WorldToLocal(Transform bone, Vector3 world)
        {
            return Quaternion.Inverse(bone.rotation) * (world - bone.position);
        }
    }
}

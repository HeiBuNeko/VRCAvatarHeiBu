using UnityEngine;
using VRC.Dynamics;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class TabakoValueCopier
    {
        public struct Options
        {
            public bool Targets;
            public bool Contacts;
            public bool Scales;
            public bool Gestures;
        }

        public struct Result
        {
            public int Targets;
            public int Contacts;
            public int Scales;
            public int Gestures;
            public bool UsedRootSpace;
            public bool UsedPalmFrames;
        }

        public static Result Copy(TabakoTemplateRefs from, TabakoTemplateRefs to, Options options)
        {
            var result = new Result();
            if (from == null || to == null || from.Script == null || to.Script == null)
                return result;

            bool boneRelative = from.IsHumanoid && to.IsHumanoid;
            bool sameAvatar = boneRelative && from.Animator.avatar == to.Animator.avatar;

            if (options.Targets)
            {
                var pairs = new (Transform src, Transform dst)[]
                {
                    (from.CaseR, to.CaseR), (from.CaseL, to.CaseL),
                    (from.TabakoR, to.TabakoR), (from.TabakoL, to.TabakoL), (from.TabakoHead, to.TabakoHead),
                    (from.LighterR, to.LighterR), (from.LighterL, to.LighterL),
                    (from.MouthTarget, to.MouthTarget),
                };
                foreach (var (src, dst) in pairs)
                {
                    if (CopyTransform(from, to, src, dst, boneRelative, ref result))
                        result.Targets++;
                }
            }

            if (options.Contacts)
            {
                foreach (var (src, dst) in new (ContactBase src, ContactBase dst)[] { (from.PocketR, to.PocketR), (from.PocketL, to.PocketL) })
                {
                    if (src == null || dst == null)
                        continue;
                    CopyTransform(from, to, src.transform, dst.transform, boneRelative, ref result);
                    UnsupportedAvatarOps.CopyContactShape(src, dst);
                    result.Contacts++;
                }
                foreach (var (src, dst) in new (ContactBase src, ContactBase dst)[] { (from.SenderR, to.SenderR), (from.SenderL, to.SenderL), (from.CaseReceiverR, to.CaseReceiverR), (from.CaseReceiverL, to.CaseReceiverL) })
                {
                    if (CopyHandContactShape(from, to, src, dst, sameAvatar, ref result))
                        result.Contacts++;
                }
            }

            if (options.Scales)
            {
                if (from.CT_Case != null && to.CT_Case != null)
                {
                    UnsupportedAvatarOps.SetCaseScale(to, from.CT_Case.localScale.x);
                    result.Scales += to.CT_Tabako != null ? 2 : 1;
                }
                if (from.CT_Lighter != null && to.CT_Lighter != null)
                {
                    UnsupportedAvatarOps.SetLighterScale(to, from.CT_Lighter.localScale.x);
                    result.Scales++;
                }
            }

            if (options.Gestures)
            {
                foreach (GestureKind kind in System.Enum.GetValues(typeof(GestureKind)))
                {
                    var over = UnsupportedAvatarOps.FindGestureOverride(from.Root, kind);
                    if (over == null || over.CustomMotion == null)
                        continue;
                    UnsupportedAvatarOps.SetGestureOverride(to, kind, over.CustomMotion);
                    result.Gestures++;
                }
            }

            UnsupportedAvatarOps.RefreshScene();
            return result;
        }

        private static bool CopyTransform(TabakoTemplateRefs from, TabakoTemplateRefs to, Transform src, Transform dst, bool boneRelative, ref Result result)
        {
            if (src == null || dst == null)
                return false;

            Transform fromRef = boneRelative ? (from.BoneTransformOf(src) ?? from.AvatarRoot) : null;
            Transform toRef = boneRelative ? (to.BoneTransformOf(dst) ?? to.AvatarRoot) : null;
            if (fromRef == null || toRef == null)
            {
                fromRef = from.Root;
                toRef = to.Root;
                result.UsedRootSpace = true;
            }

            Vector3 local = fromRef.InverseTransformPoint(src.position);
            Quaternion localRotation = Quaternion.Inverse(fromRef.rotation) * src.rotation;
            UnsupportedAvatarOps.RecordTransform(dst, "Copy values");
            dst.SetPositionAndRotation(toRef.TransformPoint(local), toRef.rotation * localRotation);
            UnsupportedAvatarOps.CommitTransform(dst);
            UnsupportedAvatarOps.TransformMoved?.Invoke(dst);
            return true;
        }

        private static bool CopyHandContactShape(TabakoTemplateRefs from, TabakoTemplateRefs to, ContactBase src, ContactBase dst, bool sameAvatar, ref Result result)
        {
            if (src == null || dst == null)
                return false;

            if (!sameAvatar && from.IsHumanoid && to.IsHumanoid)
            {
                bool left = TabakoTemplateRefs.BoneOf(src.transform) == HumanBodyBones.LeftHand;
                var fromPalm = PalmFrame.Build(from.Animator, left);
                var toPalm = PalmFrame.Build(to.Animator, left);
                if (fromPalm.IsValid && toPalm.IsValid)
                {
                    Vector3 world = ContactSpace.LocalToWorld(src.transform, src.position);
                    Quaternion worldRotation = src.transform.rotation * src.rotation;
                    Vector3 newWorld = toPalm.ToWorld(fromPalm.ToPalm(world));
                    Quaternion newRotation = toPalm.Rotation * (Quaternion.Inverse(fromPalm.Rotation) * worldRotation);
                    float scale = toPalm.HandLength / fromPalm.HandLength;
                    UnsupportedAvatarOps.SetContactShape(dst, ContactSpace.WorldToLocal(dst.transform, newWorld), Quaternion.Inverse(dst.transform.rotation) * newRotation, src.radius * scale, src.height * scale, src.shapeType);
                    result.UsedPalmFrames = true;
                    return true;
                }
            }

            UnsupportedAvatarOps.CopyContactShape(src, dst);
            return true;
        }
    }
}

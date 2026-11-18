using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf.runtime;
using net.satania_shopping.tabakosystem.runtime;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.Contact.Components;

namespace net.satania_shopping.tabakosystem.editor
{
    internal sealed class TabakoTemplateRefs
    {
        public const int SourceRight = 0;
        public const int SourceLeft = 1;
        public const int SourceMouth = 2;

        public TabakoScriptBehaviour Script { get; private set; }
        public Transform Root => Script.transform;
        public Transform AvatarRoot { get; private set; }
        public Animator Animator { get; private set; }
        public VRCAvatarDescriptor Descriptor { get; private set; }

        public VRCParentConstraint PC_Case => Script.PC_Case;
        public VRCParentConstraint PC_Tabako => Script.PC_Tabako;
        public VRCParentConstraint PC_Lighter => Script.PC_Lighter;

        public Transform CT_Case => Script.CT_Case;
        public Transform CT_Tabako => Script.CT_Tabako;
        public Transform CT_Lighter => Script.CT_Lighter;
        public Transform CT_Mouth => Script.CT_Mouth;

        public Transform CaseR { get; private set; }
        public Transform CaseL { get; private set; }
        public Transform TabakoR { get; private set; }
        public Transform TabakoL { get; private set; }
        public Transform TabakoHead { get; private set; }
        public Transform LighterR { get; private set; }
        public Transform LighterL { get; private set; }
        public Transform MouthTarget { get; private set; }

        public VRCContactSender SenderR { get; private set; }
        public VRCContactSender SenderL { get; private set; }
        public VRCContactReceiver CaseReceiverR { get; private set; }
        public VRCContactReceiver CaseReceiverL { get; private set; }
        public VRCContactReceiver PocketR { get; private set; }
        public VRCContactReceiver PocketL { get; private set; }

        public GameObject CaseMesh { get; private set; }
        public GameObject TabakoMesh { get; private set; }
        public GameObject LighterMesh { get; private set; }
        public GameObject MouthContact { get; private set; }

        public bool IsHumanoid => HumanoidBones.IsHumanoid(Animator);

        public bool IsComplete =>
            CaseR != null && CaseL != null &&
            TabakoR != null && TabakoL != null && TabakoHead != null &&
            LighterR != null && LighterL != null &&
            CT_Case != null && CT_Tabako != null && CT_Lighter != null && CT_Mouth != null;

        public static TabakoTemplateRefs Resolve(TabakoScriptBehaviour script)
        {
            if (script == null)
                return null;

            var refs = new TabakoTemplateRefs { Script = script };

            refs.AvatarRoot = RuntimeUtil.FindAvatarInParents(script.transform);
            if (refs.AvatarRoot != null)
            {
                refs.Animator = refs.AvatarRoot.GetComponent<Animator>();
                refs.Descriptor = refs.AvatarRoot.GetComponent<VRCAvatarDescriptor>();
            }

            refs.CaseR = GetSource(script.PC_Case, SourceRight);
            refs.CaseL = GetSource(script.PC_Case, SourceLeft);
            refs.TabakoR = GetSource(script.PC_Tabako, SourceRight);
            refs.TabakoL = GetSource(script.PC_Tabako, SourceLeft);
            refs.TabakoHead = GetSource(script.PC_Tabako, SourceMouth);
            refs.LighterR = GetSource(script.PC_Lighter, SourceRight);
            refs.LighterL = GetSource(script.PC_Lighter, SourceLeft);
            refs.MouthTarget = ResolveMouthTarget(script.CT_Mouth);

            foreach (var sender in script.GetComponentsInChildren<VRCContactSender>(true))
            {
                switch (BoneOf(sender.transform))
                {
                    case HumanBodyBones.RightHand: refs.SenderR = refs.SenderR ?? sender; break;
                    case HumanBodyBones.LeftHand: refs.SenderL = refs.SenderL ?? sender; break;
                }
            }

            foreach (var receiver in script.GetComponentsInChildren<VRCContactReceiver>(true))
            {
                switch (BoneOf(receiver.transform))
                {
                    case HumanBodyBones.RightHand: refs.CaseReceiverR = refs.CaseReceiverR ?? receiver; break;
                    case HumanBodyBones.LeftHand: refs.CaseReceiverL = refs.CaseReceiverL ?? receiver; break;
                    case HumanBodyBones.Hips:
                        if (HasTag(receiver, "Righthand")) refs.PocketR = refs.PocketR ?? receiver;
                        else if (HasTag(receiver, "Lefthand")) refs.PocketL = refs.PocketL ?? receiver;
                        break;
                }
            }

            foreach (var follower in script.GetComponentsInChildren<IFollowSatabakoObject>(true))
            {
                switch (follower.TargetObject)
                {
                    case IFollowSatabakoObject.SatabakoObject.Case: refs.CaseMesh = refs.CaseMesh ?? follower.gameObject; break;
                    case IFollowSatabakoObject.SatabakoObject.Tabako: refs.TabakoMesh = refs.TabakoMesh ?? follower.gameObject; break;
                    case IFollowSatabakoObject.SatabakoObject.Lighter: refs.LighterMesh = refs.LighterMesh ?? follower.gameObject; break;
                    case IFollowSatabakoObject.SatabakoObject.Mouth: refs.MouthContact = refs.MouthContact ?? follower.gameObject; break;
                }
            }

            return refs;
        }

        private static Transform ResolveMouthTarget(Transform ctMouth)
        {
            if (ctMouth == null)
                return null;

            var constraint = ctMouth.GetComponent<VRCConstraintBase>();
            if (constraint != null)
            {
                var sources = constraint.Sources;
                for (int i = 0; i < sources.Count; i++)
                {
                    if (sources[i].SourceTransform != null)
                        return sources[i].SourceTransform;
                }
            }
            return ctMouth;
        }

        public static Transform GetSource(VRCParentConstraint constraint, int index)
        {
            if (constraint == null)
                return null;

            var sources = constraint.Sources;
            if (index < 0 || index >= sources.Count)
                return null;

            return sources[index].SourceTransform;
        }

        public static HumanBodyBones BoneOf(Transform t)
        {
            if (t == null)
                return HumanBodyBones.LastBone;

            var proxy = t.GetComponent<ModularAvatarBoneProxy>();
            return proxy != null ? proxy.boneReference : HumanBodyBones.LastBone;
        }

        public Transform BoneTransform(HumanBodyBones bone)
        {
            if (!IsHumanoid || bone == HumanBodyBones.LastBone)
                return null;

            return HumanoidBones.Get(Animator, bone);
        }

        public Transform BoneTransformOf(Transform followTarget) => BoneTransform(BoneOf(followTarget));

        public List<FingerPosePreview.BoneAttachment> BoneProxyAttachments(bool includeBoneLocked = true)
        {
            var list = new List<FingerPosePreview.BoneAttachment>();
            if (Script == null)
                return list;

            foreach (var proxy in Script.GetComponentsInChildren<ModularAvatarBoneProxy>(true))
            {
                if (!includeBoneLocked && proxy.attachmentMode == BoneProxyAttachmentMode.AsChildAtRoot)
                    continue;
                Transform bone = BoneTransform(proxy.boneReference);
                if (bone != null)
                    list.Add(new FingerPosePreview.BoneAttachment(proxy.transform, bone));
            }
            return list;
        }

        private static bool HasTag(ContactBase contact, string keyword)
        {
            if (contact == null || contact.collisionTags == null)
                return false;

            foreach (var tag in contact.collisionTags)
            {
                if (!string.IsNullOrEmpty(tag) && tag.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}

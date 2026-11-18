using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using net.satania_shopping.tabakosystem.runtime;
using UnityEngine;
using VRC.Dynamics;

namespace net.satania_shopping.tabakosystem.ndmf
{
    public sealed class BoneReferencePass : Pass<BoneReferencePass>
    {
        public override string DisplayName => "Bone Reference Pass";
        private const float k_reportTolerance = 0.02f;
        private const float k_legacyTolerance = 0.05f;
        private const float k_reportMoveMm = 5f;
        private PluginDefinition.Singleton singleton => PluginDefinition.Singleton.instance;

        protected override void Execute(BuildContext ctx)
        {
            var script = singleton.tabakoScriptBehaviour;
            if (script == null)
                return;

            var animator = ctx.AvatarRootObject.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                return;

            var root = ctx.AvatarRootTransform;
            var proxies = script.GetComponentsInChildren<ModularAvatarBoneProxy>(true);
            var reference = script.GetComponent<SatabakoBoneReference>();
            if (reference == null)
            {
                WarnLegacy(animator, root, proxies, script);
                return;
            }

            var adjusted = new SortedDictionary<string, string>();
            foreach (var proxy in proxies)
            {
                if (proxy.boneReference == HumanBodyBones.LastBone || !reference.TryGet(proxy.boneReference, out var entry))
                    continue;
                Transform bone = animator.GetBoneTransform(proxy.boneReference);
                if (bone == null)
                    continue;

                Vector3 now = RelativeScale(bone, root);
                Vector3 ratio = SatabakoBoneReference.Ratio(now, entry.scale);
                float movedMm = (root.InverseTransformPoint(bone.position) - entry.position).magnitude * 1000f;
                if (proxy.attachmentMode == BoneProxyAttachmentMode.AsChildAtRoot)
                    ScaleContactShape(proxy.GetComponent<ContactBase>(), ratio);
                else
                    Replace(proxy.transform, bone, root, entry, now);

                if (Differs(ratio, k_reportTolerance) || movedMm > k_reportMoveMm)
                    adjusted[bone.name] = Describe(ratio, movedMm);
            }

            Object.DestroyImmediate(reference);

            if (adjusted.Count > 0)
            {
                string list = string.Join(", ", adjusted.Select(kv => $"{kv.Key} {kv.Value}"));
                SatabakoErrorReport.ReportInformation($"ボーンのスケールや位置が対応時と違うため、ケース・タバコ・ライター・コンタクトの位置を合わせました: {list}", script);
            }
        }

        public static Vector3 EffectiveScale(Transform bone)
        {
            var adjuster = bone.GetComponent<ModularAvatarScaleAdjuster>();
            return adjuster != null ? Vector3.Scale(bone.lossyScale, adjuster.Scale) : bone.lossyScale;
        }

        public static Vector3 RelativeScale(Transform bone, Transform avatarRoot)
        {
            return SatabakoBoneReference.Divide(EffectiveScale(bone), avatarRoot.lossyScale);
        }

        private static void Replace(Transform target, Transform bone, Transform root, SatabakoBoneReference.Entry entry, Vector3 nowScale)
        {
            Vector3 targetRoot = root.InverseTransformPoint(target.position);
            Quaternion targetRotRoot = Quaternion.Inverse(root.rotation) * target.rotation;

            Vector3 offset = Quaternion.Inverse(entry.rotation) * (targetRoot - entry.position);
            Vector3 normalized = SatabakoBoneReference.Divide(offset, entry.scale);
            Quaternion localRotation = Quaternion.Inverse(entry.rotation) * targetRotRoot;

            Vector3 boneRoot = root.InverseTransformPoint(bone.position);
            Quaternion boneRotRoot = Quaternion.Inverse(root.rotation) * bone.rotation;
            Vector3 newRoot = boneRoot + boneRotRoot * Vector3.Scale(nowScale, normalized);

            target.SetPositionAndRotation(root.TransformPoint(newRoot), root.rotation * boneRotRoot * localRotation);
        }

        private static void ScaleContactShape(ContactBase contact, Vector3 ratio)
        {
            if (contact == null)
                return;
            float uniform = (ratio.x + ratio.y + ratio.z) / 3f;
            contact.position = Vector3.Scale(ratio, contact.position);
            contact.radius *= uniform;
            contact.height *= uniform;
        }

        private static void WarnLegacy(Animator animator, Transform root, ModularAvatarBoneProxy[] proxies, TabakoScriptBehaviour script)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform baseline = hips != null && hips.parent != null ? hips.parent : root;
            Vector3 baseScale = SatabakoBoneReference.Divide(baseline.lossyScale, root.lossyScale);

            var changed = new SortedDictionary<string, Vector3>();
            foreach (var bone in proxies.Select(p => p.boneReference).Where(b => b != HumanBodyBones.LastBone).Distinct())
            {
                Transform t = animator.GetBoneTransform(bone);
                if (t == null)
                    continue;
                Vector3 ratio = SatabakoBoneReference.Ratio(RelativeScale(t, root), baseScale);
                if (Differs(ratio, k_legacyTolerance))
                    changed[t.name] = ratio;
            }

            if (changed.Count == 0)
                return;

            string list = string.Join(", ", changed.Select(kv => $"{kv.Key} ×{Describe(kv.Value)}"));
            SatabakoErrorReport.ReportWarning($"ボーンのスケールが変更されています ({list})。このプレハブには対応時のボーン情報が無いため、ケース・タバコ・ライターやコンタクトの位置がずれることがあります。非対応アバターに対応するツールで調整し直して保存すると直ります。", script);
        }

        private static bool Differs(Vector3 ratio, float tolerance)
        {
            return Mathf.Abs(ratio.x - 1f) > tolerance || Mathf.Abs(ratio.y - 1f) > tolerance || Mathf.Abs(ratio.z - 1f) > tolerance;
        }

        private static string Describe(Vector3 ratio, float movedMm)
        {
            var parts = new List<string>();
            if (Differs(ratio, k_reportTolerance))
                parts.Add("×" + Describe(ratio));
            if (movedMm > k_reportMoveMm)
                parts.Add($"{movedMm:F0}mm");
            return string.Join(" ", parts);
        }

        private static string Describe(Vector3 ratio)
        {
            if (Mathf.Abs(ratio.x - ratio.y) < 0.005f && Mathf.Abs(ratio.x - ratio.z) < 0.005f)
                return ratio.x.ToString("F2");
            return $"({ratio.x:F2}, {ratio.y:F2}, {ratio.z:F2})";
        }
    }
}

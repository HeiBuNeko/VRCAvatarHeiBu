using nadena.dev.modular_avatar.core;
using net.satania_shopping.tabakosystem.ndmf;
using net.satania_shopping.tabakosystem.runtime;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    internal static class BoneReferenceBaker
    {
        public static int Bake(TabakoTemplateRefs refs, bool withUndo = true)
        {
            if (refs == null || refs.Script == null || refs.AvatarRoot == null || !refs.IsHumanoid)
                return 0;

            var reference = refs.Script.GetComponent<SatabakoBoneReference>();
            if (reference == null)
                reference = withUndo ? Undo.AddComponent<SatabakoBoneReference>(refs.Script.gameObject) : refs.Script.gameObject.AddComponent<SatabakoBoneReference>();
            else if (withUndo)
                Undo.RecordObject(reference, "Bake bone reference");

            reference.bones.Clear();
            int count = 0;
            foreach (var proxy in refs.Script.GetComponentsInChildren<ModularAvatarBoneProxy>(true))
            {
                HumanBodyBones bone = proxy.boneReference;
                if (bone == HumanBodyBones.LastBone || reference.TryGet(bone, out _))
                    continue;
                Transform t = refs.BoneTransform(bone);
                if (t == null)
                    continue;
                reference.Set(SatabakoBoneReference.Capture(bone, t, refs.AvatarRoot, BoneReferencePass.EffectiveScale(t)));
                count++;
            }

            EditorUtility.SetDirty(reference);
            return count;
        }
    }
}

using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using net.satania_shopping.tabakosystem.runtime;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace net.satania_shopping.tabakosystem.ndmf
{
    public class IRemoveAvatarMaskPass : Pass<IRemoveAvatarMaskPass>
    {
        public override string DisplayName => "net.satania_shopping.tabakosystem.ndmf.IRemoveAvatarMaskPass";

        protected override void Execute(BuildContext ctx)
        {
            IRemoveAvatarMask[] removeMasks = ctx.AvatarRootObject.GetComponentsInChildren<IRemoveAvatarMask>(true);

            //同じLayerTypeを二重処理しないように記録 (ループ外で保持しないと毎回リセットされる)
            List<AnimLayerType> checkedLayerType = new List<AnimLayerType>();

            foreach (var removeMask in removeMasks)
            {
                if (removeMask != null)
                {
                    if (checkedLayerType.Contains(removeMask.animLayerType))
                        continue;

                    VRCAvatarDescriptor avatarDescriptor = ctx.VRChatAvatarDescriptor();
                    var controller = AvatarUtils.GetAvatarBaseLayer(avatarDescriptor, removeMask.animLayerType);
                    if (controller != null)
                    {
                        RemoveMask(controller, removeMask.removeTransformMask);
                    }

                    checkedLayerType.Add(removeMask.animLayerType);
                }
            }

            //削除
            foreach (var m in removeMasks)
            {
                if (m != null)
                    Object.DestroyImmediate(m);
            }
        }

        private void RemoveMask(AnimatorController controller, bool removeTransformMask)
        {
            var layers = controller.layers;
            foreach (var layer in layers)
            {
                if (layer != null && layer.avatarMask != null)
                {
                    if (IsHumanoidMask(layer.avatarMask))
                        layer.avatarMask = null;
                    else if (removeTransformMask && layer.avatarMask.transformCount > 0)
                        layer.avatarMask = null;
                }
            }

            controller.layers = layers;
        }

        private bool IsHumanoidMask(AvatarMask mask)
        {
            //AvatarMaskBodyPart 0~13
            for (int i = 0; i <= 13; i++)
            {
                if (mask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i))
                    return true;

            }

            return false;
        }
    }
}
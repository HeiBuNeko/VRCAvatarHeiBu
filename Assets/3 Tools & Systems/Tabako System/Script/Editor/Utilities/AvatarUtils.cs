using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

#nullable enable

namespace net.satania_shopping.tabakosystem
{
    public static class AvatarUtils
    {
        public static AnimatorController? GetAvatarBaseLayer(VRCAvatarDescriptor avatar, VRCAvatarDescriptor.AnimLayerType layerType)
        {
            RuntimeAnimatorController? runtimeAnimatorController = avatar.baseAnimationLayers.FirstOrDefault(layer => layer.type == layerType).animatorController;

            AnimatorController? controller = runtimeAnimatorController switch
            {
                AnimatorOverrideController overrideController => overrideController.runtimeAnimatorController as AnimatorController,
                AnimatorController animatorController => animatorController,
                _ => null
            };

            return controller;
        }
    }
}
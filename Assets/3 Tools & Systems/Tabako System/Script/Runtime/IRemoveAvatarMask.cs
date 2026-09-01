using UnityEngine;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace net.satania_shopping.tabakosystem.runtime
{

    [AddComponentMenu("Satania Tabako/Satabako I Remove AvatarMask")]
    public class IRemoveAvatarMask : SatabakoBehaviour
    {
        public AnimLayerType animLayerType = AnimLayerType.FX;

        /// <summary>
        /// Humanoidではなく、Transformだけが設定されているAvatarMaskも削除する
        /// </summary>
        public bool removeTransformMask = false;
    }
}
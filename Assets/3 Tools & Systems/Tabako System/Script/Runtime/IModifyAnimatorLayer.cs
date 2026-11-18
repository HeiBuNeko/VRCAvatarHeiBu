using UnityEngine;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace net.satania_shopping.tabakosystem.runtime
{
    [AddComponentMenu("Satania Tabako/Satabako I Modify Animator Layer")]
    public class IModifyAnimatorLayer : SatabakoBehaviour
    {
        public enum eAction
        {
            Remove,
            SetWeightZero
        }

        public AnimLayerType animLayerType = AnimLayerType.FX;
        public string layerName = "";
        public eAction action = eAction.Remove;
        public string[] expectedStateNames = new string[0];
        public bool strictStateCount = true;
    }
}

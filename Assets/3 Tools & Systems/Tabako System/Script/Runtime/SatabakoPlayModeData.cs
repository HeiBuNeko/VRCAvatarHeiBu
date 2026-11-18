using UnityEngine;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace net.satania_shopping.tabakosystem.runtime
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class SatabakoPlayModeData : MonoBehaviour
    {
        public Motion gestureCase;
        public Motion gestureTabako;
        public Motion gestureLighter;

        public Transform ctCase;
        public Transform ctTabako;
        public Transform ctLighter;
        public Transform ctMouth;

        public VRCParentConstraint pcCase;
        public VRCParentConstraint pcTabako;
        public VRCParentConstraint pcLighter;

        public void CopyFrom(TabakoScriptBehaviour script)
        {
            if (script == null)
                return;

            ctCase = script.CT_Case;
            ctTabako = script.CT_Tabako;
            ctLighter = script.CT_Lighter;
            ctMouth = script.CT_Mouth;
            pcCase = script.PC_Case;
            pcTabako = script.PC_Tabako;
            pcLighter = script.PC_Lighter;
        }
    }
}

using System;
using UnityEngine;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace net.satania_shopping.tabakosystem.runtime
{
    /// <summary>
    /// コンストレイント用データベース (プレイモードのみ)
    /// Tabako Script内の
    /// </summary>
    [AddComponentMenu("/")]
    [Obsolete("Trace And Optimizeで消えるので非推奨")]
    public class ConstraintDabatase : DatabaseBase
    {
        public Transform CT_Case;

        public Transform CT_Tabako;

        public Transform CT_Lighter;

        public Transform CT_Mouth;

        public VRCParentConstraint PC_Case;
        public VRCParentConstraint PC_Tabako;
        public VRCParentConstraint PC_Lighter;
    }
}
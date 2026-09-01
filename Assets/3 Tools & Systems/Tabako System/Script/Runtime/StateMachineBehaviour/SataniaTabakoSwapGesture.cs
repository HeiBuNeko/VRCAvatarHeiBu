using JetBrains.Annotations;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    /// <summary>
    /// Animatorのステートにくっつけて、ジェスチャースワップを明示的に行うためのBehaviour
    /// </summary>
    public sealed class SataniaTabakoSwapGesture : StateMachineBehaviour
    {
        public enum HandGesture
        {
            [InspectorName("Neutral (0)")] neutral,
            [InspectorName("Fist (1)")] fist,
            [InspectorName("HandOpen (2)")] handopen,
            [InspectorName("FingerPoint (3)")] fingerpoint,
            [InspectorName("Victory (4)")] victory,
            [InspectorName("RockNRoll (5)")] rocknroll,
            [InspectorName("HandGun (6)")] handgun,
            [InspectorName("ThumbsUp (7)")] thumbsup,
        }

        public enum KeyConfig
        {
            None,
            [InspectorName("Case")] Case,
            [InspectorName("Lighter")] Lighter,
            [InspectorName("Fire")] Fire,
            [InspectorName("Restore")] Restore,
            [InspectorName("Smoke")] Smoke,
            [InspectorName("Swap")] Swap,
        }

        [SerializeField]
        internal bool useSwap = true;

        [SerializeField]
        internal KeyConfig key = KeyConfig.None;

        //[SerializeField]
        //internal HandGesture beforeGesture = HandGesture.neutral;

        [PublicAPI]
        public bool UseSwap
        {
            get => useSwap;
            set => useSwap = value;
        }

        //    [PublicAPI]
        //    public HandGesture BeforeGesture
        //    {
        //        get => beforeGesture;
        //        set => beforeGesture = value;
        //    }

        [PublicAPI]
        public KeyConfig Key
        {
            get => key;
            set => key = value;
        }
    }
}
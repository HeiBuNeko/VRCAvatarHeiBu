using JetBrains.Annotations;
using UnityEngine;
using static net.satania_shopping.tabakosystem.SataniaTabakoRequireMotion;

namespace net.satania_shopping.tabakosystem.runtime
{
    /// <summary>
    /// SataniaTabakoRequireMotionで使うアニメーションファイルに使用する場合につけるBehaviour
    /// SataniaTabakoRequireMotionPassで処理
    /// </summary>
    [AddComponentMenu("Satania Tabako/Satabako I Override Hand Gesture")]
    [DisallowMultipleComponent]
    public class IOverrideHandGesture : SatabakoBehaviour
    {
        [SerializeField] private OverrideMotion overrideMotion = OverrideMotion.None;
        [SerializeField] private Motion customMotion;

        [PublicAPI]
        public OverrideMotion OverrideMotion
        {
            get => overrideMotion;
            set => overrideMotion = value;
        }

        [PublicAPI]
        public Motion CustomMotion
        {
            get => customMotion;
            set => customMotion = value;
        }
    }
}
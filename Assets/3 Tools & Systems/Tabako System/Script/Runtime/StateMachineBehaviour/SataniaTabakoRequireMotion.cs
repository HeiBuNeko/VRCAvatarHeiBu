using JetBrains.Annotations;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    /// <summary>
    /// ビルド時にモーションを組み立てたり、差し替えたりするためのStateMachineBehaviour
    /// I Controlled By Animationとセットで使う
    /// I Override Hand Gestureでも使用
    /// </summary>
    public sealed class SataniaTabakoRequireMotion : StateMachineBehaviour
    {
        public enum AutomaticMotion
        {
            None = 0,
            ContactSizeController = 1, //コンタクトサイズを調整するExMenuに使用
            TabakoFiredAnimation = 2, //タバコの長さが短くなるアニメーションに使用
            Override = 3, //主にハンドジェスチャーの置き換えに使用
            Additive = 5, //ライターのFiredアニメーションに使用
            ExhaleEmission = 4 //煙を吐く時のタバコの先端に使用
        }

        public enum FiredAnimationType
        {
            None,
            Fired,
            NonFired,
            Restore
        }

        public enum OverrideMotion
        {
            None,
            [InspectorName("Case")] Gesture_Case,
            [InspectorName("Cigarette")] Gesture_Tabacco,
            [InspectorName("Lighter")] Gesture_Lighter
        }

        public enum ExhaleAnimationType
        {
            OFF,
            ON
        }

        public enum AdditiveMotionTarget
        {
            None = -1,
            Lighter = 0,
            FireParticle = 1,
        }

        public enum LighterMotionType
        {
            NonFired = 0,
            Fired = 1
        }

        [SerializeField] internal AutomaticMotion motionID;
        [SerializeField] internal FiredAnimationType firedAnimationType;
        [SerializeField] internal OverrideMotion overrideMotion;
        [SerializeField] internal ExhaleAnimationType exhaleAnimationType;
        [SerializeField] internal AdditiveMotionTarget additiveMotionTarget = AdditiveMotionTarget.None;
        [SerializeField] internal LighterMotionType lighterMotionType = LighterMotionType.NonFired;

        [PublicAPI]
        public AutomaticMotion MotionID
        {
            get => motionID;
            set => motionID = value;
        }

        [PublicAPI]
        public FiredAnimationType FiredType
        {
            get => firedAnimationType;
            set => firedAnimationType = value;
        }

        [PublicAPI]
        public OverrideMotion OverrideMotionType
        {
            get => overrideMotion;
            set => overrideMotion = value;
        }

        [PublicAPI]
        public ExhaleAnimationType ExhaleAnimation
        {
            get => exhaleAnimationType;
            set => exhaleAnimationType = value;
        }

        public AdditiveMotionTarget AdditiveMotion => additiveMotionTarget;
        public LighterMotionType LighterMotion => lighterMotionType;
    }
}
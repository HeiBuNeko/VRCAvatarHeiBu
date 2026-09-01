using System;
using UnityEngine;
using static net.satania_shopping.tabakosystem.SataniaTabakoRequireMotion;

namespace net.satania_shopping.tabakosystem.runtime
{
    /// <summary>
    /// SataniaTabakoRequireMotionで使うアニメーションファイルに使用する場合につけるBehaviour
    /// SataniaTabakoRequireMotionPassで処理
    /// </summary>
    [AddComponentMenu("Satania Tabako/Satabako I Controlled by Animation")]
    [DisallowMultipleComponent]
    public class IControlledByAnimation : SatabakoBehaviour
    {
        public enum eEmissionMaterialType
        {
            lilToonEmission1st,
            lilToonEmission2nd,

            poiyomi
        }

        /// <summary>
        /// どのアニメーションに入れるか
        /// </summary>
        [SerializeField] private AutomaticMotion automaticMotion = AutomaticMotion.None;
        [SerializeField] private AdditiveMotionTarget additiveMotion = AdditiveMotionTarget.None;

        /*タバコが減るアニメーション用*/
        [SerializeField] private SkinnedMeshRenderer tabakoMesh;

        /// <summary>
        /// タバコの先端が燃えている状態になるシェイプキー
        /// </summary>
        [SerializeField] private string shortShapekeyName = "short";
        [SerializeField, Range(0, 100f)] private float shortShapekeyMaxValue = 100f;

        /// <summary>
        /// タバコ自体が短くなるシェイプキー
        /// </summary>
        [SerializeField] private string short2ShapekeyName = "short2";
        [SerializeField, Range(0, 100f)] private float short2ShapekeyMaxValue = 80f;

        [SerializeField] private Transform firedAnim_SmokeStart;
        [SerializeField] private Transform firedAnim_SmokeEnd;
        [SerializeField] private Transform tabakoTipPosition;
        [SerializeField] private ParticleSystem tabakoSmokeParticle;
        [SerializeField] private GameObject fireSenderGO;
        [SerializeField] private GameObject fireReceiverGO;

        //VRC公式のFireタグ用コンタクト
        [SerializeField] private GameObject fireSenderVRC_GO;
        [SerializeField] private GameObject fireReceiverVRC_GO;

        /*モーション上書き*/
        [SerializeField] private Motion gestureMotion_Case;
        [SerializeField] private Motion gestureMotion_Tabacco;
        [SerializeField] private Motion gestureMotion_Lighter;

        /*煙を吐くアニメーションのエミッション用*/
        [SerializeField, ColorUsage(true, true)] private Color offEmissiveColor;
        [SerializeField, ColorUsage(true, true)] private Color onEmissiveColor;
        [SerializeField, Range(1f, 3)] private float m_time_EmissiveON = 1f;

        private eEmissionMaterialType emissiveMaterialType = eEmissionMaterialType.lilToonEmission1st;

        /*ライターのシェイプキー用*/
        [SerializeField] private SkinnedMeshRenderer lighterMeshRenderer;
        [SerializeField] private string lighterONShapeName = "on";
        [SerializeField, Range(0, 100f)] private float lighterONShapekeyMaxValue = 100f;

        /*炎パーティクルの位置*/
        //[SerializeField] private bool useFireParticleAnimation;   //炎がゆらゆらするアニメーションを使用するか
        [SerializeField] private Transform fireParticlePosition;
        [SerializeField] private Light fireLight;                   //火のライト
        [SerializeField] public GameObject VRCLighter;              //ライターの火のコンタクト判定
        [SerializeField] public GameObject VRC_Fire;                //ライターの火のコンタクト判定 (VRC公式Fire)
        [SerializeField] private GameObject fireParticle;
        [SerializeField] private GameObject lightSound;             // ライター音
        [SerializeField] private GameObject lighterSpark;           //ライターの火花

        [SerializeField] private KeyFramePairVector3[] fireParticlePositionPairs;   //加算
        [SerializeField] private KeyFramePairFloat[] lightIntensityPairs;           //加算
        [SerializeField] private KeyFramePairFloat[] lightRangePairs;               //加算


        public AutomaticMotion Motion
        {
            get => automaticMotion;
            set => automaticMotion = value;
        }

        public SkinnedMeshRenderer TabakoMesh
        {
            get => tabakoMesh;
            set => tabakoMesh = value;
        }

        public string ShortShapekeyName
        {
            get => shortShapekeyName;
            set => shortShapekeyName = value;
        }

        public float ShortShapekeyMaxValue
        {
            get => shortShapekeyMaxValue;
            set => shortShapekeyMaxValue = value;
        }

        public float Short2ShapekeyMaxValue
        {
            get => short2ShapekeyMaxValue;
            set => short2ShapekeyMaxValue = value;
        }

        public string Short2ShapekeyName
        {
            get => short2ShapekeyName;
            set => short2ShapekeyName = value;
        }

        public Transform FiredAnim_SmokeStart
        {
            get => firedAnim_SmokeStart;
            set => firedAnim_SmokeStart = value;
        }

        public Transform FiredAnim_SmokeEnd
        {
            get => firedAnim_SmokeEnd;
            set => firedAnim_SmokeEnd = value;
        }

        public Transform TabakoTipPosition
        {
            get => tabakoTipPosition;
            set => tabakoTipPosition = value;
        }

        public ParticleSystem TabakoSmokeParticle
        {
            get => tabakoSmokeParticle;
            set => tabakoSmokeParticle = value;
        }

        public GameObject FireSenderGO
        {
            get => fireSenderGO;
            set => fireSenderGO = value;
        }

        public GameObject FireReceiverGO
        {
            get => fireReceiverGO;
            set => fireReceiverGO = value;
        }

        public GameObject FireSender_VRC => fireSenderVRC_GO;
        public GameObject FireReceiver_VRC => fireReceiverVRC_GO;

        public Motion GestureMotionCase
        {
            get => gestureMotion_Case;
        }

        public Motion GestureMotionTabacco
        {
            get => gestureMotion_Tabacco;
        }

        public Motion GestureMotionLighter
        {
            get => gestureMotion_Lighter;
        }

        public Color EmissiveOFFColor => offEmissiveColor;
        public Color EmissiveONColor => onEmissiveColor;

        public float Time_EmissiveON => m_time_EmissiveON;

        /// <summary>
        /// 後々lilToonの1stEmisssion以外にも対応したい
        /// </summary>
        public eEmissionMaterialType EmissionMaterialType => emissiveMaterialType;
        public AdditiveMotionTarget AdditiveMotion => additiveMotion;

        public SkinnedMeshRenderer LighterMeshRenderer => lighterMeshRenderer;
        public string LighterONShapeName => lighterONShapeName;
        public float LighterONShapekeyMaxValue => lighterONShapekeyMaxValue;

        public Transform FireParticlePosition => fireParticlePosition;
        public Light FireLight => fireLight;
        public GameObject FireParticle => fireParticle;
        public GameObject LightSound => lightSound;
        public GameObject LightSpark => lighterSpark;

        public KeyFramePairVector3[] FireParticlePositionPairs => fireParticlePositionPairs;
        public KeyFramePairFloat[] LightIntensityPairs => lightIntensityPairs;
        public KeyFramePairFloat[] LightRangePairs => lightRangePairs;
    }
}
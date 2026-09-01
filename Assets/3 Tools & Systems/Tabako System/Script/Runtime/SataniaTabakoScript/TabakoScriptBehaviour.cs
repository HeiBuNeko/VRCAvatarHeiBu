using JetBrains.Annotations;
using nadena.dev.ndmf.runtime;
using UnityEngine;
using VRC.SDKBase;

using System.Linq;
using nadena.dev.modular_avatar.core;
using System;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace net.satania_shopping.tabakosystem.runtime
{
    [ExecuteInEditMode]
    [HelpURL("https://saturnianjp.github.io/satania_shopping_document/docs/category/%E3%81%95%E3%81%9F%E3%81%AB%E3%81%82%E5%BC%8F%E3%82%BF%E3%83%90%E3%82%B3-1")]
    [AddComponentMenu("Satania Tabako/Satania Tabako System")]
    [DisallowMultipleComponent]
    public sealed class TabakoScriptBehaviour : MonoBehaviour, IEditorOnly
    {
        public enum BehaviourHandGesture
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

        /// <summary>
        /// 破壊的変更があった場合のみ更新される
        /// </summary>
        public enum PrefabVersion
        {
            _1, // >= 1.3.2
            _2, //1.5.4 > 1.3.2 
            _3 // >= 2.0.0
        }

        public const float k_minContactSize = 0.25f;
        public const float k_maxContactSize = 2f;

        [SerializeField] private Transform constraintTarget_Case;
        [SerializeField] private Transform constraintTarget_Tabako;
        [SerializeField] private Transform constraintTarget_Lighter;
        [SerializeField] private Transform constraintTarget_Mouth;

        [SerializeField] private VRCParentConstraint parentConstraint_Case;
        [SerializeField] private VRCParentConstraint parentConstraint_Tabako;
        [SerializeField] private VRCParentConstraint parentConstraint_Lighter;

        [SerializeField] private BehaviourHandGesture gesture_case = BehaviourHandGesture.thumbsup;
        [SerializeField] private BehaviourHandGesture gesture_lighter = BehaviourHandGesture.rocknroll;
        [SerializeField] private BehaviourHandGesture gesture_fire = BehaviourHandGesture.fist;
        [SerializeField] private BehaviourHandGesture gesture_restore = BehaviourHandGesture.rocknroll;
        [SerializeField] private BehaviourHandGesture gesture_exhalesmoke = BehaviourHandGesture.victory;
        [SerializeField] private BehaviourHandGesture gesture_swap = BehaviourHandGesture.thumbsup;

        [SerializeField/*, Range(k_minContactSize, 1f)*/] private float minContactSize = 0.5f;
        [SerializeField/*, Range(1.1f, k_maxContactSize)*/] private float maxContactSize = 1.5f;
        [SerializeField, Range(60f, 240f)] private float firedAnimationLength = 120f;

        [SerializeField, Tooltip("IDeactiveWhenBuildがついているオブジェクトをビルド時に非表示にする")]
        private bool deactiveObjectOnBuild = false;

        [SerializeField] private ISatabakoPlugin[] plugins;

        //ショップ情報・アバター情報
        [SerializeField] private string[] avatarNamesForSearcher;
        [SerializeField] private string shopName = "";
        [SerializeField] private string avatarURL = "";
        [SerializeField] private bool isPrefabObsolute = false;

        [SerializeField] private ModularAvatarParameters[] satabakoParameters;

#pragma warning disable 0414
        [SerializeField, HideInInspector] private PrefabVersion prefabVersion = PrefabVersion._3;
#pragma warning restore 0414

        private TabakoScriptBehaviour[] _someComponents;
        [PublicAPI]
        public TabakoScriptBehaviour[] SomeComponents
        {
            get
            {
                if (_someComponents == null)
                    return new TabakoScriptBehaviour[0];

                return (TabakoScriptBehaviour[])_someComponents.Clone();
            }
        }

        private Transform _avatarTransformCache;

        private bool IsParentEditorOnly(Transform t)
        {
            Transform AvatarRoot = RuntimeUtil.FindAvatarInParents(t);

            Transform parent = t.parent;
            while (parent != null && parent != AvatarRoot)
            {
                if (parent.CompareTag("EditorOnly"))
                    return true;

                parent = parent.parent;
            }

            return false;
        }

        private void GetAvatarTransform()
        {
            if (_avatarTransformCache != null)
                return;

            _avatarTransformCache = RuntimeUtil.FindAvatarInParents(transform);
            if (_avatarTransformCache == null)
                return;

            _someComponents = _avatarTransformCache.GetComponentsInChildren<TabakoScriptBehaviour>(true)
                .Where(x => x != this)
                .ToArray();

            plugins = _avatarTransformCache.GetComponentsInChildren<ISatabakoPlugin>(true)
                .Where(x => x != null && !IsParentEditorOnly(x.transform)).ToArray();

            OnHiearchyChangedHook.OnHierarchyChanged -= ClearCache;
            OnHiearchyChangedHook.OnHierarchyChanged += ClearCache;
        }

        private void ClearCache()
        {
            _avatarTransformCache = null;
            OnHiearchyChangedHook.OnHierarchyChanged -= ClearCache;
        }

        private void Update()
        {
            if (!RuntimeUtil.IsPlaying)
            {
                GetAvatarTransform();
            }
        }

        private void OnDestroy()
        {
            ClearCache();
        }

        private void OnValidate()
        {
            ClearCache();
        }

        public Transform CT_Case
        {
            get => constraintTarget_Case;
            set => constraintTarget_Case = value;
        }

        public Transform CT_Tabako
        {
            get => constraintTarget_Tabako;
            set => constraintTarget_Tabako = value;
        }

        public Transform CT_Lighter
        {
            get => constraintTarget_Lighter;
            set => constraintTarget_Lighter = value;
        }

        public Transform CT_Mouth
        {
            get => constraintTarget_Mouth;
            set => constraintTarget_Mouth = value;
        }

        public VRCParentConstraint PC_Case => parentConstraint_Case;
        public VRCParentConstraint PC_Tabako => parentConstraint_Tabako;
        public VRCParentConstraint PC_Lighter => parentConstraint_Lighter;


        [PublicAPI]
        public float MinContactSize
        {
            get => minContactSize;
            set => minContactSize = value;
        }

        [PublicAPI]
        public float MaxContactSize
        {
            get => maxContactSize;
            set => maxContactSize = value;
        }

        [PublicAPI]
        public float FiredAnimationLength
        {
            get => firedAnimationLength;
            set => firedAnimationLength = value;
        }

        [PublicAPI]
        public BehaviourHandGesture G_Case
        {
            get => gesture_case;
            set => gesture_case = value;
        }

        [PublicAPI]
        public BehaviourHandGesture G_Lighter
        {
            get => gesture_lighter;
            set => gesture_lighter = value;
        }

        [PublicAPI]
        public BehaviourHandGesture G_Fire
        {
            get => gesture_fire;
            set => gesture_fire = value;
        }

        [PublicAPI]
        public BehaviourHandGesture G_Restore
        {
            get => gesture_restore;
            set => gesture_restore = value;
        }

        [PublicAPI]
        public BehaviourHandGesture G_Smoke
        {
            get => gesture_exhalesmoke;
            set => gesture_exhalesmoke = value;
        }

        [PublicAPI]
        public BehaviourHandGesture G_Swap
        {
            get => gesture_swap;
            set => gesture_swap = value;
        }

        [PublicAPI]
        public bool DeactiveObjectsOnBuild
        {
            get => deactiveObjectOnBuild;
            set => deactiveObjectOnBuild = value;
        }

        [PublicAPI]
        public ISatabakoPlugin[] Plugins
        {
            get => plugins;
            set => plugins = value;
        }

        [PublicAPI]
        public ModularAvatarParameters[] SatabakoMAParameters => satabakoParameters;

        [PublicAPI]
        public string[] AvatarNamesForSearcher => avatarNamesForSearcher;

        [PublicAPI]
        public string ShopName => shopName;

        [PublicAPI]
        public string AvatarURL => avatarURL;

        public bool IsPrefabObsolute => isPrefabObsolute;
    }
}
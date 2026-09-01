using JetBrains.Annotations;
using nadena.dev.ndmf.runtime;
using System.Linq;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.runtime
{
    //MA Bone Proxyより遅く
    [DefaultExecutionOrder(10)]
    [ExecuteInEditMode]
    [AddComponentMenu("Satania Tabako/Satabako I Follow Satania Tabako Object")]
    [DisallowMultipleComponent]
    public class IFollowSatabakoObject : SatabakoBehaviour
    {
        public enum SatabakoObject
        {
            None,
            Case,
            Tabako,
            Lighter,
            Mouth,
        }

        private Transform _targetCache;
        private SatabakoObject _satabakoObjectCache;
        //private GameObject dummyObject;

        private TabakoScriptBehaviour _tabakoScriptBehaviour;
        private IFollowSatabakoObject[] _someSatabakoObjectComponents;
        private Transform _avatarTransform;


        [PublicAPI]
        public TabakoScriptBehaviour TabakoScriptBehaviour
        {
            get => _tabakoScriptBehaviour;
        }

        [PublicAPI]
        public IFollowSatabakoObject[] SomeTargetObjectComponents
        {
            get
            {
                if (_someSatabakoObjectComponents == null)
                    return new IFollowSatabakoObject[0];

                return (IFollowSatabakoObject[])_someSatabakoObjectComponents.Clone();
            }
        }

        [PublicAPI]
        public Transform AvatarTransform
        {
            get => _avatarTransform;
        }


        public Transform target
        {
            get
            {
                //変更されていた場合
                if (_satabakoObjectCache != TargetObject)
                {
                    _targetCache = null;
                    _tabakoScriptBehaviour = null;
                    _avatarTransform = null;
                }

                //UpdateよりOnHierarchyChangedの方が遅く実行されてtargetがnullのままになることがあるけど、次のUpdate()で取得されるのでとりあえず放置
                if (_targetCache != null)
                {
                    return _targetCache;
                }

                _targetCache = UpdateTargetTransform(out _tabakoScriptBehaviour, out _someSatabakoObjectComponents, out _avatarTransform);
                OnHiearchyChangedHook.OnHierarchyChanged -= ClearCache;
                OnHiearchyChangedHook.OnHierarchyChanged += ClearCache;

                return _targetCache;
            }
        }

        [SerializeField]
        internal SatabakoObject _object = SatabakoObject.None;

        [SerializeField]
        internal bool autoFixLocalScale = true;

        [PublicAPI]
        public SatabakoObject TargetObject
        {
            get => _object;
            set => _object = value;
        }

        [PublicAPI]
        public bool AutoFixLocalScale
        {
            get => autoFixLocalScale;
            set => autoFixLocalScale = value;
        }

        private Transform GetConstraintTarget([NotNull] TabakoScriptBehaviour tabakoScript, SatabakoObject targetObject)
        {
            switch (targetObject)
            {
                case SatabakoObject.Case:
                    return tabakoScript.CT_Case;

                case SatabakoObject.Lighter:
                    return tabakoScript.CT_Lighter;

                case SatabakoObject.Tabako:
                    return tabakoScript.CT_Tabako;

                case SatabakoObject.Mouth:
                    return tabakoScript.CT_Mouth;

                default:
                    return null;
            }
        }

        private Transform UpdateTargetTransform(out TabakoScriptBehaviour tabakoScriptBehaviour, out IFollowSatabakoObject[] someSatabakoObjectComponents, out Transform avatarTransform)
        {
            tabakoScriptBehaviour = null;
            someSatabakoObjectComponents = null;
            avatarTransform = null;

            if (TargetObject == SatabakoObject.None)
                return null;

            avatarTransform = RuntimeUtil.FindAvatarInParents(transform);
            if (avatarTransform == null)
                return null;

            tabakoScriptBehaviour = avatarTransform.GetComponent<TabakoScriptBehaviour>();
            if (tabakoScriptBehaviour == null)
            {
                tabakoScriptBehaviour = avatarTransform.GetComponentInChildren<TabakoScriptBehaviour>();

                if (tabakoScriptBehaviour == null)
                    return null;
            }

            //自身以外のTargetObjectが同じなIFollowSatabakoObjectを取得して、Editorで通知する
            someSatabakoObjectComponents = avatarTransform.GetComponentsInChildren<IFollowSatabakoObject>(true)
                .Where(x => x != this && x.TargetObject == TargetObject)
                .ToArray();

            var target = GetConstraintTarget(tabakoScriptBehaviour, TargetObject);
            _satabakoObjectCache = TargetObject;
            return target;
        }

        // Adapted from Modular Avatar (MIT). Source:
        // https://github.com/bdunderscore/modular-avatar/blob/82e9465ee24fd64d8cb4b35a750e13d3048cb827/Runtime/ModularAvatarBoneProxy.cs#L150
        public static void MatchScale(Transform t, Transform to)
        {
            if (t == null || to == null) return;

            var targetMat = to.localToWorldMatrix;

            var parentMat = t.parent != null
                ? t.parent.worldToLocalMatrix
                : Matrix4x4.identity;

            var trMat = Matrix4x4.TRS(
                t.localPosition,
                t.localRotation,
                Vector3.one
            );

            var finalMat = trMat * parentMat * targetMat;
            t.localScale = finalMat.lossyScale;
        }


        internal void Update()
        {
            if (!RuntimeUtil.IsPlaying && target != null)
            {
                //if (isDebugOption)
                //    Debug.Log(target);

                var targetTransform = target.transform;
                var myTransform = transform;

                //if (dummyObject == null)
                //{
                //    dummyObject = new GameObject("[IFollow Satabako Object] Scale Checker");
                //    dummyObject.hideFlags = HideFlags.HideAndDontSave;
                //}

                myTransform.position = targetTransform.position;
                myTransform.rotation = targetTransform.rotation;

                //Targetの子としてスケールを整える
                if (autoFixLocalScale)
                {
                    ////OldVersion
                    ////スケール取得
                    //dummyObject.transform.SetParent(targetTransform);
                    //dummyObject.transform.localScale = new Vector3(1, 1, 1);

                    ////元の場所でのlocalScale取得
                    //dummyObject.transform.SetParent(myTransform.transform.parent);

                    //Vector3 localScale = dummyObject.transform.localScale;
                    //myTransform.localScale = localScale;

                    MatchScale(myTransform, targetTransform);
                }
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ClearCache();
        }

        private void ClearCache()
        {
            _targetCache = null;
            _tabakoScriptBehaviour = null;
            _avatarTransform = null;

            OnHiearchyChangedHook.OnHierarchyChanged -= ClearCache;
        }

        private void OnDestroy()
        {
            ClearCache();

            //            if (dummyObject)
            //            {
            //#if UNITY_EDITOR
            //                DestroyImmediate(dummyObject);
            //#endif
            //            }
        }
    }
}
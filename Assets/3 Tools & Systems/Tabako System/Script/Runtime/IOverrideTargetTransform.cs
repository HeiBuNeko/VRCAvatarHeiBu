using nadena.dev.ndmf.runtime;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.runtime
{
    [ExecuteInEditMode]
    [AddComponentMenu("Satania Tabako/Satabako I Override Target Transform")]
    [DisallowMultipleComponent]
    public class IOverrideTargetTransform : SatabakoBehaviour
    {
        public enum eTargets
        {
            None,
            Case_L,
            Case_R,
            Tabako_L,
            Tabako_R,
            Tabako_Mouth,
            Lighter_L,
            Lighter_R,

            Contact_Mouth
        }

        public eTargets overrideTarget;
        private eTargets _overrideTargetCache;

        private Transform _targetCache;
        private TabakoScriptBehaviour _tabakoScriptBehaviour;
        private Transform _avatarTransform;

        public TabakoScriptBehaviour TabakoScriptBehaviour
        {
            get => _tabakoScriptBehaviour;
        }

        public Transform AvatarTransform
        {
            get => _avatarTransform;
        }

        public Transform target
        {
            get
            {
                if (_overrideTargetCache != overrideTarget)
                {
                    _targetCache = null;
                }

                //UpdateよりOnHierarchyChangedの方が遅く実行されてtargetがnullのままになることがあるけど、次のUpdate()で取得されるのでとりあえず放置
                if (_targetCache != null)
                {
                    return _targetCache;
                }

                _targetCache = UpdateTargetTransform(out _tabakoScriptBehaviour, out _avatarTransform);
                OnHiearchyChangedHook.OnHierarchyChanged -= ClearCache;
                OnHiearchyChangedHook.OnHierarchyChanged += ClearCache;

                _overrideTargetCache = overrideTarget;
                return _targetCache;
            }
        }

        private Transform GetTarget(TabakoScriptBehaviour tabakoScriptBehaviour, eTargets target)
        {
            if (tabakoScriptBehaviour == null || target == eTargets.None)
                return null;

            switch (target)
            {
                case eTargets.Case_R:
                    return GetSourceTransform(tabakoScriptBehaviour.PC_Case, 0);

                case eTargets.Case_L:
                    return GetSourceTransform(tabakoScriptBehaviour.PC_Case, 1);

                case eTargets.Tabako_R:
                    return GetSourceTransform(tabakoScriptBehaviour.PC_Tabako, 0);

                case eTargets.Tabako_L:
                    return GetSourceTransform(tabakoScriptBehaviour.PC_Tabako, 1);

                case eTargets.Tabako_Mouth:
                    return GetSourceTransform(tabakoScriptBehaviour.PC_Tabako, 2);

                case eTargets.Lighter_L:
                    return GetSourceTransform(tabakoScriptBehaviour.PC_Lighter, 0);

                case eTargets.Lighter_R:
                    return GetSourceTransform(tabakoScriptBehaviour.PC_Lighter, 1);

                case eTargets.Contact_Mouth:
                    return tabakoScriptBehaviour.CT_Mouth;

                default:
                    return null;
            }

            Transform GetSourceTransform(VRC.SDK3.Dynamics.Constraint.Components.VRCParentConstraint parentConstraint, int index)
            {
                if (parentConstraint == null)
                    return null;

                var source = parentConstraint.Sources;
                if (source.Count < index + 1)
                    return null;

                return source[index].SourceTransform;
            }
        }

        private Transform UpdateTargetTransform(out TabakoScriptBehaviour tabakoScriptBehaviour, out Transform avatarTransform)
        {
            tabakoScriptBehaviour = null;
            avatarTransform = null;

            avatarTransform = RuntimeUtil.FindAvatarInParents(transform);
            if (avatarTransform == null)
                return null;

            if (overrideTarget == eTargets.None)
                return null;

            tabakoScriptBehaviour = avatarTransform.GetComponent<TabakoScriptBehaviour>();
            if (tabakoScriptBehaviour == null)
            {
                tabakoScriptBehaviour = avatarTransform.GetComponentInChildren<TabakoScriptBehaviour>();

                if (tabakoScriptBehaviour == null)
                    return null;
            }

            var target = GetTarget(tabakoScriptBehaviour, overrideTarget);
            return target;
        }

        protected override void OnValidate()
        {
            ClearCache();
        }


        private void Update()
        {
            if (!RuntimeUtil.IsPlaying && target != null)
            {
                target.SetPositionAndRotation(transform.position, transform.rotation);
            }
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
        }
    }
}
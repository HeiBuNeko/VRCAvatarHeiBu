using JetBrains.Annotations;
using nadena.dev.ndmf.runtime;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.runtime
{
    /// <summary>
    /// NDMFのビルドが走る際に自動的に非表示にしたいオブジェクトにつける
    /// </summary>
    [AddComponentMenu("Satania Tabako/Satabako I Deactive When Build")]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10)]
    [ExecuteInEditMode]
    public class IDeactiveWhenBuild : SatabakoBehaviour
    {
        private TabakoScriptBehaviour _tabakoScriptBehaviour;
        private Transform _avatarTransform;

        [PublicAPI]
        public TabakoScriptBehaviour TabakoScriptBehaviour
        {
            get
            {
                if (_tabakoScriptBehaviour != null)
                    return _tabakoScriptBehaviour;

                _tabakoScriptBehaviour = GetTabakoScriptBehaviour(out _avatarTransform);
                OnHiearchyChangedHook.OnHierarchyChanged -= ClearCache;
                OnHiearchyChangedHook.OnHierarchyChanged += ClearCache;
                return _tabakoScriptBehaviour;
            }
        }

        [PublicAPI]
        public Transform AvatarTransform
        {
            get => _avatarTransform;
        }

        private TabakoScriptBehaviour GetTabakoScriptBehaviour(out Transform avatarTransform)
        {
            avatarTransform = RuntimeUtil.FindAvatarInParents(transform);
            if (avatarTransform == null)
                return null;

            var tabakoScriptBehaviour = avatarTransform.GetComponent<TabakoScriptBehaviour>();
            if (tabakoScriptBehaviour == null)
            {
                tabakoScriptBehaviour = avatarTransform.GetComponentInChildren<TabakoScriptBehaviour>();

                if (tabakoScriptBehaviour == null)
                    return null;
            }

            return tabakoScriptBehaviour;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ClearCache();
        }

        private void ClearCache()
        {
            _tabakoScriptBehaviour = null;

            OnHiearchyChangedHook.OnHierarchyChanged -= ClearCache;
        }

        internal void Update()
        {
            //Validate (null) -> Update (notNull)

            if (!RuntimeUtil.IsPlaying && TabakoScriptBehaviour != null)
            {

            }
        }

        private void OnDestroy()
        {
            ClearCache();
        }
    }
}
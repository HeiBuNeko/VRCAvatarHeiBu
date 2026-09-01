using nadena.dev.ndmf;
using nadena.dev.ndmf.runtime;
using net.satania_shopping.tabakosystem.runtime;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.ndmf
{
    public class IDeactiveWhenBuildPass : Pass<IDeactiveWhenBuildPass>
    {
        public override string DisplayName => "net.satania_shopping.tabakosystem.ndmf.IDeactiveWhenBuildPass";

        private PluginDefinition.Singleton singleton => PluginDefinition.Singleton.instance;

        protected override void Execute(BuildContext ctx)
        {
            IDeactiveWhenBuild[] deactives = ctx.AvatarRootObject.GetComponentsInChildren<IDeactiveWhenBuild>(true);

            if (singleton.tabakoScriptBehaviour != null)
            {
                bool deactiveOnBuild = singleton.tabakoScriptBehaviour.DeactiveObjectsOnBuild;

                if (deactiveOnBuild && !RuntimeUtil.IsPlaying)
                {
                    //プレイモードな場合はスルー (ビルド時のみ非アクティブに)         
                    foreach (var deactive in deactives)
                    {
                        deactive.gameObject.SetActive(false);
                    }
                }
            }

            //削除
            foreach (var deactive in deactives)
            {
                Object.DestroyImmediate(deactive);
            }
        }
    }
}
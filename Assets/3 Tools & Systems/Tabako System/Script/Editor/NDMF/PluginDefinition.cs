using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using nadena.dev.ndmf.runtime;
using net.satania_shopping.tabakosystem.runtime;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if AVATAR_OPTIMIZER && UNITY_EDITOR
using Anatawa12.AvatarOptimizer.API;
#endif

[assembly: ExportsPlugin(typeof(net.satania_shopping.tabakosystem.ndmf.PluginDefinition))]
namespace net.satania_shopping.tabakosystem.ndmf
{
    public sealed class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "net.satania.shopping.tabakosystem";
        public override string DisplayName => "Satabako Pass Executer";

        internal const string k_case_guid = "f3c0f68810259194fb64fc9d26e5e742";
        internal const string k_tabacco_guid = "c23f62eb0aa6d2043b827514e1e08429";
        internal const string k_lighter_guid = "9600d0fc1e4017948984baa7402c0f71";

        public class Singleton : ScriptableSingleton<Singleton>
        {
            public TabakoScriptBehaviour tabakoScriptBehaviour;
            public float MinContactSize;
            public float MaxContactSize;
        }

        public Singleton singleton => Singleton.instance;

        private static Motion ResolvePlayModeGesture(Transform avatarRoot, editor.GestureKind kind)
        {
            AnimationClip clip = editor.UnsupportedAvatarOps.ResolveGestureClip(avatarRoot, kind, out _, out Motion unsupportedOverride);
            return unsupportedOverride != null ? unsupportedOverride : clip;
        }

        protected override void Configure()
        {
            Sequence generatingSeq = InPhase(BuildPhase.Generating);
            generatingSeq
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Search Tabako Script Behaviour Phase", ctx =>
                {
                    var tabakoScripts = ctx.AvatarRootObject.GetComponentsInChildren<TabakoScriptBehaviour>(true).Where(x => x != null).ToArray();

                    if (tabakoScripts.Length > 0)
                    {
                        singleton.tabakoScriptBehaviour = tabakoScripts[0];

                        //コンタクトのサイズをキャッシュ
                        singleton.MinContactSize = singleton.tabakoScriptBehaviour.MinContactSize;
                        singleton.MaxContactSize = singleton.tabakoScriptBehaviour.MaxContactSize;

                        //被っているたばこを削除
                        if (tabakoScripts.Length > 1)
                            for (int i = 1; i < tabakoScripts.Length; i++)
                                Object.DestroyImmediate(tabakoScripts[i].gameObject);
                    }
                })
                .Then.Run("Create Play Mode Data", ctx =>
                {
                    var existing = ctx.AvatarRootObject.GetComponentsInChildren<SatabakoPlayModeData>(true);
                    if (!RuntimeUtil.IsPlaying || singleton.tabakoScriptBehaviour == null)
                    {
                        foreach (var e in existing)
                            Object.DestroyImmediate(e);
                        return;
                    }

                    var data = ctx.AvatarRootObject.GetComponent<SatabakoPlayModeData>();
                    if (data == null)
                        data = ctx.AvatarRootObject.AddComponent<SatabakoPlayModeData>();
                    data.CopyFrom(singleton.tabakoScriptBehaviour);
                    data.gestureCase = ResolvePlayModeGesture(ctx.AvatarRootTransform, editor.GestureKind.Case);
                    data.gestureTabako = ResolvePlayModeGesture(ctx.AvatarRootTransform, editor.GestureKind.Tabako);
                    data.gestureLighter = ResolvePlayModeGesture(ctx.AvatarRootTransform, editor.GestureKind.Lighter);
                })
                .Then.Run("Remove I Satabako Plugin", ctx =>
                {
                    //エディタ用の物のため削除
                    ISatabakoPlugin[] plugins = ctx.AvatarRootObject.GetComponentsInChildren<ISatabakoPlugin>(true);
                    foreach (var p in plugins)
                        Object.DestroyImmediate(p);
                })
                .Then.Run(MoveMenuItemPass.Instance)
                .Then.Run(BoneReferencePass.Instance)
                .Then.Run(IFollowSatabakoObjectPass.Instance)
                .Then.Run(IDeactiveWhenBuildPass.Instance);

            //AnimatorControllerのステートを差し替えるための処理なので、MAMergeAnimatorより後に処理
            Sequence transformingSeq = InPhase(BuildPhase.Transforming);
            transformingSeq
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run(IModifyAnimatorLayerPass.Instance)
                .Then.Run(IRemoveAvatarMaskPass.Instance) //昔のアバターによくある、FXにAvatarMaskが入ってしまっているアバター用のパス
                .Then.Run(SataniaTabakoRequireMotionPass.Instance)
                .Then.Run(SataniaTabakoSwapGesturePass.Instance)
                .Then.Run("Remove Satania Tabako Script Pass", ctx =>
                {
                    //ビルド時だけ削除
                    if (singleton.tabakoScriptBehaviour != null && !RuntimeUtil.IsPlaying)
                        Object.DestroyImmediate(singleton.tabakoScriptBehaviour);
                });

            Sequence optimizingSeq = InPhase(BuildPhase.Optimizing);
            optimizingSeq
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .BeforePlugin("net.raitichan.int-parameter-compressor")
                .Run(ParametersCompressorPass.Instance);
        }
    }

#if AVATAR_OPTIMIZER && UNITY_EDITOR
    //https://vpm.anatawa12.com/avatar-optimizer/ja/docs/developers/make-your-components-compatible-with-aao/#register-component

    [ComponentInformation(typeof(TabakoScriptBehaviour))]
    internal class TabakoScriptBehaviourInformation : ComponentInformation<TabakoScriptBehaviour>
    {
        protected override void CollectMutations(TabakoScriptBehaviour component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(TabakoScriptBehaviour component, ComponentDependencyCollector collector) { }
    }

    [ComponentInformation(typeof(SatabakoPlayModeData))]
    internal class SatabakoPlayModeDataInformation : ComponentInformation<SatabakoPlayModeData>
    {
        protected override void CollectMutations(SatabakoPlayModeData component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(SatabakoPlayModeData component, ComponentDependencyCollector collector)
        {
            collector.MarkEntrypoint();
        }
    }

    [ComponentInformation(typeof(IRemoveAvatarMask))]
    internal class IRemoveAvatarMaskInformation : ComponentInformation<IRemoveAvatarMask>
    {
        protected override void CollectMutations(IRemoveAvatarMask component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(IRemoveAvatarMask component, ComponentDependencyCollector collector) { }
    }

    [ComponentInformation(typeof(IModifyAnimatorLayer))]
    internal class IModifyAnimatorLayerInformation : ComponentInformation<IModifyAnimatorLayer>
    {
        protected override void CollectMutations(IModifyAnimatorLayer component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(IModifyAnimatorLayer component, ComponentDependencyCollector collector) { }
    }

    [ComponentInformation(typeof(IOverrideHandGesture))]
    internal class IOverrideHandGestureInformation : ComponentInformation<IOverrideHandGesture>
    {
        protected override void CollectMutations(IOverrideHandGesture component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(IOverrideHandGesture component, ComponentDependencyCollector collector) { }
    }

    [ComponentInformation(typeof(IControlledByAnimation))]
    internal class IControlledByAnimationInformation : ComponentInformation<IControlledByAnimation>
    {
        protected override void CollectMutations(IControlledByAnimation component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(IControlledByAnimation component, ComponentDependencyCollector collector) { }
    }

    [ComponentInformation(typeof(ParametersCompressor))]
    internal class ParametersCompressorInformation : ComponentInformation<ParametersCompressor>
    {
        protected override void CollectMutations(ParametersCompressor component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(ParametersCompressor component, ComponentDependencyCollector collector) { }
    }

#endif
}
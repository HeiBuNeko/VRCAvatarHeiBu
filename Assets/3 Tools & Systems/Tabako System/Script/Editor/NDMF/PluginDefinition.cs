using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using nadena.dev.ndmf.runtime;
using net.satania_shopping.tabakosystem.runtime;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Dynamics.Constraint.Components;

#if AVATAR_OPTIMIZER && UNITY_EDITOR
using Anatawa12.AvatarOptimizer.API;
#endif

[assembly: ExportsPlugin(typeof(net.satania_shopping.tabakosystem.ndmf.PluginDefinition))]
namespace net.satania_shopping.tabakosystem.ndmf
{
    /// <summary>
    /// AAOで消える対策の暫定処理なので、今後直したい
    /// </summary>
    public static class SatabakoDatabase
    {
        public static DatabaseSingleton s => DatabaseSingleton.instance;

        public class DatabaseSingleton : ScriptableSingleton<DatabaseSingleton>
        {
            public Dictionary<Transform, HandGestureDatabase> handGestureDatabases = new Dictionary<Transform, HandGestureDatabase>();
            public Dictionary<Transform, ConstraintDatabase> constraintDatabases = new Dictionary<Transform, ConstraintDatabase>();
        }

        public sealed class HandGestureDatabase
        {
            public Motion gestureCase;
            public Motion gestureLighter;
            public Motion gestureTabacco;

            public RuntimeAnimatorController handGestureController;
        }

        public sealed class ConstraintDatabase
        {
            public Transform CT_Case;

            public Transform CT_Tabako;

            public Transform CT_Lighter;

            public Transform CT_Mouth;

            public VRCParentConstraint PC_Case;
            public VRCParentConstraint PC_Tabako;
            public VRCParentConstraint PC_Lighter;
        }
    }

    public sealed class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "net.satania.shopping.tabakosystem";
        public override string DisplayName => "Satabako Pass Executer";

        private const string k_case_guid = "f3c0f68810259194fb64fc9d26e5e742";
        private const string k_tabacco_guid = "c23f62eb0aa6d2043b827514e1e08429";
        private const string k_lighter_guid = "9600d0fc1e4017948984baa7402c0f71";

        public class Singleton : ScriptableSingleton<Singleton>
        {
            public TabakoScriptBehaviour tabakoScriptBehaviour;
            public float MinContactSize;
            public float MaxContactSize;
        }

        public Singleton singleton => Singleton.instance;

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
                .Then.Run("Create Hand Gesture Database if Playmode", ctx =>
                {
                    if (singleton.tabakoScriptBehaviour == null) return;

                    //非対応アバター調整用
                    if (RuntimeUtil.IsPlaying)
                    {
                        AnimationClip m_hg_Case = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(k_case_guid));
                        AnimationClip m_hg_Tabacco = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(k_tabacco_guid));
                        AnimationClip m_hg_Lighter = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(k_lighter_guid));

                        Motion gestureCase = Object.Instantiate(m_hg_Case);
                        Motion gestureTabacco = Object.Instantiate(m_hg_Tabacco);
                        Motion gestureLighter = Object.Instantiate(m_hg_Lighter);

                        var overrides = ctx.AvatarRootObject.GetComponentsInChildren<IOverrideHandGesture>(true);
                        Motion overrideCase = overrides
                                                .Where(x => x.OverrideMotion == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Case)
                                                .Select(x => x.CustomMotion).FirstOrDefault();

                        if (overrideCase != null)
                            gestureCase = Object.Instantiate(overrideCase);

                        Motion overrideTabacco = overrides
                                                    .Where(x => x.OverrideMotion == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Tabacco)
                                                    .Select(x => x.CustomMotion).FirstOrDefault();

                        if (overrideTabacco != null)
                            gestureTabacco = Object.Instantiate(overrideTabacco);

                        Motion overrideLighter = overrides
                                                    .Where(x => x.OverrideMotion == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Lighter)
                                                    .Select(x => x.CustomMotion).FirstOrDefault();

                        if (overrideLighter != null)
                            gestureLighter = Object.Instantiate(overrideLighter);

                        var handGestureDatabase = new SatabakoDatabase.HandGestureDatabase()
                        {
                            gestureCase = gestureCase,
                            gestureTabacco = gestureTabacco,
                            gestureLighter = gestureLighter,

                        };

                        if (SatabakoDatabase.s.handGestureDatabases.ContainsKey(ctx.AvatarRootTransform))
                            SatabakoDatabase.s.handGestureDatabases[ctx.AvatarRootTransform] = handGestureDatabase;
                        else
                            SatabakoDatabase.s.handGestureDatabases.Add(ctx.AvatarRootTransform, handGestureDatabase);
                    }
                    else
                    {
#pragma warning disable 0618
                        //ビルド時は削除
                        HandGestureDatabase[] hgDatabases = ctx.AvatarRootObject.GetComponentsInChildren<HandGestureDatabase>(true);
                        foreach (var hgd in hgDatabases)
                            Object.DestroyImmediate(hgd);
#pragma warning restore 0618
                    }
                })
                .Then.Run("Create Constraint Database if Playmode", ctx =>
                {
                    if (singleton.tabakoScriptBehaviour == null) return;

                    if (RuntimeUtil.IsPlaying)
                    {
                        TabakoScriptBehaviour tabakoScriptBehaviour = singleton.tabakoScriptBehaviour;
                        GameObject tabakoScriptBehaviourGO = tabakoScriptBehaviour.gameObject;

                        SatabakoDatabase.ConstraintDatabase constraintDatabase = new SatabakoDatabase.ConstraintDatabase()
                        {
                            CT_Case = tabakoScriptBehaviour.CT_Case,
                            CT_Tabako = tabakoScriptBehaviour.CT_Tabako,
                            CT_Lighter = tabakoScriptBehaviour.CT_Lighter,
                            CT_Mouth = tabakoScriptBehaviour.CT_Mouth,

                            PC_Case = tabakoScriptBehaviour.PC_Case,
                            PC_Tabako = tabakoScriptBehaviour.PC_Tabako,
                            PC_Lighter = tabakoScriptBehaviour.PC_Lighter,
                        };

                        if (SatabakoDatabase.s.constraintDatabases.ContainsKey(ctx.AvatarRootTransform))
                            SatabakoDatabase.s.constraintDatabases[ctx.AvatarRootTransform] = constraintDatabase;
                        else
                            SatabakoDatabase.s.constraintDatabases.Add(ctx.AvatarRootTransform, constraintDatabase);
                    }
                    else
                    {
#pragma warning disable 0618
                        ConstraintDabatase[] cDatabases = ctx.AvatarRootObject.GetComponentsInChildren<ConstraintDabatase>(true);
                        foreach (var cd in cDatabases)
                            Object.DestroyImmediate(cd);
#pragma warning restore 0618
                    }
                })
                .Then.Run("Remove I Satabako Plugin", ctx =>
                {
                    //エディタ用の物のため削除
                    ISatabakoPlugin[] plugins = ctx.AvatarRootObject.GetComponentsInChildren<ISatabakoPlugin>(true);
                    foreach (var p in plugins)
                        Object.DestroyImmediate(p);
                })
                .Then.Run(MoveMenuItemPass.Instance)
                .Then.Run(IFollowSatabakoObjectPass.Instance)
                .Then.Run(IDeactiveWhenBuildPass.Instance);

            //AnimatorControllerのステートを差し替えるための処理なので、MAMergeAnimatorより後に処理
            Sequence transformingSeq = InPhase(BuildPhase.Transforming);
            transformingSeq
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run(IRemoveAvatarMaskPass.Instance) //昔のアバターによくある、FXにAvatarMaskが入ってしまっているアバター用のパス
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

        [ComponentInformation(typeof(IRemoveAvatarMask))]
    internal class IRemoveAvatarMaskInformation : ComponentInformation<IRemoveAvatarMask>
    {
        protected override void CollectMutations(IRemoveAvatarMask component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(IRemoveAvatarMask component, ComponentDependencyCollector collector) { }
    }

    [ComponentInformation(typeof(IOverrideHandGesture))]
    internal class IOverrideHandGestureInformation : ComponentInformation<IOverrideHandGesture>
    {
        protected override void CollectMutations(IOverrideHandGesture component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(IOverrideHandGesture component, ComponentDependencyCollector collector) { }
    }

#pragma warning disable 0618
    [ComponentInformation(typeof(HandGestureDatabase))]
    internal class HandGestureDatabaseInformation : ComponentInformation<HandGestureDatabase>
    {
        protected override void CollectMutations(HandGestureDatabase component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(HandGestureDatabase component, ComponentDependencyCollector collector) { }
    }

    [ComponentInformation(typeof(ConstraintDabatase))]
    internal class ConstraintDabataseInformation : ComponentInformation<ConstraintDabatase>
    {
        protected override void CollectMutations(ConstraintDabatase component, ComponentMutationsCollector collector) { }

        protected override void CollectDependency(ConstraintDabatase component, ComponentDependencyCollector collector) { }
    }
#pragma warning restore 0618

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
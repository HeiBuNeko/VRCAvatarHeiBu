using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static net.satania_shopping.tabakosystem.SataniaTabakoSwapGesture;
using net.satania_shopping.tabakosystem.runtime;

namespace net.satania_shopping.tabakosystem.ndmf
{
    public class SataniaTabakoSwapGesturePass : Pass<SataniaTabakoSwapGesturePass>
    {
        private const string k_Param_GestureLeft = "GestureLeft";
        private const string k_Param_GestureRight = "GestureRight";

        public override string DisplayName => "Satania Tabako Swap Gesture Pass";
        private PluginDefinition.Singleton singleton => PluginDefinition.Singleton.instance;
        private HandGesture GetGesture(TabakoScriptBehaviour tabakoScript, KeyConfig key)
        {
            switch (key)
            {
                case KeyConfig.Case:
                    return (HandGesture)tabakoScript.G_Case;

                case KeyConfig.Lighter:
                    return (HandGesture)tabakoScript.G_Lighter;

                case KeyConfig.Fire:
                    return (HandGesture)tabakoScript.G_Fire;

                case KeyConfig.Restore:
                    return (HandGesture)tabakoScript.G_Restore;

                case KeyConfig.Smoke:
                    return (HandGesture)tabakoScript.G_Smoke;

                case KeyConfig.Swap:
                    return (HandGesture)tabakoScript.G_Swap;

                default:
                    return HandGesture.neutral;
            }
        }

        private HandGesture GetDefaultGesture(KeyConfig key)
        {
            switch (key)
            {
                case KeyConfig.Case:
                    return HandGesture.thumbsup;

                case KeyConfig.Lighter:
                    return HandGesture.rocknroll;

                case KeyConfig.Fire:
                    return HandGesture.fist;

                case KeyConfig.Restore:
                    return HandGesture.rocknroll;

                case KeyConfig.Smoke:
                    return HandGesture.victory;

                case KeyConfig.Swap:
                    return HandGesture.thumbsup;

                default:
                    return HandGesture.neutral;
            }
        }

        protected override void Execute(BuildContext ctx)
        {
            if (singleton.tabakoScriptBehaviour == null)
                return;

            //NDMFの更新をしてない層が多そうなため、ひとまずこのまま

#pragma warning disable 0618
            AnimatorController controller = AvatarUtils.GetAvatarBaseLayer(ctx.AvatarDescriptor, VRCAvatarDescriptor.AnimLayerType.FX);
#pragma warning restore 0618
            if (controller == null)
            {
                //FXコントローラーが取得できない場合はビルドを止めず警告のみ
                SatabakoErrorReport.ReportWarning("Swap Gesture: FX コントローラーが取得できませんでした。ジェスチャーの差し替えはスキップされます。", singleton.tabakoScriptBehaviour);
                return;
            }

            (AnimatorState[], SataniaTabakoSwapGesture[][]) requireMotionStates =
                AnimatorControllerUtils.FindStatesAndBehavioursAsArrays<SataniaTabakoSwapGesture>(controller);

            if (requireMotionStates.Item1.Length == 0)
                return;

            for (int i = 0; i < requireMotionStates.Item1.Length; i++)
            {
                var state = requireMotionStates.Item1[i];
                SataniaTabakoSwapGesture[] behaviours = requireMotionStates.Item2[i];

                foreach (var behaviour in behaviours)
                {
                    //TabakoScriptから選択されたジェスチャーを取得
                    int gesture = (int)GetGesture(singleton.tabakoScriptBehaviour, behaviour.Key);
                    HandGesture defaultGesture = GetDefaultGesture(behaviour.Key);

                    var transitions = state.transitions;
                    for (int tr = 0; tr < transitions.Length; tr++)
                    {
                        var conditions = transitions[tr].conditions;
                        for (int con = 0; con < conditions.Length; con++)
                        {
                            var condition = conditions[con];

                            //GestureLeft, GestureRightだった場合
                            if (condition.parameter == k_Param_GestureLeft || condition.parameter == k_Param_GestureRight)
                            {
                                //ステートによっては2つ以上Gesture◯◯があるので、ちゃんと元の値で比較
                                if (Mathf.Approximately(condition.threshold, (float)defaultGesture))
                                {
                                    if (condition.mode == AnimatorConditionMode.Equals || condition.mode == AnimatorConditionMode.NotEqual)
                                    {
                                        condition.threshold = gesture;
                                    }
                                }
                            }

                            conditions[con] = condition;
                        }

                        transitions[tr].conditions = conditions;
                    }

                    state.transitions = transitions;

                    //削除
                    Object.DestroyImmediate(behaviour);
                }
            }
        }
    }
}
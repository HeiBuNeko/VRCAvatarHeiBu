using nadena.dev.ndmf;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using UnityObject = UnityEngine.Object;
using net.satania_shopping.tabakosystem.runtime;

#nullable enable

//[assembly: ExportsPlugin(typeof(net.satania.shopping.tabakosystem.ndmf.ParametersCompressorPlugin))]

namespace net.satania_shopping.tabakosystem.ndmf
{
    /// <summary>
    /// Satania Tabako Scriptが無くても動作するパス
    /// </summary>
    public sealed class ParametersCompressorPass : Pass<ParametersCompressorPass>
    {
        //https://github.com/bdunderscore/modular-avatar/blob/595c3f945e3194d5a0cbcbd9cb2a5d7a5faea5a1/Editor/ParameterPolicy.cs#L39
        public static ImmutableHashSet<string> VRCSDKParameters = new string[]
        {
            "IsLocal",
            "PreviewMode",
            "Viseme",
            "Voice",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "VelocityMagnitude",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation",
            "Earmuffs",
            "IsOnFriendsList",
            "AvatarVersion",
            "ScaleModified",
            "ScaleFactor",
            "ScaleFactorInverse",
            "EyeHeightAsMeters",
            "EyeHeightAsPercent",
        }.ToImmutableHashSet();

        private const string k_compressedParameterName = "__COMPRESSOR__{0}__bit{1}";

        //public override string QualifiedName => "net.satania.shopping.tabakosystem.parameters-compressor";
        public override string DisplayName => "Parameters Compressor";

        private const string IsLocal = "IsLocal";

        private readonly int[] maxBitValues = new int[]
        {
            255,    //圧縮しない
            1,      //1bit
            3,      //2bit
            7,      //3bit
            15,     //4bit
            31,     //5bit
            63,     //6bit
            127     //7bit
        };

        private bool? writeDefaultValue = true;
        private UnityObject? assetContainer;
        private int intBitSize;
        private AnimationClip? emptyAnimationClip;

        private AnimatorControllerParameterType GetOrAddIsLocalParameter(AnimatorController animatorController)
        {
            AnimatorControllerParameter isLocal = animatorController.parameters.FirstOrDefault(parameter => parameter.name == IsLocal);
            AnimatorControllerParameterType isLocalType = default;

            //パラメータがなかった場合は新たに追加
            if (isLocal == null)
            {
                animatorController.AddParameter(IsLocal, AnimatorControllerParameterType.Bool);
                return AnimatorControllerParameterType.Bool;
            }

            return isLocalType = isLocal.type;
        }

        private AnimatorStateMachine CreateStateMachine(string name, HideFlags flag = HideFlags.HideInHierarchy)
        {
            AnimatorStateMachine newStateMachine = new AnimatorStateMachine();
            newStateMachine.name = name;
            newStateMachine.hideFlags = flag;

            return newStateMachine;
        }

        private AnimatorCondition CreateIsLocalCondition(AnimatorControllerParameterType isLocalParameterType, bool toggle)
        {
            AnimatorCondition condition = new AnimatorCondition();
            condition.parameter = IsLocal;

            //変換
            //https://creators.vrchat.com/avatars/animator-parameters/#mismatched-parameter-type-conversion

            if (isLocalParameterType == AnimatorControllerParameterType.Float)
            {
                condition.mode = toggle ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less;
                condition.threshold = toggle ? 0.1f : 0.001f;
            }
            else if (isLocalParameterType == AnimatorControllerParameterType.Int)
            {
                condition.mode = AnimatorConditionMode.Equals;
                condition.threshold = toggle ? 1 : 0;
            }
            else
            {
                condition.mode = toggle ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;
            }

            return condition;
        }

        private AnimatorCondition[] CreateIsLocalConditions(AnimatorControllerParameterType isLocalParameterType, bool toggle)
        {
            return new AnimatorCondition[] { CreateIsLocalCondition(isLocalParameterType, toggle) };
        }

        private AnimatorCondition[] CreateEncodeConditions(int intValue, string parameterName, bool toggle)
        {
            AnimatorCondition condition = new AnimatorCondition();
            condition.parameter = parameterName;
            condition.threshold = intValue;
            condition.mode = toggle ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual;

            return new AnimatorCondition[] { condition };
        }

        public static bool[] ConvertToBoolArray(int decimalNumber, int bitSize)
        {
            if (decimalNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decimalNumber), "負の数はサポートされていません。");
            }
            if (bitSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitSize), "ビットサイズは正の整数である必要があります。");
            }

            string binaryString = Convert.ToString(decimalNumber, 2);

            if (binaryString.Length > bitSize)
            {
                throw new ArgumentOutOfRangeException(nameof(decimalNumber),
                    $"数値 {decimalNumber} は {bitSize} ビットで表現できません。最低でも {binaryString.Length} ビット必要です。");
            }

            string paddedBinaryString = binaryString.PadLeft(bitSize, '0');
            bool[] boolArray = paddedBinaryString.Select(c => c == '1').ToArray();

            return boolArray;
        }

        private void AddRemoteTransitions(string[] bitParameterNames, bool[] bitArray, AnimatorState remoteCentral, AnimatorState decodeState)
        {
            //変換ステートに行く場合は完全一致で    
            List<AnimatorCondition> toDecodeConditions = new List<AnimatorCondition>();
            List<AnimatorStateTransition> toCentralTransitions = new List<AnimatorStateTransition>();
            for (int bit = 0; bit < intBitSize; bit++)
            {
                AnimatorCondition condition = new AnimatorCondition();
                condition.parameter = bitParameterNames[bit];
                condition.mode = bitArray[bit] ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;
                toDecodeConditions.Add(condition);

                AnimatorStateTransition stateTransition = CreateNoTimeTransition(new AnimatorCondition[]
                {
                    new AnimatorCondition()
                    {
                        parameter = bitParameterNames[bit],
                        mode = bitArray[bit] ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If
                    },
                }, remoteCentral);
                AddObjectToContainer(stateTransition);
                toCentralTransitions.Add(stateTransition);
            }

            AnimatorStateTransition toDecodeTransition = new AnimatorStateTransition()
            {
                conditions = toDecodeConditions.ToArray(),
                exitTime = 0,
                hasExitTime = false,
                duration = 0,
                isExit = false,
                destinationState = decodeState
            };

            AddObjectToContainer(toDecodeTransition);

            //decoderからcentral
            remoteCentral.AddTransition(toDecodeTransition);

            //centralからdecoder
            foreach (var transition in toCentralTransitions)
            {
                decodeState.AddTransition(transition);
            }
        }

        private AnimatorStateTransition CreateNoTimeTransition(AnimatorCondition[] conditions, AnimatorStateMachine destinationStateMachine)
        {
            return new AnimatorStateTransition()
            {
                conditions = conditions,
                exitTime = 0,
                hasExitTime = false,
                duration = 0,
                isExit = false,
                destinationStateMachine = destinationStateMachine
            };
        }

        private AnimatorStateTransition CreateNoTimeTransition(AnimatorCondition[] conditions, AnimatorState destinationState)
        {
            return new AnimatorStateTransition()
            {
                conditions = conditions,
                exitTime = 0,
                hasExitTime = false,
                duration = 0,
                isExit = false,
                destinationState = destinationState
            };
        }

        private AnimatorState CreateNewState(string name)
        {
            bool writeDefault = writeDefaultValue == null ? false : (bool)writeDefaultValue;

            AnimatorState state = new AnimatorState() { name = name };
            state.writeDefaultValues = writeDefault;

            //一応空のアニメーションを入れる (入れなくてもいい)
            state.motion = emptyAnimationClip;
            return state;
        }

        private void AddObjectToContainer(UnityObject unityObject)
        {
            if (assetContainer != null)
                AssetDatabase.AddObjectToAsset(unityObject, assetContainer);
        }

        private void AddBitConverterLayer(
            AnimatorController controller,
            VRCExpressionParameters parameters,
            string syncedParameterName,
            BitSize bitSize,
            AnimatorControllerParameterType isLocalParameterType)
        {
            if (bitSize == BitSize.None)
                return;

            var newLayer = new AnimatorControllerLayer()
            {
                name = controller.MakeUniqueLayerName($"{syncedParameterName}_Bit_Converter"),
                defaultWeight = 0f,
                blendingMode = AnimatorLayerBlendingMode.Override,
            };

            AnimatorStateMachine topStateMachine = CreateStateMachine(newLayer.name);
            AddObjectToContainer(topStateMachine);
            newLayer.stateMachine = topStateMachine;

            AnimatorState initState = CreateNewState("Init");
            AddObjectToContainer(initState);
            newLayer.stateMachine.AddState(initState, new Vector3(0, 200, 0));

            AnimatorStateMachine localStateMachine = CreateStateMachine(newLayer.stateMachine.MakeUniqueStateMachineName("Local"));
            AnimatorStateMachine remoteStateMachine = CreateStateMachine(newLayer.stateMachine.MakeUniqueStateMachineName("Remote"));

            AddObjectToContainer(localStateMachine);
            AddObjectToContainer(remoteStateMachine);

            newLayer.stateMachine.AddStateMachine(localStateMachine, new Vector3(-200, 300, 0));
            newLayer.stateMachine.AddStateMachine(remoteStateMachine, new Vector3(200, 300, 0));

            //Local
            AnimatorState localCentral = CreateNewState("Local Central");
            localStateMachine.AddState(localCentral, new Vector3(0, 300, 0));
            AddObjectToContainer(localCentral);

            //Remote
            AnimatorState remoteCentral = CreateNewState("Remote Central");
            remoteStateMachine.AddState(remoteCentral, new Vector3(0, 300, 0));
            AddObjectToContainer(remoteCentral);


            //Local StateMachine
            AnimatorStateTransition initToLocalTransition =
                CreateNoTimeTransition(CreateIsLocalConditions(isLocalParameterType, true), localCentral);
            initState.AddTransition(initToLocalTransition);
            AddObjectToContainer(initToLocalTransition);

            //Remote StateMachine
            AnimatorStateTransition initToRemoteTransition =
                CreateNoTimeTransition(CreateIsLocalConditions(isLocalParameterType, false), remoteCentral);
            initState.AddTransition(initToRemoteTransition);
            AddObjectToContainer(initToRemoteTransition);

            //Localじゃなくなった場合はInitに帰る
            AnimatorStateTransition localToInitTransition =
                CreateNoTimeTransition(CreateIsLocalConditions(isLocalParameterType, false), initState);
            localCentral.AddTransition(localToInitTransition);
            AddObjectToContainer(localToInitTransition);

            //Remoteじゃなくなった場合はInitに帰る
            AnimatorStateTransition remoteToInitTransition =
                CreateNoTimeTransition(CreateIsLocalConditions(isLocalParameterType, true), initState);
            remoteCentral.AddTransition(remoteToInitTransition);
            AddObjectToContainer(remoteToInitTransition);

            //指定されたBitSize分bitを複製
            intBitSize = (int)bitSize;

            List<VRCExpressionParameters.Parameter> parameterList = parameters.parameters.ToList();

            string[] bitParameterNames = new string[intBitSize];
            for (int i = 0; i < intBitSize; i++)
            {
                bitParameterNames[i] = controller.MakeUniqueParameterName(string.Format(k_compressedParameterName, syncedParameterName, i));
                controller.AddParameter(new AnimatorControllerParameter() { name = bitParameterNames[i], type = AnimatorControllerParameterType.Bool });

                //パラメータがそもそもなかった場合追加 (LocomotionやGestureレイヤーにだけある場合)
                if (controller.parameters.Where(x => x.name == syncedParameterName).Count() == 0)
                {
                    controller.AddParameter(new AnimatorControllerParameter() { name = syncedParameterName, type = AnimatorControllerParameterType.Int });
                }

                if (parameterList.Where(x => x.name == bitParameterNames[i]).Count() == 0)
                    parameterList.Add(new VRCExpressionParameters.Parameter()
                    {
                        defaultValue = 0,
                        networkSynced = true,
                        saved = false,
                        valueType = VRCExpressionParameters.ValueType.Bool,
                        name = bitParameterNames[i]
                    });
            }
            parameters.parameters = parameterList.ToArray();

            int maxValue = maxBitValues[intBitSize];
            for (int intValue = 0; intValue <= maxValue; intValue++)
            {
                float sqrt = Mathf.Sqrt(maxValue + 1);

                float yPos = intValue / (int)sqrt;
                float xPos = intValue % (int)sqrt;

                AnimatorState encodeState = CreateNewState($"Encode {intValue}");
                localStateMachine.AddState(encodeState, new Vector3(-500 + (xPos * 250), 500 + (yPos * 100), 0));
                AddObjectToContainer(encodeState);

                AnimatorStateTransition localConvertConditionTrue =
                    CreateNoTimeTransition(CreateEncodeConditions(intValue, syncedParameterName, true), encodeState);
                AnimatorStateTransition localConvertConditionFalse =
                    CreateNoTimeTransition(CreateEncodeConditions(intValue, syncedParameterName, false), localCentral);

                AddObjectToContainer(localConvertConditionTrue);
                AddObjectToContainer(localConvertConditionFalse);

                localCentral.AddTransition(localConvertConditionTrue);
                encodeState.AddTransition(localConvertConditionFalse);

                //エンコード
                VRCAvatarParameterDriver encodeParameterDriver = encodeState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                for (int bit = 0; bit < intBitSize; bit++)
                {
                    encodeParameterDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Set,
                        name = bitParameterNames[bit],
                        value = (intValue >> bit) & 1
                    });
                }

                AnimatorState decodeState = CreateNewState($"Decode {intValue}");
                remoteStateMachine.AddState(decodeState, new Vector3(-500 + (xPos * 250), 500 + (yPos * 100), 0));
                AddObjectToContainer(decodeState);

                var bitArray = ConvertToBoolArray(intValue, intBitSize).Reverse().ToArray();

                VRCAvatarParameterDriver decodeParameterDriver = decodeState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                decodeParameterDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    name = syncedParameterName,
                    value = intValue
                });

                //リモート側のトランジションを設定
                AddRemoteTransitions(/*intValue, */bitParameterNames, bitArray, remoteCentral, decodeState);
            }

            controller.AddLayer(newLayer);
        }

        private void AddBitConverter(AnimatorController controller, VRCExpressionParameters expressionParameters, ParametersCompressor.CompressSetting[] compressSettings, AnimatorControllerParameterType isLocalParameterType)
        {
            VRCExpressionParameters.Parameter[] syncedIntExpressionParameters = expressionParameters.parameters
            .Where(x => x.networkSynced && x.valueType == VRCExpressionParameters.ValueType.Int)
            .ToArray();

            //compressSettingで指定されたパラメータのみを圧縮
            string[] compressSettingNames = compressSettings.Select(x => x.parameterName).ToArray();

            foreach (var syncedInt in syncedIntExpressionParameters)
            {
                string syncedParameterName = syncedInt.name;

                int index = Array.IndexOf(compressSettingNames, syncedParameterName);

                if (index == -1) continue;
                if (compressSettings[index].bitSize == BitSize.None) continue;

                //VRChat側で提供されるパラメータだった場合はスキップ
                //https://github.com/bdunderscore/modular-avatar/blob/595c3f945e3194d5a0cbcbd9cb2a5d7a5faea5a1/Editor/ParameterPolicy.cs#L315
                if (VRCSDKParameters.Contains(syncedParameterName) || string.IsNullOrEmpty(syncedParameterName)) continue;

                //動的生成されるパラメータの可能性もあるので、スキップしない
                //if (controller.parameters.Where(x => x.name.Equals(name)).Count() == 0) continue;

                var setting = compressSettings[index];

                //同期をOFF
                syncedInt.networkSynced = false;

                AddBitConverterLayer(controller, expressionParameters, syncedParameterName, setting.bitSize, isLocalParameterType);
            }
        }

        private static AnimatorState[] GetAllStates(AnimatorControllerLayer layer)
        {
            List<AnimatorState> states = new List<AnimatorState>();
            if (layer.stateMachine != null)
            {
                //ルート直下のステートとサブステートマシン内のステートを再帰的に収集
                RecursiveGetAllStates(layer.stateMachine, states);
            }

            return states.ToArray();
        }

        private static void RecursiveGetAllStates(AnimatorStateMachine stateMachine, List<AnimatorState> states)
        {
            // この階層のステートを追加
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null && !states.Contains(childState.state))
                {
                    states.Add(childState.state);
                }
            }

            // この階層のStateMachineに対して再帰呼び出し
            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                RecursiveGetAllStates(childStateMachine.stateMachine, states);
            }
        }

        //https://github.com/bdunderscore/modular-avatar/blob/1.12.5/Editor/MergeAnimatorProcessor.cs#L219
        private static bool IsWriteDefaultsSafeLayer(AnimatorControllerLayer layer)
        {
            if (layer.blendingMode == AnimatorLayerBlendingMode.Additive) return true;
            var sm = layer.stateMachine;

            if (sm.stateMachines.Length != 0) return false;
            return sm.states.Length == 1 && sm.anyStateTransitions.Length == 0 &&
                   sm.defaultState?.transitions.Length == 0 && sm.defaultState.motion is BlendTree;
        }

        //https://github.com/bdunderscore/modular-avatar/blob/1.12.5/Editor/MergeAnimatorProcessor.cs#L125
        internal static bool? AnalyzeLayerWriteDefaults(AnimatorController controller)
        {
            bool? writeDefaults = null;

            var wdStateCounter = controller.layers
                .Where(l => !IsWriteDefaultsSafeLayer(l))
                .SelectMany(l => GetAllStates(l))
                .Select(s => s.writeDefaultValues)
                .GroupBy(b => b)
                .ToDictionary(g => g.Key, g => g.Count());

            if (wdStateCounter.Count == 1) writeDefaults = wdStateCounter.First().Key;
            return writeDefaults;
        }

        protected override void Execute(BuildContext ctx)
        {
            ParametersCompressor[] compressors = ctx.AvatarRootObject.GetComponentsInChildren<ParametersCompressor>(true);
            assetContainer = ctx.AssetContainer;

            //そもそもアバター内にない場合はスキップ
            if (compressors == null || compressors.Length == 0)
                return;

            emptyAnimationClip = new AnimationClip();
            AddObjectToContainer(emptyAnimationClip);

            //NDMFの更新をしてない層が多そうなため、ひとまずこのまま
#pragma warning disable 0618
            AnimatorController? controller = AvatarUtils.GetAvatarBaseLayer(ctx.AvatarDescriptor, VRCAvatarDescriptor.AnimLayerType.FX);
            if (controller == null)
                return;

            //NDMFの更新をしてない層が多そうなため、ひとまずこのまま
            VRCExpressionParameters expressionParameters = ctx.AvatarDescriptor.expressionParameters;
#pragma warning restore 0618

            for (int i = 0; i < compressors.Length; i++)
            {
                ParametersCompressor compressor = compressors[i];

                if (compressor != null)
                {
                    try
                    {
                        writeDefaultValue = AnalyzeLayerWriteDefaults(controller);
                        AnimatorControllerParameterType isLocalParameterType = GetOrAddIsLocalParameter(controller);

                        AddBitConverter(controller, expressionParameters, compressor.compressSettings, isLocalParameterType);
                    }
                    catch (Exception ex)
                    {
                        //NDMFコンソールに警告 (発生元のParametersCompressorも表示)
                        SatabakoErrorReport.ReportAsWarning(ex, compressor);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(compressor);
                    }
                }
            }
        }
    }
}
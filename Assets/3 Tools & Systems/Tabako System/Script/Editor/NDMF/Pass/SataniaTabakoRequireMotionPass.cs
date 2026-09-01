using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using net.satania_shopping.tabakosystem.editor;
using net.satania_shopping.tabakosystem.runtime;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;

namespace net.satania_shopping.tabakosystem.ndmf
{
    public class SataniaTabakoRequireMotionPass : Pass<SataniaTabakoRequireMotionPass>
    {
        private const string k_Attribute_blendshape = "blendShape.{0}";
        private const string k_Attribute_Intensity = "m_Intensity";
        private const string k_Attribute_Range = "m_Range";
        private const string k_Attribute_IsActive = "m_IsActive";
        private const string k_Attribute_EmissionEnabled = "EmissionModule.enabled";
        private const string k_Attribute_LocalPosition = "m_LocalPosition.{0}";
        private const string k_Attribute_lilToon_EmissionColor = "material._EmissionColor.{0}"; //material._EmissionColor.r


        private static float m_minContactMultipilier = 0.5f;
        private static float m_maxContactMultipilier = 1.5f;
        private static TabakoScriptBehaviour tabakoScriptBehaviour;
        private static float m_maxFiredAnimationLength = 120f;

        public static string AttributeBlendshape => k_Attribute_blendshape;
        public static string AttributeIsActive => k_Attribute_IsActive;
        public static string AttributeEmissionEnabled => k_Attribute_EmissionEnabled;
        public static string AttributeLocalPosition => k_Attribute_LocalPosition;

        public override string DisplayName => "Satania Tabako Require Motion Pass";

        private AnimationCurve CreateContactSizeMinMaxCurve(float v)
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(new Keyframe() { time = 0f, value = v * m_minContactMultipilier });
            curve.AddKey(new Keyframe() { time = 0.5f, value = v });
            curve.AddKey(new Keyframe() { time = 1f, value = v * m_maxContactMultipilier });
            return curve;
        }

        private Keyframe[] CreateOneFrameKeyFrame(float time, float value)
        {
            return new Keyframe[]
            {
                new Keyframe()
                {
                    time = time,
                    value = value
                }
            };
        }

        private Keyframe[] CreateTwoFrameKeyFrame(float time_1, float time_2, float value, float value2)
        {
            return new Keyframe[]
            {
                new Keyframe()
                {
                    time = time_1,
                    value = value
                },

                new Keyframe()
                {
                    time = time_2,
                    value = value2
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="tabakoFiredAnimationCBA"></param>
        /// <param name="fired"></param>
        /// <param name="nonFired"></param>
        /// <param name="restore">タバコの長さをリセットする用のアニメーション</param>
        private void CreateFiredAnimationClip(BuildContext ctx, IControlledByAnimation tabakoFiredAnimationCBA,
            out AnimationClip fired,
            out AnimationClip nonFired,
            out AnimationClip restore)
        {
            string path = "";

            fired = new AnimationClip();
            nonFired = new AnimationClip();
            restore = new AnimationClip();

            AnimationCurve cActive = new AnimationCurve
            {
                keys = new Keyframe[]
                {
                    new Keyframe()
                    {
                        time = 0f,
                        value = 1f,
                    }
                }
            };
            AnimationCurve cDeactive = new AnimationCurve
            {
                keys = new Keyframe[]
                {
                    new Keyframe()
                    {
                        time = 0f,
                        value = 0f,
                    }
                }
            };

            if (tabakoFiredAnimationCBA != null)
            {
                SkinnedMeshRenderer tabakoMesh = tabakoFiredAnimationCBA.TabakoMesh;
                string shortName = tabakoFiredAnimationCBA.ShortShapekeyName;
                string short2Name = tabakoFiredAnimationCBA.Short2ShapekeyName;

                Transform firedAnimSmokeStart = tabakoFiredAnimationCBA.FiredAnim_SmokeStart;
                Transform firedAnimSmokeEnd = tabakoFiredAnimationCBA.FiredAnim_SmokeEnd;
                Transform tabakoTipPosition = tabakoFiredAnimationCBA.TabakoTipPosition;

                ParticleSystem tabakoSmokeParticle = tabakoFiredAnimationCBA.TabakoSmokeParticle;
                GameObject fireSenderGO = tabakoFiredAnimationCBA.FireSenderGO;
                GameObject fireReceiverGO = tabakoFiredAnimationCBA.FireReceiverGO;
                GameObject fireSenderVRC = tabakoFiredAnimationCBA.FireSender_VRC;
                GameObject fireReceiverVRC = tabakoFiredAnimationCBA.FireReceiver_VRC;

                //タバコのシェイプキー
                if (tabakoMesh != null)
                {
                    string hP_tabakoMesh = AnimationUtility.CalculateTransformPath(tabakoMesh.transform, ctx.AvatarRootTransform);

                    AnimationCurve zeroCurve = new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = 0,
                            }
                        }
                    };

                    AnimationCurve shortFiredCurve = new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = tabakoFiredAnimationCBA.ShortShapekeyMaxValue,
                            },
                        }
                    };

                    AnimationCurve short2FiredAnimatedCurve = new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = 0,
                            },
                            new Keyframe()
                            {
                                time = m_maxFiredAnimationLength,
                                value = tabakoFiredAnimationCBA.Short2ShapekeyMaxValue
                            }
                        }
                    };

                    nonFired.SetCurve(hP_tabakoMesh, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_blendshape, shortName), zeroCurve);
                    nonFired.SetCurve(hP_tabakoMesh, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_blendshape, short2Name), zeroCurve);

                    fired.SetCurve(hP_tabakoMesh, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_blendshape, shortName), shortFiredCurve);
                    fired.SetCurve(hP_tabakoMesh, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_blendshape, short2Name), short2FiredAnimatedCurve);

                    restore.SetCurve(hP_tabakoMesh, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_blendshape, shortName), shortFiredCurve);
                    restore.SetCurve(hP_tabakoMesh, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_blendshape, short2Name), zeroCurve);
                }
                else
                    SatabakoErrorReport.ReportWarning("tabakoMesh がnullです。", tabakoFiredAnimationCBA);

                //タバコの先端の位置
                if (tabakoTipPosition != null && firedAnimSmokeStart != null && firedAnimSmokeEnd != null)
                {
                    string hP_tipTransform = AnimationUtility.CalculateTransformPath(tabakoTipPosition, ctx.AvatarRootTransform);

                    tabakoTipPosition.position = firedAnimSmokeEnd.position;
                    Vector3 endLocalPosition = tabakoTipPosition.localPosition;

                    tabakoTipPosition.position = firedAnimSmokeStart.position;
                    Vector3 startLocalPosition = tabakoTipPosition.localPosition;


                    fired.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "x"), new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = startLocalPosition.x
                            },
                            new Keyframe()
                            {
                                time = m_maxFiredAnimationLength,
                                value = endLocalPosition.x,
                            }
                        }
                    });

                    fired.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "y"), new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = startLocalPosition.y
                            },
                            new Keyframe()
                            {
                                time = m_maxFiredAnimationLength,
                                value = endLocalPosition.y,
                            }
                        }
                    });

                    fired.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "z"), new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = startLocalPosition.z
                            },
                            new Keyframe()
                            {
                                time = m_maxFiredAnimationLength,
                                value = endLocalPosition.z,
                            }
                        }
                    });

                    AnimationCurve startPositionCurve_X = new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = startLocalPosition.x
                            }
                        }
                    };

                    AnimationCurve startPositionCurve_Y = new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = startLocalPosition.y
                            }
                        }
                    };

                    AnimationCurve startPositionCurve_Z = new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = startLocalPosition.z
                            }
                        }
                    };

                    nonFired.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "x"), startPositionCurve_X);
                    nonFired.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "y"), startPositionCurve_Y);
                    nonFired.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "z"), startPositionCurve_Z);

                    restore.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "x"), startPositionCurve_X);
                    restore.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "y"), startPositionCurve_Y);
                    restore.SetCurve(hP_tipTransform, typeof(Transform), string.Format(k_Attribute_LocalPosition, "z"), startPositionCurve_Z);
                }
                else
                {
                    if (tabakoTipPosition == null)
                        SatabakoErrorReport.ReportWarning("tabakoTipPosition がnullです。", tabakoFiredAnimationCBA);

                    if (firedAnimSmokeStart == null)
                        SatabakoErrorReport.ReportWarning("firedAnimSmokeStart がnullです。", tabakoFiredAnimationCBA);

                    if (firedAnimSmokeEnd == null)
                        SatabakoErrorReport.ReportWarning("firedAnimSmokeEnd がnullです。", tabakoFiredAnimationCBA);
                }

                //タバコの煙
                if (tabakoSmokeParticle != null)
                {
                    string hP_smokeParticle = AnimationUtility.CalculateTransformPath(tabakoSmokeParticle.transform, ctx.AvatarRootTransform);

                    fired.SetCurve(hP_smokeParticle, typeof(ParticleSystem), string.Format(k_Attribute_EmissionEnabled), new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = 1,
                            }
                        }
                    });

                    nonFired.SetCurve(hP_smokeParticle, typeof(ParticleSystem), string.Format(k_Attribute_EmissionEnabled), new AnimationCurve()
                    {
                        keys = new Keyframe[]
                        {
                            new Keyframe()
                            {
                                time = 0,
                                value = 0,
                            }
                        }
                    });

                    restore.SetCurve(hP_smokeParticle, typeof(ParticleSystem), string.Format(k_Attribute_EmissionEnabled), new AnimationCurve()
                    {
                        keys = new Keyframe[]
                    {
                            new Keyframe()
                            {
                                time = 0,
                                value = 1,
                            }
                    }
                    });
                }
                else
                    SatabakoErrorReport.ReportWarning("tabakoSmokeParticle がnullです。", tabakoFiredAnimationCBA);

                //炎センダー
                if (fireSenderGO != null)
                {
                    path = AnimationUtility.CalculateTransformPath(fireSenderGO.transform, ctx.AvatarRootTransform);

                    fired.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cActive);
                    nonFired.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cDeactive);
                    restore.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cActive);
                }
                else
                    SatabakoErrorReport.ReportWarning("fireSenderGO がnullです。", tabakoFiredAnimationCBA);

                if (fireSenderVRC != null)
                {
                    path = AnimationUtility.CalculateTransformPath(fireSenderVRC.transform, ctx.AvatarRootTransform);

                    fired.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cActive);
                    nonFired.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cDeactive);
                    restore.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cActive);
                }
                else
                    SatabakoErrorReport.ReportWarning("fireSenderVRC がnullです。", tabakoFiredAnimationCBA);

                //炎レシーバー
                if (fireReceiverGO != null)
                {
                    path = AnimationUtility.CalculateTransformPath(fireReceiverGO.transform, ctx.AvatarRootTransform);

                    fired.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cDeactive);
                    nonFired.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cActive);
                    restore.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cDeactive);
                }
                else
                    SatabakoErrorReport.ReportWarning("fireReceiverGO がnullです。", tabakoFiredAnimationCBA);

                if (fireReceiverVRC != null)
                {
                    path = AnimationUtility.CalculateTransformPath(fireReceiverVRC.transform, ctx.AvatarRootTransform);

                    fired.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cDeactive);
                    nonFired.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cActive);
                    restore.SetCurve(path, typeof(GameObject), k_Attribute_IsActive, cDeactive);
                }
                else
                    SatabakoErrorReport.ReportWarning("fireReceiverVRC がnullです。", tabakoFiredAnimationCBA);
            }
        }

        private AnimationClip CreateContactSizeControllerClip(BuildContext ctx, IControlledByAnimation[] controlledByAnimations)
        {
            AnimationClip animContactSize = new AnimationClip();

            for (int i = 0; i < controlledByAnimations.Length; i++)
            {
                var contactSizeControllerTarget = controlledByAnimations[i]?.transform;
                if (contactSizeControllerTarget == null)
                    continue;

                string hierarchyPath = AnimationUtility.CalculateTransformPath(contactSizeControllerTarget, ctx.AvatarRootTransform);
                VRCContactReceiver contactReceiver = contactSizeControllerTarget.GetComponent<VRCContactReceiver>();
                VRCContactSender contactSender = contactSizeControllerTarget.GetComponent<VRCContactSender>();
                Transform _transform = contactSizeControllerTarget.transform;

                if (contactReceiver != null || contactSender != null)
                {
                    AnimationCurve curve_x = CreateContactSizeMinMaxCurve(_transform.localScale.x);
                    AnimationCurve curve_y = CreateContactSizeMinMaxCurve(_transform.localScale.y);
                    AnimationCurve curve_z = CreateContactSizeMinMaxCurve(_transform.localScale.z);

                    animContactSize.SetCurve(hierarchyPath, typeof(Transform), "m_LocalScale.x", curve_x);
                    animContactSize.SetCurve(hierarchyPath, typeof(Transform), "m_LocalScale.y", curve_y);
                    animContactSize.SetCurve(hierarchyPath, typeof(Transform), "m_LocalScale.z", curve_z);

                }
            }

            return animContactSize;
        }

        private void CreateExhaleAnimationClip(BuildContext ctx, IControlledByAnimation exhaleAnimationCBA, out AnimationClip offClip, out AnimationClip onClip)
        {
            offClip = new AnimationClip();
            onClip = new AnimationClip();
            if (exhaleAnimationCBA != null)
            {
                SkinnedMeshRenderer tabakoMesh = exhaleAnimationCBA.TabakoMesh;

                if (tabakoMesh != null)
                {
                    string hP_tabakoMesh = AnimationUtility.CalculateTransformPath(tabakoMesh.transform, ctx.AvatarRootTransform);

                    Color offEmission = exhaleAnimationCBA.EmissiveOFFColor;
                    Color onEmission = exhaleAnimationCBA.EmissiveONColor;

                    //OFF
                    AddSingleEmissiveColorCurve(offClip, 0, hP_tabakoMesh, offEmission);

                    //ON
                    AddDoubleEmissiveColorCurve(onClip, exhaleAnimationCBA.Time_EmissiveON, hP_tabakoMesh, offEmission, onEmission);
                }
                else
                    SatabakoErrorReport.ReportWarning("ExhaleEmission: TabakoMesh が未設定です。", exhaleAnimationCBA);
            }

            void AddSingleEmissiveColorCurve(AnimationClip clip, float time, string path, Color color)
            {
                clip.SetCurve(path, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_lilToon_EmissionColor, "r"),
                    new AnimationCurve() { keys = CreateOneFrameKeyFrame(time, color.r) });

                clip.SetCurve(path, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_lilToon_EmissionColor, "g"),
                    new AnimationCurve() { keys = CreateOneFrameKeyFrame(time, color.g) });

                clip.SetCurve(path, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_lilToon_EmissionColor, "b"),
                    new AnimationCurve() { keys = CreateOneFrameKeyFrame(time, color.b) });

                clip.SetCurve(path, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_lilToon_EmissionColor, "a"),
                    new AnimationCurve() { keys = CreateOneFrameKeyFrame(time, color.a) });
            }

            void AddDoubleEmissiveColorCurve(AnimationClip clip, float onTime, string path, Color offColor, Color onColor)
            {
                clip.SetCurve(path, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_lilToon_EmissionColor, "r"),
                    new AnimationCurve() { keys = CreateTwoFrameKeyFrame(0, onTime, offColor.r, onColor.r) });

                clip.SetCurve(path, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_lilToon_EmissionColor, "g"),
                    new AnimationCurve() { keys = CreateTwoFrameKeyFrame(0, onTime, offColor.g, onColor.g) });

                clip.SetCurve(path, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_lilToon_EmissionColor, "b"),
                    new AnimationCurve() { keys = CreateTwoFrameKeyFrame(0, onTime, offColor.b, onColor.b) });

                clip.SetCurve(path, typeof(SkinnedMeshRenderer), string.Format(k_Attribute_lilToon_EmissionColor, "a"),
                    new AnimationCurve() { keys = CreateTwoFrameKeyFrame(0, onTime, offColor.a, onColor.a) });
            }
        }

        private void AddLighterFiredKey(Transform AvatarRootTransform, IControlledByAnimation lighterFiredCBA, AnimatorState state)
        {
            var lighterMesh = lighterFiredCBA.LighterMeshRenderer;

            AnimationClip clip = (AnimationClip)state.motion;
            string hierarchyPath = AnimationUtility.CalculateTransformPath(lighterMesh.transform, AvatarRootTransform);
            string propertyName = string.Format(k_Attribute_blendshape, lighterFiredCBA.LighterONShapeName);

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);

            float maxValue = lighterFiredCBA.LighterONShapekeyMaxValue;

            //これ遅いので、どうにかしたい
            foreach (EditorCurveBinding binding in curveBindings)
            {
                //すでにアニメーション内に含まれていた場合は、値だけ置き換え (パス+プロパティで一致判定)
                if (binding.path == hierarchyPath && binding.propertyName == propertyName)
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    curve.keys = CreateOneFrameKeyFrame(0, maxValue);

                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                    return;
                }
            }

            clip.SetCurve(hierarchyPath, typeof(SkinnedMeshRenderer), propertyName, new AnimationCurve() { keys = CreateOneFrameKeyFrame(0, maxValue) });
        }

        private void AddLighterNonFiredKey(Transform AvatarRootTransform, IControlledByAnimation lighterFiredCBA, AnimatorState state)
        {
            var lighterMesh = lighterFiredCBA.LighterMeshRenderer;

            AnimationClip clip = (AnimationClip)state.motion;
            string hierarchyPath = AnimationUtility.CalculateTransformPath(lighterMesh.transform, AvatarRootTransform);
            string propertyName = string.Format(k_Attribute_blendshape, lighterFiredCBA.LighterONShapeName);

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);

            //これ遅いので、どうにかしたい
            foreach (EditorCurveBinding binding in curveBindings)
            {
                //すでにアニメーション内に含まれていた場合は、値だけ置き換え (パス+プロパティで一致判定)
                if (binding.path == hierarchyPath && binding.propertyName == propertyName)
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    curve.keys = CreateOneFrameKeyFrame(0, 0);

                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                    return;
                }
            }

            clip.SetCurve(hierarchyPath, typeof(SkinnedMeshRenderer), propertyName, new AnimationCurve() { keys = CreateOneFrameKeyFrame(0, 0) });
        }

        private void Process_Gesture(BuildContext ctx, IOverrideHandGesture[] overrideHandGestures)
        {
            AnimatorController gestureController = AvatarUtils.GetAvatarBaseLayer(ctx.VRChatAvatarDescriptor(), VRCAvatarDescriptor.AnimLayerType.Gesture);
            if (gestureController == null)
                throw new System.Exception("Gesture controller is null.");

            (AnimatorState[], SataniaTabakoRequireMotion[][]) requireMotionStates = AnimatorControllerUtils.FindStatesAndBehavioursAsArrays<SataniaTabakoRequireMotion>(gestureController);

            Motion gestureCase = overrideHandGestures
                .Where(x => x != null && x.OverrideMotion == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Case)
                .Select(x => x.CustomMotion)
                .FirstOrDefault();

            Motion gestureTabacco = overrideHandGestures
                .Where(x => x != null && x.OverrideMotion == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Tabacco)
                .Select(x => x.CustomMotion)
                .FirstOrDefault();

            Motion gestureLighter = overrideHandGestures
                .Where(x => x != null && x.OverrideMotion == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Lighter)
                .Select(x => x.CustomMotion)
                .FirstOrDefault();

            for (int i = 0; i < requireMotionStates.Item1.Length; i++)
            {
                var state = requireMotionStates.Item1[i];
                var behaviours = requireMotionStates.Item2[i];

                foreach (var behaviour in behaviours)
                {
                    //今は置き換えのみ対応。Override の場合だけ motion を差し替える
                    if (behaviour.MotionID == SataniaTabakoRequireMotion.AutomaticMotion.Override)
                    {
                        if (behaviour.OverrideMotionType == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Case && gestureCase != null)
                            state.motion = gestureCase;

                        if (behaviour.OverrideMotionType == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Lighter && gestureLighter != null)
                            state.motion = gestureLighter;

                        if (behaviour.OverrideMotionType == SataniaTabakoRequireMotion.OverrideMotion.Gesture_Tabacco && gestureTabacco != null)
                            state.motion = gestureTabacco;
                    }

                    //Override 以外も含め、見つけた behaviour は必ず削除（エディタ用マーカーのため）
                    Object.DestroyImmediate(behaviour);
                }
            }

            foreach (var oHG in overrideHandGestures)
            {
                if (oHG != null)
                    Object.DestroyImmediate(oHG);
            }
        }

        private void Process_FX(BuildContext ctx, IControlledByAnimation[] CBAs)
        {
            AnimationClip cigaretteFiredAnim, cigaretteNonFiredAnim, cigaretteRestoreAnim;
            AnimationClip cigaretteEmissionOFFAnim, cigaretteEmissionONAnim;

            AnimatorController fxController = AvatarUtils.GetAvatarBaseLayer(ctx.VRChatAvatarDescriptor(), VRCAvatarDescriptor.AnimLayerType.FX);
            if (fxController == null)
                throw new System.Exception("FX controller is null.");

            IControlledByAnimation[] contactSizeControllerCBAs = CBAs
                .Where(x => x.Motion == SataniaTabakoRequireMotion.AutomaticMotion.ContactSizeController)
                .ToArray();

            IControlledByAnimation tabakoFireAnimCBA = CBAs
                .Where(x => x.Motion == SataniaTabakoRequireMotion.AutomaticMotion.TabakoFiredAnimation)
                .FirstOrDefault();

            IControlledByAnimation tabakoExhaleEmissionAnimCBA = CBAs
                .Where(x => x.Motion == SataniaTabakoRequireMotion.AutomaticMotion.ExhaleEmission)
                .FirstOrDefault();

            IControlledByAnimation lighterONShapekeyCBA = CBAs
                .Where(x => x.Motion == SataniaTabakoRequireMotion.AutomaticMotion.Additive && x.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.Lighter)
                .FirstOrDefault();

            IControlledByAnimation fireAnimationCBA = CBAs
                .Where(x => x.Motion == SataniaTabakoRequireMotion.AutomaticMotion.Additive && x.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.FireParticle)
                .FirstOrDefault();

            //コンタクトサイズを操作する用のアニメーションクリップを作成
            AnimationClip contactSizeControllerClip = CreateContactSizeControllerClip(ctx, contactSizeControllerCBAs);

            //燃焼アニメーションを作成
            CreateFiredAnimationClip(ctx, tabakoFireAnimCBA,
                out cigaretteFiredAnim, out cigaretteNonFiredAnim, out cigaretteRestoreAnim);

            //先端のアニメーションを作成
            CreateExhaleAnimationClip(ctx, tabakoExhaleEmissionAnimCBA, out cigaretteEmissionOFFAnim, out cigaretteEmissionONAnim);

            (AnimatorState[], SataniaTabakoRequireMotion[][]) requireMotionStates =
                AnimatorControllerUtils.FindStatesAndBehavioursAsArrays<SataniaTabakoRequireMotion>(fxController);

            //CBAが無い場合のInformation通知を一度だけ出すためのフラグ
            bool reportedMissingFireParticleCBA = false;
            bool reportedMissingTabakoFiredCBA = false;
            bool reportedMissingLighterCBA = false;
            bool reportedMissingExhaleEmissionCBA = false;
            bool validatedFireParticleFields = false;

            for (int i = 0; i < requireMotionStates.Item1.Length; i++)
            {
                var state = requireMotionStates.Item1[i];
                var behaviours = requireMotionStates.Item2[i];

                foreach (var behaviour in behaviours)
                {
                    SataniaTabakoRequireMotion.AutomaticMotion MotionID = behaviour.MotionID;

                    if (MotionID == SataniaTabakoRequireMotion.AutomaticMotion.ContactSizeController)
                        state.motion = contactSizeControllerClip;
                    else if (MotionID == SataniaTabakoRequireMotion.AutomaticMotion.TabakoFiredAnimation)
                    {
                        //TabakoFiredAnimation用のCBAが存在しない場合はInformationで通知 (重複は1回に抑制)
                        if (tabakoFireAnimCBA == null && !reportedMissingTabakoFiredCBA)
                        {
                            SatabakoErrorReport.ReportInformation("TabakoFiredAnimation の IControlledByAnimation が見つかりません。タバコの燃焼アニメーションは生成されません。");
                            reportedMissingTabakoFiredCBA = true;
                        }

                        var FiredType = behaviour.FiredType;

                        if (FiredType == SataniaTabakoRequireMotion.FiredAnimationType.Fired)
                            state.motion = cigaretteFiredAnim;
                        else if (FiredType == SataniaTabakoRequireMotion.FiredAnimationType.NonFired)
                            state.motion = cigaretteNonFiredAnim;
                        else if (FiredType == SataniaTabakoRequireMotion.FiredAnimationType.Restore)
                            state.motion = cigaretteRestoreAnim;
                    }
                    else if (MotionID == SataniaTabakoRequireMotion.AutomaticMotion.ExhaleEmission)
                    {
                        //ExhaleEmission用のCBAが存在しない場合はInformationで通知 (重複は1回に抑制)
                        if (tabakoExhaleEmissionAnimCBA == null && !reportedMissingExhaleEmissionCBA)
                        {
                            SatabakoErrorReport.ReportInformation("ExhaleEmission の IControlledByAnimation が見つかりません。吐き出し時の発光アニメーションは生成されません。");
                            reportedMissingExhaleEmissionCBA = true;
                        }

                        var ExhaleAnimation = behaviour.ExhaleAnimation;

                        if (ExhaleAnimation == SataniaTabakoRequireMotion.ExhaleAnimationType.OFF)
                            state.motion = cigaretteEmissionOFFAnim;
                        else if (ExhaleAnimation == SataniaTabakoRequireMotion.ExhaleAnimationType.ON)
                            state.motion = cigaretteEmissionONAnim;
                    }
                    else if (MotionID == SataniaTabakoRequireMotion.AutomaticMotion.Additive)
                    {
                        try
                        {
                            if (state.motion is not AnimationClip)
                                throw new System.Exception("ライターのNon FiredアニメーションにAnimationClip以外のMotionが入っています。");

                            if (lighterONShapekeyCBA != null && behaviour.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.Lighter)
                            {
                                if (lighterONShapekeyCBA.LighterMeshRenderer == null)
                                    throw new System.NullReferenceException("IControlledByAnimationのLighter Mesh Rendererがnullです。");

                                if (behaviour.LighterMotion == SataniaTabakoRequireMotion.LighterMotionType.NonFired)
                                    AddLighterNonFiredKey(ctx.AvatarRootTransform, lighterONShapekeyCBA, state);
                                else if (behaviour.LighterMotion == SataniaTabakoRequireMotion.LighterMotionType.Fired)
                                    AddLighterFiredKey(ctx.AvatarRootTransform, lighterONShapekeyCBA, state);
                            }
                            else if (lighterONShapekeyCBA == null && behaviour.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.Lighter)
                            {
                                //Additive(Lighter)用のCBAが存在しない場合はInformationで通知 (重複は1回に抑制)
                                if (!reportedMissingLighterCBA)
                                {
                                    SatabakoErrorReport.ReportInformation("Additive(Lighter) の IControlledByAnimation が見つかりません。ライターのアニメーションは生成されません。");
                                    reportedMissingLighterCBA = true;
                                }
                            }

                            if (fireAnimationCBA != null && behaviour.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.FireParticle)
                            {
                                //必須フィールドの未設定チェック (CBAごとに1回だけ・NonFatal警告)
                                if (!validatedFireParticleFields)
                                {
                                    validatedFireParticleFields = true;
                                    ValidateFireParticleFields(fireAnimationCBA);
                                }

                                if (behaviour.LighterMotion == SataniaTabakoRequireMotion.LighterMotionType.NonFired)
                                    AddFireParticleNonFiredKey(ctx.AvatarRootTransform, fireAnimationCBA, state);
                                else if (behaviour.LighterMotion == SataniaTabakoRequireMotion.LighterMotionType.Fired)
                                    AddFireParticleFiredKey(ctx.AvatarRootTransform, fireAnimationCBA, state);
                            }
                            else if (fireAnimationCBA == null && behaviour.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.FireParticle)
                            {
                                //Additive(FireParticle)用のCBAが存在しない場合はInformationで通知 (重複は1回に抑制)
                                if (!reportedMissingFireParticleCBA)
                                {
                                    SatabakoErrorReport.ReportInformation("Additive(FireParticle) の IControlledByAnimation が見つかりません。炎パーティクルのアニメーションは生成されません。");
                                    reportedMissingFireParticleCBA = true;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            //NDMFコンソールに警告 (発生元のコンポーネントも表示)
                            SatabakoErrorReport.ReportAsWarning(ex,
                                behaviour.AdditiveMotion == SataniaTabakoRequireMotion.AdditiveMotionTarget.Lighter
                                    ? lighterONShapekeyCBA
                                    : fireAnimationCBA);
                        }
                    }

                    Object.DestroyImmediate(behaviour);
                }
            }

            foreach (var cba in CBAs)
            {
                if (cba != null)
                    Object.DestroyImmediate(cba);
            }

        }

        //FireParticle用CBAの必須フィールドが未設定なら、それぞれNonFatal警告として報告する (ビルドは止めない)
        private void ValidateFireParticleFields(IControlledByAnimation fireAnimationCBA)
        {
            if (fireAnimationCBA.FireParticlePosition == null)
                SatabakoErrorReport.ReportWarning("FireParticle: FireParticlePosition が未設定です。", fireAnimationCBA);
            if (fireAnimationCBA.FireLight == null)
                SatabakoErrorReport.ReportWarning("FireParticle: FireLight が未設定です。", fireAnimationCBA);
            if (fireAnimationCBA.VRCLighter == null)
                SatabakoErrorReport.ReportWarning("FireParticle: VRCLighter が未設定です。", fireAnimationCBA);
            if (fireAnimationCBA.VRC_Fire == null)
                SatabakoErrorReport.ReportWarning("FireParticle: VRC_Fire が未設定です。", fireAnimationCBA);
            if (fireAnimationCBA.FireParticle == null)
                SatabakoErrorReport.ReportWarning("FireParticle: FireParticle が未設定です。", fireAnimationCBA);
            if (fireAnimationCBA.LightSound == null)
                SatabakoErrorReport.ReportWarning("FireParticle: LightSound が未設定です。", fireAnimationCBA);
            if (fireAnimationCBA.LightSpark == null)
                SatabakoErrorReport.ReportWarning("FireParticle: LightSpark が未設定です。", fireAnimationCBA);
        }

        private void AddFireParticleNonFiredKey(Transform AvatarRootTransform, IControlledByAnimation fireAnimationCBA, AnimatorState state)
        {
            if (state.motion == null || state.motion is not AnimationClip)
                return;

            AnimationClip clip = (AnimationClip)state.motion;

            if (fireAnimationCBA.FireParticlePosition)
                AddFireParticlePositionKey(clip, CalcPath(fireAnimationCBA.FireParticlePosition, AvatarRootTransform), fireAnimationCBA.FireParticlePosition);

            if (fireAnimationCBA.FireLight)
            {
                AddLightIntensityKey(clip, CalcPath(fireAnimationCBA.FireLight.transform, AvatarRootTransform), fireAnimationCBA.FireLight);
                AddLightRangeKey(clip, CalcPath(fireAnimationCBA.FireLight.transform, AvatarRootTransform), fireAnimationCBA.FireLight);
            }

            if (fireAnimationCBA.VRCLighter)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.VRCLighter.transform, AvatarRootTransform), false);

            if (fireAnimationCBA.VRC_Fire)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.VRC_Fire.transform, AvatarRootTransform), false);

            if (fireAnimationCBA.FireParticle)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.FireParticle.transform, AvatarRootTransform), false);

            if (fireAnimationCBA.LightSound)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.LightSound.transform, AvatarRootTransform), false);

            if (fireAnimationCBA.LightSpark)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.LightSpark.transform, AvatarRootTransform), false);

            void AddFireParticlePositionKey(AnimationClip clip, string path, Transform FireParticlePositionTransform)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

                string prop_x = string.Format(k_Attribute_LocalPosition, "x");
                string prop_y = string.Format(k_Attribute_LocalPosition, "y");
                string prop_z = string.Format(k_Attribute_LocalPosition, "z");

                var keys_x = CreateOneFrameKeyFrame(0, FireParticlePositionTransform.localPosition.x);
                var keys_y = CreateOneFrameKeyFrame(0, FireParticlePositionTransform.localPosition.y);
                var keys_z = CreateOneFrameKeyFrame(0, FireParticlePositionTransform.localPosition.z);

                bool found = false;

                foreach (var b in bindings)
                {
                    if (b.path == path)
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);

                        if (b.propertyName == prop_x)
                        {
                            found = true;
                            curve.keys = keys_x;
                        }
                        else if (b.propertyName == prop_y)
                        {
                            found = true;
                            curve.keys = keys_y;
                        }
                        else if (b.propertyName == prop_z)
                        {
                            found = true;
                            curve.keys = keys_z;
                        }

                        AnimationUtility.SetEditorCurve(clip, b, curve);
                        continue;
                    }
                }

                //キーがアニメーションに含まれてなかった場合は追加
                if (!found)
                {
                    clip.SetCurve(path, typeof(Transform), prop_x, new AnimationCurve() { keys = keys_x });
                    clip.SetCurve(path, typeof(Transform), prop_y, new AnimationCurve() { keys = keys_y });
                    clip.SetCurve(path, typeof(Transform), prop_z, new AnimationCurve() { keys = keys_z });
                }
            }
            void AddLightIntensityKey(AnimationClip clip, string path, Light light)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                string prop = k_Attribute_Intensity;

                Keyframe[] keys = new Keyframe[] { new Keyframe() { time = 0f, value = light.intensity } };

                bool found = false;

                foreach (var b in bindings)
                {
                    if (b.path == path)
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);

                        if (b.propertyName == prop)
                        {
                            found = true;

                            curve.keys = keys;
                        }

                        AnimationUtility.SetEditorCurve(clip, b, curve);
                        continue;
                    }
                }

                if (!found)
                {
                    clip.SetCurve(path, typeof(Light), prop, new AnimationCurve() { keys = keys });
                }
            }
            void AddLightRangeKey(AnimationClip clip, string path, Light light)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                string prop = k_Attribute_Range;

                Keyframe[] keys = new Keyframe[] { new Keyframe() { time = 0f, value = light.range } };

                bool found = false;

                foreach (var b in bindings)
                {
                    if (b.path == path)
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);

                        if (b.propertyName == prop)
                        {
                            found = true;

                            curve.keys = keys;
                        }

                        AnimationUtility.SetEditorCurve(clip, b, curve);
                        continue;
                    }
                }

                if (!found)
                {
                    clip.SetCurve(path, typeof(Light), prop, new AnimationCurve() { keys = keys });
                }
            }
            void AddIsActiveKeySingleFrame(AnimationClip clip, string path, bool active)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                string prop = k_Attribute_IsActive;

                Keyframe[] keys = CreateOneFrameKeyFrame(0, active ? 1f : 0f);

                bool found = false;

                foreach (var b in bindings)
                {
                    if (b.path == path)
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);

                        if (b.propertyName == prop)
                        {
                            found = true;
                            curve.keys = keys;
                        }

                        AnimationUtility.SetEditorCurve(clip, b, curve);
                        continue;
                    }
                }

                if (!found)
                {
                    clip.SetCurve(path, typeof(GameObject), prop, new AnimationCurve() { keys = keys });
                }
            }

            string CalcPath(Transform target, Transform root) => AnimationUtility.CalculateTransformPath(target, root);
        }

        private void AddFireParticleFiredKey(Transform AvatarRootTransform, IControlledByAnimation fireAnimationCBA, AnimatorState state)
        {
            if (state.motion == null || state.motion is not AnimationClip)
                return;

            AnimationClip clip = (AnimationClip)state.motion;

            //var FireParticlePositionTransform = fireAnimationCBA.FireParticlePosition;
            //var Light = fireAnimationCBA.FireLight;
            //var VRCLighter = fireAnimationCBA.VRCLighter;
            //var VRC_Fire = fireAnimationCBA.VRC_Fire;
            //var FireParticleGO = fireAnimationCBA.FireParticle;
            //var LightSound = fireAnimationCBA.LightSound;
            //var LightSpark = fireAnimationCBA.LightSpark;

            var fireParticlePositionPairs = fireAnimationCBA.FireParticlePositionPairs;
            var lightIntensityPairs = fireAnimationCBA.LightIntensityPairs;
            var lightRangePairs = fireAnimationCBA.LightRangePairs;

            if (fireAnimationCBA.FireParticlePosition)
                AddFireParticlePositionKey(clip, CalcPath(fireAnimationCBA.FireParticlePosition, AvatarRootTransform), fireAnimationCBA.FireParticlePosition, fireParticlePositionPairs);

            if (fireAnimationCBA.FireLight)
            {
                AddLightIntensityKey(clip, CalcPath(fireAnimationCBA.FireLight.transform, AvatarRootTransform), fireAnimationCBA.FireLight, lightIntensityPairs);
                AddLightRangeKey(clip, CalcPath(fireAnimationCBA.FireLight.transform, AvatarRootTransform), fireAnimationCBA.FireLight);
            }

            if (fireAnimationCBA.VRCLighter)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.VRCLighter.transform, AvatarRootTransform), true);

            if (fireAnimationCBA.VRC_Fire)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.VRC_Fire.transform, AvatarRootTransform), true);

            if (fireAnimationCBA.FireParticle)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.FireParticle.transform, AvatarRootTransform), true);

            if (fireAnimationCBA.LightSound)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.LightSound.transform, AvatarRootTransform), true);

            if (fireAnimationCBA.LightSpark)
                AddIsActiveKeySingleFrame(clip, CalcPath(fireAnimationCBA.LightSpark.transform, AvatarRootTransform), true);

            void AddFireParticlePositionKey(AnimationClip clip, string path, Transform FireParticlePositionTransform, KeyFramePairVector3[] pairs)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

                string prop_x = string.Format(k_Attribute_LocalPosition, "x");
                string prop_y = string.Format(k_Attribute_LocalPosition, "y");
                string prop_z = string.Format(k_Attribute_LocalPosition, "z");

                (Keyframe[] x, Keyframe[] y, Keyframe[] z) keys = EditorUtils.GetKeys(pairs);

                //元のポジションを足す
                keys.x = keys.x.Select(x =>
                {
                    x.value += FireParticlePositionTransform.localPosition.x;
                    return x;
                }).ToArray();

                keys.y = keys.y.Select(y =>
                {
                    y.value += FireParticlePositionTransform.localPosition.y;
                    return y;
                }).ToArray();

                keys.z = keys.z.Select(z =>
                {
                    z.value += FireParticlePositionTransform.localPosition.z;
                    return z;
                }).ToArray();

                bool found = false;

                foreach (var b in bindings)
                {
                    if (b.path == path)
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);

                        if (b.propertyName == prop_x)
                        {
                            found = true;
                            curve.keys = keys.x;
                        }
                        else if (b.propertyName == prop_y)
                        {
                            found = true;
                            curve.keys = keys.y;
                        }
                        else if (b.propertyName == prop_z)
                        {
                            found = true;
                            curve.keys = keys.z;
                        }

                        AnimationUtility.SetEditorCurve(clip, b, curve);
                        continue;
                    }
                }

                //キーがアニメーションに含まれてなかった場合は追加
                if (!found)
                {
                    clip.SetCurve(path, typeof(Transform), prop_x, new AnimationCurve() { keys = keys.x });
                    clip.SetCurve(path, typeof(Transform), prop_y, new AnimationCurve() { keys = keys.y });
                    clip.SetCurve(path, typeof(Transform), prop_z, new AnimationCurve() { keys = keys.z });
                }
            }
            void AddLightIntensityKey(AnimationClip clip, string path, Light light, KeyFramePairFloat[] pairs)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                string prop = k_Attribute_Intensity;

                Keyframe[] keys = EditorUtils.GetKeys(pairs);
                keys = keys.Select(x =>
                {
                    x.value += light.intensity;
                    return x;
                }).ToArray();

                bool found = false;

                foreach (var b in bindings)
                {
                    if (b.path == path)
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);

                        if (b.propertyName == prop)
                        {
                            found = true;
                            curve.keys = keys;
                        }

                        AnimationUtility.SetEditorCurve(clip, b, curve);
                        continue;
                    }
                }

                if (!found)
                {
                    clip.SetCurve(path, typeof(Light), prop, new AnimationCurve() { keys = keys });
                }
            }
            void AddLightRangeKey(AnimationClip clip, string path, Light light)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                string prop = k_Attribute_Range;

                Keyframe[] keys = EditorUtils.GetKeys(lightRangePairs);
                keys = keys.Select(x =>
                {
                    x.value += light.range;
                    return x;
                }).ToArray();

                bool found = false;

                foreach (var b in bindings)
                {
                    if (b.path == path)
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);

                        if (b.propertyName == prop)
                        {
                            found = true;

                            curve.keys = keys;
                        }

                        AnimationUtility.SetEditorCurve(clip, b, curve);
                        continue;
                    }
                }

                if (!found)
                {
                    clip.SetCurve(path, typeof(Light), prop, new AnimationCurve() { keys = keys });
                }
            }
            void AddIsActiveKeySingleFrame(AnimationClip clip, string path, bool active)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                string prop = k_Attribute_IsActive;

                Keyframe[] keys = CreateOneFrameKeyFrame(0, active ? 1f : 0f);

                bool found = false;

                foreach (var b in bindings)
                {
                    if (b.path == path)
                    {
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);

                        if (b.propertyName == prop)
                        {
                            found = true;
                            curve.keys = keys;
                        }

                        AnimationUtility.SetEditorCurve(clip, b, curve);
                        continue;
                    }
                }

                if (!found)
                {
                    clip.SetCurve(path, typeof(GameObject), prop, new AnimationCurve() { keys = keys });
                }
            }

            string CalcPath(Transform target, Transform root) => AnimationUtility.CalculateTransformPath(target, root);
        }

        protected override void Execute(BuildContext ctx)
        {
            m_minContactMultipilier = PluginDefinition.Singleton.instance.MinContactSize;
            m_maxContactMultipilier = PluginDefinition.Singleton.instance.MaxContactSize;
            tabakoScriptBehaviour = PluginDefinition.Singleton.instance.tabakoScriptBehaviour;
            if (tabakoScriptBehaviour == null)
                return;

            m_maxFiredAnimationLength = tabakoScriptBehaviour.FiredAnimationLength;

            IControlledByAnimation[] controlledByAnimations = ctx.AvatarRootObject
                .GetComponentsInChildren<IControlledByAnimation>(true)
                .Where(x => x.Motion != SataniaTabakoRequireMotion.AutomaticMotion.None)
                .ToArray();

            IOverrideHandGesture[] overrideHandGestures = ctx.AvatarRootObject.GetComponentsInChildren<IOverrideHandGesture>(true);

            try
            {
                Process_FX(ctx, controlledByAnimations);
                Process_Gesture(ctx, overrideHandGestures);
            }
            catch (System.Exception ex)
            {
                //NDMFコンソールに警告 (発生元のTabako Scriptも表示)
                SatabakoErrorReport.ReportAsWarning(ex, tabakoScriptBehaviour);
            }
        }
    }
}
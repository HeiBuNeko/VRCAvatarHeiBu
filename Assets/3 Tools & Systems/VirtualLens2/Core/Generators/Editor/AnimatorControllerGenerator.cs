#if VL2_DEVELOPMENT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using AnimatorAsCode.V0;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VirtualLens2.AV3EditorLib;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace VirtualLens2.Generators
{
    public class AnimatorControllerGenerator
    {
        [MenuItem("Window/Logilabo/VirtualLens2/Generate Animator Controller")]
        static void Generate()
        {
            var root = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .First(o => o.name == "Skeleton");
            var descriptor = root.GetComponent<VRCAvatarDescriptor>();
            var instance = new AnimatorControllerGenerator(descriptor);
            instance.Run();
        }

        private class MyAacDefaultsProvider : AacDefaultsProvider
        {
            public override string ConvertLayerNameWithSuffix(string systemName, string suffix)
            {
                return $"{systemName} {suffix}";
            }
        }

        private static readonly string ParameterPrefix = Constants.ParameterPrefix;
        private static readonly int ControlMenuStart  = 100;
        private static readonly int ControlDroneMode  = 191;
        private static readonly int ControlStateStart = 192;

        private static readonly int NumPins = Constants.NumPins;
        private static readonly int NumQuickCalls = Constants.NumQuickCalls;

        private readonly GameObject _avatar;
        private readonly AacFlBase _aac;

        private readonly List<Motion> _localBlendTreeChildren;
        private readonly List<Motion> _remoteBlendTreeChildren;

        private AnimatorControllerGenerator(VRCAvatarDescriptor avatar)
        {
            var avatarTransform = avatar.transform;
            var controller = (AnimatorController) avatar.baseAnimationLayers[4].animatorController; // FX layer

            AnimatorControllerEditor.RemoveLayers(controller, new Regex(@"^VirtualLens2 .*$"), null);
            AnimatorControllerEditor.RemoveParameters(controller, new Regex("@^VirtualLens2 .*$"));

            var baseStateMachine = controller.layers[0].stateMachine;
            AssetUtility.RemoveSubAssets(controller, o => o != baseStateMachine);

            var aac = AacV0.Create(new AacConfiguration
            {
                SystemName = "VirtualLens2",
                AvatarDescriptor = avatar,
                AnimatorRoot = avatarTransform,
                DefaultValueRoot = avatarTransform,
                AssetContainer = controller,
                AssetKey = "VirtualLens2",
                DefaultsProvider = new MyAacDefaultsProvider()
            });

            aac.RemoveAllMainLayers();
            aac.RemoveAllSupportingLayers("");
            aac.ClearPreviousAssets();

            _avatar = avatar.gameObject;
            _aac = aac;

            _localBlendTreeChildren = new List<Motion>();
            _remoteBlendTreeChildren = new List<Motion>();
        }

        private void Run()
        {
            CreateInitializationLayer();
            CreateMirrorDetectorLayer();

            CreateDeltaTimeHelperLayer(false);
            CreateDeltaTimeHelperLayer(true);

            CreateMenuLayer();
            CreateAPILayer();
            CreatePositionControlLayer();
            CreateQuickCallLayer();
            CreateAltMeshLayer();
            CreateMultiplexerLayer();
            CreateI2FLayer();

            CreateZoomLayer();

            CreatePoseControlLayer();
            CreateStabilizerLayer();
            CreateStabilizerLimiterLayer();
            CreateDropLayer();
            CreateStorePinLayer();
            CreateDroneQuickTurnLayer();
            CreateResolutionLayer();

            CreateBlendTreeLayer();

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        #region Utility Functions

        private AacFlLayer NewLayer(string suffix)
        {
            // VirtualLens2/Core/AvatarMasks/VirtualLens2_DefaultMask.mask
            var mask = AssetUtility.LoadAssetByGUID<AvatarMask>("a051bbb9cae570d4093db925c409f187");
            return _aac.CreateSupportingFxLayer(suffix).WithAvatarMask(mask);
        }

        private GameObject PlaceholderObject(string name)
        {
            return HierarchyUtility.PathToObject(_avatar, $"__AV3EL_TEMPLATE__/{name}");
        }

        private GameObject LocalRoot => HierarchyUtility.PathToObject(_avatar, "_VirtualLens_Root/Local");

        private MeshRenderer DisplayRenderer => HierarchyUtility
            .PathToObject(LocalRoot, "Compute/DisplayRenderer/Quad")
            .GetComponent<MeshRenderer>();
        
        private string ConstraintWeightProp<T>(T component, int i) where T : VRCConstraintBase
        {
            return $"Sources.source{i}.Weight";
        }

        #endregion

        #region Initialization

        private void CreateInitializationLayer()
        {
            var layer = NewLayer("Initialize");
            var initializedParam = layer.IntParameter(ParameterPrefix + "Initialized");
            var oneParam = layer.FloatParameter(ParameterPrefix + "One");
            var deltaSmoothnessParam = layer.FloatParameter(ParameterPrefix + "DeltaSmoothness");

            // Register parameters to expose configuration values
            layer.IntParameter(ParameterPrefix + "Version");
            layer.FloatParameter(ParameterPrefix + "Zoom Min");
            layer.FloatParameter(ParameterPrefix + "Zoom Max");
            layer.FloatParameter(ParameterPrefix + "Aperture Min");
            layer.FloatParameter(ParameterPrefix + "Aperture Max");
            layer.FloatParameter(ParameterPrefix + "Exposure Range");

            var init = layer.NewState("Init")
                .Drives(initializedParam, 1)
                .Drives(oneParam, 1)
                .Drives(deltaSmoothnessParam, 0.8f);
            var done = layer.NewState("Done");

            // https://feedback.vrchat.com/avatar-30/p/bug-vrc-avatar-parameter-driver-does-not-work-for-a-few-frames-after-avatar-load
            init.TransitionsTo(init)
                .WithTransitionToSelf()
                .When(initializedParam.IsEqualTo(0));
            init.TransitionsTo(done)
                .When(initializedParam.IsNotEqualTo(0));
        }

        private void CreateMirrorDetectorLayer()
        {
            // MirrorDetector + Flip
            var layer = NewLayer("MirrorDetector");
            var initializedParam = layer.IntParameter(ParameterPrefix + "Initialized");
            var detectorParam = layer.IntParameter(ParameterPrefix + "MirrorDetector");
            var flipParam = layer.FloatParameter(ParameterPrefix + "Flip");
            var falseParam = layer.BoolParameter(ParameterPrefix + "False");

            var paths = new[]
            {
                "Writer/DepthCleaners/0",
                "Writer/DepthCleaners/1",
                "Writer/DepthCleaners/2",
                "Writer/DepthCleaners/3",
                "Writer/Renderer",
            };
            var renderers = paths
                .Select(s => HierarchyUtility.PathToObject(LocalRoot, s).GetComponent(typeof(MeshRenderer)))
                .ToArray();

            var localVRClip0 = _aac.NewClip()
                .TogglingComponent(renderers, false)
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(flipParam).WithOneFrame(0.0f);
                    clip.Animates(renderers, "material._IsDesktopMode").WithOneFrame(0.0f);
                });
            var localVRClip1 = _aac.NewClip()
                .TogglingComponent(renderers, false)
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(flipParam).WithOneFrame(1.0f);
                    clip.Animates(renderers, "material._IsDesktopMode").WithOneFrame(0.0f);
                });
            var localDesktopClip0 = _aac.NewClip()
                .TogglingComponent(renderers, true)
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(flipParam).WithOneFrame(0.0f);
                    clip.Animates(renderers, "material._IsDesktopMode").WithOneFrame(1.0f);
                });
            var localDesktopClip1 = _aac.NewClip()
                .TogglingComponent(renderers, true)
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(flipParam).WithOneFrame(1.0f);
                    clip.Animates(renderers, "material._IsDesktopMode").WithOneFrame(1.0f);
                });
            var reflectionClip0 = _aac.NewClip()
                .TogglingComponent(renderers, true)
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(flipParam).WithOneFrame(0.0f);
                    clip.Animates(renderers, "material._IsDesktopMode").WithOneFrame(0.0f);
                });
            var reflectionClip1 = _aac.NewClip()
                .TogglingComponent(renderers, true)
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(flipParam).WithOneFrame(1.0f);
                    clip.Animates(renderers, "material._IsDesktopMode").WithOneFrame(0.0f);
                });

            var remote = layer.NewState("Remote");
            var fork = layer.NewState("Fork");
            var localVR = layer.NewState("PlayerLocal VR")
                .DrivingLocally()
                .Drives(detectorParam, 1)
                .WithAnimation(localVRClip1);
            var localVR0 = layer.NewState("PlayerLocal VR 0")
                .WithAnimation(localVRClip0);
            var localVR1 = layer.NewState("PlayerLocal VR 1")
                .WithAnimation(localVRClip1);
            var localDesktop = layer.NewState("PlayerLocal Desktop")
                .DrivingLocally()
                .Drives(detectorParam, 1)
                .WithAnimation(localDesktopClip1);
            var localDesktop0 = layer.NewState("PlayerLocal Desktop 0")
                .WithAnimation(localDesktopClip0);
            var localDesktop1 = layer.NewState("PlayerLocal Desktop 1")
                .WithAnimation(localDesktopClip1);
            var reflection = layer.NewState("MirrorReflection")
                .WithAnimation(reflectionClip1);
            var reflection0 = layer.NewState("MirrorReflection 0")
                .WithAnimation(reflectionClip0);
            var reflection1 = layer.NewState("MirrorReflection 1")
                .WithAnimation(reflectionClip1);

            remote.TransitionsTo(fork)
                .When(layer.Av3().ItIsLocal())
                .And(initializedParam.IsNotEqualTo(0));

            // https://github.com/VRLabs/Local-Mirror-Detection
            fork.TransitionsTo(localVR)
                .When(initializedParam.IsNotEqualTo(0))
                .And(detectorParam.IsEqualTo(0))
                .And(layer.Av3().VRMode.IsNotEqualTo(0));
            fork.TransitionsTo(localVR)
                .When(initializedParam.IsNotEqualTo(0))
                .And(detectorParam.IsEqualTo(0))
                .And(layer.Av3().TrackingType.IsEqualTo(0));
            fork.TransitionsTo(localDesktop)
                .When(initializedParam.IsNotEqualTo(0))
                .And(detectorParam.IsEqualTo(0))
                .And(layer.Av3().VRMode.IsEqualTo(0))
                .And(layer.Av3().TrackingType.IsNotEqualTo(0));
            fork.TransitionsTo(reflection)
                .When(initializedParam.IsNotEqualTo(0))
                .And(detectorParam.IsNotEqualTo(0));

            // Flip
            localVR.TransitionsTo(localVR0).When(falseParam.IsFalse());
            localVR0.TransitionsTo(localVR1).When(falseParam.IsFalse());
            localVR1.TransitionsTo(localVR0).When(falseParam.IsFalse());

            localDesktop.TransitionsTo(localDesktop0).When(falseParam.IsFalse());
            localDesktop0.TransitionsTo(localDesktop1).When(falseParam.IsFalse());
            localDesktop1.TransitionsTo(localDesktop0).When(falseParam.IsFalse());

            reflection.TransitionsTo(reflection0).When(falseParam.IsFalse());
            reflection0.TransitionsTo(reflection1).When(falseParam.IsFalse());
            reflection1.TransitionsTo(reflection0).When(falseParam.IsFalse());
        }

        #endregion

        #region Utility Parameters

        private void CreateDeltaTimeHelperLayer(bool flip)
        {
            var layer  = NewLayer("DeltaTime" + (flip ? 1 : 0));
            var flipParam = layer.FloatParameter(ParameterPrefix + "Flip");
            var deltaParam = layer.FloatParameter(ParameterPrefix + "Delta" + (flip ? 1 : 0));

            var remote = layer.NewState("Remote");
            var local = layer.NewState("Local")
                .WithAnimation(_aac.NewClip().Animating(clip =>
                {
                    clip.AnimatesAnimator(deltaParam)
                        .WithSecondsUnit(keyframes => keyframes
                            .Linear(0, 0.0f)
                            .Linear(1, 1.0f));
                }));

            // Workaround for lazy built-in parameter initialization
            remote.TransitionsTo(local).When(layer.Av3().ItIsLocal());

            // Reset counter
            local.TransitionsTo(local)
                .WithTransitionToSelf()
                .When(flip ? flipParam.IsGreaterThan(0.5f) : flipParam.IsLessThan(0.5f));
        }

        private void CreateDeltaTimeSmoothingLayer(AacFlLayer layer)
        {
            var deltaSmoothnessParam = layer.FloatParameter(ParameterPrefix + "DeltaSmoothness");
            var flipParam = layer.FloatParameter(ParameterPrefix + "Flip");
            var delta0Param = layer.FloatParameter(ParameterPrefix + "Delta0");
            var delta1Param = layer.FloatParameter(ParameterPrefix + "Delta1");
            var deltaParam = layer.FloatParameter(ParameterPrefix + "Delta");

            var zero = _aac.NewClip()
                .Animating(clip => clip.AnimatesAnimator(deltaParam).WithOneFrame(0.0f));
            var one = _aac.NewClip()
                .Animating(clip => clip.AnimatesAnimator(deltaParam).WithOneFrame(1.0f));

            var smoother = new BlendTreeEditor(_aac, deltaParam)
                .AddChild(0.0f, zero)
                .AddChild(1.0f, one);
            var updater = new BlendTreeEditor(_aac, flipParam)
                .AddChild(0.0f, new BlendTreeEditor(_aac, delta0Param)
                    .AddChild(0.0f, zero)
                    .AddChild(1.0f, one))
                .AddChild(1.0f, new BlendTreeEditor(_aac, delta1Param)
                    .AddChild(0.0f, zero)
                    .AddChild(1.0f, one));

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, deltaSmoothnessParam)
                .AddChild(0.0f, updater)
                .AddChild(1.0f, smoother)
                .Motion);
        }

        #endregion

        #region Parameter Control

        private class MenuLayerItem
        {
            public MenuTrigger Trigger;
            public string Target;
            public int Value;

            public MenuLayerItem(MenuTrigger trigger, string target, int value)
            {
                Trigger = trigger;
                Target = target;
                Value = value;
            }
        }

        private void CreateMenuLayer()
        {
            var layer = NewLayer("Menu");
            var falseParam = layer.BoolParameter(ParameterPrefix + "False");
            var controlParam = layer.IntParameter(ParameterPrefix + "Control");

            var remote = layer.NewState("Remote");
            var root = layer.NewState("Local");

            // Workaround for lazy built-in parameter initialization
            remote.TransitionsTo(root).When(layer.Av3().ItIsLocal());

            // Early exit
            root.TransitionsTo(root)
                .WithTransitionToSelf()
                .When(controlParam.IsLessThan(ControlMenuStart))
                .Or()
                .When(controlParam.IsGreaterThan(ControlStateStart - 1));

            // Create menu items
            void CreateMenuItems(MenuLayerItem[] items)
            {
                var states = new AacFlState[items.Length];
                for (var i = 0; i < items.Length; ++i)
                {
                    var item = items[i];
                    var name = item.Trigger.ToString();
                    var state = layer.NewState(name);
                    root.TransitionsTo(state).When(controlParam.IsEqualTo((int) item.Trigger));
                    var targetParam = layer.IntParameter(ParameterPrefix + item.Target);
                    if (items[i].Value < 0)
                    {
                        // Toggle
                        var disabler = layer.NewState(name + "D")
                            .DrivingLocally()
                            .Drives(targetParam, 0);
                        var enabler = layer.NewState(name + "E")
                            .DrivingLocally()
                            .Drives(targetParam, 1);
                        state.TransitionsTo(disabler)
                            .When(controlParam.IsEqualTo(0))
                            .And(targetParam.IsNotEqualTo(0));
                        state.TransitionsTo(enabler)
                            .When(controlParam.IsEqualTo(0))
                            .And(targetParam.IsEqualTo(0));
                        disabler.TransitionsTo(root).When(falseParam.IsFalse());
                        enabler.TransitionsTo(root).When(falseParam.IsFalse());
                    }
                    else
                    {
                        // Drive
                        var driver = layer.NewState(name + "D")
                            .DrivingLocally()
                            .Drives(targetParam, item.Value);
                        state.TransitionsTo(driver).When(controlParam.IsEqualTo(0));
                        driver.TransitionsTo(root).When(falseParam.IsFalse());
                    }
                    states[i] = state;
                }
                for (var i = 0; i < items.Length; ++i)
                {
                    for (var j = 0; j < items.Length; ++j)
                    {
                        if (i == j) { continue; }
                        states[i].TransitionsTo(states[j]).When(controlParam.IsEqualTo((int) items[j].Trigger));
                    }
                }
            }

            // Root
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.Enable, "Enable", -1),
            });
            // Transform Control
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.Pickup, "PositionControl", 1), // Pickup
                new MenuLayerItem(MenuTrigger.Reposition, "PositionControl", 2), // Drop/Reposition
            });
            // Transform Control / Auto Leveler
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.AutoLevelerDisable, "AutoLeveler", 0), // Disable
                new MenuLayerItem(MenuTrigger.AutoLevelerHorizontal, "AutoLeveler", 1), // Horizontal
                new MenuLayerItem(MenuTrigger.AutoLevelerVertical, "AutoLeveler", 2), // Vertical
                new MenuLayerItem(MenuTrigger.AutoLevelerAuto, "AutoLeveler", 3), // Auto
            });
            // Transform Control / More / Stabilizer
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.StabilizerDisable, "Stabilizer", 0), // Disable
                new MenuLayerItem(MenuTrigger.StabilizerWeak, "Stabilizer", 1), // Weak
                new MenuLayerItem(MenuTrigger.StabilizerMedium, "Stabilizer", 2), // Medium
                new MenuLayerItem(MenuTrigger.StabilizerStrong, "Stabilizer", 3), // Strong
            });
            // Transform Control / More / Pins
            {
                // Hold checker: 1.0 seconds
                var holdClip = _aac.NewClip().Animating(clip =>
                    clip.Animates("_ignored", typeof(GameObject), "m_IsActive")
                        .WithFixedSeconds(1.0f, 0));
                var loadParam = layer.IntParameter(ParameterPrefix + "LoadPin");
                var storeParam = layer.IntParameter(ParameterPrefix + "StorePin");
                int offset = (int) MenuTrigger.Pin0;
                var states = new AacFlState[NumPins];
                var holdStates = new AacFlState[NumPins];
                for (var i = 0; i < NumPins; ++i)
                {
                    var trigger = offset + i;
                    var name = trigger.ToString();
                    var state = layer.NewState(name).WithAnimation(holdClip);
                    var holdState = layer.NewState(name + "H");
                    var loadState = layer.NewState(name + "L")
                        .DrivingLocally()
                        .Drives(loadParam, i + 1);
                    var storeState = layer.NewState(name + "S")
                        .DrivingLocally()
                        .Drives(storeParam, i + 1);
                    root.TransitionsTo(state).When(controlParam.IsEqualTo(trigger));
                    state.TransitionsTo(holdState).AfterAnimationFinishes();
                    state.TransitionsTo(loadState).When(controlParam.IsEqualTo(0));
                    holdState.TransitionsTo(storeState).When(controlParam.IsEqualTo(0));
                    loadState.TransitionsTo(root).When(falseParam.IsFalse());
                    storeState.TransitionsTo(root).When(falseParam.IsFalse());
                    states[i] = state;
                    holdStates[i] = holdState;
                }
                for (var i = 0; i < NumPins; ++i)
                {
                    for (var j = 0; j < NumPins; ++j)
                    {
                        if (i == j) { continue; }
                        var trigger = offset + j;
                        states[i].TransitionsTo(states[j]).When(controlParam.IsEqualTo(trigger));
                        holdStates[i].TransitionsTo(states[j]).When(controlParam.IsEqualTo(trigger));
                    }
                }
            }
            // Transform Control / More / Reposition Scale
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.RepositionScale1X, "RepositionScale", 0), // 1x
                new MenuLayerItem(MenuTrigger.RepositionScale3X, "RepositionScale", 1), // 3x
                new MenuLayerItem(MenuTrigger.RepositionScale10X, "RepositionScale", 2), // 10x
                new MenuLayerItem(MenuTrigger.RepositionScale30X, "RepositionScale", 3), // 30x
            });
            // Image Control / AF Mode
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.PointAutoFocus, "AFMode", 0), // Point AF
                new MenuLayerItem(MenuTrigger.FaceAutoFocus, "AFMode", 1), // Face AF
                new MenuLayerItem(MenuTrigger.SelfieAutoFocus, "AFMode", 2), // Selfie AF
            });
            // Image Control / Tracking Speed
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.TrackingSpeedImmediate, "TrackingSpeed", 0), // Immediate
                new MenuLayerItem(MenuTrigger.TrackingSpeedFast, "TrackingSpeed", 1), // Fast
                new MenuLayerItem(MenuTrigger.TrackingSpeedMedium, "TrackingSpeed", 2), // Medium
                new MenuLayerItem(MenuTrigger.TrackingSpeedSlow, "TrackingSpeed", 3), // Slow
            });
            // Image Control / Focusing Speed
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.FocusingSpeedImmediate, "AFSpeed", 0), // Immediate
                new MenuLayerItem(MenuTrigger.FocusingSpeedFast, "AFSpeed", 1), // Fast
                new MenuLayerItem(MenuTrigger.FocusingSpeedMedium, "AFSpeed", 2), // Medium
                new MenuLayerItem(MenuTrigger.FocusingSpeedSlow, "AFSpeed", 3), // Slow
            });
            // Advanced Settings
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.MeshVisibility, "Hide", -1), // Mesh Visibility
            });
            // Advanced Settings / Display Settings
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.DisplayInformation, "Information", -1), // Information
                new MenuLayerItem(MenuTrigger.DisplayLeveler, "Level", -1), // Leveler
            });
            // Advanced Settings / Display Settings / Grid
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.GridNone, "Grid", 0), // None
                new MenuLayerItem(MenuTrigger.Grid3X3, "Grid", 1), // 3x3
                new MenuLayerItem(MenuTrigger.Grid3X3Diag, "Grid", 2), // 3x3+Diagonal
                new MenuLayerItem(MenuTrigger.Grid6X4, "Grid", 3), // 6x4
                new MenuLayerItem(MenuTrigger.GridCustom0, "Grid", 4), // Custom0
                new MenuLayerItem(MenuTrigger.GridCustom1, "Grid", 5), // Custom1
                new MenuLayerItem(MenuTrigger.GridCustom2, "Grid", 6), // Custom2
                new MenuLayerItem(MenuTrigger.GridCustom3, "Grid", 7), // Custom3
            });
            // Advanced Settings / Display Settings / Peaking
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.PeakingNone, "Peaking", 0), // None
                new MenuLayerItem(MenuTrigger.PeakingManualOnly, "Peaking", 1), // MF only
                new MenuLayerItem(MenuTrigger.PeakingAlways, "Peaking", 2), // Always
            });
            // Advanced Settings / Far Plane
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.FarPlaneDefault, "FarPlane", 0), // Default
                new MenuLayerItem(MenuTrigger.FarPlane10X, "FarPlane", 1), // 10x
                new MenuLayerItem(MenuTrigger.FarPlane100X, "FarPlane", 2), // 100x
            });
            // Advanced Settings / Depth Enabler
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.DepthEnablerDisable, "DepthEnabler", 0), // Disable
                new MenuLayerItem(MenuTrigger.DepthEnablerEnable, "DepthEnabler", 1), // Enable
            });
            // Advanced Settings / Depth Cleaner
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.DepthCleanerDisable, "DepthCleaner", 0), // Disable
                new MenuLayerItem(MenuTrigger.DepthCleanerNearest, "DepthCleaner", 1), // Nearest
                new MenuLayerItem(MenuTrigger.DepthCleanerFarthest, "DepthCleaner", 2), // Farthest
            });
            // Advanced Settings / Resolution
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.Resolution1080P, "Resolution", 0), // 1080p (FHD)
                new MenuLayerItem(MenuTrigger.Resolution1440P, "Resolution", 1), // 1440p (WQHD)
                new MenuLayerItem(MenuTrigger.Resolution2160P, "Resolution", 2), // 2160p (4K)
                new MenuLayerItem(MenuTrigger.Resolution4320P, "Resolution", 3), // 4320p (8K)
            });
            // Quick Calls
            CreateMenuItems(new[]
            {
                new MenuLayerItem(MenuTrigger.QuickCall0, "QuickCall", 1),
                new MenuLayerItem(MenuTrigger.QuickCall1, "QuickCall", 2),
                new MenuLayerItem(MenuTrigger.QuickCall2, "QuickCall", 3),
                new MenuLayerItem(MenuTrigger.QuickCall3, "QuickCall", 4),
                new MenuLayerItem(MenuTrigger.QuickCall4, "QuickCall", 5),
                new MenuLayerItem(MenuTrigger.QuickCall5, "QuickCall", 6),
                new MenuLayerItem(MenuTrigger.QuickCall6, "QuickCall", 7),
                new MenuLayerItem(MenuTrigger.QuickCall7, "QuickCall", 8),
            });
        }

        private void CreateAPILayer()
        {
            var layer = NewLayer("API");
            var falseParam = layer.BoolParameter(ParameterPrefix + "False");
            var controlParam = layer.IntParameter(ParameterPrefix + "Control");

            var remote = layer.NewState("Remote");
            var root = layer.NewState("Local");

            // Workaround for lazy built-in parameter initialization
            remote.TransitionsTo(root).When(layer.Av3().ItIsLocal());

            // Early exit
            root.TransitionsTo(root)
                .WithTransitionToSelf()
                .When(controlParam.IsEqualTo(0))
                .Or()
                .When(controlParam.IsGreaterThan(ControlMenuStart - 1));

            // API definitions
            void CreateAPIState(int index, string key, int value)
            {
                var state = layer.NewState("API " + index)
                    .Drives(layer.IntParameter(ParameterPrefix + key), value)
                    .Drives(controlParam, 0);
                root.TransitionsTo(state).When(controlParam.IsEqualTo(index));
                state.TransitionsTo(root).When(falseParam.IsFalse());
            }

            // Enable/Disable
            CreateAPIState(2, "Enable", 0);  // Disable
            CreateAPIState(3, "Enable", 1);  // Enable
            // Auto leveler
            CreateAPIState(4, "AutoLeveler", 0);  // Disable
            CreateAPIState(5, "AutoLeveler", 1);  // Horizontal
            CreateAPIState(6, "AutoLeveler", 2);  // Vertical
            CreateAPIState(7, "AutoLeveler", 3);  // Auto
            // Stabilizer
            CreateAPIState(8, "Stabilizer", 0);  // Disable
            CreateAPIState(9, "Stabilizer", 1);  // Weak
            CreateAPIState(10, "Stabilizer", 2);  // Medium
            CreateAPIState(11, "Stabilizer", 3);  // Strong
            // Pickup / Drop / Reposition
            CreateAPIState(12, "PositionControl", 1); // Pickup
            CreateAPIState(13, "PositionControl", 3); // Drop
            CreateAPIState(14, "PositionControl", 4); // Reposition
            // Reposition Scale
            CreateAPIState(16, "RepositionScale", 0);  // 1x
            CreateAPIState(17, "RepositionScale", 1);  // 3x
            CreateAPIState(18, "RepositionScale", 2);  // 10x
            CreateAPIState(19, "RepositionScale", 3);  // 30x
            // Pins
            for (int i = 0; i < NumPins; ++i)
            {
                CreateAPIState(20 + i, "StorePin", i + 1); // Store i
                CreateAPIState(24 + i, "LoadPin", i + 1);  // Load i
            }
            // External Pose
            CreateAPIState(28, "ExternalPose", 0);  // Disable
            CreateAPIState(29, "ExternalPose", 1);  // Enable
            // Focus Lock
            CreateAPIState(30, "FocusLock", 0);  // Disable
            CreateAPIState(31, "FocusLock", 1);  // Enable
            // AF Mode
            CreateAPIState(32, "AFMode", 0);  // Point AF
            CreateAPIState(33, "AFMode", 1);  // Face AF
            CreateAPIState(34, "AFMode", 2);  // Selfie AF
            // Tracking Speed
            CreateAPIState(36, "TrackingSpeed", 0);  // Immediate
            CreateAPIState(37, "TrackingSpeed", 1);  // Fast
            CreateAPIState(38, "TrackingSpeed", 2);  // Intermediate
            CreateAPIState(39, "TrackingSpeed", 3);  // Slow
            // Focusing Speed
            CreateAPIState(40, "AFSpeed", 0);  // Immediate
            CreateAPIState(41, "AFSpeed", 1);  // Fast
            CreateAPIState(42, "AFSpeed", 2);  // Intermediate
            CreateAPIState(43, "AFSpeed", 3);  // Slow
            // Mesh Visibility
            CreateAPIState(44, "Hide", 0);  // Show
            CreateAPIState(45, "Hide", 1);  // Hide
            // Grid
            CreateAPIState(46, "Grid", 0);  // None
            CreateAPIState(47, "Grid", 1);  // 3x3
            CreateAPIState(48, "Grid", 2);  // 3x3+Diagonal
            CreateAPIState(49, "Grid", 3);  // 6x4
            // Information
            CreateAPIState(52, "Information", 0);  // Hide
            CreateAPIState(53, "Information", 1);  // Show
            // Leveler
            CreateAPIState(54, "Level", 0);  // Hide
            CreateAPIState(55, "Level", 1);  // Show
            // Peaking
            CreateAPIState(56, "Peaking", 0);  // Disable
            CreateAPIState(57, "Peaking", 1);  // MF only
            CreateAPIState(58, "Peaking", 2);  // Always
            // Far Plane
            CreateAPIState(60, "FarPlane", 0);  // Default
            CreateAPIState(61, "FarPlane", 1);  // 10x
            CreateAPIState(62, "FarPlane", 2);  // 100x
            // Transfer Mode (removed in v2.9.3)
            //   CreateAPIState(64, "TransferMode", 0);  // Permissive
            //   CreateAPIState(65, "TransferMode", 1);  // Strict
            // Depth Enabler
            CreateAPIState(66, "DepthEnabler", 0);  // Disable
            CreateAPIState(67, "DepthEnabler", 1);  // Enable
        }

        private void CreatePositionControlLayer()
        {
            var layer = NewLayer("PositionControl");
            var controlParam = layer.IntParameter(ParameterPrefix + "Control");
            var positionControlParam = layer.IntParameter(ParameterPrefix + "PositionControl");
            var qsTriggerParam = layer.IntParameter(ParameterPrefix + "QuickSelfieTrigger");
            var modeParam = layer.IntParameter(ParameterPrefix + "PositionMode");

            var remote = layer.NewState("Remote");
            var neutral = layer.NewState("Neutral")
                .DrivingLocally()
                .Drives(positionControlParam, 0)
                .Drives(modeParam, (int) PositionMode.Neutral);
            var drop = layer.NewState("Drop")
                .DrivingLocally()
                .Drives(positionControlParam, 0)
                .Drives(modeParam, (int) PositionMode.Drop);
            var reposition = layer.NewState("Reposition")
                .DrivingLocally()
                .Drives(positionControlParam, 0)
                .Drives(modeParam, (int) PositionMode.Reposition);
            var drone = layer.NewState("Drone")
                .DrivingLocally()
                .Drives(positionControlParam, 0)
                .Drives(modeParam, (int) PositionMode.Drone);
            var quickSelfie = layer.NewState("QuickSelfie")
                .DrivingLocally()
                .Drives(positionControlParam, 0)
                .Drives(modeParam, (int) PositionMode.QuickSelfie);

            remote.TransitionsTo(neutral).When(layer.Av3().ItIsLocal());

            // Position Control: keep, pickup, drop/reposition, drop, reposition, quick-selfie
            neutral.TransitionsTo(drop).When(positionControlParam.IsEqualTo(2));
            neutral.TransitionsTo(drop).When(positionControlParam.IsEqualTo(3));
            neutral.TransitionsTo(drone).When(controlParam.IsEqualTo(ControlDroneMode));
            neutral.TransitionsTo(quickSelfie).When(positionControlParam.IsEqualTo(5));
            neutral.TransitionsTo(quickSelfie).When(qsTriggerParam.IsNotEqualTo(0));

            drop.TransitionsTo(neutral).When(positionControlParam.IsEqualTo(1));
            drop.TransitionsTo(reposition).When(positionControlParam.IsEqualTo(2));
            drop.TransitionsTo(reposition).When(positionControlParam.IsEqualTo(4));
            drop.TransitionsTo(drone).When(controlParam.IsEqualTo(ControlDroneMode));
            drop.TransitionsTo(quickSelfie).When(positionControlParam.IsEqualTo(5));
            drop.TransitionsTo(quickSelfie).When(qsTriggerParam.IsNotEqualTo(0));

            reposition.TransitionsTo(neutral).When(positionControlParam.IsEqualTo(1));
            reposition.TransitionsTo(drop).When(positionControlParam.IsEqualTo(2));
            reposition.TransitionsTo(drop).When(positionControlParam.IsEqualTo(3));
            reposition.TransitionsTo(drone).When(controlParam.IsEqualTo(ControlDroneMode));
            reposition.TransitionsTo(quickSelfie).When(positionControlParam.IsEqualTo(5));
            reposition.TransitionsTo(quickSelfie).When(qsTriggerParam.IsNotEqualTo(0));
            
            quickSelfie.TransitionsTo(neutral).When(positionControlParam.IsEqualTo(1));
            quickSelfie.TransitionsTo(drop).When(positionControlParam.IsEqualTo(2));
            quickSelfie.TransitionsTo(drop).When(positionControlParam.IsEqualTo(3));
            quickSelfie.TransitionsTo(drone).When(controlParam.IsEqualTo(ControlDroneMode));

            // Load a pin -> Drop
            for (int i = 0; i < NumPins; ++i)
            {
                var loadPinParam = layer.IntParameter(ParameterPrefix + "LoadPin");
                var existPinParam = layer.IntParameter(ParameterPrefix + "ExistPin" + (i + 1));
                neutral.TransitionsTo(drop)
                    .When(loadPinParam.IsEqualTo(i + 1))
                    .And(existPinParam.IsNotEqualTo(0));
                reposition.TransitionsTo(drop)
                    .When(loadPinParam.IsEqualTo(i + 1))
                    .And(existPinParam.IsNotEqualTo(0));
                drone.TransitionsTo(drop)
                    .When(loadPinParam.IsEqualTo(i + 1))
                    .And(existPinParam.IsNotEqualTo(0));
                quickSelfie.TransitionsTo(drop)
                    .When(loadPinParam.IsEqualTo(i + 1))
                    .And(existPinParam.IsNotEqualTo(0));
            }

            // API call (overwriting controller) closes 2D puppet
            drone.TransitionsTo(drop).When(controlParam.IsNotEqualTo(ControlDroneMode));
        }

        private void CreateQuickCallLayer()
        {
            var layer = NewLayer("QuickCall");
            var falseParam = layer.BoolParameter(ParameterPrefix + "False");
            var enableParam = layer.IntParameter(ParameterPrefix + "Enable");
            var quickCallParam = layer.IntParameter(ParameterPrefix + "QuickCall");

            var remote = layer.NewState("Remote");
            var neutral = layer.NewState("Neutral");
            var calls = new AacFlState[NumQuickCalls];
            for (var i = 0; i < NumQuickCalls; ++i)
            {
                calls[i] = layer.NewState(i.ToString())
                    .Drives(quickCallParam, 0)
                    .Drives(enableParam, 1);
            }

            remote.TransitionsTo(neutral).When(layer.Av3().ItIsLocal());
            for (var i = 0; i < NumQuickCalls; ++i)
            {
                neutral.TransitionsTo(calls[i]).When(quickCallParam.IsEqualTo(i + 1));
                calls[i].TransitionsTo(neutral).When(falseParam.IsFalse());
            }
        }

        private void CreateAltMeshLayer()
        {
            var layer = NewLayer("AltMesh");
            var modeParam = layer.IntParameter(ParameterPrefix + "PositionMode");
            var stabilizerParam = layer.IntParameter(ParameterPrefix + "Stabilizer");
            var externalParam = layer.IntParameter(ParameterPrefix + "ExternalPose");
            var altMeshParam = layer.IntParameter(ParameterPrefix + "AltMesh");
            var altMeshRemoteParam = layer.IntParameter(ParameterPrefix + "AltMeshRemote");

            var remote = layer.NewState("Remote");
            var neutral = layer.NewState("Neutral")
                .DrivingLocally()
                .Drives(altMeshParam, 0)
                .Drives(altMeshRemoteParam, 0);
            var local = layer.NewState("Local")
                .DrivingLocally()
                .Drives(altMeshParam, 1)
                .Drives(altMeshRemoteParam, 0);
            var global = layer.NewState("Global")
                .DrivingLocally()
                .Drives(altMeshParam, 1)
                .Drives(altMeshRemoteParam, 1);

            remote.TransitionsTo(neutral).When(layer.Av3().ItIsLocal());

            neutral.TransitionsTo(local)
                .When(modeParam.IsEqualTo((int) PositionMode.Neutral))
                .And(externalParam.IsEqualTo(0))
                .And(stabilizerParam.IsNotEqualTo(0));
            neutral.TransitionsTo(global)
                .When(modeParam.IsNotEqualTo((int) PositionMode.Neutral));
            neutral.TransitionsTo(global)
                .When(externalParam.IsNotEqualTo(0));

            local.TransitionsTo(neutral)
                .When(modeParam.IsEqualTo((int) PositionMode.Neutral))
                .And(externalParam.IsEqualTo(0))
                .And(stabilizerParam.IsEqualTo(0));
            local.TransitionsTo(global)
                .When(modeParam.IsNotEqualTo((int) PositionMode.Neutral));
            local.TransitionsTo(global)
                .When(externalParam.IsNotEqualTo(0));

            global.TransitionsTo(neutral)
                .When(modeParam.IsEqualTo((int) PositionMode.Neutral))
                .And(externalParam.IsEqualTo(0))
                .And(stabilizerParam.IsEqualTo(0));
            global.TransitionsTo(local)
                .When(modeParam.IsEqualTo((int) PositionMode.Neutral))
                .And(externalParam.IsEqualTo(0))
                .And(stabilizerParam.IsNotEqualTo(0));
        }

        private void CreateMultiplexerLayer()
        {
            var layer = NewLayer("Multiplexer");
            var controlParam = layer.IntParameter(ParameterPrefix + "Control");
            var enableParam = layer.IntParameter(ParameterPrefix + "Enable");
            var altMeshParam = layer.IntParameter(ParameterPrefix + "AltMesh");
            var altMeshRemoteParam = layer.IntParameter(ParameterPrefix + "AltMeshRemote");
            var hideMeshParam = layer.IntParameter(ParameterPrefix + "Hide");
            var sendParams = new[] {enableParam, altMeshRemoteParam, hideMeshParam};
            var recvParams = new[] {enableParam, altMeshParam, hideMeshParam};
            var n = sendParams.Length;

            var remote = layer.NewState("Remote");
            var local = layer.NewState("Local");
            remote.TransitionsTo(local).When(layer.Av3().ItIsLocal());

            for (var i = 0; i < (1 << n); ++i)
            {
                var value = ControlStateStart + i;
                var state = layer.NewState("R" + i);
                for (var j = 0; j < n; ++j)
                {
                    var param = recvParams[j];
                    var data = (i >> j) & 1;
                    state
                        .Drives(param, data)
                        .Drives(layer.FloatParameter(param.Name + "F"), data);
                }
                remote.TransitionsTo(state).When(controlParam.IsEqualTo(value));
                state.TransitionsTo(local).When(layer.Av3().ItIsLocal());
                state.TransitionsTo(remote)
                    .When(controlParam.IsGreaterThan(ControlDroneMode - 1))
                    .And(controlParam.IsNotEqualTo(value));
            }
            {
                var state = layer.NewState("RDrone")
                    .Drives(altMeshParam, 1)
                    .Drives(layer.FloatParameter(altMeshParam.Name + "F"), 1);
                remote.TransitionsTo(state).When(controlParam.IsEqualTo(ControlDroneMode));
                state.TransitionsTo(local).When(layer.Av3().ItIsLocal());
                state.TransitionsTo(remote)
                    .When(controlParam.IsGreaterThan(ControlDroneMode - 1))
                    .And(controlParam.IsNotEqualTo(ControlDroneMode));
            }

            for (var i = 0; i < (1 << n); ++i)
            {
                var value = ControlStateStart + i;
                var state = layer.NewState("L" + i).Drives(controlParam, value);
                var conditions0 =
                    local.TransitionsTo(state).When(controlParam.IsLessThan(ControlMenuStart));
                var conditions1 =
                    local.TransitionsTo(state).When(controlParam.IsGreaterThan(ControlStateStart - 1));
                for (int j = 0; j < n; ++j)
                {
                    conditions0.And(sendParams[j].IsEqualTo((i >> j) & 1));
                    conditions1.And(sendParams[j].IsEqualTo((i >> j) & 1));
                    state.TransitionsTo(local).When(sendParams[j].IsNotEqualTo((i >> j) & 1));
                }
                // API call and exit from drone mode
                state.TransitionsTo(local).When(controlParam.IsLessThan(ControlMenuStart));
            }
        }

        private void CreateI2FLayer()
        {
            var layer = NewLayer("I2F");
            var remote = layer.NewState("Remote");
            var local = layer.NewState("Local")
                .DrivingLocally();

            void AddCast(string name)
            {
                local.DrivingCasts(
                    layer.IntParameter(ParameterPrefix + name), 0, 255,
                    layer.FloatParameter(ParameterPrefix + name + "F"), 0.0f, 255.0f);
            }

            AddCast("Control");
            AddCast("Enable");
            AddCast("AltMesh");
            AddCast("PositionMode");
            AddCast("PositionControl");
            AddCast("AutoLeveler");
            AddCast("Stabilizer");
            AddCast("RepositionScale");
            AddCast("ExternalPose");
            AddCast("AFMode");
            AddCast("AFSpeed");
            AddCast("TrackingSpeed");
            AddCast("FocusLock");
            AddCast("Grid");
            AddCast("Information");
            AddCast("Level");
            AddCast("Peaking");
            AddCast("Hide");
            AddCast("FarPlane");
            AddCast("DepthEnabler");
            AddCast("DepthCleaner");
            AddCast("LocalPlayerMask");
            AddCast("RemotePlayerMask");
            AddCast("UIMask");
            AddCast("PreviewHUD");
            AddCast("TouchOverride");
            AddCast("Resolution");

            remote.TransitionsTo(local).When(layer.Av3().ItIsLocal());
            local.TransitionsTo(local)
                .WithTransitionToSelf()
                .When(layer.Av3().ItIsLocal());
        }

        #endregion

        #region Real Parameters

        private void CreateZoomLayer()
        {
            // TODO framerate compensation
            var droneZoomMaxSpeed = 0.5f;
            var droneZoomDeadZone = 0.25f;
            var droneZoomSpeedResolution = 8;

            var layer = NewLayer("Zoom");
            var zoomParam = layer.FloatParameter(ParameterPrefix + "Zoom");
            var droneZoomParam = layer.IntParameter(ParameterPrefix + "DroneZoom");
            var modeParam = layer.IntParameter(ParameterPrefix + "PositionMode");
            var yParam = layer.FloatParameter(ParameterPrefix + "Y");

            // VirtualLens2/Core/Animations/Placeholders/Zoom.anim
            var placeholder = AssetUtility.LoadAssetByGUID<AnimationClip>("4605d0f9cfdde2d4c80f18af6265c401");

            var remote = layer.NewState("Remote");
            var local = layer.NewState("Local")
                .WithAnimation(placeholder)
                .MotionTime(zoomParam);
            remote.TransitionsTo(local).When(layer.Av3().ItIsLocal());

            var m = droneZoomSpeedResolution * 2 + 1;
            
            var zoomSpeeds = new float[m];
            for (var j = 0; j < m; ++j)
            {
                var jj = j - droneZoomSpeedResolution;
                zoomSpeeds[j] = jj * droneZoomMaxSpeed / droneZoomSpeedResolution / 45.0f;
            }
            
            var yBias = (1.0f - droneZoomDeadZone) / droneZoomSpeedResolution * 0.2f;
            var yThresholds = new float[m + 1];
            for (var j = 0; j < droneZoomSpeedResolution; ++j)
            {
                var t = j * ((1.0f - droneZoomDeadZone) / droneZoomSpeedResolution) + droneZoomDeadZone;
                yThresholds[droneZoomSpeedResolution - j] = -t;
                yThresholds[droneZoomSpeedResolution + j + 1] = t;
            }
            yThresholds[0] = Single.MinValue;
            yThresholds[m] = Single.MaxValue;

            var droneAddStates = new AacFlState[m];
            var droneClampStates = new AacFlState[m];
            for (var j = 0; j < m; ++j)
            {
                var speed = zoomSpeeds[j];
                var addState = layer.NewState($"Drone_{j}")
                    .WithAnimation(placeholder)
                    .MotionTime(zoomParam);
                if (speed != 0.0f) { addState.DrivingIncreases(zoomParam, speed); }
                var clampState = layer.NewState($"Drone_{j}C")
                    .Drives(zoomParam, speed < 0.0f ? 0.0f : 1.0f)
                    .WithAnimation(placeholder)
                    .MotionTime(zoomParam);
                addState.TransitionsTo(local)
                    .When(modeParam.IsNotEqualTo((int)PositionMode.Drone))
                    .Or()
                    .When(droneZoomParam.IsEqualTo(0));
                clampState.TransitionsTo(local)
                    .When(modeParam.IsNotEqualTo((int)PositionMode.Drone))
                    .Or()
                    .When(droneZoomParam.IsEqualTo(0));
                var selfLo = yThresholds[j] - yBias;
                var selfHi = yThresholds[j + 1] + yBias;
                if (speed < 0.0f)
                {
                    addState.TransitionsTo(clampState)
                        .When(yParam.IsGreaterThan(selfLo))
                        .And(yParam.IsLessThan(selfHi))
                        .And(zoomParam.IsLessThan(-speed));
                    addState.TransitionsTo(addState)
                        .WithTransitionToSelf()
                        .When(yParam.IsGreaterThan(selfLo))
                        .And(yParam.IsLessThan(selfHi));
                    clampState.TransitionsTo(addState)
                        .When(yParam.IsGreaterThan(selfLo))
                        .And(yParam.IsLessThan(selfHi))
                        .And(zoomParam.IsGreaterThan(-speed));
                }
                else if (speed > 0.0f)
                {
                    addState.TransitionsTo(clampState)
                        .When(yParam.IsGreaterThan(selfLo))
                        .And(yParam.IsLessThan(selfHi))
                        .And(zoomParam.IsGreaterThan(1.0f - speed));
                    addState.TransitionsTo(addState)
                        .WithTransitionToSelf()
                        .When(yParam.IsGreaterThan(selfLo))
                        .And(yParam.IsLessThan(selfHi));
                    clampState.TransitionsTo(addState)
                        .When(yParam.IsGreaterThan(selfLo))
                        .And(yParam.IsLessThan(selfHi))
                        .And(zoomParam.IsLessThan(1.0f - speed));
                }
                else
                {
                    addState.TransitionsTo(addState)
                        .WithTransitionToSelf()
                        .When(yParam.IsGreaterThan(selfLo))
                        .And(yParam.IsLessThan(selfHi));
                    clampState = addState;
                }
                droneAddStates[j] = addState;
                droneClampStates[j] = clampState;
            }

            local.TransitionsTo(droneAddStates[droneZoomSpeedResolution])
                .When(modeParam.IsEqualTo((int)PositionMode.Drone))
                .And(droneZoomParam.IsNotEqualTo(0));
            
            for (var j = 0; j < m; ++j)
            {
                var addState = droneAddStates[j];
                var clampState = droneClampStates[j];
                for (var d = 1; d < m; ++d)
                {
                    if (j - d >= 0)
                    {
                        var k = j - d;
                        var speed = zoomSpeeds[k];
                        if (speed < 0.0f)
                        {
                            addState.TransitionsTo(droneClampStates[k])
                                .When(yParam.IsGreaterThan(yThresholds[k] - yBias))
                                .And(yParam.IsLessThan(yThresholds[k + 1]))
                                .And(zoomParam.IsLessThan(-speed));
                            clampState.TransitionsTo(droneClampStates[k])
                                .When(yParam.IsGreaterThan(yThresholds[k] - yBias))
                                .And(yParam.IsLessThan(yThresholds[k + 1]))
                                .And(zoomParam.IsLessThan(-speed));
                        }
                        else if (speed > 0.0f)
                        {
                            addState.TransitionsTo(droneClampStates[k])
                                .When(yParam.IsGreaterThan(yThresholds[k] - yBias))
                                .And(yParam.IsLessThan(yThresholds[k + 1]))
                                .And(zoomParam.IsGreaterThan(1.0f - speed));
                            clampState.TransitionsTo(droneClampStates[k])
                                .When(yParam.IsGreaterThan(yThresholds[k] - yBias))
                                .And(yParam.IsLessThan(yThresholds[k + 1]))
                                .And(zoomParam.IsGreaterThan(1.0f - speed));
                        }
                        addState.TransitionsTo(droneAddStates[k])
                            .When(yParam.IsGreaterThan(yThresholds[k] - yBias))
                            .And(yParam.IsLessThan(yThresholds[k + 1]));
                        clampState.TransitionsTo(droneAddStates[k])
                            .When(yParam.IsGreaterThan(yThresholds[k] - yBias))
                            .And(yParam.IsLessThan(yThresholds[k + 1]));
                    }
                    if (j + d < m)
                    {
                        var k = j + d;
                        var speed = zoomSpeeds[k];
                        if (speed < 0.0f)
                        {
                            addState.TransitionsTo(droneClampStates[k])
                                .When(yParam.IsGreaterThan(yThresholds[k]))
                                .And(yParam.IsLessThan(yThresholds[k + 1] + yBias))
                                .And(zoomParam.IsLessThan(-speed));
                            clampState.TransitionsTo(droneClampStates[k])
                                .When(yParam.IsGreaterThan(yThresholds[k]))
                                .And(yParam.IsLessThan(yThresholds[k + 1] + yBias))
                                .And(zoomParam.IsLessThan(-speed));
                        }
                        else if (speed > 0.0f)
                        {
                            addState.TransitionsTo(droneClampStates[k])
                                .When(yParam.IsGreaterThan(yThresholds[k]))
                                .And(yParam.IsLessThan(yThresholds[k + 1] + yBias))
                                .And(zoomParam.IsGreaterThan(1.0f - speed));
                            clampState.TransitionsTo(droneClampStates[k])
                                .When(yParam.IsGreaterThan(yThresholds[k]))
                                .And(yParam.IsLessThan(yThresholds[k + 1] + yBias))
                                .And(zoomParam.IsGreaterThan(1.0f - speed));
                        }
                        addState.TransitionsTo(droneAddStates[k])
                            .When(yParam.IsGreaterThan(yThresholds[k]))
                            .And(yParam.IsLessThan(yThresholds[k + 1] + yBias));
                        clampState.TransitionsTo(droneAddStates[k])
                            .When(yParam.IsGreaterThan(yThresholds[k]))
                            .And(yParam.IsLessThan(yThresholds[k + 1] + yBias));
                    }
                }
            }
            
            // VirtualLens2/Core/Animations/Placeholders/Zoom.anim
            // var placeholder = AssetUtility.LoadAssetByGUID<AnimationClip>("4605d0f9cfdde2d4c80f18af6265c401");
            // CreateRealParameterLayer("Zoom", placeholder);
        }

        private void CreateApertureLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "Aperture");

            // VirtualLens2/Core/Animations/Placeholders/ApertureMin.anim
            var min = AssetUtility.LoadAssetByGUID<AnimationClip>("5be4c8a7f6a92ab4cbb4ec8d8e7ef5db");
            // VirtualLens2/Core/Animations/Placeholders/ApertureMax.anim
            var max = AssetUtility.LoadAssetByGUID<AnimationClip>("113089bf8eec90542ab08f080bfa460c");

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, min)
                .AddChild(1.0f, max)
                .Motion);
        }

        private void CreateManualFocusLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "Distance");

            // VirtualLens2/Core/Animations/Placeholders/DistanceMin.anim
            var min = AssetUtility.LoadAssetByGUID<AnimationClip>("0252f150775f383449351e783fa0279f");
            // VirtualLens2/Core/Animations/Placeholders/DistanceMax.anim
            var max = AssetUtility.LoadAssetByGUID<AnimationClip>("d6421ea187b66994197cbf16d653e9e1");

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, min)
                .AddChild(1.0f, max)
                .Motion);
        }

        private void CreateExposureLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "Exposure");

            // VirtualLens2/Core/Animations/Placeholders/ExposureMin.anim
            var min = AssetUtility.LoadAssetByGUID<AnimationClip>("10d1b7ec22a81aa41bc9e77c18d78164");
            // VirtualLens2/Core/Animations/Placeholders/ExposureMax.anim
            var max = AssetUtility.LoadAssetByGUID<AnimationClip>("860f373828cb7f64597c80252183779e");

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, min)
                .AddChild(1.0f, max)
                .Motion);
        }

        #endregion

        #region Core Control

        private void CreateEnableLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "EnableF");

            var cameraRoot = PlaceholderObject("CameraRoot");
            var optionalObjects = PlaceholderObject("OptionalObjects");
            var optionalObjectsNegated = PlaceholderObject("OptionalObjectsNegated");
            var screenTouchers = PlaceholderObject("ScreenTouchers");

            // Not verified: material with queue=background must be set by animation
            var depthCleaners = HierarchyUtility
                .PathToObject(LocalRoot, "Writer/DepthCleaners")
                .GetComponentsInChildren<MeshRenderer>(true);

            // VirtualLens2/Core/Materials/System/DepthCleaner1000.mat
            var material = AssetUtility.LoadAssetByGUID<Material>("b017697620518e3449e695bc6f5a76dd");

            var remoteDisable = _aac.NewClip()
                .Toggling(cameraRoot, false)
                .Toggling(optionalObjects, false)
                .Toggling(optionalObjectsNegated, true)
                .Toggling(LocalRoot, false)
                .Scaling(new[] { LocalRoot }, Vector3.one);
            var remoteEnable = _aac.NewClip()
                .Toggling(cameraRoot, true)
                .Toggling(optionalObjects, true)
                .Toggling(optionalObjectsNegated, false)
                .Toggling(LocalRoot, false)
                .Scaling(new[] { LocalRoot }, Vector3.one);
            var localDisable = _aac.NewClip()
                .Toggling(cameraRoot, false)
                .Toggling(optionalObjects, false)
                .Toggling(optionalObjectsNegated, true)
                .Toggling(LocalRoot, false)
                .Scaling(new[] { LocalRoot }, Vector3.one)
                .Toggling(screenTouchers, false);
            var localEnable = _aac.NewClip()
                .Toggling(cameraRoot, true)
                .Toggling(optionalObjects, true)
                .Toggling(optionalObjectsNegated, false)
                .Toggling(LocalRoot, true)
                .Scaling(new[] { LocalRoot }, Vector3.one)
                .Toggling(screenTouchers, true);
            foreach (var dc in depthCleaners)
            {
                localDisable.SwappingMaterial(dc, 0, material);
                localEnable.SwappingMaterial(dc, 0, material);
            }

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, localDisable)
                .AddChild(1.0f, localEnable)
                .Motion);
            _remoteBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, remoteDisable)
                .AddChild(1.0f, remoteEnable)
                .Motion);
        }

        private void CreateAltMeshVisibilityLayer(AacFlLayer layer)
        {
            var altMeshParam = layer.FloatParameter(ParameterPrefix + "AltMeshF");
            var hideParam = layer.FloatParameter(ParameterPrefix + "HideF");

            var altMesh = HierarchyUtility.PathToObject(LocalRoot, "Capture/AltMesh");
            var previewMeshes =
                PlaceholderObject("PreviewMeshes").GetComponent<MeshRenderer>();
            var previewSkinnedMeshes =
                PlaceholderObject("PreviewSkinnedMeshes").GetComponent<SkinnedMeshRenderer>();
            var hideablePreviewMeshes =
                PlaceholderObject("HideablePreviewMeshes").GetComponent<MeshRenderer>();
            var hideablePreviewSkinnedMeshes =
                PlaceholderObject("HideablePreviewSkinnedMeshes").GetComponent<SkinnedMeshRenderer>();
            var nonPreviewMeshes =
                PlaceholderObject("NonPreviewMeshes").GetComponent<MeshRenderer>();
            var nonPreviewSkinnedMeshes =
                PlaceholderObject("NonPreviewSkinnedMeshes").GetComponent<SkinnedMeshRenderer>();
            var hideableNonPreviewMeshes =
                PlaceholderObject("HideableNonPreviewMeshes").GetComponent<MeshRenderer>();
            var hideableNonPreviewSkinnedMeshes =
                PlaceholderObject("HideableNonPreviewSkinnedMeshes").GetComponent<SkinnedMeshRenderer>();

            var remoteDisable = _aac.NewClip()
                .TogglingComponent(previewMeshes, true)
                .TogglingComponent(previewSkinnedMeshes, true)
                .TogglingComponent(nonPreviewMeshes, true)
                .TogglingComponent(nonPreviewSkinnedMeshes, true)
                .TogglingComponent(hideablePreviewMeshes, true)
                .TogglingComponent(hideablePreviewSkinnedMeshes, true)
                .TogglingComponent(hideableNonPreviewMeshes, true)
                .TogglingComponent(hideableNonPreviewSkinnedMeshes, true);
            var remoteDisableHide = _aac.NewClip()
                .TogglingComponent(previewMeshes, true)
                .TogglingComponent(previewSkinnedMeshes, true)
                .TogglingComponent(nonPreviewMeshes, true)
                .TogglingComponent(nonPreviewSkinnedMeshes, true)
                .TogglingComponent(hideablePreviewMeshes, false)
                .TogglingComponent(hideablePreviewSkinnedMeshes, false)
                .TogglingComponent(hideableNonPreviewMeshes, false)
                .TogglingComponent(hideableNonPreviewSkinnedMeshes, false);
            var remoteEnable = _aac.NewClip()
                .TogglingComponent(previewMeshes, false)
                .TogglingComponent(previewSkinnedMeshes, false)
                .TogglingComponent(nonPreviewMeshes, false)
                .TogglingComponent(nonPreviewSkinnedMeshes, false)
                .TogglingComponent(hideablePreviewMeshes, false)
                .TogglingComponent(hideablePreviewSkinnedMeshes, false)
                .TogglingComponent(hideableNonPreviewMeshes, false)
                .TogglingComponent(hideableNonPreviewSkinnedMeshes, false);
            var remoteEnableHide = _aac.NewClip()
                .TogglingComponent(previewMeshes, false)
                .TogglingComponent(previewSkinnedMeshes, false)
                .TogglingComponent(nonPreviewMeshes, false)
                .TogglingComponent(nonPreviewSkinnedMeshes, false)
                .TogglingComponent(hideablePreviewMeshes, false)
                .TogglingComponent(hideablePreviewSkinnedMeshes, false)
                .TogglingComponent(hideableNonPreviewMeshes, false)
                .TogglingComponent(hideableNonPreviewSkinnedMeshes, false);
            var localDisable = _aac.NewClip()
                .TogglingComponent(previewMeshes, true)
                .TogglingComponent(previewSkinnedMeshes, true)
                .TogglingComponent(nonPreviewMeshes, true)
                .TogglingComponent(nonPreviewSkinnedMeshes, true)
                .TogglingComponent(hideablePreviewMeshes, true)
                .TogglingComponent(hideablePreviewSkinnedMeshes, true)
                .TogglingComponent(hideableNonPreviewMeshes, true)
                .TogglingComponent(hideableNonPreviewSkinnedMeshes, true)
                .Toggling(altMesh, false);
            var localDisableHide = _aac.NewClip()
                .TogglingComponent(previewMeshes, true)
                .TogglingComponent(previewSkinnedMeshes, true)
                .TogglingComponent(nonPreviewMeshes, true)
                .TogglingComponent(nonPreviewSkinnedMeshes, true)
                .TogglingComponent(hideablePreviewMeshes, true)
                .TogglingComponent(hideablePreviewSkinnedMeshes, true)
                .TogglingComponent(hideableNonPreviewMeshes, false)
                .TogglingComponent(hideableNonPreviewSkinnedMeshes, false)
                .Toggling(altMesh, false);
            var localEnable = _aac.NewClip()
                .TogglingComponent(previewMeshes, true)
                .TogglingComponent(previewSkinnedMeshes, true)
                .TogglingComponent(nonPreviewMeshes, false)
                .TogglingComponent(nonPreviewSkinnedMeshes, false)
                .TogglingComponent(hideablePreviewMeshes, true)
                .TogglingComponent(hideablePreviewSkinnedMeshes, true)
                .TogglingComponent(hideableNonPreviewMeshes, false)
                .TogglingComponent(hideableNonPreviewSkinnedMeshes, false)
                .Toggling(altMesh, true);
            var localEnableHide = _aac.NewClip()
                .TogglingComponent(previewMeshes, true)
                .TogglingComponent(previewSkinnedMeshes, true)
                .TogglingComponent(nonPreviewMeshes, false)
                .TogglingComponent(nonPreviewSkinnedMeshes, false)
                .TogglingComponent(hideablePreviewMeshes, true)
                .TogglingComponent(hideablePreviewSkinnedMeshes, true)
                .TogglingComponent(hideableNonPreviewMeshes, false)
                .TogglingComponent(hideableNonPreviewSkinnedMeshes, false)
                .Toggling(altMesh, true);

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, altMeshParam)
                .AddChild(0.0f, new BlendTreeEditor(_aac, hideParam)
                    .AddChild(0.0f, localDisable)
                    .AddChild(1.0f, localDisableHide))
                .AddChild(1.0f, new BlendTreeEditor(_aac, hideParam)
                    .AddChild(0.0f, localEnable)
                    .AddChild(1.0f, localEnableHide))
                .Motion);
            _remoteBlendTreeChildren.Add(new BlendTreeEditor(_aac, altMeshParam)
                .AddChild(0.0f, new BlendTreeEditor(_aac, hideParam)
                    .AddChild(0.0f, remoteDisable)
                    .AddChild(1.0f, remoteDisableHide))
                .AddChild(1.0f, new BlendTreeEditor(_aac, hideParam)
                    .AddChild(0.0f, remoteEnable)
                    .AddChild(1.0f, remoteEnableHide))
                .Motion);
        }

        private void CreatePreviewMaterialLayer(AacFlLayer layer)
        {
            // VirtualLens2/Core/Animations/Placeholders/ReplacePreviewMaterials.anim
            var clip = AssetUtility.LoadAssetByGUID<AnimationClip>("c05e3ba36ad7e9a4d99677c51a5abdc1");
            _localBlendTreeChildren.Add(clip);
        }

        private void CreateLevelDetectorLayer(AacFlLayer layer)
        {
            var autoLevelerParam = layer.FloatParameter(ParameterPrefix + "AutoLevelerF");
            var previewHUDParam = layer.FloatParameter(ParameterPrefix + "PreviewHUDF");

            var root = HierarchyUtility.PathToObject(LocalRoot, "Capture/LevelDetector");
            var detectors = root.GetComponentsInChildren<VRCPhysBone>(true);

            var disable = _aac.NewClip().TogglingComponent(detectors, false);
            var enable = _aac.NewClip().TogglingComponent(detectors, true);

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, previewHUDParam)
                .AddChild(0.0f, new BlendTreeEditor(_aac, autoLevelerParam)
                    .AddChild(0.0f, disable)
                    .AddChild(1.0f, disable)
                    .AddChild(2.0f, disable)
                    .AddChild(3.0f, enable))
                .AddChild(1.0f, enable)
                .Motion);
        }

        #endregion

        #region Transform Control

        private void CreatePoseControlLayer()
        {
            var layer = NewLayer("PoseControl");
            var modeParam = layer.IntParameter(ParameterPrefix + "PositionMode");

            var result = HierarchyUtility.PathToObject(LocalRoot, "Transform/WorldFixed/Unstabilized");
            var resultPosition = result.GetComponent<VRCPositionConstraint>();
            var resultRotation = result.GetComponent<VRCRotationConstraint>();

            var remote = layer.NewState("Remote");
            var neutral = layer.NewState("Neutral").WithAnimation(_aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 0)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 1)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 2)).WithOneFrame(1.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 3)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 0)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 1)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 2)).WithOneFrame(1.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 3)).WithOneFrame(0.0f);
                }));
            var reposition = layer.NewState("Reposition").WithAnimation(_aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 0)).WithOneFrame(1.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 1)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 2)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 3)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 0)).WithOneFrame(1.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 1)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 2)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 3)).WithOneFrame(0.0f);
                }));
            var drone = layer.NewState("Drone").WithAnimation(_aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 0)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 1)).WithOneFrame(1.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 2)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 3)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 0)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 1)).WithOneFrame(1.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 2)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 3)).WithOneFrame(0.0f);
                }));
            var quickSelfie = layer.NewState("QuickSelfie").WithAnimation(_aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 0)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 1)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 2)).WithOneFrame(0.0f);
                    clip.Animates(resultPosition, ConstraintWeightProp(resultPosition, 3)).WithOneFrame(1.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 0)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 1)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 2)).WithOneFrame(0.0f);
                    clip.Animates(resultRotation, ConstraintWeightProp(resultRotation, 3)).WithOneFrame(1.0f);
                }));

            remote.TransitionsTo(neutral).When(layer.Av3().ItIsLocal());

            neutral.TransitionsTo(reposition)
                .When(modeParam.IsEqualTo((int) PositionMode.Reposition));
            neutral.TransitionsTo(drone)
                .When(modeParam.IsEqualTo((int)PositionMode.Drone));
            neutral.TransitionsTo(quickSelfie)
                .When(modeParam.IsEqualTo((int) PositionMode.QuickSelfie));

            reposition.TransitionsTo(neutral).When(modeParam.IsNotEqualTo((int) PositionMode.Reposition));
            drone.TransitionsTo(neutral).When(modeParam.IsNotEqualTo((int) PositionMode.Drone));
            quickSelfie.TransitionsTo(neutral)
                .When(modeParam.IsNotEqualTo((int) PositionMode.QuickSelfie));
        }

        private void CreateStabilizerLayer()
        {
            const int numKeyframes = 256;
            var weights = new[] {1.0f, 0.1f, 0.02f, 0.005f};

            void GenerateUnstabilizedCurve(AacFlSettingKeyframes keyframes, float weight)
            {
                var cur = 0.0f;
                for (int i = 0; i <= numKeyframes; ++i)
                {
                    keyframes.Linear(i, cur);
                    cur = weight + (1.0f - cur) * cur;
                }
            }

            void GenerateResultCurve(AacFlSettingKeyframes keyframes, float weight)
            {
                var cur = 0.0f;
                for (int i = 0; i <= numKeyframes; ++i)
                {
                    keyframes.Linear(i, 1.0f - cur);
                    cur = weight + (1.0f - cur) * cur;
                }
            }

            var layer = NewLayer("Stabilizer");
            var modeParam = layer.IntParameter(ParameterPrefix + "PositionMode");
            var stabilizerParam = layer.IntParameter(ParameterPrefix + "Stabilizer");
            var deltaParam = layer.FloatParameter(ParameterPrefix + "Delta");

            var result = HierarchyUtility.PathToObject(LocalRoot, "Transform/WorldFixed/Result");
            var position = result.GetComponent<VRCPositionConstraint>();
            var rotation = result.GetComponent<VRCRotationConstraint>();

            var enableClips = new AacFlClip[weights.Length];
            for (var i = 1; i < weights.Length; ++i)
            {
                var w = weights[i];
                enableClips[i] = _aac.NewClip().Animating(clip =>
                    {
                        clip.Animates(position, ConstraintWeightProp(position, 0)).WithOneFrame(0.0f);
                        clip.Animates(position, ConstraintWeightProp(position, 1))
                            .WithFrameCountUnit(kf => GenerateResultCurve(kf, w));
                        clip.Animates(position, ConstraintWeightProp(position, 2))
                            .WithFrameCountUnit(kf => GenerateUnstabilizedCurve(kf, w));
                        clip.Animates(rotation, ConstraintWeightProp(rotation, 0)).WithOneFrame(0.0f);
                        clip.Animates(rotation, ConstraintWeightProp(rotation, 1))
                            .WithFrameCountUnit(kf => GenerateResultCurve(kf, w));
                        clip.Animates(rotation, ConstraintWeightProp(rotation, 2))
                            .WithFrameCountUnit(kf => GenerateUnstabilizedCurve(kf, w));
                    });
            }

            var droneClips = new AacFlClip[4];
            for (var i = 1; i < weights.Length; ++i)
            {
                var w = weights[i];
                droneClips[i] = _aac.NewClip().Animating(clip =>
                    {
                        clip.Animates(position, ConstraintWeightProp(position, 0)).WithOneFrame(1.0f);
                        clip.Animates(position, ConstraintWeightProp(position, 1)).WithOneFrame(0.0f);
                        clip.Animates(position, ConstraintWeightProp(position, 2)).WithOneFrame(0.0f);
                        clip.Animates(rotation, ConstraintWeightProp(rotation, 0)).WithOneFrame(0.0f);
                        clip.Animates(rotation, ConstraintWeightProp(rotation, 1))
                            .WithFrameCountUnit(kf => GenerateResultCurve(kf, w));
                        clip.Animates(rotation, ConstraintWeightProp(rotation, 2))
                            .WithFrameCountUnit(kf => GenerateUnstabilizedCurve(kf, w));
                    });
            }

            var remote = layer.NewState("Remote");
            var disable = layer.NewState("Disable").WithAnimation(_aac.NewClip().Animating(clip =>
                {
                    clip.Animates(position, ConstraintWeightProp(position, 0)).WithOneFrame(1.0f);
                    clip.Animates(position, ConstraintWeightProp(position, 1)).WithOneFrame(0.0f);
                    clip.Animates(position, ConstraintWeightProp(position, 2)).WithOneFrame(0.0f);
                    clip.Animates(rotation, ConstraintWeightProp(rotation, 0)).WithOneFrame(1.0f);
                    clip.Animates(rotation, ConstraintWeightProp(rotation, 1)).WithOneFrame(0.0f);
                    clip.Animates(rotation, ConstraintWeightProp(rotation, 2)).WithOneFrame(0.0f);
                }));

            var enableStates = new AacFlState[weights.Length];
            var droneStates = new AacFlState[weights.Length];
            for (var i = 1; i < weights.Length; ++i)
            {
                enableStates[i] = layer.NewState($"Enable{i}")
                    .MotionTime(deltaParam)
                    .WithAnimation(enableClips[i]);
                droneStates[i] = layer.NewState($"Drone{i}")
                    .MotionTime(deltaParam)
                    .WithAnimation(droneClips[i]);
            }

            remote.TransitionsTo(disable).When(layer.Av3().ItIsLocal());

            for (var i = 1; i < weights.Length; ++i)
            {
                disable.TransitionsTo(enableStates[i])
                    .When(stabilizerParam.IsEqualTo(i))
                    .And(modeParam.IsNotEqualTo((int)PositionMode.Drone));
                disable.TransitionsTo(droneStates[i])
                    .When(stabilizerParam.IsEqualTo(i))
                    .And(modeParam.IsEqualTo((int)PositionMode.Drone));

                enableStates[i].TransitionsTo(disable).When(stabilizerParam.IsEqualTo(0));
                droneStates[i].TransitionsTo(disable).When(stabilizerParam.IsEqualTo(0));

                for (var j = 1; j < weights.Length; ++j)
                {
                    enableStates[i].TransitionsTo(enableStates[j])
                        .When(stabilizerParam.IsEqualTo(j))
                        .And(modeParam.IsNotEqualTo((int)PositionMode.Drone));
                    enableStates[i].TransitionsTo(droneStates[j])
                        .When(stabilizerParam.IsEqualTo(j))
                        .And(modeParam.IsEqualTo((int)PositionMode.Drone));
                    droneStates[i].TransitionsTo(enableStates[j])
                        .When(stabilizerParam.IsEqualTo(j))
                        .And(modeParam.IsNotEqualTo((int)PositionMode.Drone));
                    droneStates[i].TransitionsTo(droneStates[j])
                        .When(stabilizerParam.IsEqualTo(j))
                        .And(modeParam.IsEqualTo((int)PositionMode.Drone));
                }
            }
        }

        private void CreateStabilizerLimiterLayer()
        {
            var layer = NewLayer("StabilizerLimiter");
            var falseParam = layer.BoolParameter(ParameterPrefix + "False");
            var stabilizerParam = layer.IntParameter(ParameterPrefix + "Stabilizer");
            var loadPinParam = layer.IntParameter(ParameterPrefix + "LoadPin");

            var limiter = HierarchyUtility.PathToObject(LocalRoot, "Transform/WorldFixed/Limiter");
            var parent = limiter.GetComponent<VRCParentConstraint>();
            var rigid = limiter.GetComponent<Rigidbody>();

            var resetClip = _aac.NewClip()
                .TogglingComponent(parent, true)
                .Animating(clip => { clip.Animates(rigid, "m_IsKinematic").WithOneFrame(1.0f); });

            // Not verified: resetting rigid body states requires 2 or 3 frames
            var remote = layer.NewState("Remote");
            var reset0 = layer.NewState("Reset0").WithAnimation(resetClip);
            var reset1 = layer.NewState("Reset1").WithAnimation(resetClip);
            var reset2 = layer.NewState("Reset2").WithAnimation(resetClip);
            var neutral = layer.NewState("Neutral").WithAnimation(_aac.NewClip()
                .TogglingComponent(parent, false)
                .Animating(clip => { clip.Animates(rigid, "m_IsKinematic").WithOneFrame(0.0f); }));
            remote.TransitionsTo(reset0).When(layer.Av3().ItIsLocal());

            reset0.TransitionsTo(neutral)
                .When(stabilizerParam.IsNotEqualTo(0))
                .And(loadPinParam.IsEqualTo(0));

            neutral.TransitionsTo(reset2).When(stabilizerParam.IsEqualTo(0));
            neutral.TransitionsTo(reset2).When(loadPinParam.IsNotEqualTo(0));

            reset2.TransitionsTo(reset1).When(falseParam.IsFalse());
            reset1.TransitionsTo(reset0).When(falseParam.IsFalse());
        }

        private void CreateDropLayer()
        {
            var layer = NewLayer("Drop");
            var modeParam = layer.IntParameter(ParameterPrefix + "PositionMode");
            var loadPinParam = layer.IntParameter(ParameterPrefix + "LoadPin");

            var unstabilized = HierarchyUtility.PathToObject(LocalRoot, "Transform/WorldFixed/Unstabilized");
            var position = unstabilized.GetComponent<VRCPositionConstraint>();
            var rotation = unstabilized.GetComponent<VRCRotationConstraint>();
            var parent = unstabilized.GetComponent<VRCParentConstraint>();

            var remote = layer.NewState("Remote");
            var neutral = layer.NewState("Neutral")
                .DrivingLocally()
                .Drives(loadPinParam, 0)
                .WithAnimation(_aac.NewClip()
                    .TogglingComponent(position, true)
                    .TogglingComponent(rotation, true)
                    .TogglingComponent(parent, false)
                    .Animating(clip =>
                    {
                        for (var i = 0; i < NumPins; ++i)
                        {
                            clip.Animates(parent, ConstraintWeightProp(parent, i)).WithOneFrame(0.0f);
                        }
                    }));
            var drop = layer.NewState("Drop")
                .DrivingLocally()
                .Drives(loadPinParam, 0)
                .WithAnimation(_aac.NewClip()
                    .TogglingComponent(position, false)
                    .TogglingComponent(rotation, false)
                    .TogglingComponent(parent, false)
                    .Animating(clip =>
                    {
                        for (var i = 0; i < NumPins; ++i)
                        {
                            clip.Animates(parent, ConstraintWeightProp(parent, i)).WithOneFrame(0.0f);
                        }
                    }));
            var pins = new AacFlState[NumPins];
            for (var i = 0; i < NumPins; ++i)
            {
                pins[i] = layer.NewState($"Pin{i + 1}")
                    .DrivingLocally()
                    .Drives(loadPinParam, 0)
                    .WithAnimation(_aac.NewClip()
                        .TogglingComponent(position, false)
                        .TogglingComponent(rotation, false)
                        .TogglingComponent(parent, true)
                        .Animating(clip =>
                        {
                            for (var j = 0; j < NumPins; ++j)
                            {
                                clip.Animates(parent, ConstraintWeightProp(parent, j))
                                    .WithOneFrame(i == j ? 1.0f : 0.0f);
                            }
                        }));
            }

            remote.TransitionsTo(neutral).When(layer.Av3().ItIsLocal());
            neutral.TransitionsTo(drop)
                .When(modeParam.IsEqualTo((int) PositionMode.Drop))
                .And(loadPinParam.IsEqualTo(0));
            drop.TransitionsTo(neutral)
                .When(modeParam.IsNotEqualTo((int) PositionMode.Drop));
            for (var i = 0; i < NumPins; ++i)
            {
                var existPinParam = layer.IntParameter(ParameterPrefix + "ExistPin" + (i + 1));
                neutral.TransitionsTo(pins[i])
                    .When(modeParam.IsEqualTo((int) PositionMode.Drop))
                    .And(loadPinParam.IsEqualTo(i + 1))
                    .And(existPinParam.IsNotEqualTo(0));
                neutral.TransitionsTo(neutral)
                    .WithTransitionToSelf()
                    .When(modeParam.IsEqualTo((int) PositionMode.Neutral))
                    .And(loadPinParam.IsEqualTo(i + 1))
                    .And(existPinParam.IsEqualTo(0));
                drop.TransitionsTo(pins[i])
                    .When(modeParam.IsEqualTo((int) PositionMode.Drop))
                    .And(loadPinParam.IsEqualTo(i + 1))
                    .And(existPinParam.IsNotEqualTo(0));
                drop.TransitionsTo(drop)
                    .WithTransitionToSelf()
                    .When(modeParam.IsEqualTo((int) PositionMode.Drop))
                    .And(loadPinParam.IsEqualTo(i + 1))
                    .And(existPinParam.IsEqualTo(0));
                pins[i].TransitionsTo(neutral)
                    .When(modeParam.IsNotEqualTo((int) PositionMode.Drop));
                pins[i].TransitionsTo(drop)
                    .When(modeParam.IsEqualTo((int) PositionMode.Drop));
            }
        }

        private void CreateStorePinLayer()
        {
            var layer = NewLayer("StorePin");
            var storePinParam = layer.IntParameter(ParameterPrefix + "StorePin");

            var constraints = new VRCParentConstraint[NumPins];
            var existPinParams = new AacFlIntParameter[NumPins];
            for (var i = 0; i < NumPins; ++i)
            {
                constraints[i] = HierarchyUtility
                    .PathToObject(LocalRoot, $"Transform/Pins/{i + 1}")
                    .GetComponent<VRCParentConstraint>();
                existPinParams[i] = layer.IntParameter(ParameterPrefix + $"ExistPin{i + 1}");
            }

            var remote = layer.NewState("Remote");
            var neutral = layer.NewState("Neutral").WithAnimation(_aac.NewClip().Animating(clip =>
                {
                    clip.Animates(constraints, "FreezeToWorld").WithOneFrame(1.0f);
                }));
            var pins = new AacFlState[NumPins];
            for (var i = 0; i < NumPins; ++i)
            {
                var clip = _aac.NewClip().Animating(clip =>
                {
                    for (var j = 0; j < NumPins; ++j)
                    {
                        clip.Animates(constraints[j], "FreezeToWorld").WithOneFrame(i == j ? 0.0f : 1.0f);
                    }
                });
                pins[i] = layer.NewState($"{i + 1}")
                    .DrivingLocally()
                    .Drives(storePinParam, 0)
                    .Drives(existPinParams[i], 1)
                    .WithAnimation(clip);
            }

            remote.TransitionsTo(neutral).When(layer.Av3().ItIsLocal());
            for (var i = 0; i < NumPins; ++i)
            {
                neutral.TransitionsTo(pins[i]).When(storePinParam.IsEqualTo(i + 1));
                pins[i].TransitionsTo(neutral).When(storePinParam.IsEqualTo(0));
            }
        }

        private void CreateRepositionLayer(AacFlLayer layer)
        {
            var modeParam = layer.FloatParameter(ParameterPrefix + "PositionModeF");

            var sourceOrigin = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/Reposition/SourceOrigin")
                .GetComponent<VRCParentConstraint>();
            var targetOrigin = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/Reposition/TargetOrigin")
                .GetComponent<VRCParentConstraint>();

            var neutral = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(sourceOrigin, "FreezeToWorld").WithOneFrame(0.0f);
                clip.Animates(targetOrigin, "FreezeToWorld").WithOneFrame(0.0f);
            });
            var reposition = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(sourceOrigin, "FreezeToWorld").WithOneFrame(1.0f);
                clip.Animates(targetOrigin, "FreezeToWorld").WithOneFrame(1.0f);
            });

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, modeParam)
                .AddChild((int)PositionMode.Neutral, neutral)
                .AddChild((int)PositionMode.Drop, neutral)
                .AddChild((int)PositionMode.Reposition, reposition)
                .AddChild((int)PositionMode.Drone, neutral)
                .AddChild((int)PositionMode.QuickSelfie, neutral)
                .Motion);
        }

        private void CreateRepositionScaleLayer(AacFlLayer layer)
        {
            var repositionScales = new[] {1.0f, 3.0f, 10.0f, 30.0f};
            var modeParam = layer.FloatParameter(ParameterPrefix + "PositionModeF");
            var scaleParam = layer.FloatParameter(ParameterPrefix + "RepositionScaleF");

            var control = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/Reposition/TargetOrigin/RepositionScaler");

            var neutral = _aac.NewClip()
                .Scaling(new[]{control}, new Vector3(1.0f, 1.0f, 1.0f));
            var repositions = repositionScales
                .Select(scale => _aac.NewClip()
                    .Scaling(new[] { control }, new Vector3(scale, scale, scale)))
                .ToArray();

            var repositionTree = new BlendTreeEditor(_aac, scaleParam);
            for (var i = 0; i < repositions.Length; ++i)
            {
                repositionTree.AddChild(i, repositions[i]);
            }

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, modeParam)
                .AddChild((int)PositionMode.Neutral, neutral)
                .AddChild((int)PositionMode.Drop, neutral)
                .AddChild((int)PositionMode.Reposition, repositionTree)
                .AddChild((int)PositionMode.Drone, neutral)
                .AddChild((int)PositionMode.QuickSelfie, neutral)
                .Motion);
        }

        private void CreateDroneLayer(AacFlLayer layer)
        {
            var modeParam = layer.FloatParameter(ParameterPrefix + "PositionModeF");

            var enabler = HierarchyUtility.PathToObject(LocalRoot, "Transform/Controller/Enabler");
            var controllerPosition = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/Controller")
                .GetComponent<VRCPositionConstraint>();
            var controllerRotation = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/Controller")
                .GetComponent<VRCRotationConstraint>();
            var accumulator = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/WorldFixed/Accumulator")
                .GetComponent<VRCParentConstraint>();

            var neutral = _aac.NewClip()
                .Toggling(enabler, false)
                .Animating(clip =>
                {
                    clip.Animates(controllerPosition, "FreezeToWorld").WithOneFrame(0.0f);
                    clip.Animates(controllerRotation, "FreezeToWorld").WithOneFrame(0.0f);
                    clip.Animates(accumulator, ConstraintWeightProp(accumulator, 0)).WithOneFrame(1.0f);
                    clip.Animates(accumulator, ConstraintWeightProp(accumulator, 1)).WithOneFrame(0.0f);
                });
            var drone = _aac.NewClip()
                .Toggling(enabler, true)
                .Animating(clip =>
                {
                    clip.Animates(controllerPosition, "FreezeToWorld").WithOneFrame(1.0f);
                    clip.Animates(controllerRotation, "FreezeToWorld").WithOneFrame(1.0f);
                    clip.Animates(accumulator, ConstraintWeightProp(accumulator, 0)).WithOneFrame(0.0f);
                    clip.Animates(accumulator, ConstraintWeightProp(accumulator, 1)).WithOneFrame(1.0f);
                });

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, modeParam)
                .AddChild((int)PositionMode.Neutral, neutral)
                .AddChild((int)PositionMode.Drop, neutral)
                .AddChild((int)PositionMode.Reposition, neutral)
                .AddChild((int)PositionMode.Drone, drone)
                .AddChild((int)PositionMode.QuickSelfie, neutral)
                .Motion);
        }

        private void CreateDroneSpeedLayer(AacFlLayer layer)
        {
            var modeParam = layer.FloatParameter(ParameterPrefix + "PositionModeF");
            var deltaParam = layer.FloatParameter(ParameterPrefix + "Delta");

            var compensation = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/WorldFixed/Accumulator/DroneSpeed/Compensation")
                .transform;

            var scaleMinClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(compensation, "m_LocalScale.x").WithOneFrame(0.0f);
                clip.Animates(compensation, "m_LocalScale.y").WithOneFrame(0.0f);
                clip.Animates(compensation, "m_LocalScale.z").WithOneFrame(0.0f);
            });
            var scaleMaxClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(compensation, "m_LocalScale.x").WithOneFrame(1.0f);
                clip.Animates(compensation, "m_LocalScale.y").WithOneFrame(1.0f);
                clip.Animates(compensation, "m_LocalScale.z").WithOneFrame(1.0f);
            });

            var neutral = _aac.NewClip().Scaling(new[] {compensation.gameObject}, Vector3.one);
            var scale = new BlendTreeEditor(_aac, deltaParam)
                .AddChild(0.0f, scaleMinClip)
                .AddChild(1.0f, scaleMaxClip)
                .Motion;

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, modeParam)
                .AddChild((int)PositionMode.Neutral, neutral)
                .AddChild((int)PositionMode.Drop, neutral)
                .AddChild((int)PositionMode.Reposition, neutral)
                .AddChild((int)PositionMode.Drone, scale)
                .AddChild((int)PositionMode.QuickSelfie, neutral)
                .Motion);
        }

        private void CreateDroneYawLayer(AacFlLayer layer)
        {
            var modeParam = layer.FloatParameter(ParameterPrefix + "PositionModeF");
            var xParam = layer.FloatParameter(ParameterPrefix + "X");
            var deltaParam = layer.FloatParameter(ParameterPrefix + "Delta");

            var controller = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/WorldFixed/Accumulator/DroneSpeed/Compensation/Controller")
                .transform;

            var zeroClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(controller, "localRotation.x").WithOneFrame(0.0f);
                clip.Animates(controller, "localRotation.y").WithOneFrame(0.0f);
                clip.Animates(controller, "localRotation.z").WithOneFrame(0.0f);
                clip.Animates(controller, "localRotation.w").WithOneFrame(1.0f);
            }).Clip;
            // VirtualLens2/Core/Animations/Placeholders/DroneYaw*.anim
            var posClip = AssetUtility.LoadAssetByGUID<AnimationClip>("b4050538ebd7d8e458e33ca45dbd3843");
            var negClip = AssetUtility.LoadAssetByGUID<AnimationClip>("952ad7fdd2030264d9ade8b34e56ce8c");

            BlendTree CreateTree(string deltaParamName)
            {
                var posTree = _aac.NewBlendTreeAsRaw();
                posTree.hideFlags = HideFlags.HideInHierarchy;
                posTree.blendParameter = deltaParamName;
                posTree.blendType = BlendTreeType.Simple1D;
                posTree.useAutomaticThresholds = false;
                posTree.minThreshold = 0.0f;
                posTree.maxThreshold = 0.1f;
                posTree.AddChild(zeroClip, 0.0f);
                posTree.AddChild(posClip, 0.1f);

                var negTree = _aac.NewBlendTreeAsRaw();
                negTree.hideFlags = HideFlags.HideInHierarchy;
                negTree.blendParameter = deltaParamName;
                negTree.blendType = BlendTreeType.Simple1D;
                negTree.useAutomaticThresholds = false;
                negTree.minThreshold = 0.0f;
                negTree.maxThreshold = 0.1f;
                negTree.AddChild(zeroClip, 0.0f);
                negTree.AddChild(negClip, 0.1f);

                var tree = _aac.NewBlendTreeAsRaw();
                tree.hideFlags = HideFlags.HideInHierarchy;
                tree.blendParameter = xParam.Name;
                tree.blendType = BlendTreeType.Simple1D;
                tree.useAutomaticThresholds = false;
                tree.minThreshold = -1.0f;
                tree.maxThreshold = 1.0f;
                tree.AddChild(negTree, -1.0f);
                tree.AddChild(zeroClip, -0.1f);
                tree.AddChild(zeroClip, 0.1f);
                tree.AddChild(posTree, 1.0f);
                return tree;
            }

            var neutral = zeroClip;
            var scale = CreateTree(deltaParam.Name);

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, modeParam)
                .AddChild((int)PositionMode.Neutral, neutral)
                .AddChild((int)PositionMode.Drop, neutral)
                .AddChild((int)PositionMode.Reposition, neutral)
                .AddChild((int)PositionMode.Drone, scale)
                .AddChild((int)PositionMode.QuickSelfie, neutral)
                .Motion);
        }

        private void CreateDroneQuickTurnLayer()
        {
            var lowerThreshold = 0.1f;
            var upperThreshold = 0.9f;
            var timeThreshold = 0.2f;
            var turnTime = 0.5f;

            var layer = NewLayer("QuickTurn");
            var falseParam = layer.BoolParameter(ParameterPrefix + "False");
            var modeParam = layer.IntParameter(ParameterPrefix + "PositionMode");
            var quickTurnParam = layer.IntParameter(ParameterPrefix + "DroneQuickTurn");
            var xParam = layer.FloatParameter(ParameterPrefix + "X");

            var follower = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/QuickTurn")
                .GetComponent<VRCRotationConstraint>();
            var controller = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/WorldFixed/Accumulator/DroneSpeed/Compensation/Controller/QuickTurn")
                .GetComponent<VRCRotationConstraint>();

            var followClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(follower, "FreezeToWorld").WithOneFrame(0.0f);
                clip.Animates(controller, "GlobalWeight").WithOneFrame(0.0f);
            });
            var fixClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(follower, "FreezeToWorld").WithOneFrame(1.0f);
                clip.Animates(controller, "GlobalWeight").WithFixedSeconds(timeThreshold, 0.0f);
            });
            var turnClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(follower, "FreezeToWorld").WithOneFrame(1.0f);
                clip.Animates(controller, "GlobalWeight").WithFixedSeconds(turnTime, 0.5f);
            });
            var submitClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(follower, "FreezeToWorld").WithOneFrame(1.0f);
                clip.Animates(controller, "GlobalWeight").WithOneFrame(1.0f);
            });

            var remote = layer.NewState("Remote");
            var invalid = layer.NewState("Invalid").WithAnimation(fixClip);
            var neutral = layer.NewState("Neutral").WithAnimation(followClip);
            var l0 = layer.NewState("L0").WithAnimation(fixClip);
            var l1 = layer.NewState("L1").WithAnimation(fixClip);
            var l2 = layer.NewState("L2").WithAnimation(fixClip);
            var l3 = layer.NewState("L3").WithAnimation(fixClip);
            var l4 = layer.NewState("L4").WithAnimation(turnClip);
            var l5 = layer.NewState("L5").WithAnimation(submitClip);
            var r0 = layer.NewState("R0").WithAnimation(fixClip);
            var r1 = layer.NewState("R1").WithAnimation(fixClip);
            var r2 = layer.NewState("R2").WithAnimation(fixClip);
            var r3 = layer.NewState("R3").WithAnimation(fixClip);
            var r4 = layer.NewState("R4").WithAnimation(turnClip);
            var r5 = layer.NewState("R5").WithAnimation(submitClip);

            remote.TransitionsTo(invalid)
                .When(layer.Av3().ItIsLocal());

            invalid.TransitionsTo(neutral)
                .When(modeParam.IsEqualTo((int)PositionMode.Drone))
                .And(quickTurnParam.IsNotEqualTo(0))
                .And(xParam.IsGreaterThan(-lowerThreshold))
                .And(xParam.IsLessThan(lowerThreshold));

            neutral.TransitionsTo(l0)
                .When(modeParam.IsEqualTo((int)PositionMode.Drone))
                .And(quickTurnParam.IsNotEqualTo(0))
                .And(xParam.IsLessThan(-lowerThreshold));
            neutral.TransitionsTo(r0)
                .When(modeParam.IsEqualTo((int)PositionMode.Drone))
                .And(quickTurnParam.IsNotEqualTo(0))
                .And(xParam.IsGreaterThan(lowerThreshold));

            void SetFailTransitions(AacFlState state)
            {
                state.TransitionsTo(invalid).AfterAnimationFinishes();
                state.TransitionsTo(invalid)
                    .When(modeParam.IsNotEqualTo((int)PositionMode.Drone))
                    .Or()
                    .When(quickTurnParam.IsEqualTo(0));
            }

            SetFailTransitions(l0);
            SetFailTransitions(r0);
            l0.TransitionsTo(l1).When(xParam.IsLessThan(-upperThreshold));
            r0.TransitionsTo(r1).When(xParam.IsGreaterThan(upperThreshold));

            SetFailTransitions(l1);
            SetFailTransitions(r1);
            l1.TransitionsTo(l2)
                .When(xParam.IsGreaterThan(-lowerThreshold))
                .And(xParam.IsLessThan(lowerThreshold));
            r1.TransitionsTo(r2)
                .When(xParam.IsGreaterThan(-lowerThreshold))
                .And(xParam.IsLessThan(lowerThreshold));

            SetFailTransitions(l2);
            SetFailTransitions(r2);
            l2.TransitionsTo(l3).When(xParam.IsLessThan(-upperThreshold));
            r2.TransitionsTo(r3).When(xParam.IsGreaterThan(upperThreshold));

            SetFailTransitions(l3);
            SetFailTransitions(r3);
            l3.TransitionsTo(l4)
                .When(xParam.IsGreaterThan(-lowerThreshold))
                .And(xParam.IsLessThan(lowerThreshold));
            r3.TransitionsTo(r4)
                .When(xParam.IsGreaterThan(-lowerThreshold))
                .And(xParam.IsLessThan(lowerThreshold));

            l4.TransitionsTo(invalid)
                .When(modeParam.IsNotEqualTo((int)PositionMode.Drone))
                .Or()
                .When(quickTurnParam.IsEqualTo(0));
            r4.TransitionsTo(invalid)
                .When(modeParam.IsNotEqualTo((int)PositionMode.Drone))
                .Or()
                .When(quickTurnParam.IsEqualTo(0));
            l4.TransitionsTo(l5).AfterAnimationFinishes();
            r4.TransitionsTo(r5).AfterAnimationFinishes();

            l5.TransitionsTo(invalid).When(falseParam.IsFalse());
            r5.TransitionsTo(invalid).When(falseParam.IsFalse());
        }

        private void CreatePoseSourceLayer(AacFlLayer layer)
        {
            var modeParam = layer.FloatParameter(ParameterPrefix + "PositionModeF");
            var stabilizerParam = layer.FloatParameter(ParameterPrefix + "StabilizerF");

            var constraint = PlaceholderObject("CameraNonPreviewRoot").GetComponent<VRCParentConstraint>();

            var pickup = _aac.NewClip().Animating(
                clip => clip.Animates(constraint, "GlobalWeight").WithOneFrame(0.0f));
            var release = _aac.NewClip().Animating(
                clip => clip.Animates(constraint, "GlobalWeight").WithOneFrame(1.0f));

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, modeParam)
                .AddChild((int)PositionMode.Neutral, new BlendTreeEditor(_aac, stabilizerParam)
                    .AddChild(0.0f, pickup)
                    .AddChild(1.0f, release))
                .AddChild((int)PositionMode.Drop, release)
                .AddChild((int)PositionMode.Reposition, release)
                .AddChild((int)PositionMode.Drone, release)
                .AddChild((int)PositionMode.QuickSelfie, release)
                .Motion);
        }

        private void CreateAutoLevelerLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "AutoLevelerF");
            var detector0 = layer.FloatParameter(ParameterPrefix + "LevelDetector0_Angle");
            var detector1 = layer.FloatParameter(ParameterPrefix + "LevelDetector1_Angle");

            var lookAt = HierarchyUtility
                .PathToObject(LocalRoot, "Capture/Camera")
                .GetComponent<VRCLookAtConstraint>();

            var disable = _aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(lookAt, "GlobalWeight").WithOneFrame(0.0f);
                    clip.Animates(lookAt, "Roll").WithOneFrame(0.0f);
                });
            var horizontal = _aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(lookAt, "GlobalWeight").WithOneFrame(1.0f);
                    clip.Animates(lookAt, "Roll").WithOneFrame(0.0f);
                });
            var vertical = _aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(lookAt, "GlobalWeight").WithOneFrame(1.0f);
                    clip.Animates(lookAt, "Roll").WithOneFrame(90.0f);
                });

            var snapDeg = 30;
            var eps = 1e-6f;
            var autoPositive = new BlendTreeEditor(_aac, detector0);
            var autoNegative = new BlendTreeEditor(_aac, detector0);
            for (var d = 0; d <= 180; d += snapDeg)
            {
                float angle = d;
                var positive = _aac.NewClip()
                    .Animating(clip =>
                    {
                        clip.Animates(lookAt, "GlobalWeight").WithOneFrame(1.0f);
                        clip.Animates(lookAt, "Roll").WithOneFrame(angle);
                    });
                var negative = _aac.NewClip()
                    .Animating(clip =>
                    {
                        clip.Animates(lookAt, "GlobalWeight").WithOneFrame(1.0f);
                        clip.Animates(lookAt, "Roll").WithOneFrame(-angle);
                    });
                var threshMin = (d - snapDeg / 2.0f) / 180.0f + eps;
                var threshMax = (d + snapDeg / 2.0f) / 180.0f - eps;
                autoPositive.AddChild(threshMin, positive);
                autoPositive.AddChild(threshMax, positive);
                autoNegative.AddChild(threshMin, negative);
                autoNegative.AddChild(threshMax, negative);
            }
            var auto = new BlendTreeEditor(_aac, detector1)
                .AddChild(0.5f - eps, autoPositive)
                .AddChild(0.5f + eps, autoNegative);

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, disable)
                .AddChild(1.0f, horizontal)
                .AddChild(2.0f, vertical)
                .AddChild(3.0f, auto)
                .Motion);
        }

        private void CreateQuickSelfieDistanceLayer(AacFlLayer layer)
        {
            var maxDistance = 10.0f;
            var distanceParam = layer.FloatParameter(ParameterPrefix + "QuickSelfieDistance");

            var source = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/QuickSelfie/QuickSelfieSource")
                .transform;

            var minClip = _aac.NewClip().Animating(
                clip => clip.Animates(source, "localPosition.z").WithOneFrame(0.0f));
            var maxClip = _aac.NewClip().Animating(
                clip => clip.Animates(source, "localPosition.z").WithOneFrame(maxDistance));

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, distanceParam)
                .AddChild(0.0f, minClip)
                .AddChild(1.0f, maxClip)
                .Motion);
        }

        private void CreateExternalLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "ExternalPoseF");

            var capture = HierarchyUtility
                .PathToObject(LocalRoot, "Capture")
                .GetComponent<VRCParentConstraint>();

            var disable = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(capture, ConstraintWeightProp(capture, 0)).WithOneFrame(1.0f);
                clip.Animates(capture, ConstraintWeightProp(capture, 1)).WithOneFrame(0.0f);
            });
            var enable = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(capture, ConstraintWeightProp(capture, 0)).WithOneFrame(0.0f);
                clip.Animates(capture, ConstraintWeightProp(capture, 1)).WithOneFrame(1.0f);
            });

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, disable)
                .AddChild(1.0f, enable)
                .Motion);
        }

        private void CreateGizmoLayer(AacFlLayer layer)
        {
            var controlParam = layer.FloatParameter(ParameterPrefix + "ControlF");
            var modeParam = layer.FloatParameter(ParameterPrefix + "PositionModeF");

            var gizmoConstraint = HierarchyUtility
                .PathToObject(LocalRoot, "Transform/Gizmo")
                .GetComponent<VRCParentConstraint>();
            var gizmoMesh = HierarchyUtility.PathToObject(LocalRoot, "Transform/Gizmo/Gizmo");

            var disable = _aac.NewClip()
                .Toggling(gizmoMesh, false)
                .Animating(clip =>
                {
                    clip.Animates(gizmoConstraint, ConstraintWeightProp(gizmoConstraint, 0)).WithOneFrame(1.0f);
                    clip.Animates(gizmoConstraint, ConstraintWeightProp(gizmoConstraint, 1)).WithOneFrame(0.0f);
                });
            var drone = _aac.NewClip()
                .Toggling(gizmoMesh, true)
                .Animating(clip =>
                {
                    clip.Animates(gizmoConstraint, ConstraintWeightProp(gizmoConstraint, 0)).WithOneFrame(1.0f);
                    clip.Animates(gizmoConstraint, ConstraintWeightProp(gizmoConstraint, 1)).WithOneFrame(0.0f);
                });
            var reposition = _aac.NewClip()
                .Toggling(gizmoMesh, true)
                .Animating(clip =>
                {
                    clip.Animates(gizmoConstraint, ConstraintWeightProp(gizmoConstraint, 0)).WithOneFrame(0.0f);
                    clip.Animates(gizmoConstraint, ConstraintWeightProp(gizmoConstraint, 1)).WithOneFrame(1.0f);
                });

            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, modeParam)
                .AddChild((int)PositionMode.Neutral, disable)
                .AddChild((int)PositionMode.Drop, new BlendTreeEditor(_aac, controlParam)
                    .AddChild((int)MenuTrigger.Reposition - 1, disable)
                    .AddChild((int)MenuTrigger.Reposition, reposition)
                    .AddChild((int)MenuTrigger.Reposition + 1, disable))
                .AddChild((int)PositionMode.Reposition, disable)
                .AddChild((int)PositionMode.Drone, drone)
                .AddChild((int)PositionMode.QuickSelfie, disable)
                .Motion);
        }

        #endregion

        #region Focus Control

        private void CreateAFModeLayer(AacFlLayer layer)
        {
            var modeParam = layer.FloatParameter(ParameterPrefix + "AFModeF");
            var distanceParam = layer.FloatParameter(ParameterPrefix + "Distance");

            var faceDetector = HierarchyUtility.PathToObject(LocalRoot, "Compute/FaceFocusCompute");
            var avatarDepth = HierarchyUtility.PathToObject(LocalRoot, "Capture/Camera/AvatarDepth");
            var selfieDetector = HierarchyUtility.PathToObject(LocalRoot, "Capture/Camera/SelfieDetector");
            var selfieMarker = PlaceholderObject("SelfieMarker");
            var stateUpdater = HierarchyUtility
                .PathToObject(LocalRoot, "Compute/StateUpdater/Quad")
                .GetComponent<MeshRenderer>();

            var point = _aac.NewClip()
                .Toggling(faceDetector, false)
                .Toggling(avatarDepth, false)
                .Toggling(selfieDetector, false)
                .Toggling(selfieMarker, false)
                .Animating(clip =>
                {
                    clip.Animates(stateUpdater, "material._AFMode").WithOneFrame(0);
                    clip.Animates(DisplayRenderer, "material._AFMode").WithOneFrame(0);
                });
            var face = _aac.NewClip()
                .Toggling(faceDetector, true)
                .Toggling(avatarDepth, true)
                .Toggling(selfieDetector, false)
                .Toggling(selfieMarker, false)
                .Animating(clip =>
                {
                    clip.Animates(stateUpdater, "material._AFMode").WithOneFrame(1);
                    clip.Animates(DisplayRenderer, "material._AFMode").WithOneFrame(1);
                });
            var selfie = _aac.NewClip()
                .Toggling(faceDetector, false)
                .Toggling(avatarDepth, false)
                .Toggling(selfieDetector, true)
                .Toggling(selfieMarker, true)
                .Animating(clip =>
                {
                    clip.Animates(stateUpdater, "material._AFMode").WithOneFrame(2);
                    clip.Animates(DisplayRenderer, "material._AFMode").WithOneFrame(2);
                });
            var manual = _aac.NewClip()
                .Toggling(faceDetector, false)
                .Toggling(avatarDepth, false)
                .Toggling(selfieDetector, false)
                .Toggling(selfieMarker, false)
                .Animating(clip =>
                {
                    clip.Animates(stateUpdater, "material._AFMode").WithOneFrame(0);
                    clip.Animates(DisplayRenderer, "material._AFMode").WithOneFrame(0);
                });

            const float eps = 1.175494351e-38f;
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, distanceParam)
                .AddChild(0.0f, new BlendTreeEditor(_aac, modeParam)
                    .AddChild(0.0f, point)
                    .AddChild(1.0f, face)
                    .AddChild(2.0f, selfie))
                .AddChild(eps, manual)
                .Motion);
        }

        private void CreateAFSpeedLayer(AacFlLayer layer)
        {
            var speeds = new[] {float.MaxValue, 16.0f, 8.0f, 2.0f};
            var param = layer.FloatParameter(ParameterPrefix + "AFSpeedF");

            var stateUpdater = HierarchyUtility
                .PathToObject(LocalRoot, "Compute/StateUpdater/Quad")
                .GetComponent<MeshRenderer>();

            var tree = new BlendTreeEditor(_aac, param);
            for (var i = 0; i < speeds.Length; ++i)
            {
                var index = i;
                tree.AddChild(i, _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._AFSpeed").WithOneFrame(speeds[index])));
            }
            _localBlendTreeChildren.Add(tree.Motion);
        }

        private void CreateTrackingSpeedLayer(AacFlLayer layer)
        {
            var speeds = new[] {float.MaxValue, 12.0f, 6.0f, 3.0f};
            var param = layer.FloatParameter(ParameterPrefix + "TrackingSpeedF");

            var faceDetector = HierarchyUtility
                .PathToObject(LocalRoot, "Compute/FaceFocusCompute/Quad")
                .GetComponent<MeshRenderer>();

            var tree = new BlendTreeEditor(_aac, param);
            for (var i = 0; i < speeds.Length; ++i)
            {
                var index = i;
                tree.AddChild(i, _aac.NewClip().Animating(clip =>
                    clip.Animates(faceDetector, "material._TrackingSpeed").WithOneFrame(speeds[index])));
            }
            _localBlendTreeChildren.Add(tree.Motion);
        }

        private void CreateFocusLockLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "FocusLockF");

            var stateUpdater = HierarchyUtility
                .PathToObject(LocalRoot, "Compute/StateUpdater/Quad")
                .GetComponent<MeshRenderer>();

            var disable = _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._FocusLock").WithOneFrame(0.0f));
            var enable = _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._FocusLock").WithOneFrame(1.0f));
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, disable)
                .AddChild(1.0f, enable)
                .Motion);
        }

        #endregion

        #region Display Settings

        private void CreateDisplaySettingsLayer(AacFlLayer layer, string name, string property, int count)
        {
            var param = layer.FloatParameter(ParameterPrefix + name + "F");
            var tree = new BlendTreeEditor(_aac, param);
            for (var i = 0; i < count; ++i)
            {
                var index = i;
                tree.AddChild(i, _aac.NewClip().Animating(clip =>
                    clip.Animates(DisplayRenderer, $"material.{property}").WithOneFrame(index)));
            }
            _localBlendTreeChildren.Add(tree.Motion);
        }

        private void CreateGridLayer(AacFlLayer layer)
        {
            CreateDisplaySettingsLayer(layer, "Grid", "_GridType", 8);
        }

        private void CreateGridOpacityLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "GridOpacity");
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, _aac.NewClip().Animating(clip => 
                    clip.Animates(DisplayRenderer, "material._GridOpacity").WithOneFrame(0.0f)))
                .AddChild(1.0f, _aac.NewClip().Animating(clip => 
                    clip.Animates(DisplayRenderer, "material._GridOpacity").WithOneFrame(1.0f)))
                .Motion);
        }

        private void CreateInformationLayer(AacFlLayer layer)
        {
            CreateDisplaySettingsLayer(layer, "Information", "_ShowInfo", 2);
        }

        private void CreateLevelLayer(AacFlLayer layer)
        {
            CreateDisplaySettingsLayer(layer, "Level", "_ShowLevel", 2);
        }

        private void CreatePeakingLayer(AacFlLayer layer)
        {
            CreateDisplaySettingsLayer(layer, "Peaking", "_PeakingMode", 3);
        }

        private void CreateQuickSelfieDisplayLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "PositionModeF");
            var touchers = PlaceholderObject("ScreenTouchers").GetComponent<MeshRenderer>();
            var hud = HierarchyUtility.PathToObject(LocalRoot, "Writer/PreviewHUD").GetComponent<MeshRenderer>();
            var disableClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(DisplayRenderer, "material._HorizontalFlip").WithOneFrame(0.0f);
                clip.Animates(touchers, "material._HorizontalFlip").WithOneFrame(0.0f);
                clip.Animates(hud, "material._HorizontalFlip").WithOneFrame(0.0f);
            });
            var enableClip = _aac.NewClip().Animating(clip =>
            {
                clip.Animates(DisplayRenderer, "material._HorizontalFlip").WithOneFrame(1.0f);
                clip.Animates(touchers, "material._HorizontalFlip").WithOneFrame(1.0f);
                clip.Animates(hud, "material._HorizontalFlip").WithOneFrame(1.0f);
            });
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild((int) PositionMode.QuickSelfie - 1, disableClip)
                .AddChild((int) PositionMode.QuickSelfie, enableClip)
                .AddChild((int) PositionMode.QuickSelfie + 1, disableClip)
                .Motion);
        }

        #endregion

        #region Advanced Settings

        private void CreateHideMeshLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "HideF");

            var mr = PlaceholderObject("HideableMeshes").GetComponent<MeshRenderer>();
            var smr = PlaceholderObject("HideableSkinnedMeshes").GetComponent<SkinnedMeshRenderer>();

            var show = _aac.NewClip()
                    .TogglingComponent(mr, true)
                    .TogglingComponent(smr, true);
            var hide = _aac.NewClip()
                    .TogglingComponent(mr, false)
                    .TogglingComponent(smr, false);
            var tree = new BlendTreeEditor(_aac, param)
                .AddChild(0, show)
                .AddChild(1, hide);
            _localBlendTreeChildren.Add(tree.Motion);
            _remoteBlendTreeChildren.Add(tree.Motion);
        }

        private void CreateMaskLayer(AacFlLayer layer)
        {
            var parameters = new[]
            {
                layer.FloatParameter(ParameterPrefix + "LocalPlayerMaskF"),
                layer.FloatParameter(ParameterPrefix + "RemotePlayerMaskF"),
                layer.FloatParameter(ParameterPrefix + "UIMaskF"),
            };
            var motions = new Motion[8];
            var resolutions = new[] { "1080p", "1440p", "2160p", "4320p" };
            for (var i = 0; i < 8; ++i)
            {
                var clip = _aac.NewClip();
                for (var j = 0; j < 8; ++j)
                {
                    var bits = $"{(j >> 0) & 1}{(j >> 1) & 1}{(j >> 2) & 1}";
                    foreach (var resolution in resolutions)
                    {
                        var rgb = HierarchyUtility.PathToObject(
                            LocalRoot, $"Capture/Camera/{resolution}/{bits}/RGB");
                        var depth = HierarchyUtility.PathToObject(
                            LocalRoot, $"Capture/Camera/{resolution}/{bits}/Depth");
                        clip.Toggling(rgb, i == j)
                            .Toggling(depth, i == j);
                    }
                }
                for (var j = 0; j < 4; ++j)
                {
                    var bits = $"{(j >> 0) & 1}{(j >> 1) & 1}";
                    var avatarDepth = HierarchyUtility.PathToObject(LocalRoot, $"Capture/Camera/AvatarDepth/{bits}");
                    clip.Toggling(avatarDepth, (i & 3) == j);
                }
                motions[i] = clip.Clip;
            }
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, parameters[2])
                .AddChild(0.0f, new BlendTreeEditor(_aac, parameters[1])
                    .AddChild(0.0f, new BlendTreeEditor(_aac, parameters[0])
                        .AddChild(0.0f, motions[0])
                        .AddChild(1.0f, motions[1]))
                    .AddChild(1.0f, new BlendTreeEditor(_aac, parameters[0])
                        .AddChild(0.0f, motions[2])
                        .AddChild(1.0f, motions[3])))
                .AddChild(1.0f, new BlendTreeEditor(_aac, parameters[1])
                    .AddChild(0.0f, new BlendTreeEditor(_aac, parameters[0])
                        .AddChild(0.0f, motions[4])
                        .AddChild(1.0f, motions[5]))
                    .AddChild(1.0f, new BlendTreeEditor(_aac, parameters[0])
                        .AddChild(0.0f, motions[6])
                        .AddChild(1.0f, motions[7])))
                .Motion);
        }

        private void CreatePreviewHUDLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "PreviewHUDF");
            var hud = HierarchyUtility.PathToObject(LocalRoot, "Writer/PreviewHUD");

            var disable = _aac.NewClip().Toggling(hud, false);
            var enable = _aac.NewClip().Toggling(hud, true);
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, disable)
                .AddChild(1.0f, enable)
                .Motion);
        }

        private void CreateFarPlaneLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "FarPlaneF");

            // VirtualLens2/Core/Animations/Placeholders/FarPlane_x[1,10,100].anim
            var clips = new[]
            {
                "218573be9fc8f3c4cab9db15b50f632e",
                "0ef0ad9869d03e6488c695ae8debf683",
                "b0f40d73030ae20429dd8544c42cdfbe"
            };

            var tree = new BlendTreeEditor(_aac, param);
            for (var i = 0; i < clips.Length; ++i)
            {
                tree.AddChild(i, AssetUtility.LoadAssetByGUID<AnimationClip>(clips[i]));
            }
            _localBlendTreeChildren.Add(tree.Motion);
        }

        private void CreateDepthEnablerLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "DepthEnablerF");
            var enabler = HierarchyUtility.PathToObject(LocalRoot, "DepthEnabler");

            var disable = _aac.NewClip()
                .Toggling(enabler, false)
                .Animating(clip => clip
                    .Animates(DisplayRenderer, "material._DepthEnabler")
                    .WithOneFrame(0.0f));
            var enable = _aac.NewClip()
                .Toggling(enabler, true)
                .Animating(clip => clip
                    .Animates(DisplayRenderer, "material._DepthEnabler")
                    .WithOneFrame(1.0f));
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, disable)
                .AddChild(1.0f, enable)
                .Motion);
        }

        private void CreateDepthCleanerLayer(AacFlLayer layer)
        {
            var param = layer.FloatParameter(ParameterPrefix + "DepthCleanerF");
            var renderers = HierarchyUtility
                .PathToObject(LocalRoot, "Writer/DepthCleaners")
                .GetComponentsInChildren<MeshRenderer>(true);

            var disable = _aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(renderers, "material._EnableShadowCaster").WithOneFrame(0.0f);
                    clip.Animates(renderers, "material._ShadowCasterDepth").WithOneFrame(0.0f);
                });
            var nearest = _aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(renderers, "material._EnableShadowCaster").WithOneFrame(1.0f);
                    clip.Animates(renderers, "material._ShadowCasterDepth").WithOneFrame(0.0f);
                });
            var farthest = _aac.NewClip()
                .Animating(clip =>
                {
                    clip.Animates(renderers, "material._EnableShadowCaster").WithOneFrame(1.0f);
                    clip.Animates(renderers, "material._ShadowCasterDepth").WithOneFrame(1.0f);
                });
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, disable)
                .AddChild(1.0f, nearest)
                .AddChild(2.0f, farthest)
                .Motion);
        }

        private void CreateTouchOverrideLayer(AacFlLayer layer)
        {
            var enableParam = layer.FloatParameter(ParameterPrefix + "TouchOverrideF");
            var xParam = layer.FloatParameter(ParameterPrefix + "TouchOverrideX");
            var yParam = layer.FloatParameter(ParameterPrefix + "TouchOverrideY");

            var stateUpdater = HierarchyUtility
                .PathToObject(LocalRoot, "Compute/StateUpdater/Quad")
                .GetComponent<MeshRenderer>();

            var disable = _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._TouchOverride").WithOneFrame(0.0f));
            var enable = _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._TouchOverride").WithOneFrame(1.0f));
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, enableParam)
                .AddChild(0.0f, disable)
                .AddChild(1.0f, enable)
                .Motion);
            
            var xMin = _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._TouchOverrideX").WithOneFrame(0.0f));
            var xMax = _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._TouchOverrideX").WithOneFrame(1.0f));
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, xParam)
                .AddChild(-1.0f, xMin)
                .AddChild(1.0f, xMax)
                .Motion);
            
            var yMin = _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._TouchOverrideY").WithOneFrame(0.0f));
            var yMax = _aac.NewClip().Animating(clip =>
                    clip.Animates(stateUpdater, "material._TouchOverrideY").WithOneFrame(1.0f));
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, yParam)
                .AddChild(-1.0f, yMin)
                .AddChild(1.0f, yMax)
                .Motion);
        }

        private void CreateAvatarScalingLayer(AacFlLayer layer)
        {
            var maxScale = Constants.MaxScaling;
            var param = layer.FloatParameter("ScaleFactor");

            var neutralClip = AssetUtility.LoadAssetByGUID<AnimationClip>("517f8b3e9b5e9ba48b82a5fac6cb2f00");
            var negativeClip = AssetUtility.LoadAssetByGUID<AnimationClip>("6c21e36a857210446b973ec421e67205");
            var positiveClip = AssetUtility.LoadAssetByGUID<AnimationClip>("ff621afe1778d064fb295ab4e051dd0e");
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, negativeClip)
                .AddChild(1.0f, neutralClip)
                .AddChild(maxScale, positiveClip)
                .Motion);
        }

        private void CreateInvAvatarScalingLayer(AacFlLayer layer)
        {
            const float maxScale = Constants.MaxScaling;
            var param = layer.FloatParameter("ScaleFactorInverse");

            var neutralClip = AssetUtility.LoadAssetByGUID<AnimationClip>("bf3b63f0b74c38643a62ee3c8aac1ab6");
            var negativeClip = AssetUtility.LoadAssetByGUID<AnimationClip>("4914a1e1c43c25244af2ce714f57271e");
            var positiveClip = AssetUtility.LoadAssetByGUID<AnimationClip>("f7be67ecd714d984297105a912e47c3a");
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, param)
                .AddChild(0.0f, negativeClip)
                .AddChild(1.0f, neutralClip)
                .AddChild(maxScale, positiveClip)
                .Motion);
        }

        private void CreateMaxBlurrinessLayer()
        {
            _localBlendTreeChildren.Add(
                AssetUtility.LoadAssetByGUID<AnimationClip>("3fa459b4565178345b1c4741517645ad"));
        }

        private void CreateResolutionLayer()
        {
            var layer = NewLayer("Resolution");
            var resolutionParam = layer.IntParameter(ParameterPrefix + "Resolution");
            var resolutionXParam = layer.IntParameter(ParameterPrefix + "Resolution X");
            var resolutionYParam = layer.IntParameter(ParameterPrefix + "Resolution Y");

            // VirtualLens2/Core/Animations/Placeholders/Resolution[1080, 1440, 2160, 4320]p.anim
            var placeholders = new[]
            {
                "5ea813845fa645a4597f227fae261fc4",
                "2a450deb2ff77fb4b92111e051e02ea6",
                "e924c23abfe26fb49a926f834bc6cd28",
                "bb6eb09c42fe35740959d7dab56a20cd"
            };

            var remote = layer.NewState("Remote");
            var local = layer.NewState("Local");
            var resolution1080 = layer.NewState("1080p")
                .WithWriteDefaultsSetTo(true)
                .Drives(resolutionXParam, 1920)
                .Drives(resolutionYParam, 1080)
                .WithAnimation(AssetUtility.LoadAssetByGUID<AnimationClip>(placeholders[0]));
            var resolution1440 = layer.NewState("1440p")
                .WithWriteDefaultsSetTo(true)
                .Drives(resolutionXParam, 2560)
                .Drives(resolutionYParam, 1440)
                .WithAnimation(AssetUtility.LoadAssetByGUID<AnimationClip>(placeholders[1]));
            var resolution2160 = layer.NewState("2160p")
                .WithWriteDefaultsSetTo(true)
                .Drives(resolutionXParam, 3840)
                .Drives(resolutionYParam, 2160)
                .WithAnimation(AssetUtility.LoadAssetByGUID<AnimationClip>(placeholders[2]));
            var resolution4320 = layer.NewState("4320p")
                .WithWriteDefaultsSetTo(true)
                .Drives(resolutionXParam, 7680)
                .Drives(resolutionYParam, 4320)
                .WithAnimation(AssetUtility.LoadAssetByGUID<AnimationClip>(placeholders[3]));

            remote.TransitionsTo(local)
                .When(layer.Av3().ItIsLocal());

            var localStates = new[] { local, resolution1080, resolution1440, resolution2160, resolution4320 };
            foreach (var from in localStates)
            {
                from.TransitionsTo(resolution1080)
                    .When(resolutionParam.IsEqualTo(0));
                from.TransitionsTo(resolution1440)
                    .When(resolutionParam.IsEqualTo(1));
                from.TransitionsTo(resolution2160)
                    .When(resolutionParam.IsEqualTo(2));
                from.TransitionsTo(resolution4320)
                    .When(resolutionParam.IsEqualTo(3));
            }
        }

        private void CreateResolutionBlendTreeLayer(AacFlLayer layer)
        {
            var floatResolutionParam = layer.FloatParameter(ParameterPrefix + "ResolutionF");

            var g1080p = HierarchyUtility.PathToObject(LocalRoot, "Capture/Camera/1080p");
            var g1440p = HierarchyUtility.PathToObject(LocalRoot, "Capture/Camera/1440p");
            var g2160p = HierarchyUtility.PathToObject(LocalRoot, "Capture/Camera/2160p");
            var g4320p = HierarchyUtility.PathToObject(LocalRoot, "Capture/Camera/4320p");

            var dofRoot = HierarchyUtility.PathToObject(LocalRoot, "Compute/DepthOfField");

            var computeCoc1080p = HierarchyUtility.PathToObject(dofRoot, "ComputeCoc/Camera1080p");
            var computeCoc1440p = HierarchyUtility.PathToObject(dofRoot, "ComputeCoc/Camera1440p");
            var computeCoc2160p = HierarchyUtility.PathToObject(dofRoot, "ComputeCoc/Camera2160p");
            var computeCoc4320p = HierarchyUtility.PathToObject(dofRoot, "ComputeCoc/Camera4320p");

            var computeTiles1080p = HierarchyUtility.PathToObject(dofRoot, "ComputeTiles/Camera1080p");
            var computeTiles1440p = HierarchyUtility.PathToObject(dofRoot, "ComputeTiles/Camera1440p");
            var computeTiles2160p = HierarchyUtility.PathToObject(dofRoot, "ComputeTiles/Camera2160p");
            var computeTiles4320p = HierarchyUtility.PathToObject(dofRoot, "ComputeTiles/Camera4320p");

            var downsample1440p = HierarchyUtility.PathToObject(dofRoot, "Downsample/Camera1440p");
            var downsample2160p = HierarchyUtility.PathToObject(dofRoot, "Downsample/Camera2160p");
            var downsample4320p = HierarchyUtility.PathToObject(dofRoot, "Downsample/Camera4320p");
            
            _localBlendTreeChildren.Add(new BlendTreeEditor(_aac, floatResolutionParam)
                .AddChild((int)InternalResolution.Resolution1080p, _aac.NewClip()
                    .Toggling(g1080p, true)
                    .Toggling(g1440p, false)
                    .Toggling(g2160p, false)
                    .Toggling(g4320p, false)
                    .Toggling(computeCoc1080p, true)
                    .Toggling(computeCoc1440p, false)
                    .Toggling(computeCoc2160p, false)
                    .Toggling(computeCoc4320p, false)
                    .Toggling(computeTiles1080p, true)
                    .Toggling(computeTiles1440p, false)
                    .Toggling(computeTiles2160p, false)
                    .Toggling(computeTiles4320p, false)
                    .Toggling(downsample1440p, false)
                    .Toggling(downsample2160p, false)
                    .Toggling(downsample4320p, false)
                    .Animating(clip => clip
                        .Animates(DisplayRenderer, "material._Resolution")
                        .WithOneFrame(0.0f)))
                .AddChild((int)InternalResolution.Resolution1440p, _aac.NewClip()
                    .Toggling(g1080p, false)
                    .Toggling(g1440p, true)
                    .Toggling(g2160p, false)
                    .Toggling(g4320p, false)
                    .Toggling(computeCoc1080p, false)
                    .Toggling(computeCoc1440p, true)
                    .Toggling(computeCoc2160p, false)
                    .Toggling(computeCoc4320p, false)
                    .Toggling(computeTiles1080p, false)
                    .Toggling(computeTiles1440p, true)
                    .Toggling(computeTiles2160p, false)
                    .Toggling(computeTiles4320p, false)
                    .Toggling(downsample1440p, true)
                    .Toggling(downsample2160p, false)
                    .Toggling(downsample4320p, false)
                    .Animating(clip => clip
                        .Animates(DisplayRenderer, "material._Resolution")
                        .WithOneFrame(1.0f)))
                .AddChild((int)InternalResolution.Resolution2160p, _aac.NewClip()
                    .Toggling(g1080p, false)
                    .Toggling(g1440p, false)
                    .Toggling(g2160p, true)
                    .Toggling(g4320p, false)
                    .Toggling(computeCoc1080p, false)
                    .Toggling(computeCoc1440p, false)
                    .Toggling(computeCoc2160p, true)
                    .Toggling(computeCoc4320p, false)
                    .Toggling(computeTiles1080p, false)
                    .Toggling(computeTiles1440p, false)
                    .Toggling(computeTiles2160p, true)
                    .Toggling(computeTiles4320p, false)
                    .Toggling(downsample1440p, false)
                    .Toggling(downsample2160p, true)
                    .Toggling(downsample4320p, false)
                    .Animating(clip => clip
                        .Animates(DisplayRenderer, "material._Resolution")
                        .WithOneFrame(2.0f)))
                .AddChild((int)InternalResolution.Resolution4320p, _aac.NewClip()
                    .Toggling(g1080p, false)
                    .Toggling(g1440p, false)
                    .Toggling(g2160p, false)
                    .Toggling(g4320p, true)
                    .Toggling(computeCoc1080p, false)
                    .Toggling(computeCoc1440p, false)
                    .Toggling(computeCoc2160p, false)
                    .Toggling(computeCoc4320p, true)
                    .Toggling(computeTiles1080p, false)
                    .Toggling(computeTiles1440p, false)
                    .Toggling(computeTiles2160p, false)
                    .Toggling(computeTiles4320p, true)
                    .Toggling(downsample1440p, false)
                    .Toggling(downsample2160p, false)
                    .Toggling(downsample4320p, true)
                    .Animating(clip => clip
                        .Animates(DisplayRenderer, "material._Resolution")
                        .WithOneFrame(3.0f)))
                .Motion);
        }

        #endregion

        #region Blend Trees

        void CreateBlendTreeLayer()
        {
            var layer = NewLayer("BlendTree");
            var oneParam = layer.FloatParameter(ParameterPrefix + "One");

            _localBlendTreeChildren.Clear();
            _remoteBlendTreeChildren.Clear();
            
            CreateDeltaTimeSmoothingLayer(layer);

            CreateApertureLayer(layer);
            CreateManualFocusLayer(layer);
            CreateExposureLayer(layer);

            CreateEnableLayer(layer);
            CreateAltMeshVisibilityLayer(layer);
            CreatePreviewMaterialLayer(layer);
            CreateLevelDetectorLayer(layer);

            CreateRepositionLayer(layer);
            CreateRepositionScaleLayer(layer);
            CreateDroneLayer(layer);
            CreateDroneSpeedLayer(layer);
            CreateDroneYawLayer(layer);
            CreatePoseSourceLayer(layer);
            CreateAutoLevelerLayer(layer);
            CreateQuickSelfieDistanceLayer(layer);
            CreateExternalLayer(layer);
            CreateGizmoLayer(layer);

            CreateAFModeLayer(layer);
            CreateAFSpeedLayer(layer);
            CreateTrackingSpeedLayer(layer);
            CreateFocusLockLayer(layer);

            CreateGridLayer(layer);
            CreateGridOpacityLayer(layer);
            CreateInformationLayer(layer);
            CreateLevelLayer(layer);
            CreatePeakingLayer(layer);
            CreateQuickSelfieDisplayLayer(layer);

            CreateHideMeshLayer(layer);
            CreateMaskLayer(layer);
            CreatePreviewHUDLayer(layer);
            CreateFarPlaneLayer(layer);
            CreateDepthEnablerLayer(layer);
            CreateDepthCleanerLayer(layer);
            CreateTouchOverrideLayer(layer);
            CreateAvatarScalingLayer(layer);
            CreateInvAvatarScalingLayer(layer);
            CreateMaxBlurrinessLayer();
            CreateResolutionBlendTreeLayer(layer);

            var localTree = _aac.NewBlendTreeAsRaw();
            localTree.hideFlags = HideFlags.HideInHierarchy;
            localTree.blendParameter = oneParam.Name;
            localTree.blendType = BlendTreeType.Direct;
            localTree.useAutomaticThresholds = false;
            localTree.minThreshold = 0.0f;
            localTree.maxThreshold = 0.0f;
            localTree.children = _localBlendTreeChildren
                .Select(motion => new ChildMotion
                {
                    directBlendParameter = oneParam.Name,
                    motion = motion,
                    timeScale = 1.0f,
                })
                .ToArray();

            var remoteTree = _aac.NewBlendTreeAsRaw();
            remoteTree.hideFlags = HideFlags.HideInHierarchy;
            remoteTree.blendParameter = oneParam.Name;
            remoteTree.blendType = BlendTreeType.Direct;
            remoteTree.useAutomaticThresholds = false;
            remoteTree.minThreshold = 0.0f;
            remoteTree.maxThreshold = 0.0f;
            remoteTree.children = _remoteBlendTreeChildren
                .Select(motion => new ChildMotion
                {
                    directBlendParameter = oneParam.Name,
                    motion = motion,
                    timeScale = 1.0f,
                })
                .ToArray();

            var remote = layer.NewState("Remote (WD On)")
                .WithAnimation(remoteTree)
                .WithWriteDefaultsSetTo(true);
            var local = layer.NewState("Local (WD On)")
                .WithAnimation(localTree)
                .WithWriteDefaultsSetTo(true);
            remote.TransitionsTo(local).When(layer.Av3().ItIsLocal());
        }

        #endregion

    }
}

#endif
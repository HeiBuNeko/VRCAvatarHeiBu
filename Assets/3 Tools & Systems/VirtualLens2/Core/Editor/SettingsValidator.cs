using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VirtualLens2.AV3EditorLib;

#if WITH_MODULAR_AVATAR
using nadena.dev.modular_avatar.core;
#endif

namespace VirtualLens2
{
    public class ValidationMessage
    {
        public MessageType Type { get; }
        public string Key { get; }
        public string[] Substitutions { get; internal set; }
        public Object[] ObjectReferences { get; internal set; }

        public ValidationMessage(MessageType type, string key)
        {
            Type = type;
            Key = key;
            Substitutions = new string[] { };
            ObjectReferences = new Object[] { };
        }
    }

    public class ValidationMessageEditor
    {
        private readonly ValidationMessage _target;
        public ValidationMessageEditor(ValidationMessage target) { _target = target; }

        public ValidationMessageEditor Substitutions(params string[] substitutions)
        {
            _target.Substitutions = substitutions;
            return this;
        }

        public ValidationMessageEditor ObjectReferences(params Object[] objects)
        {
            _target.ObjectReferences = objects;
            return this;
        }
    }

    public class ValidationMessageList : IEnumerable<ValidationMessage>
    {
        private readonly List<ValidationMessage> _messages = new List<ValidationMessage>();

        public ValidationMessageEditor Error(string key)
        {
            var message = new ValidationMessage(MessageType.Error, key);
            _messages.Add(message);
            return new ValidationMessageEditor(message);
        }

        public ValidationMessageEditor Warning(string key)
        {
            var message = new ValidationMessage(MessageType.Warning, key);
            _messages.Add(message);
            return new ValidationMessageEditor(message);
        }

        public ValidationMessageEditor Info(string key)
        {
            var message = new ValidationMessage(MessageType.Info, key);
            _messages.Add(message);
            return new ValidationMessageEditor(message);
        }

        public IEnumerator<ValidationMessage> GetEnumerator() { return _messages.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    public class SettingsValidator
    {
        private const string ParameterPrefix = Constants.ParameterPrefix;

        private readonly ValidationMessageList _messages;
        private readonly ImplementationSettings _settings;

        private bool CheckUnityVersion()
        {
            var version = Application.unityVersion.Split('.');
            if (version.Length == 0 || !int.TryParse(version[0], out var year) || year < 2019)
            {
                _messages.Error("validation.environment.legacy_unity_editor");
                return false;
            }
            return true;
        }

        private bool CheckAvatar()
        {
            var avatar = _settings.Avatar;
            if (avatar == null)
            {
                _messages.Error("validation.avatar.not_selected");
                return false;
            }
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                _messages.Error("validation.avatar.no_descriptor")
                    .ObjectReferences(avatar);
                return false;
            }
            var animator = avatar.GetComponent<Animator>();
            if (animator != null && animator.avatar != null && animator.avatar.isHuman) return true;
            _messages.Error("validation.avatar.non_humanoid")
                .ObjectReferences(avatar);
            return false;
        }

        private void CheckCameraObjects()
        {
            if (_settings.CameraRoot == null)
            {
                _messages.Error("validation.camera.root.not_selected");
            }
            if (!HierarchyUtility.IsDescendant(_settings.Avatar, _settings.CameraRoot))
            {
                _messages.Error("validation.camera.root.not_in_avatar")
                    .ObjectReferences(_settings.CameraRoot);
            }
            if (_settings.CameraNonPreviewRoot == null)
            {
                _messages.Error("validation.camera.non_preview_root.not_selected");
            }
            if (!HierarchyUtility.IsDescendant(_settings.CameraRoot, _settings.CameraNonPreviewRoot))
            {
                _messages.Error("validation.camera.non_preview_root.not_in_camera")
                    .ObjectReferences(_settings.CameraNonPreviewRoot);
            }
        }

        private void CheckMarkers()
        {
            if (!MarkerDetector.DetectOrigin(_settings.CameraRoot))
            {
                _messages.Error("validation.markers.origin_not_found");
            }
            if (!MarkerDetector.DetectPreview(_settings.CameraRoot))
            {
                _messages.Warning("validation.markers.preview_not_found");
            }
            foreach (var item in _settings.ScreenTouchers)
            {
                if (item != null && !HierarchyUtility.IsDescendant(_settings.Avatar, item))
                {
                    _messages.Error("validation.common.object_not_in_avatar")
                        .Substitutions(item.name)
                        .ObjectReferences(item);
                }
            }
            if (_settings.DroneController != null &&
                !HierarchyUtility.IsDescendant(_settings.Avatar, _settings.DroneController))
            {
                _messages.Error("validation.common.object_not_in_avatar")
                    .Substitutions(_settings.DroneController.name)
                    .ObjectReferences(_settings.DroneController);
            }
            if (_settings.RepositionOrigin != null &&
                !HierarchyUtility.IsDescendant(_settings.Avatar, _settings.RepositionOrigin))
            {
                _messages.Error("validation.common.object_not_in_avatar")
                    .Substitutions(_settings.RepositionOrigin.name)
                    .ObjectReferences(_settings.RepositionOrigin);
            }
            if (_settings.SelfieMarkerLeft != null &&
                !HierarchyUtility.IsDescendant(_settings.Avatar, _settings.SelfieMarkerLeft))
            {
                _messages.Error("validation.common.object_not_in_avatar")
                    .Substitutions(_settings.SelfieMarkerLeft.name)
                    .ObjectReferences(_settings.SelfieMarkerLeft);
            }
            if (_settings.SelfieMarkerRight != null &&
                !HierarchyUtility.IsDescendant(_settings.Avatar, _settings.SelfieMarkerRight))
            {
                _messages.Error("validation.common.object_not_in_avatar")
                    .Substitutions(_settings.SelfieMarkerRight.name)
                    .ObjectReferences(_settings.SelfieMarkerRight);
            }
        }

        private void CheckEyeBones()
        {
            var avatar = _settings.Avatar;
            var animator = avatar.GetComponent<Animator>();
            var leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
            var leftInvalid = _settings.SelfieMarkerLeft == null && leftEye == null;
            var rightInvalid = _settings.SelfieMarkerRight == null && rightEye == null;
            if (leftInvalid || rightInvalid)
            {
                _messages.Warning("validation.avatar.no_eye_bones")
                    .ObjectReferences(avatar);
            }
        }

        private void CheckPathUniqueness()
        {
            var root = _settings.Avatar.transform;

            // Construct actual parent-child relationship to care MA Bone Proxy
            var parentMap = new Dictionary<Transform, Transform>();

            void ConstructParentMap(Transform tf)
            {
                foreach (Transform child in tf)
                {
                    var parent = tf;
#if WITH_MODULAR_AVATAR
                    var boneProxy = child.GetComponent<ModularAvatarBoneProxy>();
                    if (boneProxy != null && boneProxy.target != null) { parent = boneProxy.target; }
#endif
                    parentMap.Add(child, parent);
                    ConstructParentMap(child);
                }
            }

            ConstructParentMap(root);

            // Count number of objects for each paths
            string GetTransformPath(Transform tf)
            {
                var tokens = new List<string>();
                var visited = new HashSet<Transform>();
                while (tf != null && tf != root && !visited.Contains(tf))
                {
                    visited.Add(tf);
                    tokens.Add(tf.name);
                    tf = parentMap[tf];
                }
                tokens.Reverse();
                return string.Join("/", tokens);
            }

            var counters = new Dictionary<string, List<Transform>>();
            foreach (var tf in parentMap.Keys)
            {
                var path = GetTransformPath(tf);
                if (counters.TryGetValue(path, out var counter))
                {
                    counter.Add(tf);
                }
                else
                {
                    counters[path] = new List<Transform> { tf };
                }
            }

            // Enumerate objects manipulated by VirtualLens2 animations
            var leaves = new List<Transform>();
            if (_settings.CameraRoot) { leaves.Add(_settings.CameraRoot.transform); }
            if (_settings.CameraNonPreviewRoot) { leaves.Add(_settings.CameraNonPreviewRoot.transform); }
            leaves.AddRange(_settings.HideableMeshes
                .Where(obj => obj != null)
                .Select(obj => obj.transform));
            leaves.AddRange(_settings.OptionalObjects
                .Select(elem => elem?.GameObject)
                .Where(obj => obj != null)
                .Select(obj => obj.transform));
            leaves.AddRange(_settings.ScreenTouchers
                .Where(obj => obj != null)
                .Select(obj => obj.transform));

            // Check path uniqueness
            var errors = new Dictionary<Transform, List<Transform>>();
            foreach (var leaf in leaves)
            {
                if (leaf == null) { continue; }
                if (!HierarchyUtility.IsDescendant(root, leaf)) { continue; }
                var path = GetTransformPath(leaf);
                if (counters[path].Count >= 2) { errors.Add(leaf, counters[path]); }
            }
            foreach (var elem in errors)
            {
                var path = HierarchyUtility.RelativePath(root, elem.Key);
                var listText = string.Join("\n", elem.Value
                    .Select(tf => $"- {HierarchyUtility.RelativePath(root, tf)}"));
                _messages.Error("validation.animation.not_unique_paths")
                    .Substitutions(path, listText)
                    .ObjectReferences(elem.Key);
            }
        }

        private void CheckExpressionMenu()
        {
            var descriptor = _settings.Avatar.GetComponent<VRCAvatarDescriptor>();
            var menu = descriptor.expressionsMenu;
            if (menu == null) { return; }

            const int maxN = 8;
            int n = menu.controls.Count;
            foreach (var item in menu.controls)
            {
                if (item.name == "VirtualLens2") { --n; }
                if (item.name == "VirtualLens2 Quick Calls") { --n; }
            }
            ++n;
            if (n > maxN)
            {
                _messages.Error("validation.av3.no_enough_menu_slots")
                    .ObjectReferences(menu);
            }
        }

        private void CheckExpressionParameters()
        {
            var descriptor = _settings.Avatar.GetComponent<VRCAvatarDescriptor>();
            var parameters = descriptor.expressionParameters;
            if (parameters == null) return;

            var wrapper = new VrcExpressionParametersWrapper(parameters);
            var costSum = wrapper.Parameters
                .Where(p => !p.Name.StartsWith(ParameterPrefix))
                .Sum(p => p.Cost);

            var forceSync = !VrcExpressionParametersWrapper.SupportsNotSynchronizedVariables();
            var intCost = VrcExpressionParametersWrapper.TypeCost(VRCExpressionParameters.ValueType.Int);
            var floatCost = VrcExpressionParametersWrapper.TypeCost(VRCExpressionParameters.ValueType.Float);
            costSum += intCost; // State
            if (_settings.FocalLengthSyncRemote || forceSync)
            {
                costSum += floatCost;
            }
            if (_settings.ApertureEnabled && (_settings.ApertureFNumberSyncRemote || forceSync))
            {
                costSum += floatCost;
            }
            if (_settings.ManualFocusingEnabled && (_settings.ManualFocusingDistanceSyncRemote || forceSync))
            {
                costSum += floatCost;
            }
            if (_settings.ExposureEnabled && (_settings.ExposureSyncRemote || forceSync))
            {
                costSum += floatCost;
            }
            if (_settings.DroneEnabled && forceSync)
            {
                costSum += floatCost;
            }
            if (costSum > VrcExpressionParametersWrapper.MaxParameterCost())
            {
                _messages.Error("validation.av3.no_enough_parameter_memory")
                    .ObjectReferences(parameters);
            }
        }


        private static bool IsFinite(float x) { return !(float.IsNaN(x) || float.IsInfinity(x)); }

        private static bool IsPositiveFinite(float x) { return IsFinite(x) && x > 0.0f; }

        private static bool CheckRangeMinMax(float min, float max)
        {
            if (!IsFinite(min)) { return false; }
            if (!IsFinite(max)) { return false; }
            return min < max;
        }

        private static bool CheckInRange(float x, float min, float max)
        {
            if (!IsFinite(x)) { return false; }
            return min <= x && x <= max;
        }

        private void CheckPositiveRange(string category, float min, float max, float def)
        {
            bool valid = true;
            if (!IsPositiveFinite(min))
            {
                _messages.Error($"validation.{category}.min.positiveness");
                valid = false;
            }
            if (!IsPositiveFinite(max))
            {
                _messages.Error($"validation.{category}.max.positiveness");
                valid = false;
            }
            if (valid && !CheckRangeMinMax(min, max))
            {
                _messages.Error($"validation.{category}.range_validity");
                valid = false;
            }
            if (valid && !CheckInRange(def, min, max))
            {
                _messages.Error($"validation.{category}.default_in_range");
            }
        }

        private void CheckFocalLengths()
        {
            CheckPositiveRange(
                "focal_length",
                _settings.FocalLengthMin,
                _settings.FocalLengthMax,
                _settings.FocalLengthDefault);
        }

        private void CheckAperture()
        {
            if (!_settings.ApertureEnabled) { return; }
            CheckPositiveRange(
                "aperture",
                _settings.ApertureFNumberMin,
                _settings.ApertureFNumberMax,
                _settings.ApertureFNumberDefault);
        }

        private void CheckManualFocusing()
        {
            if (!_settings.ManualFocusingEnabled) { return; }
            CheckPositiveRange(
                "manual_focusing",
                _settings.ManualFocusingDistanceMin,
                _settings.ManualFocusingDistanceMax,
                _settings.ManualFocusingDistanceMin);
        }

        private void CheckExposure()
        {
            if (!_settings.ExposureEnabled) { return; }
            bool valid = true;
            var min = -_settings.ExposureRange;
            var max = _settings.ExposureRange;
            if (!IsPositiveFinite(max))
            {
                _messages.Error("validation.exposure.range.positiveness");
                valid = false;
            }
            if (valid && !CheckInRange(_settings.ExposureDefault, min, max))
            {
                _messages.Error("validation.exposure.default_in_range");
            }
        }

        private void CheckDrone()
        {
            if (!_settings.DroneEnabled) { return; }
            if (!IsPositiveFinite(_settings.DroneLinearSpeedScale))
            {
                _messages.Error("validation.drone.speed_scale.positiveness");
            }
            if (!IsPositiveFinite(_settings.DroneLinearDeadZoneSize))
            {
                _messages.Error("validation.drone.dead_zone.positiveness");
            }
            if (!IsPositiveFinite(_settings.DroneYawSpeedScale))
            {
                _messages.Error("validation.drone.yaw_speed.positiveness");
            }
            const float yawSpeedLimit = 1500.0f;
            if (_settings.DroneYawSpeedScale >= yawSpeedLimit)
            {
                _messages.Error("validation.drone.yaw_speed.limit")
                    .Substitutions(yawSpeedLimit.ToString(CultureInfo.CurrentCulture));
            }
        }

        private void CheckClippingPlanes()
        {
            bool valid = true;
            var min = _settings.ClippingNear;
            var max = _settings.ClippingFar;
            if (!IsPositiveFinite(min))
            {
                _messages.Error("validation.clipping_planes.near.positiveness");
                valid = false;
            }
            if (!IsPositiveFinite(max))
            {
                _messages.Error("validation.clipping_planes.far.positiveness");
                valid = false;
            }
            if (valid && !CheckRangeMinMax(min, max))
            {
                _messages.Error("validation.clipping_planes.range_validity");
            }
        }

        private void CheckOptionalObjects()
        {
            var avatar = _settings.Avatar;
            foreach (var item in _settings.OptionalObjects)
            {
                var obj = item.GameObject;
                if (obj == null) { continue; }
                if (!HierarchyUtility.IsDescendant(avatar, obj))
                {
                    _messages.Error("validation.common.object_not_in_avatar")
                        .Substitutions(obj.name)
                        .ObjectReferences(obj);
                }
            }
        }

        private void CheckHideableObjects()
        {
            var avatar = _settings.Avatar;
            foreach (var obj in _settings.HideableMeshes)
            {
                if (obj == null) { continue; }
                if (!HierarchyUtility.IsDescendant(avatar, obj))
                {
                    _messages.Error("validation.common.object_not_in_avatar")
                        .Substitutions(obj.name)
                        .ObjectReferences(obj);
                }
                if (!obj.GetComponent<MeshRenderer>() && !obj.GetComponent<SkinnedMeshRenderer>())
                {
                    _messages.Warning("validation.hideables.no_mesh_renderer")
                        .Substitutions(obj.name)
                        .ObjectReferences(obj);
                }
            }
        }

        private void CheckQuickCalls()
        {
            if (_settings.QuickCalls.Count > Constants.NumQuickCalls)
            {
                _messages.Error("validation.quick_calls.too_many_items");
            }
            var minFocal = _settings.FocalLengthMin;
            var maxFocal = _settings.FocalLengthMax;
            var minAperture = _settings.ApertureFNumberMin;
            var maxAperture = _settings.ApertureFNumberMax;
            var minExposure = -_settings.ExposureRange;
            var maxExposure = _settings.ExposureRange;
            foreach (var item in _settings.QuickCalls)
            {
                if (item.Focal != null && !CheckInRange((float)item.Focal, minFocal, maxFocal))
                {
                    _messages.Error("validation.quick_calls.focal_length_in_range")
                        .Substitutions(item.Name);
                }
                if (item.Aperture != null && !CheckInRange((float)item.Aperture, minAperture, maxAperture))
                {
                    _messages.Error("validation.quick_calls.aperture_in_range")
                        .Substitutions(item.Name);
                }
                if (item.Exposure != null && !CheckInRange((float)item.Exposure, minExposure, maxExposure))
                {
                    _messages.Error("validation.quick_calls.exposure_in_range")
                        .Substitutions(item.Name);
                }
            }
        }

        private void CheckCustomGrids()
        {
            if(_settings.CustomGrids.Count > Constants.NumCustomGrids)
            {
                _messages.Error("validation.custom_grids.too_many_items");
            }
            foreach (var item in _settings.CustomGrids)
            {
                var texture = item.Texture;
                if (texture == null)
                {
                    _messages.Error("validation.custom_grids.texture_not_selected");
                    continue;
                }
                const int expectedWidth = 1280, expectedHeight = 720;
                if (texture.width != expectedWidth || texture.height != expectedHeight)
                {
                    _messages.Warning("validation.custom_grid.texture_resolution_mismatch")
                        .Substitutions(texture.name, expectedWidth.ToString(), expectedHeight.ToString())
                        .ObjectReferences(texture);
                }
                var path = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(path)) { continue; }
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { continue; }
                if (importer.npotScale != TextureImporterNPOTScale.None)
                {
                    _messages.Warning("validation.custom_grid.texture_with_pot")
                        .Substitutions(texture.name)
                        .ObjectReferences(texture);
                }
                if (!importer.DoesSourceTextureHaveAlpha())
                {
                    _messages.Warning("validation.custom_grid.texture_without_alpha")
                        .Substitutions(texture.name)
                        .ObjectReferences(texture);
                }
                if (importer.mipmapEnabled)
                {
                    _messages.Warning("validation.custom_grid.texture_has_mipmap")
                        .Substitutions(texture.name)
                        .ObjectReferences(texture);
                }
            }
        }

        private void CheckPlayableLayers()
        {
            var descriptor = _settings.Avatar.GetComponent<VRCAvatarDescriptor>();
            var layers = descriptor.baseAnimationLayers;
            var numFXLayers = layers.Count(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX);
            if (numFXLayers == 1) { return; }

            var isFX3 = layers.Length == 5 && layers[3].type == VRCAvatarDescriptor.AnimLayerType.FX;
            var isFX4 = layers.Length == 5 && layers[4].type == VRCAvatarDescriptor.AnimLayerType.FX;
            if (numFXLayers == 2 && isFX3 && isFX4)
            {
                // https://feedback.vrchat.com/sdk-bug-reports/p/sdk202009250008-switching-a-rig-from-generic-to-the-humanoid-rig-type-causes-dup
                _messages.Warning("validation.av3.multiple_fx_layers")
                    .ObjectReferences(descriptor);
            }
            else
            {
                // Unknown layer configuration
                _messages.Error("validation.av3.unknown_playable_layer_set")
                    .ObjectReferences(descriptor);
            }
        }

        private AnimatorController FindFXAnimatorController()
        {
            var descriptor = _settings.Avatar.GetComponent<VRCAvatarDescriptor>();
            foreach (var layer in descriptor.baseAnimationLayers)
            {
                if (layer.type != VRCAvatarDescriptor.AnimLayerType.FX) { continue; }
                return layer.animatorController as AnimatorController;
            }
            return null;
        }

        private void CheckWriteDefaults()
        {
            if (_settings.WriteDefaults == WriteDefaultsOverrideMode.Auto) { return; }
            var expect = _settings.WriteDefaults == WriteDefaultsOverrideMode.ForceEnable;
            var controller = FindFXAnimatorController();
            if (controller == null) { return; }
            bool valid = true;
            foreach (var layer in controller.layers)
            {
                if (layer == null) { continue; }
                if (layer.name.StartsWith("VirtualLens2 ")) { continue; }
                if (layer.stateMachine == null || layer.stateMachine.states == null) { continue; }
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name.Contains("(WD On)")) continue;
                    if (state.state.name.Contains("(WD Off)")) continue;
                    if (state.state.writeDefaultValues != expect) valid = false;
                }
            }
            if (!valid)
            {
                _messages.Warning("validation.animation.mixed_write_defaults")
                    .ObjectReferences(controller);
            }
        }

        private void CheckDuplicatedParameters()
        {
            // https://feedback.vrchat.com/avatar-30/p/bug-vrc-avatar-parameter-driver-does-not-work-when-there-are-duplicated-paramete
            var controller = FindFXAnimatorController();
            if (controller == null) { return; }

            var history = new HashSet<string>();
            foreach (var p in controller.parameters)
            {
                if (history.Contains(p.name))
                {
                    _messages.Warning("validation.animation.duplicated_parameters")
                        .Substitutions(p.name)
                        .ObjectReferences(controller);
                }
                history.Add(p.name);
            }
        }

        private bool CheckParameterUsage(Motion motion, string name)
        {
            if (motion is BlendTree tree)
            {
                if (tree.blendParameter == name) { return true; }
                if (tree.blendParameterY == name) { return true; }
                foreach (var child in tree.children)
                {
                    if (child.directBlendParameter == name) { return true; }
                    if (CheckParameterUsage(child.motion, name)) { return true; }
                }
            }
            return false;
        }

        private bool CheckParameterUsage(AnimatorState state, string name)
        {
            if (CheckParameterUsage(state.motion, name)) { return true; }
            if (state.mirrorParameterActive && state.mirrorParameter == name) { return true; }
            if (state.speedParameterActive && state.speedParameter == name) { return true; }
            if (state.timeParameterActive && state.timeParameter == name) { return true; }
            return state.transitions
                .Any(t => t.conditions.Any(c => c.parameter == name));
        }

        private bool CheckParameterUsage(AnimatorStateMachine sm, string name)
        {
            if (sm.states.Any(s => CheckParameterUsage(s.state, name))) { return true; }
            if (sm.stateMachines.Any(s => CheckParameterUsage(s.stateMachine, name))) { return true; }
            return false;
        }

        private void CheckAndLogParameterUsage(AnimatorController controller, string name)
        {
            foreach (var layer in controller.layers)
            {
                if (CheckParameterUsage(layer.stateMachine, name))
                {
                    _messages.Info("validation.animation.conflicting_parameter_usage")
                        .Substitutions(name, layer.name)
                        .ObjectReferences(controller);
                }
            }
        }

        private void CheckBuiltInParametersType()
        {
            var controller = FindFXAnimatorController();
            if (controller == null) { return; }

            var usage = new Dictionary<string, AnimatorControllerParameterType>
            {
                {"IsLocal", AnimatorControllerParameterType.Bool},
                {"VRMode", AnimatorControllerParameterType.Int},
                {"TrackingType", AnimatorControllerParameterType.Int},
                {"ScaleFactor", AnimatorControllerParameterType.Float},
                {"ScaleFactorInverse", AnimatorControllerParameterType.Float},
            };
            foreach (var p in controller.parameters)
            {
                if (!usage.ContainsKey(p.name)) { continue; }
                var expected = usage[p.name];
                var actual = p.type;
                if (actual != expected)
                {
                    _messages.Error("validation.animation.parameter_type_conflict")
                        .Substitutions(p.name, expected.ToString(), actual.ToString())
                        .ObjectReferences(controller);
                    CheckAndLogParameterUsage(controller, p.name);
                }
            }
        }

        private bool InArtifactsFolder(Object asset)
        {
            if (string.IsNullOrEmpty(_settings.ArtifactsFolder)) { return false; }
            return AssetDatabase.GetAssetPath(asset).StartsWith(_settings.ArtifactsFolder);
        }

        private bool IsMerged(AnimatorController controller)
        {
            if (controller.layers.Length == 0) { return true; }
            var baseLayer = controller.layers[0];
            if (baseLayer.stateMachine.states.Length != 0) { return true; }
            for (var i = 1; i < controller.layers.Length; ++i)
            {
                var layer = controller.layers[i];
                if (!layer.name.StartsWith(ParameterPrefix)) { return true; }
            }
            return controller.parameters
                .Any(p =>
                    p.name != "IsLocal" &&
                    p.name != "VRMode" &&
                    p.name != "TrackingType" &&
                    p.name != "ScaleFactor" &&
                    p.name != "ScaleFactorInverse" &&
                    !p.name.StartsWith(ParameterPrefix));
        }

        private bool IsMerged(VRCExpressionsMenu menu)
        {
            return menu.controls.Any(item => item.name != "VirtualLens2");
        }

        private bool IsMerged(VRCExpressionParameters parameters)
        {
            var items = parameters.parameters;
            if (items.Length < 3) { return true; }
            if (items[0].name != "VRCEmote") { return true; }
            if (items[1].name != "VRCFaceBlendH") { return true; }
            if (items[2].name != "VRCFaceBlendV") { return true; }
            for (var i = 3; i < items.Length; ++i)
            {
                if (!items[i].name.StartsWith(ParameterPrefix)) { return true; }
            }
            return false;
        }

        private void CheckAutoGeneratedObjectsInPrefabRecur(GameObject obj, Regex regex)
        {
            if (!obj) return;
            foreach (Transform transform in obj.transform)
            {
                var child = transform.gameObject;
                if (regex.IsMatch(child.name))
                {
                    var root = PrefabUtility.GetOutermostPrefabInstanceRoot(child);
                    if (root && root != child)
                    {
                        _messages.Error("validation.avatar.generated_objects_in_prefab")
                            .Substitutions(child.name, root.name)
                            .ObjectReferences(child, root);
                    }
                }
                else
                {
                    CheckAutoGeneratedObjectsInPrefabRecur(child, regex);
                }
            }
        }

        private void CheckAutoGeneratedObjectsInPrefab()
        {
            CheckAutoGeneratedObjectsInPrefabRecur(_settings.Avatar, new Regex(@"^_VirtualLens_.*$"));
        }

        private void CheckModifiedArtifacts()
        {
            var controller = FindFXAnimatorController();
            if (InArtifactsFolder(controller) && IsMerged(controller))
            {
                _messages.Warning("validation.artifacts.contains_modified_controller")
                    .ObjectReferences(controller);
            }
            var descriptor = _settings.Avatar.GetComponent<VRCAvatarDescriptor>();
            var menu = descriptor.expressionsMenu;
            if (InArtifactsFolder(menu) && IsMerged(menu))
            {
                _messages.Warning("validation.artifacts.contains_modified_expressions_menu")
                    .ObjectReferences(menu);
            }
            var parameters = descriptor.expressionParameters;
            if (InArtifactsFolder(parameters) && IsMerged(parameters))
            {
                _messages.Warning("validation.artifacts.contains_modified_expression_parameters")
                    .ObjectReferences(parameters);
            }
        }

        private void CheckOldLilycalInventory()
        {
#if WITH_OLD_LILYCAL_INVENTORY
            _messages.Error("validation.compatibility.old_lilycal_inventory");
#endif
        }

        private SettingsValidator(VirtualLensSettings raw)
        {
            _settings = new ImplementationSettings(raw);
            _messages = new ValidationMessageList();
            var isDestructive = _settings.BuildMode == BuildMode.Destructive;

            if (!CheckUnityVersion()) { return; }
            if (!CheckAvatar()) { return; }
            if (isDestructive) { CheckPlayableLayers(); }
            if (isDestructive) { CheckExpressionMenu(); }
            if (isDestructive) { CheckExpressionParameters(); }
            CheckCameraObjects();
            CheckMarkers();
            CheckEyeBones();
            CheckPathUniqueness();
            CheckFocalLengths();
            CheckAperture();
            CheckManualFocusing();
            CheckExposure();
            CheckDrone();
            CheckClippingPlanes();
            CheckOptionalObjects();
            CheckHideableObjects();
            CheckQuickCalls();
            CheckCustomGrids();
            if (isDestructive) { CheckWriteDefaults(); }
            CheckDuplicatedParameters();
            if (isDestructive) { CheckBuiltInParametersType(); }
            CheckAutoGeneratedObjectsInPrefab();
            if (isDestructive) { CheckModifiedArtifacts(); }
            CheckOldLilycalInventory();
        }

        public static ValidationMessageList Validate(VirtualLensSettings settings)
        {
            var self = new SettingsValidator(settings);
            return self._messages;
        }
    }
}

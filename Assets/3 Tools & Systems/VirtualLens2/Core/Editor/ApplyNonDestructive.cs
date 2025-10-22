#if WITH_NDMF

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VirtualLens2.AV3EditorLib;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

#if WITH_NDMF_1_3
using nadena.dev.ndmf.localization;
#endif

[assembly: ExportsPlugin(typeof(VirtualLens2.ApplyNonDestructive))]

namespace VirtualLens2
{
    public class ApplyNonDestructive : Plugin<ApplyNonDestructive>
    {
        public override string QualifiedName => "dev.logilabo.virtuallens2.apply-non-destructive";
        public override string DisplayName => "VirtualLens2";

#if WITH_NDMF_1_3
        // VirtualLens2/Core/Images/logo.png
        public override Texture2D LogoTexture =>
            AssetUtility.LoadAssetByGUID<Texture2D>("5d843ccf12924a14686317ad41d0f951");

        private class ErrorMessage : SimpleError
        {
            private readonly ValidationMessage _message;

            public override Localizer Localizer { get; }

            public override string[] TitleSubst { get; }

            public override string TitleKey => _message.Key;

            public override ErrorSeverity Severity
            {
                get
                {
                    switch (_message.Type)
                    {
                        case MessageType.Info: return ErrorSeverity.Information;
                        case MessageType.Warning: return ErrorSeverity.NonFatal;
                        default: return ErrorSeverity.Error;
                    }
                }
            }

            public ErrorMessage(VirtualLensSettings component, ValidationMessage message)
            {
                Localizer = Localization.GetNdmfLocalizer();
                TitleSubst = message.Substitutions;
                _message = message;
                _references.Add(ObjectRegistry.GetReference(component));
                foreach (var obj in message.ObjectReferences)
                {
                    _references.Add(ObjectRegistry.GetReference(obj));
                }
            }
        }
#endif

        protected override void Configure()
        {
            InPhase(BuildPhase.Generating).Run("Apply VirtualLens2", ctx =>
            {
                var avatar = ctx.AvatarRootObject;

                // Find VirtualLens object
                var components = avatar
                    .GetComponentsInChildren<VirtualLensSettings>(true)
                    .Where(c => !c.gameObject.CompareTag("EditorOnly") && c.buildMode == BuildMode.NonDestructive)
                    .ToArray();
                if (components.Length == 0) { return; }
                if (components.Length > 1)
                {
#if WITH_NDMF_1_3
                    ErrorReport.ReportError(
                        Localization.GetNdmfLocalizer(), ErrorSeverity.Error,
                        "validation.avatar.multiple_virtual_lens_settings",
                        components.Select(c => (object)c));
                    return;
#else
                    EditorUtility.DisplayDialog(
                        "VirtualLens2",
                        "Failed to apply VirtualLens2.\n" +
                        "The avatar can contain up to one VirtualLens object.",
                        "OK");
                    throw new InvalidOperationException("Multiple VirtualLens Settings are found");
#endif
                }
                var component = components[0];

                // Run migration
                SettingsMigrator.Migrate(new SerializedObject(component));

                // Validate settings
                var messages = SettingsValidator.Validate(component);
                var defaultLocalizationTable = Localization.GetDefaultLocalizationTable();
                foreach (var message in messages)
                {
                    var text = string.Format(
                        defaultLocalizationTable.GetLocalizedString(message.Key),
                        message.Substitutions.Select(s => (object)s).ToArray());
                    switch (message.Type)
                    {
                        case MessageType.Error:
                            Debug.LogError(text);
                            break;
                        case MessageType.Warning:
                            Debug.LogWarning(text);
                            break;
                        case MessageType.Info:
                            Debug.Log(text);
                            break;
                    }
#if WITH_NDMF_1_3
                    ErrorReport.ReportError(new ErrorMessage(component, message));
#endif
                }
                if (messages.Count(m => m.Type == MessageType.Error) > 0)
                {
#if WITH_NDMF_1_3
                    // Use NDMF error reporting system
                    return;
#else
                    EditorUtility.DisplayDialog(
                        "VirtualLens2",
                        "Failed to apply VirtualLens2.\n" +
                        "Please check validation report and fix problems.",
                        "OK");
                    Selection.activeObject = component;
                    throw new InvalidOperationException("Invalid VirtualLens configuration");
#endif
                }

                // Prepare abstract settings object
                var settings = new ImplementationSettings(component);

                // Generate marker objects
                GenerateMarkers(settings);

                // Apply VirtualLens2
                Applier.Apply(settings, ctx.AssetContainer);

                // Purge VirtualLens Settings
                Object.DestroyImmediate(component);

                // Remove EditorOnly Objects
                void RemoveEditorOnlyRecur(GameObject obj, bool flag)
                {
                    if (obj.CompareTag("EditorOnly")) { flag = true; }
                    foreach (Transform tf in obj.transform)
                    {
                        RemoveEditorOnlyRecur(tf.gameObject, flag);
                    }
                    if (flag) { Object.DestroyImmediate(obj); }
                }

                var root = HierarchyUtility.PathToObject(settings.Avatar, "_VirtualLens_Root");
                RemoveEditorOnlyRecur(root, false);
            });
        }

        private static void GenerateMarkers(ImplementationSettings settings)
        {
            if (settings.ScreenTouchers.All(obj => obj == null))
            {
                var markers = MarkerGenerator.GenerateScreenToucher(settings.Avatar);
                foreach (var obj in markers)
                {
                    PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }
                settings.ScreenTouchers = markers.ToImmutableList();
            }
            if (!settings.DroneController)
            {
                settings.DroneController = MarkerGenerator.GenerateDroneController(settings.Avatar);
                PrefabUtility.UnpackPrefabInstance(settings.DroneController, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }
            if (!settings.RepositionOrigin)
            {
                settings.RepositionOrigin = MarkerGenerator.GenerateRepositionOrigin(settings.Avatar);
                PrefabUtility.UnpackPrefabInstance(settings.RepositionOrigin, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }
            if (!settings.SelfieMarkerLeft || !settings.SelfieMarkerRight)
            {
                var pair = MarkerGenerator.GenerateEyeMarkers(
                    settings.Avatar,
                    !settings.SelfieMarkerLeft,
                    !settings.SelfieMarkerRight);
                foreach (var obj in pair)
                {
                    if (obj)
                    {
                        PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely,
                            InteractionMode.AutomatedAction);
                    }
                }
                if (!settings.SelfieMarkerLeft) { settings.SelfieMarkerLeft = pair[0]; }
                if (!settings.SelfieMarkerRight) { settings.SelfieMarkerRight = pair[1]; }
            }
        }
    }
}

#endif
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using nadena.dev.modular_avatar.core;
using Numeira;
using VRC.Core;

namespace io.github.azukimochi;

partial class LightLimitChangerProcessor
{
    private sealed class Configurator : IDisposable
    {
        private LightLimitChangerComponent component;
        private LightLimitChangerProcessor processor;

        private LightLimitChangerComponent defaultValueProvider;

        public Configurator(LightLimitChangerProcessor processor)
        {
            component = processor.Component;
            this.processor = processor;
            var defaultValueProviderObj = new GameObject("LLC DefaultValueProvider");
            defaultValueProviderObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            defaultValueProvider = defaultValueProviderObj.AddComponent<LightLimitChangerComponent>();

            // force serialization
            EditorUtility.CopySerializedIfDifferent(defaultValueProvider, defaultValueProvider);
        }

        public void Dispose()
        {
            if (defaultValueProvider != null)
            {
                Object.DestroyImmediate(defaultValueProvider.gameObject);
                defaultValueProvider = null;
            }
        }

        public void ConfigureSettings()
        {
            foreach(var group in Metadata.AllParameterGroups)
            {
                var settings = group.Key;
                if (!settings.GetEnable(component))
                    continue;

                var menuGroup = processor.menuRoot.GetOrAdd(settings.MenuPath, translateKey: $"category:{settings.Id}");
                menuGroup.gameObject.GetOrAddComponent<LightLimitChangerMenuInfo>().IsGroupFolder = true;
                menuGroup.Control.icon = settings.Icon;

                foreach (var parameters in group.GroupBy(x => x.Group ?? ""))
                {
                    var items = parameters.Select(x => (Info: x, Parameter: x.Get(component)));
                    var isVectorParameter = items.Any(x => x.Info.VectorField != null && x.Parameter.Enable && x.Parameter.Animation);

                    foreach (var (info, parameter) in items)
                    {
                        if (!parameter.IsAnimated)
                            if (!isVectorParameter)
                                continue;

                        if (info.Id == Metadata.ID.General.LightingControl.LightDirection.Id)
                        {
                            if (!component.General.LightingControl.AllowAdvancedLightDirectionControl)
                                goto Break;

                            GenerateLightDirectionControlBranch(info, menuGroup);

                            continue;

                        Break:
                            { }
                        }

                        GenerateBlendTreeBranch(info, parameter, menuGroup);
                    }
                }
            }
        }

        public void OverwriteMaterialSettings(ShaderProcessor processor, IEnumerable<Material> materials)
        {
            foreach (var group in Metadata.AllParameterGroups)
            {
                var settings = group.Key;
                if (!settings.GetEnable(component))
                    continue;

                foreach(var parameters in group.GroupBy(x => x.Group ?? ""))
                {
                    var isVectorParameter = parameters.Any(x => x.VectorField != null && x.Get(component).Enable);

                    foreach (var item in parameters)
                    {
                        if (!item.Get(component).Enable)
                            if (!isVectorParameter)
                                continue;

                        OverwriteMaterialParameter(item, processor, materials);
                    }
                }
            }
        }

        private void OverwriteMaterialParameter(Metadata.ParameterInfo parameterInfo, ShaderProcessor processor, IEnumerable<Material> materials)
        {
            var parameter = parameterInfo.Get(component);
            if (!parameter.Enable || parameterInfo.DisableOverwriteMaterialValue)
                return;

            var propertyNames = parameterInfo.GetPropertyNames(processor);
            var m = materials
                .Where(x => !this.processor.Excludes.TryGetValue(x, out var options) || options is not { SkipAnimation: false, SkipOverrideParameters: true })
                .ToArray();
            foreach (var propertyName in propertyNames)
            {
                if (parameterInfo.Overrider.FirstOrDefault(x => x.Key.Get(component).Enable)?.Contains(new(processor.QualifiedName, propertyName)) ?? false)
                    continue;
                processor.OverrideMaterialValue(new OverrideMaterialValueContext() { ParameterInfo = parameterInfo, PropertyName = propertyName, Materials = m });
            }
        }

        private void GenerateBlendTreeBranch(Metadata.ParameterInfo parameterInfo, Parameter parameter, ModularAvatarMenuItem menu)
        {
            var parameterBuffer = (stackalloc float[8]);

            var group = GetBlendTreeGroup(parameterInfo.Parent.Id);
            var ranges = parameterInfo.GetRanges(component);
            bool isColor = parameterInfo.ParameterType == typeof(Color);

            ReadOnlySpan<float> values = parameterBuffer;
            if (!isColor)
            {
                values = parameterBuffer[..parameter.GetValues(parameterBuffer)];
            }
            else
            {
                var color = parameter.GetValueDirect<Color>();
                if (color.maxColorComponent > 1)
                {
                    var normalized = Vector3.Normalize(Unsafe.As<Color, Vector3>(ref color));

                    parameterBuffer[0] = normalized.x;
                    parameterBuffer[1] = normalized.y;
                    parameterBuffer[2] = normalized.z;
                    parameterBuffer[3] = color.a;

                    values = parameterBuffer[..4];
                }
                else
                {
                    values = parameterBuffer[..parameter.GetValues(parameterBuffer)];
                }
            }

            float colorMaxComponent = !isColor ? Mathf.Round(parameter.MinMaxRange.y) : ranges[4].y;

            bool useIntensity = parameterInfo.ColorSettings.HDR &&
                                isColor &&
                                parameter.IsAnimated &&
                                colorMaxComponent > 1;

            ParameterConfig intensity = default;

            if (useIntensity)
            {
                var x = MathF.Max(0, parameter.GetValueDirect<Color>().maxColorComponent - 1);
                intensity = new ParameterConfig()
                {
                    nameOrPrefix = $"{parameterInfo.ParameterName}.intensity",
                    remapTo = $"{AvatarParameterNamePrefix}{parameterInfo.ParameterName}.intensity",
                    defaultValue = Utils.NormalizeInRange(x, 0, colorMaxComponent - 1),
                    syncType = ParameterSyncType.Float,
                    saved = parameter.Saved,
                    localOnly = !parameter.Synced,
                };
            }

            if (values.Length > 1)
            {
                group = group.AddDirectBlendTree(Path.GetFileName(parameterInfo.Id));
            }

            var t = parameterInfo.ParameterType;

            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                string postfix = values.Length == 1 ? "" : t == typeof(Vector4) ? $".{"xyzw"[i]}" : $".{"rgba"[i]}";
                var name = $"{parameterInfo.Name}{postfix}";
                string btName = string.IsNullOrEmpty(postfix) ? null : postfix.TrimStart('.').ToUpperInvariant();

                // Constant
                if (!parameter.Enable || !parameter.Animation)
                {
                    foreach (var shaderProcessor in processor.processors.AsSpan())
                    {
                        if (!processor.ShaderRendererPair.TryGetValue(shaderProcessor, out var renderers))
                            continue;

                        var propertyNames = parameterInfo.GetPropertyNames(shaderProcessor);
                        foreach (var propertyName in propertyNames)
                        {
                            if (parameterInfo.Overrider.FirstOrDefault(x => x.Key.Get(component).IsAnimated)?.Contains(new(shaderProcessor.QualifiedName, propertyName)) ?? false)
                                continue;

                            if (!parameter.Enable)
                            {
                                var buffer = parameterBuffer[4..];
                                buffer = buffer[..parameterInfo.Get(defaultValueProvider).GetValues(buffer)];
                                value = buffer[i];
                            }

                            var ctx = new ConfigureAnimationContext()
                            {
                                AnimationName = $"{LightLimitChanger.Title} {name}",
                                BlendTreeName = btName,
                                ParameterInfo = parameterInfo,
                                PropertyName = $"{propertyName}{postfix}",
                                Renderers = renderers,
                                Value = value,
                            };
                            shaderProcessor.ConfigureAnimation(ctx, group);
                        }
                    }
                    continue;
                }

                var avatarParameter = new ParameterConfig()
                {
                    nameOrPrefix = $"{parameterInfo.ParameterName}{postfix}",
                    remapTo = $"{AvatarParameterNamePrefix}{parameterInfo.ParameterName}{postfix}",
                    defaultValue = Utils.NormalizeInRange(value, ranges[i].x, ranges[i].y),
                    syncType =
                        t == typeof(bool) ? ParameterSyncType.Bool :
                        t == typeof(int) ? ParameterSyncType.Int :
                        t == typeof(float) || t == typeof(Vector4) || t == typeof(Color) ? ParameterSyncType.Float :
                    ParameterSyncType.NotSynced,
                    saved = parameter.Saved,
                    localOnly = !parameter.Synced,
                };

                processor.avatarParameters.Add(avatarParameter);

                // Animation
                {
                    foreach (var shaderProcessor in processor.processors.AsSpan())
                    {
                        if (!processor.ShaderRendererPair.TryGetValue(shaderProcessor, out var renderers))
                            continue;

                        if (!parameterInfo.ShaderFeatures.IsEmpty && parameterInfo.ShaderFeatures.IndexOf(shaderProcessor.QualifiedName) == -1)
                        {
                            continue;
                        }

                        var propertyNames = parameterInfo.GetPropertyNames(shaderProcessor);
                        foreach (var propertyName in propertyNames)
                        {
                            if (string.IsNullOrEmpty(propertyName) || (parameterInfo.Overrider.FirstOrDefault(x => x.Key.Get(component).IsAnimated)?.Contains(new(shaderProcessor.QualifiedName, propertyName)) ?? false))
                                continue;

                            var ctx = new ConfigureAnimationContext()
                            {
                                AnimationName = $"{LightLimitChanger.Title} {name}",
                                BlendTreeName = btName,
                                BlendParameter = avatarParameter.nameOrPrefix,
                                ParameterInfo = parameterInfo,
                                PropertyName = $"{propertyName}{postfix}",
                                Renderers = renderers,
                                Range = ranges[i],
                            };

                            shaderProcessor.ConfigureAnimation(ctx, group);
                        }
                    }
                }

                if (isColor && !parameterInfo.ColorSettings.Alpha && i == 3)
                    continue;

                string menuPath = parameterInfo.Name;
                string menuNameKey = $"settings:{parameterInfo.Id}/label";

                var menu2 = menu;
                if (values.Length != 1)
                {
                    menu2 = menu2.GetOrAdd(menuPath, icon: parameterInfo.Icon, translateKey: menuNameKey);
                    menuPath = $"{(char)(postfix[1] & ~0x20)}";
                    menuNameKey = $"ex-menu:postfix/{postfix[1]}";
                }

                var menuItem = menu2.GetOrAdd(menuPath, (MAMenuItem menu) =>
                {
                    if (t == typeof(bool))
                    {
                        return (VRCExMenuControlType.Toggle, avatarParameter.nameOrPrefix, 1);
                    }
                    else if (processor.Component.General.CompressionExpressionParameters)
                    {
                        menu.Control.parameter = new() { name = ParameterName_ParameterSyncerOverrideIndex };
                        return (VRCExMenuControlType.RadialPuppet, avatarParameter.nameOrPrefix, processor.avatarParameters.Count);
                    }
                    else
                    {
                        return (VRCExMenuControlType.RadialPuppet, avatarParameter.nameOrPrefix, 0);
                    }
                }, parameterInfo.Icon, translateKey: menuNameKey);
            }

            if (useIntensity)
            {
                var tree = group.AddDirectBlendTree("Intensity");

                var count = MathF.Round(parameter.MinMaxRange.y);

                var children = group.Children.AsSpan()[..3]; // RGB...

                DirectBlendTree mul = tree;
                for (int i = 0; i < count - 1; i++)
                {
                    mul = mul.AddDirectBlendTree($"{i + 2}x");
                    mul.DirectBlendParameter = intensity.nameOrPrefix;
                    foreach (var child in children)
                    {
                        mul.Append(child.BlendTree, child.threshold);
                    }
                }

                processor.avatarParameters.Add(intensity);
                string menuPath = parameterInfo.Name;
                menuPath += $"//Intensity";
                var menuItem = menu.GetOrAdd(menuPath, (MAMenuItem menu) =>
                {
                    if (component.General.CompressionExpressionParameters)
                    {
                        menu.Control.parameter = new() { name = ParameterName_ParameterSyncerOverrideIndex };
                        return (VRCExMenuControlType.RadialPuppet, intensity.nameOrPrefix, processor.avatarParameters.Count);
                    }
                    else
                    {
                        return (VRCExMenuControlType.RadialPuppet, intensity.nameOrPrefix, 0);
                    }
                }, parameterInfo.Icon, "ex-menu:color/intensity");
            }
        }

        private void GenerateLightDirectionControlBranch(Metadata.ParameterInfo parameterInfo, ModularAvatarMenuItem menu)
        {
            Debug.Assert(parameterInfo.Id is "lighting/light-direction");

            var parameterBuffer = (stackalloc float[8]);
            var parameter = parameterInfo.Get(component);
            var group = GetBlendTreeGroup(parameterInfo.Parent.Id);

            parameter.GetValueDirect<float>();

            var parameters = (
                X: CreateParameter($"{parameterInfo.ParameterName}.X", parameter.Saved, !parameter.Synced),
                Y: CreateParameter($"{parameterInfo.ParameterName}.Y", parameter.Saved, !parameter.Synced),
                Enable: CreateParameter($"{parameterInfo.ParameterName}.Enable", parameter.Saved, !parameter.Synced, asBool: true)
                );

            static ParameterConfig CreateParameter(string name, bool saved, bool localOnly, bool asBool = false) => new()
            {
                nameOrPrefix = name,
                remapTo = $"{AvatarParameterNamePrefix}{name}",
                defaultValue = asBool ? 0 : 0.5f,
                syncType = asBool ? ParameterSyncType.Bool : ParameterSyncType.Float,
                saved = saved,
                localOnly = localOnly,
            };

            processor.avatarParameters.Add(parameters.X);
            processor.avatarParameters.Add(parameters.Y);
            processor.avatarParameters.Add(parameters.Enable);

            var treeRoot = group.AddBlendTree("Light Direction");
            treeRoot.BlendParameter = parameters.Enable.nameOrPrefix;

            var clips = (
                X: new AnimationClip() { name = "X" },
                Y: new AnimationClip() { name = "Y" },
                Z: new AnimationClip() { name = "Z" },
                W: new AnimationClip() { name = "W" },
                O: new AnimationClip() { name = "Other" },
                D: new AnimationClip() { name = "Default" }
                );

            foreach (var shaderProcessor in processor.processors.AsSpan())
            {
                if (!processor.ShaderRendererPair.TryGetValue(shaderProcessor, out var renderers))
                    continue;

                var propertyNames = parameterInfo.GetPropertyNames(shaderProcessor);
                if (propertyNames.IsEmpty)
                    continue;

                foreach (var propertyName in propertyNames)
                {
                    shaderProcessor.ConfigureLightDirectionControl(renderers, propertyName, clips.D, clips.X, clips.Y, clips.Z, clips.W, clips.O);
                }
            }

            {
                treeRoot.AddMotion(clips.D, 0);
                var tree = treeRoot.AddDirectBlendTree("ON", 1f);

                var x = tree.AddMotionTime(clips.X);
                var y = tree.AddMotionTime(clips.Y);
                var z = tree.AddMotionTime(clips.Z);
                var w = tree.AddMotion(clips.W);
                var o = tree.AddMotion(clips.O);

                x.BlendParameter = parameters.X.nameOrPrefix;
                y.BlendParameter = parameters.Y.nameOrPrefix;
                z.BlendParameter = parameters.X.nameOrPrefix;
            }

            {
                string menuPath = parameterInfo.Name;
                string menuNameKey = $"settings:{parameterInfo.Id}/label";

                menu = menu.GetOrAdd(menuPath, icon: parameterInfo.Icon, translateKey: menuNameKey);

                var e = menu.GetOrAdd(parameters.Enable.nameOrPrefix, menu => (VRCExMenuControlType.Toggle, parameters.Enable.nameOrPrefix, 1), AssetUtils.FromGUID<Texture2D>(Icons.UseBacklight), translateKey: "common:label/enable");
                var x = menu.GetOrAdd(parameters.X.nameOrPrefix, menu => (VRCExMenuControlType.RadialPuppet, parameters.X.nameOrPrefix), AssetUtils.FromGUID<Texture2D>(Icons.DistanceFadeX), translateKey: "ex-menu:direction/horizontal");
                var z = menu.GetOrAdd(parameters.Y.nameOrPrefix, menu => (VRCExMenuControlType.RadialPuppet, parameters.Y.nameOrPrefix), AssetUtils.FromGUID<Texture2D>(Icons.Vertical_Start), translateKey: "ex-menu:direction/vertical");
            }
        }

        private DirectBlendTree GetBlendTreeGroup(string name)
        {
            DirectBlendTree result = null;

            if (!name.Contains("/"))
            {
                result = processor.blendTree.Find<DirectBlendTree>(name);
            }
            else
            {
                var ranges = (stackalloc Range[8]);
                var span = name.AsSpan();
                ranges = ranges[..span.SplitAny(ranges, "/")];

                foreach(var range in ranges)
                {
                    var tree = result ?? processor.blendTree;
                    result = tree.Find<DirectBlendTree>(span[range]) ?? tree.AddDirectBlendTree(span[range].ToString());
                }
            }

            return result ?? processor.blendTree.AddDirectBlendTree(name);
        }
    }
}

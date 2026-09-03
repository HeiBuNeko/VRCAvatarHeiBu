using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using Anatawa12.AvatarOptimizer;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using Numeira;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace io.github.azukimochi;

internal sealed partial class LightLimitChangerProcessor : IDisposable
{
    public LightLimitChangerComponent Component { get; }
    public HashSet<Object> AssetContainer { get; }
    public TextureBaker TextureBaker { get; }
    public Dictionary<Object, ExcludeOptions> Excludes { get; }

    private readonly BuildContext context;
    private readonly List<ShaderProcessor> processors = new();
    private readonly HashSet<ParameterConfig> avatarParameters = new();

    private Configurator configurator;

    private ModularAvatarMenuItem menuRoot;
    private DirectBlendTree blendTree;
    private Material[] targetMaterials;
    private ImmutableDictionary<ShaderProcessor, Material[]> shaderMaterialPair;
    private ImmutableDictionary<ShaderProcessor, Renderer[]> shaderRendererPair;
    private AnimatorController animatorController;
    private AnimationClip blankClip;

    public ReadOnlySpan<ShaderProcessor> Processors => processors.AsSpan();
    public IEnumerable<Renderer> TargetRenderers => shaderRendererPair.SelectMany(x => x.Value);
    public ReadOnlySpan<Material> TargetMaterials => targetMaterials;
    public ImmutableDictionary<ShaderProcessor, Material[]> ShaderMaterialPair => shaderMaterialPair;
    public ImmutableDictionary<ShaderProcessor, Renderer[]> ShaderRendererPair => shaderRendererPair;

    #region ParameterNames

    internal const string AllParameterNamePrefix = "LightLimitChanger/";
    internal const string AvatarParameterNamePrefix = AllParameterNamePrefix + "Parameter/";
    internal const string InternalParameterPrefix = AllParameterNamePrefix + "System/";
    internal const string ParameterName_ParameterSyncerIndex = InternalParameterPrefix + "ParameterSyncerIndex";
    internal const string ParameterName_CurrentParameterSyncerIndex = InternalParameterPrefix + "CurrentParameterSyncIndex";
    internal const string ParameterName_ParameterSyncerValue = InternalParameterPrefix + "ParameterSyncerValue";
    internal const string ParameterName_ParameterSyncerOverrideIndex = InternalParameterPrefix + "ParameterSyncerOverrideIndex";
    internal const string ParameterName_PresetIndex = InternalParameterPrefix + "PresetIndex";
    internal const string ParameterName_DynamicPresetPrefix = InternalParameterPrefix + "DynamicPreset/";
    internal const string ParameterName_ManualSync = InternalParameterPrefix + "ManualSync";

    #endregion

    public LightLimitChangerProcessor(BuildContext context)
    {
        this.context = context;
        Component = context.AvatarRootObject.GetComponentInChildren<LightLimitChangerComponent>(false);
        AssetContainer = new HashSet<Object>();
        TextureBaker = new(AssetContainer, context.ErrorReport);
        Excludes = Component is null ? new() : Component.Excludes.SelectMany(x => x.GetTargetObjects().Select(y => (x, y))).ToDictionary(x => x.y, x => x.x);
    }

    internal void CloneMaterials()
    {
        var excludeMaterials = Excludes.Where(x => x.Value is { Object: Material, SkipAnimation: true }).Select(x => x.Key as Material).Where(x => x != null).ToHashSet();
        var excludeRenderers = Excludes.Keys.Select(x => x is GameObject go && go != null ? go.GetComponent<Renderer>() : null)
            .Where(x => x != null)
            .ToHashSet();

        Dictionary<Material, Material> cloned = new();
        Dictionary<Material, ShaderProcessor> materialProcessorPair = new();
        Dictionary<ShaderProcessor, List<Material>> shaderMaterialPair = new();
        Dictionary<ShaderProcessor, HashSet<Renderer>> shaderRendererPair = new();

        var providers = new IMaterialProvider[] 
        { 
            new RendererMaterialProvider(),
            new AnimatedMaterialProvider(),
            new MaterialSwapMaterialProvider(), 
            new MaterialSetterMaterialProvider() 
        };

        Material Clone(Material material, Renderer renderer = null)
        {
            if (material == null)
                return null;

            if (excludeMaterials.Contains(material))
                return material;

            if (!materialProcessorPair.TryGetValue(material, out var targetProcessor))
            {
                foreach (var processor in Processors)
                {
                    if (!processor.IsTargetMaterial(material))
                        continue;
                    targetProcessor = processor;
                    break;
                }
            }

            if (cloned.TryGetValue(material, out var clone))
            {
                if (renderer != null && targetProcessor is not null)
                    shaderRendererPair.GetOrAdd(targetProcessor, _ => new()).Add(renderer);
                return clone;
            }

            if (targetProcessor is null)
            {
                clone = material;
            }
            else
            {
                materialProcessorPair.Add(material, targetProcessor);
                if (renderer != null)
                    shaderRendererPair.GetOrAdd(targetProcessor, _ => new()).Add(renderer);

                using (MaterialEditorReflection.BeginNoApplyMaterialPropertyDrawers())
                    clone = new Material(material);
                clone.parent = null;
                clone.name = $"{material.name}(LLC Clone)";
                AssetContainer.Add(clone);
                ObjectRegistry.RegisterReplacedObject(material, clone);

                shaderMaterialPair.GetOrAdd(targetProcessor, _ => new()).Add(clone);
                cloned.Add(clone, clone);
            }

            cloned.Add(material, clone);

            return clone;
        }

        foreach (var provider in providers)
        {
            provider.Collect(context, Clone);
        }

        this.shaderMaterialPair = shaderMaterialPair.ToImmutableDictionary(x => x.Key, x => x.Value.ToArray());
        this.shaderRendererPair = shaderRendererPair.ToImmutableDictionary(x => x.Key, x => x.Value.ToArray());
        targetMaterials = shaderMaterialPair.Values.SelectMany(x => x).Distinct().ToArray();

        foreach (var pair in shaderMaterialPair)
        {
            pair.Key.OnMaterialCloned(pair.Value);
        }

        if (!Preferences.Local.ErrorSuppression.MixedShaderWarning)
            CheckShaderCompatibility();
    }

    private void CheckShaderCompatibility()
    {
        var supportedMaterials = shaderMaterialPair.Values
            .SelectMany(x => x)
            .ToHashSet();

        var excludeRenderers = Excludes.Keys
            .Select(x => x is GameObject go && go != null ? go.GetComponent<Renderer>() : null)
            .Where(x => x != null)
            .ToHashSet();

        var excludeMaterials = Excludes
            .Where(x => x.Value is { Object: Material, SkipAnimation: true })
            .Select(x => x.Key as Material)
            .Where(x => x != null)
            .ToHashSet();

        foreach (var renderer in context.AvatarRootObject.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer && !Component.General.IncludeParticleSystem)
                continue;
            if (excludeRenderers.Contains(renderer))
                continue;

            bool hasSupportedMaterial = false;
            HashSet<Shader> unsupportedShaders = new();
            List<Material> unsupportedMaterials = new();

            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || excludeMaterials.Contains(material))
                    continue;

                if (supportedMaterials.Contains(material))
                {
                    hasSupportedMaterial = true;
                }
                else
                {
                    if (material.shader != null)
                        unsupportedShaders.Add(material.shader);
                    unsupportedMaterials.Add(material);
                }
            }

            if (!hasSupportedMaterial || unsupportedShaders.Count == 0)
                continue;

            var msg = NdmfMessage.Create(ErrorSeverity.Information, "warning:renderer/mixed-shaders");
            msg.DetailsSubstitutions = new[]
            {
                string.Join(", ", unsupportedShaders.Select(s => s.name).OrderBy(x => x)),
            };

            var rendererRef = ObjectRegistry.GetReference(renderer);
            msg.AddReference(rendererRef);

            var capturedMaterials = unsupportedMaterials.Distinct().ToArray();
            msg.AutoFix = report => MixedShaderFixDialog.Show(report, rendererRef, capturedMaterials);

            ErrorReport.ReportError(msg);
        }
    }

    internal void GenerateAnimations()
    {
        SetupAnimations();

        configurator = new Configurator(this);
        configurator.ConfigureSettings();

        GenerateParameters();

        {
            animatorController.AddLayer(LightLimitChanger.Title);
            var assets = new HashSet<Object>();

            var layer = animatorController.layers[0];
            var state = layer.stateMachine.AddState("LightLimitChanger (WD On)");
            state.writeDefaultValues = true;
            state.motion = blendTree.ToBlendTree(assets);

            context.AssetSaver.SaveAssets(assets);
        }

        try
        {
            AnimatorControllerBuilder acb = new() { Name = $"{LightLimitChanger.Title} Parameter Control Animator" };

            CreatePresetLoader(acb, out int presetCount);
            CreateDynamicPresetLoader(acb, presetCount);
            CompressionAnimatorParameters(acb);

            var container = new AssetCacheContainer();

            var mama = Component.gameObject.AddComponent<MAMergeAnimator>();
            mama.matchAvatarWriteDefaults = Component.WriteDefaults == WriteDefaultsSetting.MatchAvatar;
            mama.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
            mama.animator = acb.Build(container);
            mama.pathMode = MergeAnimatorPathMode.Absolute;

            context.AssetSaver.SaveAssets(container.GeneratedAssets);
        }
        catch (AvatarParameterDriverCreationFailedExeption)
        {
            // 他のスクリプトでコンパイルエラーが起きてるとParameterDriverの追加に失敗するらしい
            // LLCが原因であると誤解されないためにエラーメッセージを表示してビルドを停止するべき

            var key = "error:failed-to-add/vrc-avatar-parameter-driver";
            Debug.LogError($"[LightLimitChanger] {L10n.TrStr(key)}");
            ErrorReport.ReportError(L10n.Localizer, ErrorSeverity.Error, key);
        }

        ModifyMenus();
        RemoveEmptySubMenus(menuRoot);
        UnfoldSingleSubMenus(menuRoot);
        UnfoldGroupMenus(menuRoot);

        TranslateItems();

        OverrideMeshSettings();

        SetMenuTextColor(menuRoot, Component.MenuTextColor);
    }

    internal void NormalizeMaterials()
    {
        var ref2proc = shaderMaterialPair.SelectMany(
            x => x.Value.Select(
                y => (Key: x.Key, Value: ObjectRegistry.GetReference(y))
                )
            ).GroupBy(x => x.Value, x => x.Key).ToDictionary(x => x.Key, x => x.FirstOrDefault());

        HashSet<ObjectReference> processed = new();

        Material Normalize(Material material, Renderer _)
        {
            if (material == null || !context.AssetSaver.IsTemporaryAsset(material))
                return material;

            var reference = ObjectRegistry.GetReference(material);
            if (!processed.Add(reference))
                return material;

            if (ref2proc.TryGetValue(reference, out var processor))
                processor?.NormalizeMaterial(material);

            return material;
        }

        new RendererMaterialProvider().Collect(context, Normalize);
        new AnimatedMaterialProvider().Collect(context, Normalize);
    }

    internal void OverwriteMaterialSettings()
    {
        var ref2proc = shaderMaterialPair.SelectMany(
            x => x.Value.Select(
                y => (Key: x.Key, Value: ObjectRegistry.GetReference(y))
                )
            ).GroupBy(x => x.Value, x => x.Key).ToDictionary(x => x.Key, x => x.FirstOrDefault());

        HashSet<ObjectReference> processed = new();
        Dictionary<ShaderProcessor, HashSet<Material>> pairs = new();

        Material Collect(Material material, Renderer _)
        {
            if (material == null || !context.AssetSaver.IsTemporaryAsset(material))
                return material;

            var reference = ObjectRegistry.GetReference(material);
            if (!processed.Add(reference))
                return material;

            if (ref2proc.TryGetValue(reference, out var processor))
            {
                pairs.GetOrAdd(processor, _ => new()).Add(material);
            }

            return material;
        }

        new RendererMaterialProvider().Collect(context, Collect);
        new AnimatedMaterialProvider().Collect(context, Collect);

        if (pairs.Count == 0)
            return;

        foreach(var (key, values) in pairs)
        {
            configurator.OverwriteMaterialSettings(key, values);
        }

    }

    private void SetupAnimations()
    {
        var go = Component.gameObject;
        DirectBlendTree.DefaultDirectBlendParameter = "LightLimitChanger/One";
        blendTree = new DirectBlendTree() { Name = LightLimitChanger.Title };
        animatorController = new AnimatorController() { name = LightLimitChanger.Title };

        AssetContainer.Add(animatorController);
        animatorController.AddParameter(new() { defaultFloat = 1, name = "LightLimitChanger/One", type = AnimatorControllerParameterType.Float });

        var mama = go.GetOrAddComponent<ModularAvatarMergeAnimator>();
        mama.animator = animatorController;
        // DirectBlendTreeは常にWD Onで稼働させる必要がある
        mama.matchAvatarWriteDefaults = false; //Component.WriteDefaults == WriteDefaultsSetting.MatchAvatar;
        mama.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        mama.pathMode = MergeAnimatorPathMode.Absolute;
        mama.deleteAttachedAnimator = true;

        if (!go.TryGetComponent<ModularAvatarMenuInstaller>(out var mami))
        {
            mami = go.AddComponent<ModularAvatarMenuInstaller>();
        }

        var mam = go.AddComponent<ModularAvatarMenuItem>();
        mam.Control.type = VRCExMenuControlType.SubMenu;
        mam.Control.icon = Component.MenuIcon == null ? AssetUtils.FromGUID<Texture2D>(Icons.LLC) : Component.MenuIcon;
        mam.MenuSource = SubmenuSource.Children;
        menuRoot = mam;
        // メニュー表示名を設定する
        go.name = $"{LightLimitChanger.Title}";

        var blank = new AnimationClip() { name = "Blank" };
        AssetContainer.Add(blank);
        blankClip = blank;
    }

    private void TranslateItems()
    {
        foreach(var tag in context.AvatarRootObject.GetComponentsInChildren<TranslateTag>(true))
        {
            tag.gameObject.name = L10n.TrStr(tag.Key, tag.gameObject.name);
            Object.DestroyImmediate(tag);
        }
    }

    private void GenerateParameters()
    {
        var mapa = Component.gameObject.GetOrAddComponent<ModularAvatarParameters>();
        mapa.parameters.AddRange(avatarParameters);

        foreach (var parameter in mapa.parameters.AsSpan())
        {
            animatorController.AddParameter(new AnimatorControllerParameter()
            {
                name = parameter.nameOrPrefix,
                defaultFloat = parameter.defaultValue,
                defaultBool = parameter.defaultValue != 0,
                defaultInt = (int)parameter.defaultValue,
                type = parameter.syncType switch
                {
                    //ParameterSyncType.Int => AnimatorControllerParameterType.Int,
                    //ParameterSyncType.Bool => AnimatorControllerParameterType.Bool,
                    //ParameterSyncType.Float => AnimatorControllerParameterType.Float,
                    //_ => default,
                    _ => AnimatorControllerParameterType.Float, // Floatへ自動変換されるのでこれでOK
                },
            });
        }
    }

    private void CompressionAnimatorParameters(AnimatorControllerBuilder controller)
    {
        if (!Component.General.CompressionExpressionParameters)
            return;

        var mapa = Component.gameObject.GetOrAddComponent<ModularAvatarParameters>();
        var targetParameters = mapa.parameters.Where(x => !x.nameOrPrefix.StartsWith(InternalParameterPrefix)).ToArray();
        if (targetParameters.Length == 0)
            return;

        foreach(ref var param in mapa.parameters.AsSpan())
        {
            param.localOnly = true;
        }

        const string Index = ParameterName_ParameterSyncerIndex;
        const string Value = ParameterName_ParameterSyncerValue;
        const string Override = ParameterName_ParameterSyncerOverrideIndex;
        const string IsLocal = "IsLocal";
        const string PresetIndex = ParameterName_PresetIndex;
        const string CurrentSyncIndex = ParameterName_CurrentParameterSyncerIndex;
        const string ManualSync = ParameterName_ManualSync;

        controller.AddParameter(IsLocal, AnimatorControllerParameterType.Bool);
        controller.AddParameter(Index, AnimatorControllerParameterType.Int);
        controller.AddParameter(Value, AnimatorControllerParameterType.Float);
        controller.AddParameter(Override, AnimatorControllerParameterType.Int);
        controller.AddParameter(PresetIndex, AnimatorControllerParameterType.Int);
        controller.AddParameter(CurrentSyncIndex, AnimatorControllerParameterType.Int);
        controller.AddParameter(ManualSync, AnimatorControllerParameterType.Bool);

        var wd = Component.WriteDefaults != WriteDefaultsSetting.OFF;
        var layer = controller.AddLayer($"{LightLimitChanger.Title} Parameter Compressor");
        var stateMachine = layer.StateMachine
            .WithDefaultWriteDefaults(wd)
            .WithDefaultMotion(blankClip);

        StateBuilder AddState(string name, Vector3? position = null)
        {
            if (name.Contains("."))
                name = name.Replace(".", "_");

            var state = stateMachine.AddState(name, position ?? Vector3.zero);

            return state;
        }

        var idlePosition = stateMachine.EntryPosition + new Vector2 (0, 100);
        var idle = AddState("Idle", idlePosition);

        CreateRemoteStates();
        CreateLocalStates();
        
        mapa.parameters.Add(new() { syncType = ParameterSyncType.Int, defaultValue = 0, nameOrPrefix = Index, saved = false, });
        mapa.parameters.Add(new() { syncType = ParameterSyncType.Float, defaultValue = 0, nameOrPrefix = Value, saved = false, });
        mapa.parameters.Add(new() { syncType = ParameterSyncType.Int, defaultValue = 0, nameOrPrefix = Override, saved = false, localOnly = true, });
        mapa.parameters.Add(new() { syncType = ParameterSyncType.Int, defaultValue = 0, nameOrPrefix = CurrentSyncIndex, saved = false, localOnly = true, });
        if (Component.General.AddManualSyncButton)
            mapa.parameters.Add(new() { syncType = ParameterSyncType.Bool, defaultValue = 0, nameOrPrefix = ManualSync, saved = false, localOnly = true });

        if (Component.General.AddManualSyncButton)
        {
            menuRoot.GetOrAdd(L10n.TrStr("ex-menu:manual-sync", "Manual Sync"), _ => (VRCExMenuControlType.Button, ManualSync));
        }

        return;

        void CreateRemoteStates()
        {
            var position = idlePosition;
            position.y += 100;
            var remote = AddState("Remote", position);
            idle.AddTransition(remote).IfNot(IsLocal);
            var parameters = targetParameters.AsSpan();

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                position.y += 100;
                var set = AddState($"Set {param.nameOrPrefix}", position);

                remote.AddTransition(set).Equals(Index, i + 1);
                set.AddAvatarParameterDriver().Copy(Value, param.nameOrPrefix);

                remote.AddTransition(remote).IfNot(IsLocal);
                set.AddTransition(remote).NotEqual(Index, i + 1);
                set.AddTransition(set).WithDuration(2f / 30f).Equals(Index, i + 1);
            }
        }

        void CreateLocalStates()
        {
            float SyncStartFrequency = Component.General.ParameterSyncFrequency;
            const float SyncTransitionFrequency = 2 / 30f;

            var position = idlePosition;
            position.x += 250;
            position.y += 100;
            var localFirstTime = AddState("Local [First]", position with { x = position.x + 0, y = position.y + 0 });
            idle.AddTransition(localFirstTime).If(IsLocal);

            var local = AddState("Local [Idle]", position with { x = position.x + 250, y = position.y + 0 });
            idle.AddTransition(local).If(IsLocal);
            idle.AddAvatarParameterDriver().Set(CurrentSyncIndex, 0);

            position.y += 100;
            var syncStart = AddState("Sync Start", position with { x = position.x + 0, y = position.y + 0 });
            local.AddTransition(syncStart).WithExitTime(SyncStartFrequency);

            if (Component.General.AddManualSyncButton)
            {
                local.AddTransition(syncStart).If(ManualSync);
            }

            localFirstTime.AddTransition(syncStart).If(IsLocal);

            var presetChanged = AddState("Preset Changed", position with { x = position.x + 500, y = position.y + 0 });
            presetChanged.AddTransition(syncStart).Equals(PresetIndex, 0);

            position.y += 100;

            var parameters = targetParameters.AsSpan();

            var statesBuffer = new StateBuilder[parameters.Length * 2];
            var syncStates = statesBuffer.AsSpan()[..parameters.Length];
            var overrideSyncStates = statesBuffer.AsSpan()[parameters.Length..];

            _ = syncStates.Length;
            _ = overrideSyncStates.Length;

            // 初期化
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                syncStates[i] = AddState($"Sync {param.nameOrPrefix}", position + new Vector2(0, 100 * i));
                overrideSyncStates[i] = AddState($"Override {param.nameOrPrefix}", position + new Vector2(250, 100 * i));
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var sync = syncStates[i];
                var @override = overrideSyncStates[i];
                
                //定期同期 (一定周期で動く
                {
                    var state = i > 0 ? syncStates[i - 1] : syncStart;
                    state.AddTransition(sync).WithDuration(SyncTransitionFrequency).If(IsLocal);
                    // プリセットが変更されたら最初から同期しなおす
                    sync.AddTransition(presetChanged).NotEqual(PresetIndex, 0);

                    sync.AddAvatarParameterDriver()
                        .Copy(param.nameOrPrefix, Value)
                        .Set(Index, i + 1)
                        .Set(CurrentSyncIndex, i + 1);
                }

                //優先同期 (Puppet開いてる物を優先的に同期する)
                {
                    local.AddTransition(@override).Equals(Override, i + 1);

                    @override.AddTransition(@override).Equals(Override, i + 1);
                    @override.AddTransition(local).NotEqual(Override, i + 1).Equals(CurrentSyncIndex, 0);

                    @override.AddAvatarParameterDriver()
                        .Copy(param.nameOrPrefix, Value)
                        .Set(Index, i + 1);
                }

                for (int i2 = 0; i2 < parameters.Length; i2++)
                {
                    sync.AddTransition(overrideSyncStates[i2]).Equals(Override, i2 + 1);
                    @override.AddTransition(syncStates[i2]).NotEqual(Override, i2 + 1).Equals(CurrentSyncIndex, i2 + 1);
                }
            }

            syncStates[^1].AddTransition(local).If(IsLocal);
        }
    }

    private void CreatePresetLoader(AnimatorControllerBuilder controller, out int presetCount)
    {
        var children = Component.GetComponentsInChildren<LightLimitChangerComponent>(true);
        foreach(var x in children.Skip(1))
        {
            if (x.TryGetComponent<MAMenuInstaller>(out var c))
                Object.DestroyImmediate(c);
        }
        var presets = children.Where(x => x.gameObject.activeInHierarchy);
        if (!Component.General.GenerateDefaultPreset)
            presets = presets.Skip(1);

        presetCount = presets.Count();
        if (presetCount == 0)
            return;

        var wd = Component.WriteDefaults != WriteDefaultsSetting.OFF;

        const string IsLocal = "IsLocal";
        const string Preset = ParameterName_PresetIndex;

        controller.AddParameter(IsLocal, AnimatorControllerParameterType.Bool);
        controller.AddParameter(Preset, AnimatorControllerParameterType.Int);

        var layer = controller.AddLayer($"{LightLimitChanger.Title} Presets");
        var stateMachine = layer.StateMachine
            .WithDefaultWriteDefaults(wd)
            .WithDefaultMotion(blankClip);

        var pos = stateMachine.EntryPosition + new Vector2(200, 0);
        var idle = stateMachine.AddState("Idle", pos);

        var presetMenuRoot = menuRoot.GetOrAdd("preset", menu => (VRCExMenuControlType.SubMenu, null), AssetUtils.FromGUID<Texture2D>(Icons.PresetFolder), translateKey: "common:label/preset");
        var parameterBuffer = (stackalloc float[4]);
        foreach (var (child, i) in children.Select((x, i) => (x, i + 1)))
        {
            var menuName = i == 1 ? L10n.TrStr("common:label/default-preset", "Default") : child.name;
            var state = stateMachine.AddState($"{i}", new Vector3((pos.x + stateMachine.ExitPosition.x) / 2, pos.y + 100 * (i - 1)));
            idle.AddTransition(state).If(IsLocal).Equals(Preset, i);

            state.AddExitTransition().NotEqual(Preset, i);

            var parameters = animatorController.parameters;

            var dr = state.AddAvatarParameterDriver();

            foreach (var group in Metadata.AllParameterGroups)
            {
                foreach (var item in group)
                {
                    var parameter = item.Get(child);
                    if (!parameter.Enable)
                        continue;

                    if (item.Id == GeneralControlIDs.LightDirection && Component.General.LightingControl.AllowAdvancedLightDirectionControl)
                    {
                        dr.Set($"{item.ParameterName}.X", 0.5f);
                        dr.Set($"{item.ParameterName}.Y", 0.5f);
                        dr.Set($"{item.ParameterName}.Enable", 0f);
                        continue;
                    }

                    var ranges = item.GetRanges(child);
                    var originalRanges = item.GetRanges(Component);
                    ReadOnlySpan<float> values = parameterBuffer[..parameter.GetValues(parameterBuffer)];

                    if (item.ParameterType == typeof(Color) && item.ColorSettings.HDR)
                    {
                        var color = parameter.GetValueDirect<Color>();
                        var value = Utils.NormalizeInRange(Math.Max(0, color.maxColorComponent - 1), originalRanges[4].x, originalRanges[4].y);
                        var name = $"{item.ParameterName}.intensity";
                        if (parameters.Any(x => x.name == name))
                            dr.Set(name, value);

                        if (color.maxColorComponent > 1)
                        {
                            var normalized = Unsafe.As<Color, Vector3>(ref color) / color.maxColorComponent;

                            parameterBuffer[0] = normalized.x;
                            parameterBuffer[1] = normalized.y;
                            parameterBuffer[2] = normalized.z;
                            parameterBuffer[3] = color.a;

                            values = parameterBuffer[..4];
                        }
                    }

                    for (int i2 = 0; i2 < values.Length; i2++)
                    {
                        var value = Utils.NormalizeInRange(values[i2], originalRanges[i2].x, originalRanges[i2].y);
                        string postfix = values.Length == 1 ? "" : item.ParameterType == typeof(Vector4) ? $".{"xyzw"[i2]}" : $".{"rgba"[i2]}";
                        var name = $"{item.ParameterName}{postfix}";
                        if (parameters.Any(x => x.name == name))
                            dr.Set(name, value);
                    }
                }
            }

            presetMenuRoot.GetOrAdd(menuName, menu =>
            {
                return (VRCExMenuControlType.Button, Preset, i);
            }, AssetUtils.FromGUID<Texture2D>(Icons.Preset));
        }

        var mapa = Component.gameObject.GetOrAddComponent<ModularAvatarParameters>();
        mapa.parameters.Add(new() { nameOrPrefix = Preset, saved = false, syncType = ParameterSyncType.Int, localOnly = true });
    }

    private void CreateDynamicPresetLoader(AnimatorControllerBuilder controller, int currentPresetCount)
    {
        if (!Component.General.GenerateDynamicPreset || Component.General.DynamicPresetSlotCount < 0)
            return;

        var wd = Component.WriteDefaults != WriteDefaultsSetting.OFF;

        const string IsLocal = "IsLocal";
        const string Preset = ParameterName_PresetIndex;
        const string Prefix = ParameterName_DynamicPresetPrefix + "Slot";
        const string PresetMode = ParameterName_DynamicPresetPrefix + "Mode";

        controller.AddParameter(IsLocal, AnimatorControllerParameterType.Bool);
        controller.AddParameter(Preset, AnimatorControllerParameterType.Int);

        var layer = controller.AddLayer($"{LightLimitChanger.Title} Dynamic Presets");
        var stateMachine = layer.StateMachine
            .WithDefaultWriteDefaults(wd)
            .WithDefaultMotion(blankClip);

        var pos = stateMachine.EntryPosition + new Vector2(200, 0);
        var idle = stateMachine.AddState("Idle", pos);
        stateMachine.ExitPosition = pos + new Vector2(1000, 0);

        int length = Component.General.DynamicPresetSlotCount;

        var mapa = Component.gameObject.GetOrAddComponent<ModularAvatarParameters>();
        var parameters = mapa.parameters.Where(x => !x.nameOrPrefix.StartsWith(InternalParameterPrefix)).ToArray();

        mapa.parameters.Add(new() { nameOrPrefix = PresetMode, syncType = ParameterSyncType.Int, localOnly = true });
        controller.AddParameter(PresetMode, AnimatorControllerParameterType.Int);

        var presetMenuRoot = menuRoot.GetOrAdd("preset", menu => (VRCExMenuControlType.SubMenu, null), AssetUtils.FromGUID<Texture2D>(Icons.PresetFolder), translateKey: "common:label/preset");

        presetMenuRoot = presetMenuRoot.GetOrAdd("dynamic-preset", menu =>
        {
            return (VRCExMenuControlType.SubMenu, null);
        }, AssetUtils.FromGUID<Texture2D>(Icons.PresetFolder), translateKey: "ex-menu:dynamic-preset");

        for (int type = 0; type < 2; type++)
        {
            var typeStr = type switch { 0 => "Load", _ => "Save" };

            var menu = presetMenuRoot.GetOrAdd(typeStr, menu =>
            {
                return (VRCExMenuControlType.SubMenu, PresetMode, type);
            }, AssetUtils.FromGUID<Texture2D>(Icons.PresetFolder), translateKey: $"ex-menu:dynamic-preset/{typeStr.ToLowerInvariant()}");

            var icon = AssetUtils.FromGUID<Texture2D>(type switch { 0 => Icons.DynamicPresetLoad, _ => Icons.DynamicPresetSave });

            for (int i = 0; i < length; i++)
            {
                var index = i + 1;

                var menuName = $"{L10n.TrStr("common:label/slot", "Slot")} {index}";
                menu.GetOrAdd(menuName, menu =>
                {
                    return (VRCExMenuControlType.Button, Preset, currentPresetCount + index);
                }, icon);
            }
        }

        for (int i = 0; i < length; i++)
        {
            var index = i + 1;
            var parameterPrefix = $"{Prefix}{index}/";

            var position = pos + new Vector2(250, i * 200);
            var entry = stateMachine.AddState($"Slot{index} Entry", position);

            idle.AddTransition(entry).If(IsLocal).Equals(Preset, currentPresetCount + index);

            for (int type = 0; type < 2; type++)
            {
                var typeStr = type switch { 0 => "Load", _ => "Save" };

                var waiting = stateMachine.AddState($"Slot{index} {typeStr} Waiting", position + new Vector2(250, 100 * type));

                var state = stateMachine.AddState($"Slot{index} {typeStr}", position + new Vector2(500, 100 * type));

                var dr = state.AddAvatarParameterDriver();

                foreach (var parameter in parameters)
                {
                    var name = $"{parameterPrefix}{parameter.nameOrPrefix}";
                    if (type == 0)
                        dr.Copy(name, parameter.nameOrPrefix);
                    else
                        dr.Copy(parameter.nameOrPrefix, name);
                }

                entry.AddTransition(waiting).WithExitTime(0.05f).Equals(PresetMode, type);

                waiting.AddTransition(state).NotEqual(Preset, currentPresetCount);
                state.AddExitTransition().NotEqual(Preset, currentPresetCount + index);
            }

            foreach (var parameter in parameters)
            {
                var name = $"{parameterPrefix}{parameter.nameOrPrefix}";

                var p = parameter;
                p.nameOrPrefix = name;
                p.remapTo = null;
                p.localOnly = true;
                p.saved = true;
                mapa.parameters.Add(p);

                controller.AddParameter(p.nameOrPrefix, AnimatorControllerParameterType.Float);
            }

            entry.AddExitTransition().NotEqual(Preset, currentPresetCount + index);
        }
    }


    private void ModifyMenus()
    {
        foreach(var processor in Processors)
        {
            processor.ModifyExpressionMenuTree(menuRoot.transform);
        }

        var favoriteParameterIds = Component.MenuSettings.FavoriteParameterIds;
        if (favoriteParameterIds.Count == 0)
            return;

        var favoriteMenuRoot = menuRoot.GetOrAdd("Favorite", icon: AssetUtils.FromGUID<Texture2D>(Icons.Star), translateKey: "favorite-menu:ex-menu/title");

        foreach(var id in favoriteParameterIds.AsSpan())
        {
            var metadata = Metadata.GetMetadataById(id);
            if (metadata is not Metadata.ParameterInfo parameter)
                continue;

            var menuPath = $"{parameter.Parent!.MenuPath}//{parameter.Name}";
            var menu = menuRoot.GetMenu(menuPath);
            if (menu == null)
                continue;

            Object.Instantiate(menu.gameObject, favoriteMenuRoot.transform).name = menu.name;
        }
    }

    private static void RemoveEmptySubMenus(MAMenuItem menu)
    {
        if (menu.Control.type != VRCExpressionsMenu.Control.ControlType.SubMenu)
            return;

        var children = menu.transform.Cast<Transform>().Select(x => x.GetComponent<MAMenuItem>()).Where(x => x != null);
        foreach (var child in children)
        {
            RemoveEmptySubMenus(child);
        }

        if (!children.Any(x => x != null))
        {
            Object.DestroyImmediate(menu);
        }
    }

    private void UnfoldGroupMenus(MAMenuItem menu)
    {
        if (!Component.MenuSettings.UnfoldGroupMenus)
            return;

        Visit(menu);

        static void Visit(MAMenuItem menu)
        {
            var children = menu.transform.Cast<Transform>().Select(x => x.GetComponent<MAMenuItem>()).Where(x => x != null).ToArray();

            foreach (var child in children)
            {
                Visit(child);
            }

            children = menu.transform.Cast<Transform>().Select(x => x.GetComponent<MAMenuItem>()).Where(x => x != null).ToArray();

            if (menu.GetComponent<LightLimitChangerMenuInfo>() is { } menuInfo)
            {
                if (menuInfo.IsGroupFolder)
                {
                    foreach (var child in children)
                    {
                        child.transform.parent = menu.transform.parent;
                    }

                    Object.DestroyImmediate(menu.gameObject);
                }
            }
        }
    }

    private void UnfoldSingleSubMenus(MAMenuItem menuRoot)
    {
        var children = menuRoot.transform.Cast<Transform>().Select(x => x.GetComponent<MAMenuItem>()).Where(x => x != null).ToArray();
        if (children.Length > 1 || children[0] is not { Control.type: VRCExMenuControlType.SubMenu } child)
            return;

        foreach (var x in child.transform.Cast<Transform>().ToArray())
        {
            x.parent = menuRoot.transform;
        }

        Object.DestroyImmediate(child);
    }

    private void OverrideMeshSettings()
    {
        var animator = context.AvatarRootObject.GetComponent<Animator>();
        var meshSettings = Component.MeshSettings;
        if (!meshSettings.EnableMeshSettigsOverride)
            return;

        var rootMeshSettings = context.AvatarRootObject.GetOrAddComponent<MAMeshSettings>();
        foreach(var x in context.AvatarRootObject.GetComponentsInChildren<MAMeshSettings>().Skip(1))
        {
            if (x.InheritProbeAnchor != ModularAvatarMeshSettings.InheritMode.DontSet)
            {
                x.InheritProbeAnchor = ModularAvatarMeshSettings.InheritMode.Inherit;
            }

            if (x.InheritBounds != ModularAvatarMeshSettings.InheritMode.DontSet)
            {
                x.InheritBounds = ModularAvatarMeshSettings.InheritMode.Inherit;
            }
        }
        rootMeshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.Set;
        rootMeshSettings.Bounds = meshSettings.Bounds;
        SetReference(ref rootMeshSettings.RootBone, meshSettings.RootBone, HumanBodyBones.Hips);

        rootMeshSettings.InheritProbeAnchor = ModularAvatarMeshSettings.InheritMode.Set;
        SetReference(ref rootMeshSettings.ProbeAnchor, meshSettings.Anchor, HumanBodyBones.Chest);

        void SetReference(ref AvatarObjectReference target, AvatarObjectReference source, HumanBodyBones defaultBone)
        {
            if (!string.IsNullOrEmpty(source.referencePath))
            {
                target = source;
                return;
            }

            target ??= new();

            Transform targetTransform = context.AvatarRootObject.transform;
            try
            {
                targetTransform = animator.GetBoneTransform(defaultBone);
            }
            catch { }

            if (targetTransform == null || targetTransform.gameObject == null)
                return;

            target.Set(targetTransform.gameObject);
        }
    }

    private static void SetMenuTextColor(MAMenuItem menuRoot, Color color, bool rainbow = false)
    {
        if (color == Color.white)
            return;

        var items = menuRoot.gameObject.GetComponentsInChildren<MAMenuItem>(true).Skip(1);
        if (rainbow)
        {
            float h = 0;
            StringBuilder sb = new();
            foreach (var item in items)
            {
                sb.Clear();
                sb.Append($"<nobr>");
                foreach(var x in item.name)
                {
                    sb.Append($"<color={Color.HSVToRGB(h, 1, 1).GetColorCodeRGB()}>");
                    sb.Append(x);
                    sb.Append("</color>");
                    h = (h + 0.05f) % 1f;
                }
                sb.Append($"</nobr>");
                item.name = sb.ToString();
            }
            return;
        }
        foreach (var item in items)
        {
            item.name = $"<color={color.GetColorCodeRGB()}>{item.name}</color>";
        }
    }

    public LightLimitChangerProcessor AddProcessor<T>() where T : ShaderProcessor, new()
        => AddProcessor(new T());

    public LightLimitChangerProcessor AddProcessor<T>(T instance) where T : ShaderProcessor
    {
        (instance as ILightLimitChangerProcessorReceiver).Initialize(this);
        processors.Add(instance);
        return this;
    }

    internal void SaveAssets(IEnumerable<Object> assets, bool force = false)
    {
        if (!force)
        {
            foreach(var asset in assets) 
                AssetContainer.Add(asset);
        }
        else
        {
            context.AssetSaver.SaveAssets(assets);
        }
    }

    public void Dispose()
    {
        foreach(var processor in Processors)
        {
            processor.Dispose();
        }
        processors.Clear();
        configurator?.Dispose();
        configurator = null;
        context?.AssetSaver.SaveAssets(AssetContainer);
        AssetContainer.Clear();
    }
}

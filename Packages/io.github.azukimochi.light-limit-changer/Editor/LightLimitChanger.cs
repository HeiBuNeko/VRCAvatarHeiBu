using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using nadena.dev.ndmf.util;

[assembly: ExportsPlugin(typeof(io.github.azukimochi.LightLimitChanger))]

namespace io.github.azukimochi;

[InitializeOnLoad]
public sealed partial class LightLimitChanger : Plugin<LightLimitChanger>
{
    internal const string Title = "Light Limit Changer";

    public override string QualifiedName => "io.github.azukimochi.light-limit-changer";

    public override string DisplayName => Title;

    private const string ModularAvatarQualifiedName = "nadena.dev.modular-avatar";

    public override Color? ThemeColor => new Color(0.0f, 0.2f, 0.8f);

    public static SemVer Version { get; private set; }

    // Assets/banner.png
    public override Texture2D LogoTexture => AssetUtils.FromGUID<Texture2D>("bfc93e465e38bcf4b9c7f3a51c6a4ead");

    static LightLimitChanger()
    {
        EditorApplication.delayCall += () =>
        {
            Version = GetCurrentVersion();
        };
    }

    protected override void Configure()
    {
        InPhase(BuildPhase.Transforming)
            .BeforePlugin(ModularAvatarQualifiedName)
            .WithRequiredExtensions(new Type[] { typeof(AnimatorServicesContext) }, beforeMA =>
            {
                beforeMA.Run($"{DisplayName} Pre Debug Display", context => context.GetState(LightLimitChangerState.New).PreDebugDisplay(context));
                beforeMA.Run(Passes.CloneMaterialsPass.Instance);
                beforeMA.Run(Passes.GenerateAnimationsPass.Instance);

            });

        InPhase(BuildPhase.Transforming)
            .AfterPlugin(ModularAvatarQualifiedName)
            .WithRequiredExtensions(new Type[] { typeof(AnimatorServicesContext) }, afterMA =>
            {
                afterMA.Run(Passes.NormalizeMaterialsPass.Instance);
                afterMA.Run(Passes.OverwriteMaterialSettingsPass.Instance);
                afterMA.Run($"{DisplayName} Post Debug Display", context => context.GetState(LightLimitChangerState.New).PostDebugDisplay(context));

                afterMA.Run($"{DisplayName} Cleanup", context => context.GetState(LightLimitChangerState.New).Dispose());
            });

        ConfigureCompatiblePlugins();

        InPhase(BuildPhase.PlatformFinish).WithRequiredExtensions(new Type[] { typeof(AnimatorServicesContext) }, sequence =>
        {
            sequence.Run("Check Duplicate Names", static context =>
            {
                Dictionary<string, Object> items = new();
                HashSet<string> names = new();
                void Visit(Transform tr)
                {
                    names.Clear();
                    var children = tr.Cast<Transform>().ToArray();

                    if (children.Length > 1)
                    {
                        foreach (var child in children)
                        {
                            if (!names.Add(child.gameObject.name))
                            {
                                items.TryAdd(child.AvatarRootPath(), child);
                                continue;
                            }
                        }
                    }

                    foreach(var child in children)
                    {
                        Visit(child);
                    }
                }

                Visit(context.AvatarRootTransform);

                if (items.Count == 0)
                    return;

                var msg = NdmfMessage.Create(ErrorSeverity.NonFatal, "error:duplicate-object-name");

                foreach (var item in items.Values)
                    msg.AddReference(ObjectRegistry.GetReference(item));

                ErrorReport.ReportError(msg);
                return;
            });
        });
    }

    private static SemVer GetCurrentVersion()
    {
        var packageInfo = JsonUtility.FromJson<PackageInfo>(
            File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GUIDToAssetPath("a82bfa088b3f7634aaadfdea98eb87e0"))));

        if (packageInfo != null)
        {
            return SemVer.Parse(packageInfo.version);
        }

        return default;
    }

    [Serializable]
    private sealed class PackageInfo
    {
        public string version;
    }
}

internal sealed class LightLimitChangerState : IDisposable
{
    public static LightLimitChangerState New(BuildContext context) => new(context);

    public LightLimitChangerProcessor Processor => runnable ? processor : null;
    public LightLimitChangerComponent Component => Processor?.Component;
    private LightLimitChangerProcessor processor;
    private readonly bool runnable = true;
    private readonly BuildContext context;

    public LightLimitChangerState(BuildContext context)
    {
        this.context = context;
        var processor = this.processor = new LightLimitChangerProcessor(context);
        if (processor.Component == null)
        {
            if (context.AvatarRootObject.GetComponentInChildren<LightLimitChangerSettings>() is { } oldComponent)
            {
                var error = NdmfMessage.CreateSimply(ErrorSeverity.NonFatal, "error:found-old-version");
                error.AddReference(ObjectRegistry.GetReference(oldComponent));
                ErrorReport.ReportError(error);
            }

            runnable = false;
            return;
        }

        if (!Authenticator.Verified)
        {
            var error = NdmfMessage.CreateSimply(ErrorSeverity.NonFatal, "error:not-verified");
            error.AddReference(ObjectRegistry.GetReference(processor.Component));
            ErrorReport.ReportError(error);
            Debug.LogError($"[LightLimitChanger] {L10n.TrStr("error:not-verified")}");
            runnable = false;
            return;
        }

        ConfigureBuiltinProcessors(processor);
    }

    internal static void ConfigureBuiltinProcessors(LightLimitChangerProcessor processor)
    {
        var targetShader = processor.Component?.TargetShader ?? TargetShaderContainer.Nothing;

        if (targetShader.Contains(BuiltinSupportedShaders.LilToon))
        {
            processor.AddProcessor<LilToonProcessor>();
        }

        if (targetShader.Contains(BuiltinSupportedShaders.Poiyomi))
        {
            //processor.AddProcessor<PoiyomiProcessor>();
            PoiyomiProcessor.RegisterSubProcessors(processor);
        }

        if (targetShader.Contains(BuiltinSupportedShaders.Sunao))
        {
            processor.AddProcessor<SunaoProcessor>();
        }

        if (targetShader.Contains(BuiltinSupportedShaders.UnlitWF))
        {
            processor.AddProcessor<UnlitWFProcessor>();
        }
    }

    public void PreDebugDisplay(BuildContext context)
    {
    }

    public void PostDebugDisplay(BuildContext context)
    {
        if (!runnable || processor.Component == null || processor.Component.DebugOptions is not { DisplayDebugInformation: true } debugOptions)
            return;

        StringBuilder sb = new();
        sb.AppendLine("Light Limit Changer Debug Information:");

        if (debugOptions.Component)
        {
            void Recursive(string name, object o, int depth = 0)
            {
                if (o == null)
                    return;

                sb.Append(' ', depth * 2);
                sb.Append($"{name}: ");
                if (o is object[] array)
                {
                    sb.AppendLine();
                    for (int i = 0; i < array.Length; i++)
                    {
                        object x = array[i];
                        Recursive($"Element {i}", x, depth + 1);
                    }
                }
                else if (o is not (string or int or float or Material or Vector4 or bool or Vector2 or Color or Enum))
                {
                    var items = o.GetType().GetFields().Where(x => !x.IsStatic && !x.IsLiteral).ToArray();
                    sb.AppendLine();
                    foreach (var x in items)
                    {
                        Recursive(x.Name, x.GetValue(o), depth + 1);
                    }
                }
                else
                {
                    sb.Append(o);
                    sb.AppendLine();
                }
            }

            Recursive("Component", processor.Component);
            sb.AppendLine();
        }

        if (debugOptions.Animation)
        {
            sb.AppendLine("Animations:");
            foreach (var x in Processor.Component.GetComponents<MAMergeAnimator>().Select(x => x.animator).Where(x => x != null).SelectMany(x => x.animationClips).Distinct())
            {
                sb.Append("  ");
                sb.AppendLine($"{x.name}");
                var binds = AnimationUtility.GetCurveBindings(x);
                if (binds.Length != 0)
                    sb.Append($"    {string.Join("\t", binds.Select(bind => $"{bind.path}: {bind.propertyName}"))}");
            }
            sb.AppendLine();
        }

        if (debugOptions.Renderers)
        {
            sb.AppendLine("Renderers:");
            sb.AppendLine("  Cloned:");
            foreach (var x in Processor.TargetRenderers)
            {
                sb.AppendLine($"    {MiscHelpers.AvatarRootPath(x)} ({x.GetType().Name})");
            }

            sb.AppendLine("  Management:");
            foreach (var (shaderProcessor, renderers) in Processor.ShaderRendererPair)
            {
                sb.AppendLine($"    {shaderProcessor.DisplayName} ({shaderProcessor.QualifiedName})");
                foreach (var x in renderers)
                {
                    sb.AppendLine($"      {MiscHelpers.AvatarRootPath(x)} ({x.GetType().Name})");
                }
            }
            sb.AppendLine();
        }

        if (debugOptions.Materials)
        {
            sb.AppendLine("Materials:");
            sb.AppendLine("  Cloned:");

            foreach (var x in Processor.TargetMaterials)
            {
                sb.AppendLine($"    {x.name} ({x.shader?.name ?? "Missing Shader"})");
            }

            sb.AppendLine("  Management:");
            foreach (var (shaderProcessor, materials) in Processor.ShaderMaterialPair)
            {
                sb.AppendLine($"    {shaderProcessor.DisplayName} ({shaderProcessor.QualifiedName})");
                foreach (var x in materials)
                {
                    sb.AppendLine($"      {x.name} ({x.shader?.name ?? "Missing Shader"})");
                }
            }
            sb.AppendLine();
        }

        ErrorReport.ReportError(new DebugMessage() { Message = sb.ToString() });
    }

    public void Dispose()
    {
        if (processor is null)
            return;

        processor.Dispose();
        processor = null;

        foreach (var component in context.AvatarRootObject.GetComponentsInChildren<LightLimitChangerSettings>(true))
        {
            Object.DestroyImmediate(component);
        }
        foreach (var component in context.AvatarRootObject.GetComponentsInChildren<LightLimitChangerComponent>(true))
        {
            Object.DestroyImmediate(component);
        }
    }
}
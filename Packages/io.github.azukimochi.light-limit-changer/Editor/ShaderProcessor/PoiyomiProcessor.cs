
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using nadena.dev.ndmf;

namespace io.github.azukimochi;

internal abstract partial class PoiyomiProcessor : ShaderProcessor
{
    public override string QualifiedName => $"{BuiltinSupportedShaders.Poiyomi}";
    public override string DisplayName => $"Poiyomi Toon v{MinimumMajorVersion}";

    protected abstract int MinimumMajorVersion { get; }

    private static readonly HashSet<SemVer> SupportedVersions = new();
    private static readonly Dictionary<SemVer, HashSet<Object>> UnsupportedMaterials = new();

    public PoiyomiProcessor()
    {
        UnsupportedMaterials.Clear();
    }

    public static void RegisterSubProcessors(LightLimitChangerProcessor processor) 
        => processor.AddProcessor<V10>(new())
                    .AddProcessor< V9>(new())
                    .AddProcessor< V8>(new())
        ;

    public override bool IsTargetMaterial(Material material)
    {
        if (material == null || material.shader == null)
            return false;

        var shaderName = material.shader.name;
        if (!shaderName.Contains("poiyomi", StringComparison.OrdinalIgnoreCase) && !shaderName.Contains("PCSS4Poi", StringComparison.Ordinal))
            return false;

        if (!TryGetVersion(material.shader, out var version))
            return false;

        bool flag = IsTargetVersion(version);
        if (!flag)
        {
            UnsupportedMaterials.GetOrAdd(version, _ => new()).Add(material);
            return false;
        }
        SupportedVersions.Add(version);

        return true;
    }
    
    private static bool TryGetVersion(Shader shader, out SemVer version)
    {
        try
        {
            var index = shader.FindPropertyIndex("shader_master_label");
            if (index == -1)
                goto Fail;
            var name = shader.GetPropertyDescription(index).AsSpan();
            if (name.IsEmpty)
                goto Fail;

            var start = name.IndexOf(">");
            var end = name.LastIndexOf("</");
            if (start != -1 && end != -1)
            {
                name = name[(start + 1)..end];
            }

            if (!name.StartsWith("Poiyomi ", StringComparison.OrdinalIgnoreCase))
                goto Fail;

            version = SemVer.Parse(name[name.IndexOfAny("0123456789")..]);
            return !version.IsDefault;
        }
        catch(Exception e) { Debug.LogException(e); }

    Fail:
        version = default;
        return false;
    }

    protected virtual bool IsTargetVersion(SemVer version) => version.Major >= MinimumMajorVersion;

    protected virtual Material GetBakeMaterial(Shader? shader = null)
    {
        shader ??= AssetUtils.FromGUID<Shader>("f00567d274195f44fa56eaee97d153f2");
        if (shader == null)
            return null;
        var material = new Material(shader);

        material.SetInt("_Mode", 3); // Transparent
        material.SetFloat("_LightingMinLightBrightness", 1);
        material.SetFloat("_AlphaForceOpaque", 0);

        material.EnableKeyword("_LIGHTINGMODE_FLAT");

        return material;
    }

    public override void Dispose()
    {
        var dict = UnsupportedMaterials;
        foreach(var x in SupportedVersions)
            dict.Remove(x);

        foreach(var x in dict)
        {
            var message = NdmfMessage.Create(ErrorSeverity.Information, "error:poiyomi/exist-unsupported-version");
            message.TitleSubstitutions = new[] { $"{x.Key}" };
            foreach (var y in x.Value)
            {
                message.AddReference(ObjectRegistry.GetReference(y));
            }
            ErrorReport.ReportError(message);
        }
        dict.Clear();
    }

    [InitializeOnLoad]
    internal static class ShaderOptimizer
    {
        [ProxyMethod("ShaderOptimizer", "SetLockedForAllMaterials", namespaceParts: new[] { "Poiyomi", "Thry" })]
        private delegate bool SetLockedForAllMaterialsDelegate(IEnumerable<Material> materials, int lockState, bool showProgressbar = false, bool showDialog = false, bool allowCancel = true, MaterialProperty shaderOptimizer = null);

        private static readonly SetLockedForAllMaterialsDelegate SetLockedForAllMaterialsProxy = ProxyMethodAttribute.CreateProxyMethod<SetLockedForAllMaterialsDelegate>();

        [ProxyMethod("ShaderOptimizer", "UnlockMaterials", namespaceParts: new[] { "Poiyomi", "Thry" })]
        private delegate bool UnlockMaterialsDelegate(IEnumerable<Material> materials, int progressType = 0);

        private static readonly UnlockMaterialsDelegate UnlockMaterialsProxy = ProxyMethodAttribute.CreateProxyMethod<UnlockMaterialsDelegate>();

        public static bool UnlockMaterials(IEnumerable<Material> materials)
        {
            return UnlockMaterialsProxy?.Invoke(materials, 0) ?? SetLockedForAllMaterials(materials, 0, false, false, false, null);
        }

        public static bool SetLockedForAllMaterials(IEnumerable<Material> materials, int lockState, bool showProgressbar = false, bool showDialog = false, bool allowCancel = true, MaterialProperty shaderOptimizer = null)
        {
            return SetLockedForAllMaterialsProxy?.Invoke(materials, lockState, showProgressbar, showDialog, allowCancel, shaderOptimizer) ?? false;
        }
    }
}

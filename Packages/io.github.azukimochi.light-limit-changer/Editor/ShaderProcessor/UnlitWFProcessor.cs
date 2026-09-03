
using nadena.dev.ndmf;

namespace io.github.azukimochi;

internal sealed class UnlitWFProcessor : ShaderProcessor
{
    public override string QualifiedName => BuiltinSupportedShaders.UnlitWF;
    public override string DisplayName => "UnlitWF";

    public override bool IsTargetMaterial(Material material)
    {
        if (material == null || material.shader == null)
            return false;

        if (material.shader.name.Contains("unlitwf", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static bool isShaderNotFoundErrorAlreadyShowed = false;

    public override void NormalizeMaterial(Material material)
    {
        if (NeedSkipTextureBake(material))
            return;

        var shader = Shader.Find("Hidden/UnlitWF/WF_UnToon_BakeTexture");
        if (shader == null)
        {
            if (isShaderNotFoundErrorAlreadyShowed)
                return;
            var message = NdmfMessage.Create(ErrorSeverity.NonFatal, "error:unlitwf/bake-shader-not-found");
            message.AddReference(ObjectRegistry.GetReference(material));
            ErrorReport.ReportError(message);
            isShaderNotFoundErrorAlreadyShowed = true;
            return;
        }
        var bakeMat = new Material(shader);
        using var disposer = AssetDisposer.Create(bakeMat);

        bool flag = false;
        var maintex = material.Get<Texture>("_MainTex", Texture2D.whiteTexture);
        flag |= NormalizeMainColor(material, bakeMat);
        flag &= maintex is Texture2D;

        if (!flag)
            return;


        var baked = Processor.TextureBaker.GetOrBake(maintex, bakeMat);

        if (Processor.Component.General.AllowForceOverrideSameReference)
            TextureBaker.ReplaceTextures(material, maintex, baked);
        material.SetTexture("_MainTex", baked);
    }

    private bool NormalizeMainColor(Material material, Material bakeMaterial)
    {
        var colorCtrl = Processor.Component.General.ColorControl;
        if (!colorCtrl.ColorTemperature.Enable)
            return false;
        var color = material.GetColor("_Color");
        if (color == Color.white)
            return false;
        bakeMaterial.SetColor("_Color", color);
        material.SetColor("_Color", Color.white);

        return true;
    }
}
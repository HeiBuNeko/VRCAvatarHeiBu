using System.Collections.Generic;
using Anatawa12.AvatarOptimizer;
using nadena.dev.ndmf;
using UnityEditor.AssetImporters;
using UnityEngine.Rendering;

namespace io.github.azukimochi;

internal sealed class TextureBaker 
{
    private static Dictionary<int, Texture2D> cache = new();

    private readonly IObjectRegistry registry;
    private readonly HashSet<Object> assetContainer;
    private readonly ErrorReport errorReport;

    public TextureBaker(HashSet<Object> assetContainer, ErrorReport errorReport, IObjectRegistry registry = null)
    {
        this.assetContainer = assetContainer;
        this.errorReport = errorReport;
        this.registry = registry ?? ObjectRegistry.ActiveRegistry;
    }

    public Texture2D GetOrBake(Texture texture, Material material, bool? forceAlphaOverride = null)
    {
        if (texture == null)
            return null;

        HashCode hash = new();
        hash.Add(texture.GetHashCode());
        hash.Add(material.ComputeCRC());
        hash.Add(forceAlphaOverride);
        var hashCode = hash.ToHashCode();

        if (cache.TryGetValue(hashCode, out var result) && result != null)
            return result;

        result = Bake(texture, material, forceAlphaOverride);
        cache[hashCode] = result;
        if (!IsBuiltinTexture(texture))
        registry.RegisterReplacedObject(texture, result);
        assetContainer.Add(result);
        return result;
    }

    private static bool IsBuiltinTexture(Texture texture)
    {
        if (texture == null) return false;

        return texture == AssetUtils.WhiteTexture
            || texture == AssetUtils.BlackTexture
            || texture == Texture2D.whiteTexture 
            || texture == Texture2D.blackTexture 
            || texture == Texture2D.grayTexture
            || texture == Texture2D.linearGrayTexture
            || texture == Texture2D.redTexture
            || texture == Texture2D.normalTexture
            ;
    }

    public Texture2D Bake(Texture texture, Material material, bool? forceAlphaOverride = null)
    {
        if (material == null)
            return null;

        var (width, height) = (1, 1);
        var source = texture == null ? Texture2D.whiteTexture : texture;
        if (source != null)
        {
            (width, height) = (source.width, source.height);
        }

        var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var temp = RenderTexture.active;
        try
        {
            Graphics.Blit(source, rt, material);

            var request = AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32);
            request.WaitForCompletion();
            using var data = request.GetData<Color32>();
            var settings = source.GetGenerationSettings(errorReport);
            var color = material.color;

            if (forceAlphaOverride ?? color.a < 1)
            {
                settings.textureImporterSettings.alphaIsTransparency = true;
                settings.sourceTextureInformation.containsAlpha = true;
            }

            var output = TextureGenerator.GenerateTexture(settings, data).output as Texture2D;
            output.name = $"{source.name}_llc_{GUID.Generate()}";
            output.Apply(); // 後続のプラグインのためにGPUにテクスチャをロードする
            return output;
        }
        finally
        {
            RenderTexture.active = temp;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    public static TextureFormat GetTextureFormat(Texture sourceTexture, Color baseColor)
    {
        if (sourceTexture is not Texture2D tex)
        {
            return baseColor.a < 1 ? TextureFormat.DXT5 : TextureFormat.DXT1;
        }
        else
        {
            var format = tex.format;
            if (baseColor.a < 1)
            {
                switch (format)
                {
                    case TextureFormat.RGB24: format = TextureFormat.RGBA32; break;
                    case TextureFormat.DXT1: format = TextureFormat.DXT5; break;
                    case TextureFormat.DXT1Crunched: format = TextureFormat.DXT5Crunched; break;
                }
            }
            return format;
        }
    }

    public static bool ReplaceTextures(Material material, Texture source, Texture destination)
    {
        using (var so = new SerializedObject(material))
        {
            foreach (var prop in so.ObjectReferenceProperties())
            {
                if (prop.objectReferenceValue == source)
                    prop.objectReferenceValue = destination;
            }

            return so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public Texture2D EditMaskTexture(Texture2D source, Color? multiply = null, Color? additive = null)
    {
        var mat = new Material(Shader.Find("Hidden/LightLimitChanger/MaskBaker"))
        {
            mainTexture = source,
        };
        using var disposer = AssetDisposer.Create(mat);

        mat.SetColor("_Color", multiply ?? Color.white);
        mat.SetColor("_ColorAdditive", additive ?? new(0,0,0,0));

        return GetOrBake(source, mat);
    }

}
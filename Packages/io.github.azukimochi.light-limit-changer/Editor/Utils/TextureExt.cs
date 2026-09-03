using System.Reflection;
using System.Runtime.InteropServices;
using nadena.dev.ndmf;
using UnityEditor.AssetImporters;
using UnityEngine.Rendering;

namespace io.github.azukimochi;

internal static class TextureExt
{
    public static TextureGenerationSettings GetGenerationSettings(this Texture texture, ErrorReport errorReport)
    {
        if (!ObjectRegistry.GetReference(texture).TryResolve(errorReport, out var sourceObj) || sourceObj is not Texture source)
            source = texture;

        TextureImporter importer = null;
        {
            var path = AssetDatabase.GetAssetPath(source);
            if (!string.IsNullOrEmpty(path) )
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
        }

        TextureGenerationSettings settings = new(importer?.textureType ?? TextureImporterType.Default);
        if (importer != null)
        {
            importer.ReadTextureSettings(settings.textureImporterSettings);
            var platform = importer.GetPlatformTextureSettings("");
            platform?.CopyTo(settings.platformSettings);
        }
        else
        {
            settings.textureImporterSettings.aniso = texture.anisoLevel;
            settings.textureImporterSettings.filterMode = texture.filterMode;
            settings.textureImporterSettings.readable = texture.isReadable;
            settings.textureImporterSettings.sRGBTexture = texture.isDataSRGB;
        }

        settings.enablePostProcessor = false;

        settings.textureImporterSettings.streamingMipmaps = true;
        settings.textureImporterSettings.readable = true;

        if (importer != null)
        {
            var info = (_GetSourceTextureInformationMethodInfo ??= typeof(TextureImporter).GetMethod("GetSourceTextureInformation", BindingFlags.NonPublic | BindingFlags.Instance))
                .Invoke(importer, null) as SourceTextureInformation;

            settings.sourceTextureInformation.width = source.width;
            settings.sourceTextureInformation.height = source.height;
            settings.sourceTextureInformation.hdr = info.hdr;
            settings.sourceTextureInformation.containsAlpha = info.containsAlpha;
        }
        else
        {
            settings.sourceTextureInformation.width = source.width;
            settings.sourceTextureInformation.height = source.height;

            if (source is Texture2D tex2d)
            {
                settings.sourceTextureInformation.hdr = tex2d.format is TextureFormat.RGBAFloat or TextureFormat.RGBAFloat;
                settings.sourceTextureInformation.containsAlpha = tex2d.alphaIsTransparency;
            }

            if (!settings.sourceTextureInformation.containsAlpha)
            {
                settings.sourceTextureInformation.containsAlpha = source.ContainsAlpha();
            }
        }

        return settings;
    }

    private static MethodInfo _GetSourceTextureInformationMethodInfo;

    private static bool ContainsAlpha(this Texture texture)
    {
        var alphaBinarizer = AssetUtils.FromGUID<Shader>("12354eec1321cd549bfff20a3054e38b"); // Shader.Find("Hidden/LightLimitChanger/AlphaBinarization");
        if (alphaBinarizer == null)
        {
            return ContainsAlphaFallback(texture);
        }
        var temp = RenderTexture.GetTemporary(32, 32, 0, RenderTextureFormat.R8);
        var active = RenderTexture.active;
        var mat = new Material(alphaBinarizer);
        try
        {
            Graphics.Blit(texture, temp, mat);
            var request = AsyncGPUReadback.Request(temp, 0, TextureFormat.R8);
            request.WaitForCompletion();

            using var data = request.GetData<byte>();
            var span = MemoryMarshal.Cast<byte, ulong>(data.AsReadOnlySpan());
            const ulong AllBitSets = unchecked((ulong)-1); // 0xFF_FF_FF_FF_FF_FF_FF_FF
            for (int i = 0; i < span.Length; i++)
            {
                var x = span[i];
                //Debug.LogError($"{i} => {Convert.ToString((long)x, 16).ToUpper().PadLeft(16, '0')}");
                if (x - AllBitSets != 0)
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temp);
            Object.DestroyImmediate(mat);
        }
    }

    private static bool ContainsAlphaFallback(this Texture texture)
    {
        // シェーダーが見つからないことがあるらしい
        // ので、遅いけど仕方ないのでフォールバックを用意しておく

        var temp = RenderTexture.GetTemporary(32, 32, 0);
        var active = RenderTexture.active;
        try
        {
            Graphics.Blit(texture, temp);
            var request = AsyncGPUReadback.Request(temp, 0, TextureFormat.RGBA32);
            request.WaitForCompletion();

            using var data = request.GetData<Color32>();

            foreach (var x in data.AsReadOnlySpan())
            {
                if (x.a < 1)
                    return true;
            }

            return false;
        }
        finally
        {
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temp);
        }
    }
}
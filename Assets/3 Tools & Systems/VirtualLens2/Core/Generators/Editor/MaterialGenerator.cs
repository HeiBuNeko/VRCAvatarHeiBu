#if VL2_DEVELOPMENT

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using VirtualLens2.AV3EditorLib;

namespace VirtualLens2.Generators
{
    public class MaterialGenerator
    {
        [MenuItem("Window/Logilabo/VirtualLens2/Generate DoF Materials")]
        static void Generate()
        {
            var instance = new MaterialGenerator();
            instance.Run();
        }

        private static readonly string[] Resolutions = new[] { "1080p", "1440p", "2160p", "4320p" };
        private static readonly string[] MSAALevels = new[] { "1x", "2x", "4x", "8x" };
        private static readonly string[] PostAntiAliasing = new[] { "", "FXAA", "SMAA" };

        private RenderTexture GetCaptureTarget(string kind, string resolution, string msaa)
        {
            return AssetDatabase.LoadAssetAtPath<RenderTexture>(
                $"Assets/VirtualLens2/Core/Textures/LogiBokeh/{resolution}/{kind}/{kind}_{resolution}_{msaa}.renderTexture");
        }

        private void Run()
        {
            var stateTex =
                AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/VirtualLens2/Core/Textures/State.renderTexture");
            var resultTex =
                AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/VirtualLens2/Core/Textures/Result.renderTexture");
            var smaaAreaTex =
                AssetDatabase.LoadAssetAtPath<Texture>("Assets/VirtualLens2/Core/Textures/SMAA/AreaTexDX10.tga");
            var smaaSearchTex =
                AssetDatabase.LoadAssetAtPath<Texture>("Assets/VirtualLens2/Core/Textures/SMAA/SearchTex.tga");
            foreach (var resolution in Resolutions)
            {
                var is1080p = resolution == "1080p";
                var resolutionKeyword = $"LB_TARGET_{resolution.ToUpper()}";
                var cocTex = AssetDatabase.LoadAssetAtPath<RenderTexture>(
                    $"Assets/VirtualLens2/Core/Textures/LogiBokeh/{resolution}/CocTex.renderTexture");
                var tileTex = AssetDatabase.LoadAssetAtPath<RenderTexture>(
                    $"Assets/VirtualLens2/Core/Textures/LogiBokeh/{resolution}/Tiles.renderTexture");
                var downsampledTex = is1080p
                    ? null
                    : AssetDatabase.LoadAssetAtPath<RenderTexture>(
                        $"Assets/VirtualLens2/Core/Textures/LogiBokeh/{resolution}/Downsampled.renderTexture");

                foreach (var msaa in MSAALevels)
                {
                    var useMSAA = msaa != "1x";
                    var colorTex = GetCaptureTarget("RGB", resolution, msaa);
                    var depthTex = GetCaptureTarget("Depth", resolution, msaa);

                    // ComputeCoc
                    {
                        var shader = Shader.Find("VirtualLens2/LogiBokeh/Unified/ComputeCoc");
                        var mat = new Material(shader);
                        if (useMSAA) { mat.SetKeyword(new LocalKeyword(shader, "WITH_MULTI_SAMPLING"), true); }
                        mat.SetKeyword(new LocalKeyword(shader, resolutionKeyword), true);
                        mat.SetTexture("_DepthTex", depthTex);
                        mat.SetTexture("_StateTex", stateTex);
                        AssetDatabase.CreateAsset(mat,
                            $"Assets/VirtualLens2/Core/Materials/LogiBokeh/Unified/ComputeCoc_{resolution}_{msaa}.mat");
                    }

                    // Downsample
                    if (!is1080p)
                    {
                        var shader = Shader.Find("VirtualLens2/LogiBokeh/Unified/Downsample");
                        var mat = new Material(shader);
                        if (useMSAA) { mat.SetKeyword(new LocalKeyword(shader, "WITH_MULTI_SAMPLING"), true); }
                        mat.SetKeyword(new LocalKeyword(shader, resolutionKeyword), true);
                        mat.SetTexture("_MainTex", colorTex);
                        mat.SetTexture("_CocTex", cocTex);
                        mat.SetTexture("_DepthTex", depthTex);
                        mat.SetTexture("_TileTex", tileTex);
                        mat.SetTexture("_StateTex", stateTex);
                        AssetDatabase.CreateAsset(mat,
                            $"Assets/VirtualLens2/Core/Materials/LogiBokeh/Unified/Downsample_{resolution}_{msaa}.mat");
                    }

                    foreach (var aa in PostAntiAliasing)
                    {
                        var isSMAA = aa == "SMAA";
                        // Preview
                        {
                            var postfix = (resolution == "1080p" ? "1080p" : "") + aa;
                            var shader = Shader.Find("VirtualLens2/LogiBokeh/Unified/Preview" + postfix);
                            var mat = new Material(shader);
                            if (useMSAA) { mat.SetKeyword(new LocalKeyword(shader, "WITH_MULTI_SAMPLING"), true); }
                            if (!is1080p) { mat.SetKeyword(new LocalKeyword(shader, resolutionKeyword), true); }
                            mat.SetTexture("_MainTex", colorTex);
                            if (!is1080p) { mat.SetTexture("_DownsampledTex", downsampledTex); }
                            mat.SetTexture("_DepthTex", depthTex);
                            mat.SetTexture("_CocTex", cocTex);
                            mat.SetTexture("_TileTex", tileTex);
                            mat.SetTexture("_StateTex", stateTex);
                            if (isSMAA)
                            {
                                mat.SetTexture("_SMAAAreaTex", smaaAreaTex);
                                mat.SetTexture("_SMAASearchTex", smaaSearchTex);
                            }
                            AssetDatabase.CreateAsset(mat,
                                $"Assets/VirtualLens2/Core/Materials/LogiBokeh/Unified/Preview{aa}_{resolution}_{msaa}.mat");
                        }

                        // Render
                        if (is1080p)
                        {
                            var shader = Shader.Find("VirtualLens2/LogiBokeh/Unified/CopyResult");
                            var mat = new Material(shader);
                            mat.SetTexture("_ResultTex", resultTex);
                            AssetDatabase.CreateAsset(mat,
                                $"Assets/VirtualLens2/Core/Materials/LogiBokeh/Unified/Render{aa}_{resolution}_{msaa}.mat");
                        }
                        else
                        {
                            var shader = Shader.Find("VirtualLens2/LogiBokeh/Unified/Full" + aa);
                            var mat = new Material(shader);
                            if (useMSAA) { mat.SetKeyword(new LocalKeyword(shader, "WITH_MULTI_SAMPLING"), true); }
                            mat.SetTexture("_MainTex", colorTex);
                            mat.SetTexture("_DownsampledTex", downsampledTex);
                            mat.SetTexture("_DepthTex", depthTex);
                            mat.SetTexture("_CocTex", cocTex);
                            mat.SetTexture("_TileTex", tileTex);
                            mat.SetTexture("_ResultTex", resultTex);
                            if (isSMAA)
                            {
                                mat.SetTexture("_SMAAAreaTex", smaaAreaTex);
                                mat.SetTexture("_SMAASearchTex", smaaSearchTex);
                            }
                            AssetDatabase.CreateAsset(mat,
                                $"Assets/VirtualLens2/Core/Materials/LogiBokeh/Unified/Render{aa}_{resolution}_{msaa}.mat");
                        }
                    }
                }

                // ComputeTiles
                {
                    var shader = Shader.Find("VirtualLens2/LogiBokeh/Unified/ComputeTiles");
                    var mat = new Material(shader);
                    mat.SetKeyword(new LocalKeyword(shader, $"LB_TARGET_{resolution.ToUpper()}"), true);
                    mat.SetTexture("_CocTex", cocTex);
                    AssetDatabase.CreateAsset(mat,
                        $"Assets/VirtualLens2/Core/Materials/LogiBokeh/Unified/ComputeTiles_{resolution}.mat");
                }
            }
        }
    }
}

#endif

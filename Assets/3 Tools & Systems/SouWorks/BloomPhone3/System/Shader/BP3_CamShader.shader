Shader "SouWorks/BP3_CamShader"
{
    Properties
    {
        _RenderTexture ("RenderTexture", 2D) = "black" {}

        [Toggle] _FlipX ("Flip X", Int) = 0
        [Toggle] _FlipY ("Flip Y", Int) = 0

        [Toggle] _OverwriteEnable ("Overwrite Camera", Int) = 0
        _OverwriteRadius ("Overwrite Radius", Range(0,0.2)) = 0.15

        _LensZoom ("Lens Zoom", Range(0.5,2)) = 1
        _LensDistortion ("Lens Distortion", Range(-2,2)) = 0
        _LensDistortionCurve ("Distortion Curve", Range(0.25,8)) = 2

        _Exposure ("Exposure", Range(-10,10)) = 0
        _Temperature ("Temperature", Range(-3,3)) = 0
        _Tint ("Tint", Range(-3,3)) = 0
        _HighlightRollOff ("HighlightRollOff", Range(0,1)) = 1
        _Contrast ("Contrast", Range(0,4)) = 1
        _Saturation ("Saturation", Range(0,4)) = 1

        _CAAmount ("Chromatic Aberration", Range(0,0.25)) = 0
        _CACurve ("CA Curve", Range(0.25,8)) = 2

        _DOFFocusRadius ("DOF Focus Radius", Range(0,1)) = 0.12
        _DOFFalloff ("DOF Falloff", Range(0.01,6)) = 0.6
        _DOFBlurMaxPx ("DOF Max Blur (px)", Range(0,256)) = 0
        [Enum(Taps10,10,Taps8,8,Taps6,6,Taps4,4)] _DOFTaps ("DOF Samples", Float) = 6

        _SharpenStrength ("Sharpen Strength", Range(0,10)) = 0
        _SharpenEdgeScale ("Sharpen Edge Scale", Range(0.5,8)) = 2
        _SharpenEdgeThreshold ("Sharpen Edge Threshold", Range(0,0.3)) = 0.02
        _SharpenClamp ("Sharpen Clamp", Range(0,1)) = 0.3
        _SharpenFocusBias ("Sharpen Focus Bias", Range(0,1)) = 1

        _VignetteStrength ("Vignette Strength", Range(0,3)) = 0
        _VignetteSize ("Vignette Size", Range(0,1)) = 0.75
        _VignetteFeather ("Vignette Feather", Range(0,1)) = 0.45
        _VignetteShape ("Vignette Shape", Range(0,1)) = 1
        _VignetteColor ("Vignette Color", Color) = (0,0,0,1)

        [Enum(Off,0,Filter1,1,Filter2,2,Filter3,3,Filter4,4,Filter5,5,Filter6,6)] _FilterSelect ("Filter", Float) = 0
        _FilterOpacity ("Filter Opacity", Range(0,1)) = 0
        [Enum(Normal,0,Add,1,Multiply,2,Screen,3,Overlay,4)] _FilterBlendMode ("Filter Blend Mode", Float) = 0
        [NoScaleOffset] _FilterTex1 ("Filter 1", 2D) = "white" {}
        [NoScaleOffset] _FilterTex2 ("Filter 2", 2D) = "white" {}
        [NoScaleOffset] _FilterTex3 ("Filter 3", 2D) = "white" {}
        [NoScaleOffset] _FilterTex4 ("Filter 4", 2D) = "white" {}
        [NoScaleOffset] _FilterTex5 ("Filter 5", 2D) = "white" {}
        [NoScaleOffset] _FilterTex6 ("Filter 6", 2D) = "white" {}

        [NoScaleOffset] _PreviewUITex ("Preview UI Texture", 2D) = "white" {}
        _PreviewUIOpacity ("Preview UI Opacity", Range(0,1)) = 0
        _PreviewUITint ("Preview UI Tint", Color) = (1,1,1,1)

        _ShutterColor ("Shutter Color", Color) = (1,1,1,1)
        _ShutterOpacity ("Shutter Opacity", Range(0,1)) = 0

        _OutputOpacity ("Output Opacity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"
        #pragma target 3.0

        #if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            #define BP3_XR_ACTIVE 1
        #else
            #define BP3_XR_ACTIVE 0
        #endif

        sampler2D _RenderTexture;
        float4 _RenderTexture_TexelSize;

        float _FlipX;
        float _FlipY;

        float _OverwriteEnable;
        float _OverwriteRadius;

        float _LensZoom;
        float _LensDistortion;
        float _LensDistortionCurve;

        float _Exposure;
        float _Temperature;
        float _Tint;
        float _HighlightRollOff;
        float _Contrast;
        float _Saturation;

        float _CAAmount;
        float _CACurve;

        float _DOFFocusRadius;
        float _DOFFalloff;
        float _DOFBlurMaxPx;
        float _DOFTaps;

        float _SharpenStrength;
        float _SharpenEdgeScale;
        float _SharpenEdgeThreshold;
        float _SharpenClamp;
        float _SharpenFocusBias;

        float _VignetteStrength;
        float _VignetteSize;
        float _VignetteFeather;
        float _VignetteShape;
        float4 _VignetteColor;

        float _FilterSelect;
        float _FilterOpacity;
        float _FilterBlendMode;
        sampler2D _FilterTex1;
        sampler2D _FilterTex2;
        sampler2D _FilterTex3;
        sampler2D _FilterTex4;
        sampler2D _FilterTex5;
        sampler2D _FilterTex6;

        sampler2D _PreviewUITex;
        float _PreviewUIOpacity;
        float4 _PreviewUITint;

        float4 _ShutterColor;
        float _ShutterOpacity;

        float _OutputOpacity;

        static const float3 kLuma = float3(0.299, 0.587, 0.114);

        static const float2 kDofOffsets9[9] =
        {
            float2(-0.173800,  0.159215),
            float2( 0.035691, -0.406685),
            float2( 0.320675,  0.418264),
            float2(-0.614077, -0.108622),
            float2( 0.596625, -0.379524),
            float2(-0.202942,  0.754934),
            float2(-0.391696, -0.754186),
            float2( 0.857479,  0.313150),
            float2(-0.898302,  0.370806)
        };

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv     : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f_preview
        {
            float4 pos : SV_POSITION;
            float2 uv  : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct v2f_overwrite
        {
            float4 pos         : SV_POSITION;
            float4 scrPos      : TEXCOORD0;
            float  enabledMask : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float3 ObjectOriginWS()
        {
            return mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
        }

        float3 CameraPosWS()
        {
        #if BP3_XR_ACTIVE
            return unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex];
        #else
            return _WorldSpaceCameraPos;
        #endif
        }

        float InsideTriggerSphereWS()
        {
            float r = saturate(_OverwriteRadius / 0.2) * 0.2;
            float3 d = CameraPosWS() - ObjectOriginWS();
            return dot(d, d) <= (r * r) ? 1.0 : 0.0;
        }

        float3 ToLinear(float3 c)
        {
        #if defined(UNITY_COLORSPACE_GAMMA)
            return GammaToLinearSpace(c);
        #else
            return c;
        #endif
        }

        float3 ToGamma(float3 c)
        {
        #if defined(UNITY_COLORSPACE_GAMMA)
            return LinearToGammaSpace(c);
        #else
            return c;
        #endif
        }

        float2 ClampUV(float2 uv)
        {
            return clamp(uv, 0.0, 1.0);
        }

        float Aspect()
        {
            return _RenderTexture_TexelSize.z / max(1.0, _RenderTexture_TexelSize.w);
        }

        float2 FixUVForRenderTexture(float2 uv)
        {
        #if UNITY_UV_STARTS_AT_TOP
            if (_RenderTexture_TexelSize.y < 0) uv.y = 1.0 - uv.y;
        #endif
            return uv;
        }

        float2 ApplyFlip(float2 uv)
        {
            if (_FlipX > 0.5) uv.x = 1.0 - uv.x;
            if (_FlipY > 0.5) uv.y = 1.0 - uv.y;
            return uv;
        }

        float2 LensUV(float2 uv)
        {
            float2 c = float2(0.5, 0.5);
            float2 d = uv - c;

            float z = max(1e-5, _LensZoom);
            d /= z;

            float kDist = _LensDistortion;
            if (abs(kDist) <= 1e-6)
                return ClampUV(c + d);

            float a = Aspect();
            float2 da = float2(d.x * a, d.y);
            float r = length(da);
            float k = 1.0 + kDist * pow(max(0.0, r), _LensDistortionCurve);

            return ClampUV(c + d * k);
        }

        float3 WhiteBalanceMul(float t, float g)
        {
            float3 m = float3(1.0 + t * 0.18 + g * 0.06,
                              1.0 - g * 0.12,
                              1.0 - t * 0.18 + g * 0.06);
            float s = (m.x + m.y + m.z) * (1.0 / 3.0);
            return m / max(1e-5, s);
        }

        float3 EvalExposureMul()
        {
            return exp2(_Exposure) * WhiteBalanceMul(_Temperature, _Tint);
        }

        float3 ApplyHighlightRollOff(float3 rgb)
        {
            float t = saturate(_HighlightRollOff);
            float3 soft = rgb / (1.0 + max(rgb, 0.0));
            return lerp(rgb, soft, t);
        }

        float3 ApplyContrastSaturation(float3 rgb)
        {
            rgb = saturate((rgb - 0.5) * _Contrast + 0.5);
            float l = dot(rgb, kLuma);
            rgb = lerp(l.xxx, rgb, _Saturation);
            return saturate(rgb);
        }

        float3 SampleRenderTextureLinear(float2 uvIn)
        {
            float2 u = FixUVForRenderTexture(uvIn);
            u = ApplyFlip(u);
            u = LensUV(u);
            return ToLinear(tex2D(_RenderTexture, u).rgb);
        }

        float3 SampleExposure(float2 uvIn, float3 exposureMul)
        {
            return max(SampleRenderTextureLinear(uvIn) * exposureMul, 0.0);
        }

        float3 SampleCA(float2 uvIn, float3 exposureMul)
        {
            float a = _CAAmount;
            float3 res = 0.0;

            if (a <= 0.0)
            {
                res = SampleExposure(uvIn, exposureMul);
            }
            else
            {
                float2 c = float2(0.5, 0.5);
                float2 d = uvIn - c;
                float r = length(float2(d.x * Aspect(), d.y));
                float f = pow(max(0.0, r), _CACurve) * a;

                float2 uvR = ClampUV(c + d * (1.0 + f));
                float2 uvG = ClampUV(c + d);
                float2 uvB = ClampUV(c + d * (1.0 - f));

                float3 cr = SampleExposure(uvR, exposureMul);
                float3 cg = SampleExposure(uvG, exposureMul);
                float3 cb = SampleExposure(uvB, exposureMul);

                res = float3(cr.r, cg.g, cb.b);
            }

            return res;
        }

        float Radial01(float2 uv)
        {
            float a = Aspect();
            float2 d = uv - 0.5;
            d.x *= a;
            float maxD = length(float2(0.5 * a, 0.5));
            return length(d) / max(1e-5, maxD);
        }

        float DOFMask(float2 uv)
        {
            float r = Radial01(uv);
            float fr = saturate(_DOFFocusRadius);
            float x = saturate((r - fr) / max(1e-5, 1.0 - fr));
            return pow(x, max(1e-4, _DOFFalloff));
        }

        int GetDOFTaps()
        {
            int taps = (int)floor(_DOFTaps + 0.5);
            if (taps >= 9) return 10;
            if (taps >= 7) return 8;
            if (taps >= 5) return 6;
            return 4;
        }

        float3 ApplyDOF(float2 uvIn, float3 center, float3 exposureMul, out float dofMask)
        {
            dofMask = (_DOFBlurMaxPx > 0.0) ? DOFMask(uvIn) : 0.0;

            float blurPx = _DOFBlurMaxPx * dofMask;
            if (blurPx < 0.5) return center;

            float2 texel = abs(_RenderTexture_TexelSize.xy);
            float2 s = blurPx * texel;

            int taps = GetDOFTaps();
            float3 sum = center;

            if (taps == 10)
            {
                [unroll]
                for (int i = 0; i < 9; i++)
                    sum += SampleCA(uvIn + kDofOffsets9[i] * s, exposureMul);
                return sum * 0.1;
            }

            if (taps == 8)
            {
                [unroll]
                for (int i = 0; i < 7; i++)
                    sum += SampleCA(uvIn + kDofOffsets9[i] * s, exposureMul);
                return sum * 0.125;
            }

            if (taps == 6)
            {
                [unroll]
                for (int i = 0; i < 5; i++)
                    sum += SampleCA(uvIn + kDofOffsets9[i] * s, exposureMul);
                return sum * (1.0 / 6.0);
            }

            [unroll]
            for (int i = 0; i < 3; i++)
                sum += SampleCA(uvIn + kDofOffsets9[i] * s, exposureMul);

            return sum * 0.25;
        }

        float Smooth01(float x)
        {
            x = saturate(x);
            return x * x * (3.0 - 2.0 * x);
        }

        float3 ApplySharpen(float2 uvIn, float3 rgbAfterDof, float focusMask)
        {
            float strength = _SharpenStrength;
            if (strength <= 0.0) return rgbAfterDof;

            float amount = strength * lerp(1.0, focusMask, saturate(_SharpenFocusBias));
            if (amount <= 0.0) return rgbAfterDof;

            float L = dot(rgbAfterDof, kLuma);

            float2 texSize = float2(_RenderTexture_TexelSize.z, _RenderTexture_TexelSize.w);
            float2 duvdx = ddx(uvIn);
            float2 duvdy = ddy(uvIn);

            float fp = max(length(duvdx * texSize), length(duvdy * texSize));
            float s = max(0.5, _SharpenEdgeScale);
            float fpScaled = max(1e-5, fp * s);

            float edge = fwidth(L) / fpScaled;

            float thr = _SharpenEdgeThreshold;
            float knee = thr * 0.5 + 0.01;
            float m = (edge - thr) / max(1e-5, knee);
            float edgeMask = Smooth01(m);

            float a01 = saturate(amount * 0.1);

            float protect = smoothstep(0.02, 0.15, L) * (1.0 - smoothstep(0.85, 0.98, L));
            float boost = 1.0 + (3.0 * a01) * edgeMask * protect;

            float Lc = saturate((L - 0.5) * boost + 0.5);

            float maxDelta = lerp(0.0, 0.20, saturate(_SharpenClamp));
            float dL = clamp(Lc - L, -maxDelta, maxDelta);

            float L2 = max(L + dL, 0.0);
            float scale = (L > 1e-5) ? (L2 / L) : 1.0;

            return rgbAfterDof * scale;
        }

        float3 ApplyVignette(float2 uv, float3 rgb)
        {
            float strength = _VignetteStrength;
            if (strength <= 0.0) return rgb;

            float a = Aspect();
            float2 d = uv - 0.5;

            float2 dRect = d;
            float2 dAsp  = float2(d.x * a, d.y);

            float shape = saturate(_VignetteShape);
            float2 dv = lerp(dRect, dAsp, shape);

            float maxD = length(float2(0.5 * lerp(1.0, a, shape), 0.5));
            float dist = length(dv) / max(1e-5, maxD);

            float w = max(1e-5, _VignetteFeather);
            float v = saturate((dist - _VignetteSize) / w);
            v = v * v * (3.0 - 2.0 * v);

            float k = exp2(-strength * v * 2.0);
            float3 vc = ToLinear(_VignetteColor.rgb);

            return lerp(vc, rgb, k);
        }

        float3 BlendScreen(float3 base, float3 over)
        {
            base = saturate(base);
            over = saturate(over);
            return 1.0 - (1.0 - base) * (1.0 - over);
        }

        float3 BlendOverlay(float3 base, float3 over)
        {
            base = saturate(base);
            over = saturate(over);
            float3 lt = 2.0 * base * over;
            float3 gt = 1.0 - 2.0 * (1.0 - base) * (1.0 - over);
            float3 s = step(0.5, base);
            return lerp(lt, gt, s);
        }

        int FilterSelectInt()
        {
            int sel = (int)floor(_FilterSelect + 0.5);
            return max(0, min(6, sel));
        }

        float4 SampleFilterTex(float2 uv, int sel)
        {
            float4 t = float4(0.0, 0.0, 0.0, 0.0);

            if (sel == 1) t = tex2D(_FilterTex1, uv);
            else if (sel == 2) t = tex2D(_FilterTex2, uv);
            else if (sel == 3) t = tex2D(_FilterTex3, uv);
            else if (sel == 4) t = tex2D(_FilterTex4, uv);
            else if (sel == 5) t = tex2D(_FilterTex5, uv);
            else if (sel == 6) t = tex2D(_FilterTex6, uv);

            return t;
        }

        float3 ApplyFilter(float2 uvIn, float3 base)
        {
            if (_FilterOpacity <= 0.0) return base;

            int sel = FilterSelectInt();
            if (sel == 0) return base;

            float4 t = SampleFilterTex(uvIn, sel);
            float a = saturate(_FilterOpacity * t.a);
            if (a <= 0.0) return base;

            float3 over = ToLinear(t.rgb);

            float3 blend;
            if (_FilterBlendMode < 0.5)
                blend = over;
            else if (_FilterBlendMode < 1.5)
                blend = base + over;
            else if (_FilterBlendMode < 2.5)
                blend = base * over;
            else if (_FilterBlendMode < 3.5)
                blend = BlendScreen(base, over);
            else
                blend = BlendOverlay(base, over);

            return lerp(base, blend, a);
        }

        float3 ApplyShutter(float3 rgb)
        {
            float a = saturate(_ShutterOpacity * _ShutterColor.a);
            if (a <= 0.0) return rgb;

            float3 c = ToLinear(_ShutterColor.rgb);
            return lerp(rgb, c, a);
        }

        float4 SampleProcessedLinear(float2 uvIn)
        {
            float3 exposureMul = EvalExposureMul();

            float3 center = SampleCA(uvIn, exposureMul);

            float dofMask;
            float3 rgbAfterDof = ApplyDOF(uvIn, center, exposureMul, dofMask);

            float focusMask = saturate(1.0 - dofMask);
            float3 rgb = ApplySharpen(uvIn, rgbAfterDof, focusMask);

            rgb = ApplyHighlightRollOff(rgb);
            rgb = ApplyContrastSaturation(rgb);

            rgb = ApplyVignette(uvIn, rgb);
            rgb = ApplyFilter(uvIn, rgb);

            return float4(rgb, _OutputOpacity);
        }

        float3 ApplyPreviewUI(float2 uvIn, float3 rgb)
        {
            float ua = _PreviewUIOpacity * _PreviewUITint.a;
            if (ua <= 0.0) return rgb;

            float4 u = tex2D(_PreviewUITex, uvIn);
            float a = saturate(ua * u.a);
            if (a <= 0.0) return rgb;

            float3 ui = ToLinear(u.rgb) * ToLinear(_PreviewUITint.rgb);
            return lerp(rgb, ui, a);
        }
        ENDCG

        Pass
        {
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex VertPreview
            #pragma fragment FragPreview
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            v2f_preview VertPreview(appdata v)
            {
                v2f_preview o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 FragPreview(v2f_preview i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uvIn = i.uv;
                float4 p = SampleProcessedLinear(uvIn);
                float3 rgb = ApplyPreviewUI(uvIn, p.rgb);
                rgb = ApplyShutter(rgb);

                rgb = saturate(rgb);
                rgb = ToGamma(rgb);

                return float4(rgb, p.a);
            }
            ENDCG
        }

        Pass
        {
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex VertOverwrite
            #pragma fragment FragOverwrite
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            v2f_overwrite VertOverwrite(appdata v)
            {
                v2f_overwrite o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            #if BP3_XR_ACTIVE
                o.pos = float4(0.0, 0.0, 2.0, 1.0);
                o.scrPos = 0.0;
                o.enabledMask = 0.0;
                return o;
            #else
                float enabled = (_OverwriteEnable > 0.5) ? 1.0 : 0.0;
                float inside = InsideTriggerSphereWS();
                float s = enabled * inside;

                float2 ndc = v.uv * 2.0 - 1.0;
                o.pos = float4(ndc * s, 0.0, 1.0);
                o.scrPos = ComputeScreenPos(o.pos);
                o.enabledMask = s;
                return o;
            #endif
            }

            fixed4 FragOverwrite(v2f_overwrite i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uvIn = i.scrPos.xy / i.scrPos.w;
                uvIn = UnityStereoTransformScreenSpaceTex(uvIn);

                float4 p = SampleProcessedLinear(uvIn);

                float3 rgb = saturate(p.rgb);
                rgb = ToGamma(rgb);

                float a = p.a * i.enabledMask;

                return float4(rgb, a);
            }
            ENDCG
        }
    }

    FallBack Off
}
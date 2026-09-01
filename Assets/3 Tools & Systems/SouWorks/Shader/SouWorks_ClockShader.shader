Shader "SouWorks/SouWorksClockShader"
{
    Properties
    {
        [Header(SouWorksClock)]
        [NoScaleOffset] _ClockNumbers ("Clock Numbers", 2D) = "white" {}

        [Enum(HHMM,0, HHMMSS,1)] _TimeFormat ("Time Format", Float) = 0
        [Toggle] _Use12Hour ("12 Hour Format", Float) = 0
        [Toggle] _ShowHourLeadingZero ("Show Hour Leading Zero", Float) = 0
        [Toggle] _BlinkColons ("Blink Colons", Float) = 0

        _ClockScale ("Clock Scale", Range(0.01, 4.0)) = 1
        _OffsetX ("Offset X", Float) = 0
        _OffsetY ("Offset Y", Float) = 0
        _Tracking ("Tracking", Range(-0.99, 2.00)) = 0

        [Toggle] _AutoFit ("Auto Fit", Float) = 1
        _FitPadding ("Fit Padding", Range(0, 0.25)) = 0

        [HDR] _ClockColor ("Clock Color", Color) = (1,1,1,1)
        _ClockOpacity ("Clock Opacity", Range(0,1)) = 1
        _Emission ("Emission", Range(0,10)) = 0

        [Toggle] _AlphaClip ("Alpha Clip", Float) = 0
        _ClipThreshold ("Clip Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Packages/com.vrchat.base/ShaderLibrary/VRCTime.cginc"

            sampler2D _ClockNumbers;
            float4 _ClockNumbers_TexelSize;

            float  _TimeFormat;
            float  _Use12Hour;
            float  _ShowHourLeadingZero;
            float  _BlinkColons;

            float  _ClockScale;
            float  _OffsetX;
            float  _OffsetY;
            float  _Tracking;

            float  _AutoFit;
            float  _FitPadding;

            float4 _ClockColor;
            float  _ClockOpacity;
            float  _Emission;

            float  _AlphaClip;
            float  _ClipThreshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            void CodeToCell(int code, out int col, out int rowTop)
            {
                if (code >= 1 && code <= 8)
                {
                    uint idx = (uint)(code - 1);
                    rowTop = (int)(idx >> 2);
                    col    = (int)(idx & 3u);
                }
                else
                {
                    rowTop = 2;
                    col = (code == 9) ? 0 : ((code == 0) ? 1 : (code - 8));
                }
            }

            half SampleGlyphA(int code, float gx, float gy)
            {
                if (code < 0) return 0;

                int col, rowTop;
                CodeToCell(code, col, rowTop);

                const float cellW = 0.25;
                const float cellH = 1.0 / 3.0;

                float2 baseUV = float2((float)col * cellW, 1.0 - (float)(rowTop + 1) * cellH);

                float2 halfTexel = _ClockNumbers_TexelSize.xy * 0.5;
                float2 cellMin = baseUV + halfTexel;
                float2 cellMax = baseUV + float2(cellW, cellH) - halfTexel;

                float2 t = saturate(float2(gx, gy));
                float2 atlasUV = lerp(cellMin, cellMax, t);

                return tex2Dlod(_ClockNumbers, float4(atlasUV, 0, 0)).a;
            }

            void SplitTensOnes(uint v, out uint tens, out uint ones)
            {
                float fv = (float)v;
                tens = (uint)(fv * 0.1 + 1e-3);
                ones = v - tens * 10u;
            }

            int CanonicalCode(int idx, uint ht, uint ho, uint mt, uint mo, uint st, uint so)
            {
                if (idx == 0) return (int)ht;
                if (idx == 1) return (int)ho;
                if (idx == 2) return 10;
                if (idx == 3) return (int)mt;
                if (idx == 4) return (int)mo;
                if (idx == 5) return 10;
                if (idx == 6) return (int)st;
                if (idx == 7) return (int)so;
                return -1;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return 0;

                uint h, m, s, ms;
                VRC_GetLocalTime(h, m, s, ms);

                if (_Use12Hour > 0.5)
                {
                    if (h >= 12u) h -= 12u;
                    if (h == 0u) h = 12u;
                }

                uint ht, ho, mt, mo, st, so;
                SplitTensOnes(h, ht, ho);
                SplitTensOnes(m, mt, mo);
                SplitTensOnes(s, st, so);

                bool includeSeconds = (_TimeFormat > 0.5);
                bool suppressHourTens = (ht == 0u) && (_ShowHourLeadingZero < 0.5);

                int L = includeSeconds ? 8 : 5;
                if (suppressHourTens) L -= 1;

                const float baseGw = 1.0 / 5.0;
                float stride = baseGw * (1.0 + _Tracking);
                stride = max(1e-4, stride);

                const float glyphWH = 0.75;
                float baseGh = baseGw / glyphWH;

                float totalW = (float)(L - 1) * stride + baseGw;
                float totalH = baseGh;

                float pad = saturate(_FitPadding);
                float usableW = max(1e-4, 1.0 - 2.0 * pad);
                float usableH = max(1e-4, 1.0 - 2.0 * pad);

                float fit = 1.0;
                if (_AutoFit > 0.5)
                {
                    fit = min(1.0, min(usableW / totalW, usableH / totalH));
                }

                float scale = max(1e-4, _ClockScale) * fit;

                float gw = baseGw * scale;
                float gh = baseGh * scale;
                float strideS = stride * scale;

                float2 center = float2(0.5 + _OffsetX, 0.5 + _OffsetY);
                float totalWS = (float)(L - 1) * strideS + gw;
                float originX = center.x - 0.5 * totalWS;

                float x = uv.x - originX;
                if (x < 0.0 || x > totalWS) return 0;

                float y0 = center.y - 0.5 * gh;
                float y1 = y0 + gh;
                if (uv.y < y0 || uv.y > y1) return 0;

                float gy = (uv.y - y0) / gh;

                float invStride = rcp(strideS);
                int sMin = (int)ceil((x - gw) * invStride);
                int sMax = (int)floor(x * invStride);
                sMin = clamp(sMin, 0, L - 1);
                sMax = clamp(sMax, 0, L - 1);
                if (sMin > sMax) return 0;

                float colonMul = 1.0;
                if (_BlinkColons > 0.5)
                {
                    colonMul = ((s & 1u) == 0u) ? 1.0 : 0.0;
                }

                int idxOffset = suppressHourTens ? 1 : 0;
                int idxLimit  = includeSeconds ? 7 : 4;

                half aMax = 0;
                for (int slot = sMin; slot <= sMax; slot++)
                {
                    float localX = x - (float)slot * strideS;
                    float gx = localX / gw;

                    int idx = slot + idxOffset;
                    if (idx > idxLimit) continue;

                    int code = CanonicalCode(idx, ht, ho, mt, mo, st, so);
                    half a = SampleGlyphA(code, gx, gy);
                    if (code == 10) a *= (half)colonMul;

                    aMax = max(aMax, a);
                }

                half opacity = (half)saturate(_ClockOpacity);
                half outA = aMax * (half)_ClockColor.a * opacity;

                if (_AlphaClip > 0.5)
                {
                    clip((float)outA - _ClipThreshold);
                }

                half3 rgb = (half3)_ClockColor.rgb * (1.0h + (half)_Emission);
                return fixed4(rgb, outA);
            }
            ENDCG
        }
    }
}
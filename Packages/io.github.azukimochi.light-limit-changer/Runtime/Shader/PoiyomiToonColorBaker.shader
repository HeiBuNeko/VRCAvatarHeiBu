Shader "Hidden/LightLimitChanger/PoiyomiToonColorBaker"
{
    Properties
    {
        [sRGBWarning(true)]
        _MainTex    ("Main Texture",        2D)             = "white" {}
        _Color      ("Main Color (Tint)",   Color)          = (1, 1, 1, 1)
        _Saturation ("Saturation",          Range(-1, 10))  = 0
        _Cutoff     ("Alpha Cutoff",        Range(0, 1))    = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue"      = "Geometry"
        }
        Cull Off
        ZWrite On
        ZTest LEqual
        Blend Off

        Pass
        {
            Name "POI_BAKE_MAIN_COLOR_SAT"
            Tags { "LightMode" = "Always" }

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            float     _Saturation;

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
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            inline float RGBToLuminance(float3 rgb)
            {
                return dot(rgb, float3(0.2126, 0.7152, 0.0722));
            }

            inline float3 AdjustSaturation(float3 color, float satValue)
            {
                float lum = RGBToLuminance(color);
                return lerp(float3(lum, lum, lum), color, 1.0 + satValue);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 texColor = tex2D(_MainTex, i.uv);
                float3 saturated = AdjustSaturation(texColor.rgb, _Saturation);
                float3 finalRGB = saturated * _Color.rgb;
                float  finalA   = texColor.a * _Color.a;

                return float4(finalRGB, finalA);
            }
            ENDCG
        }
    }
}
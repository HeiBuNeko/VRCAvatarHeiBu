Shader "Hidden/SataniaTabako/WizardHighlight"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _FillOpacity ("Fill Opacity", Range(0, 1)) = 0.15
        _Width ("Outline Width (px)", Float) = 2
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }

        Pass
        {
            Name "Fill"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _FillOpacity;

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(_Color.rgb, _FillOpacity);
            }
            ENDCG
        }

        Pass
        {
            Name "Outline"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Width;

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 dir = viewNormal.xy;
                float len = length(dir);
                dir = len > 1e-5 ? dir / len : float2(0, 0);
                clip.xy += dir * (_Width * 2.0 / _ScreenParams.xy) * clip.w;
                o.pos = clip;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
    Fallback Off
}

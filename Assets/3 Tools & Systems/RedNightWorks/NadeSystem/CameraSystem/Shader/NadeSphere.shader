Shader "RedNightWorks/NadeSphere"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _RimCurve("Rim Curve", Float) = 1.0
    }
    SubShader
    {
        Tags
        {  
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "VRCFallback"="Hidden"
        }
        LOD 100

        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normalDir : TEXCOORD0;
                float3 worldPos : TEXCORD1;
            };

            fixed4 _Color;
            fixed _RimCurve;
            float _VRChatCameraMode;
            //0 - Rendering normally
            //1 - Rendering in VR handheld camera
            //2 - Rendering in Desktop handheld camera
            //3 - Rendering for a screenshot

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_VRChatCameraMode != 0) discard;

                fixed4 color = _Color;

                half3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                half rimFade = saturate(dot(viewDirection, i.normalDir));
                rimFade = pow(rimFade, _RimCurve); // Adjust the exponent for rim effect strength

                color.rgb *= rimFade;

                return color;
            }
            ENDCG
        }
    }
}

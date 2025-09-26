Shader "RedNightWorks/NadeShadow"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 0)
        _DepthCurve("Depth Curve", Float) = 1.6
        _DepthTrend("Depth Trend", Float) = 1.5
        _MinLight("Minimum Light", Range(0,1)) = 0.05
        _DistFadeTrend("Distance Fade Trend", Float) = 10.0
        _RimFadeCurve("Rim Fade Curve", Float) = 3
        [Space(10)]
        [Toggle]
        _DepthDebug("Depth Debug", int) = 0
    }
    SubShader
    {
        Tags{"Queue"="Transparent+2000" "VRCFallback"="Hidden"}

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            ZWrite Off
            ZTest Always

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 grabPos : TEXCORD0;
                float3 worldPos : TEXCORD2;
                float3 objPos : TEXCORD3;
                float3 normalDir : TEXCORD4;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed _DepthCurve;
            fixed _DepthTrend;
            fixed _MinLight;
            fixed _DistFadeTrend;
            fixed _RimFadeCurve;
            int _DepthDebug;
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthTexture);

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.objPos = unity_ObjectToWorld._m03_m13_m23;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 outColor;

                //球の半径
                fixed radius = length(i.worldPos - i.objPos);
                //スクリーンスペースUV取得
                fixed2 screenSpaceUV = i.grabPos.xy / i.grabPos.w;
                //Depth取得
                float depth = LinearEyeDepth(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, screenSpaceUV)).x;

                depth = pow(depth*_DepthTrend, _DepthCurve) + _MinLight;
                depth = saturate(depth);

                //距離フェード
                half dist = length(_WorldSpaceCameraPos.xyz - i.objPos.xyz);
                half distFade = saturate(dist*_DistFadeTrend - _DistFadeTrend*radius);
                depth = lerp(depth, 1, distFade);

                //リムフェード
                half3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                half rimFade = saturate(-dot(viewDirection, i.normalDir)); //メッシュの裏側なので必要なのは-側
                rimFade = pow(rimFade, _RimFadeCurve);
                depth = lerp(1, depth, rimFade);


                if(_DepthDebug)
                {
                    outColor.a = 1;
                    outColor.rgb = depth;
                }
                else{
                    outColor.a = (1-depth) * _Color.a;
                    outColor.rgb = _Color;
                }

                return outColor;
            }
            ENDCG
        }
    }
}

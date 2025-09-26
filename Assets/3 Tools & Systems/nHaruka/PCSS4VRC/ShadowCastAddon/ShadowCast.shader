Shader "Hidden/PCSS4VRC/ShadowCast"
{
    Properties
    {
        [IntRange] Blocker_Samples("Blocker_Samples", Range(4, 32)) = 8
        [IntRange] PCF_Samples("PCF_Samples", Range(8, 64)) = 16
        Blocker_GradientBias("Blocker_GradientBias", Range(0, 0.01)) = 0
        PCF_GradientBias("PCF_GradientBias", Range(0, 0.01)) = 0
        Softness("Softness", Range(0, 0.02)) = 0.001
        SoftnessFalloff("SoftnessFalloff", Range(0, 0.5)) = 0.1
        SoftnessRange("SoftnessRange", Range(0.0001, 0.5)) = 0.2
        PenumbraWithMaxSamples("PenumbraWithMaxSamples", Range(1, 50)) = 10
        _ShadowDensity("ShadowDensity", Range(0, 1)) = 0.5
        _ShadowColor("ShadowColor", Color) = (0,0,0,0)
        MaxDistance("MaxDistance", Range(0, 1)) = 1
    }
        SubShader
    {
        Tags { "LightMode" = "ForwardAdd" "Queue" = "AlphaTest+49"}
        //LOD 100

        Pass
        {
            Stencil
            {
                Ref 0
                Comp Equal
            }
            Blend DstColor Zero
            ZWrite Off
            Cull Front
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows 

            #include "UnityCG.cginc"

#if defined(SPOT) && !defined(SHADOWS_SOFT)
#define SHADOWS_SOFT
#endif

#if defined(SPOT) && defined(SHADOWS_DEPTH)

            float3 _normal = float3(0, 0, 0);
            float3 _wpos;
            
            #include "Assets/nHaruka/PCSS4VRC/PCSSLogic/AutoLight_mod.cginc"
#else
            #include "AutoLight.cginc"
#endif    
            #include "Lighting.cginc"

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            uniform float _ShadowDensity = 0.5;
            uniform float4 _ShadowColor;

            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 spos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD4;
                UNITY_LIGHTING_COORDS(2, 3)

            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.spos = ComputeScreenPos(o.pos);
                UNITY_TRANSFER_LIGHTING(o, v.uv1.xy);

                return o;
            }

            /*

            float3 ReconstructViewPos(float2 uv, float depth)
            {
                float2 p11_22 = (unity_CameraProjection._11, unity_CameraProjection._22);
                float2 p13_31 = (unity_CameraProjection._13, unity_CameraProjection._23);
                return float3((uv * 2 - 1 - p13_31) / p11_22, depth);
            }

            float3 EstimateNormal(float2 uv)
            {
                float2 texelSize = 1.0 / _ScreenParams.xy;

                float depthL = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(-texelSize.x, 0));
                float depthR = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(texelSize.x, 0));
                float depthU = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(0, texelSize.y));
                float depthD = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(0, -texelSize.y));

                float3 posL = ReconstructViewPos(uv + float2(-texelSize.x, 0), depthL);
                float3 posR = ReconstructViewPos(uv + float2(texelSize.x, 0), depthR);
                float3 posU = ReconstructViewPos(uv + float2(0, texelSize.y), depthU);
                float3 posD = ReconstructViewPos(uv + float2(0, -texelSize.y), depthD);

                float3 dx = posR - posL;
                float3 dy = posU - posD;

                float3 normal = normalize(cross(dy, dx));
                return mul(unity_CameraToWorld, normal);
            }

            inline float3 curvatureInterpolation(float3 worldPos, float3 worldNormal)
            {
                float3 normalNormalized = normalize(worldNormal);
                float curvature = length(fwidth(normalNormalized)) / length(fwidth(worldPos));
                float3 interpolatedWorldPos = curvature < 0.001 ? worldPos : worldPos + 1 / curvature * (normalNormalized - worldNormal);

                return interpolatedWorldPos;
            }

            */

            fixed4 frag(v2f i) : SV_Target
            {
#if defined(SPOT) && defined(SHADOWS_DEPTH)

                //_LightColor0.rgb = max(max(_LightColor0.r, _LightColor0.g), _LightColor0.b) <= 0.01 ? _LightColor0.rgb * 100 : 0;
                
                clip(0.01 - max(max(_LightColor0.r, _LightColor0.g), _LightColor0.b));

                _LightColor0.rgb *= 100;

                float4 screenPos = float4(i.spos.xyz, i.spos.w + 0.00000000001);

                float4 screenPosNorm = screenPos / screenPos.w;
                screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;

                //float3 normal = EstimateNormal(screenPosNorm.xy);

                float eyeDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenPosNorm.xy));
                float3 cameraViewDir = -UNITY_MATRIX_V._m20_m21_m22;
                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float3 wpos = ((eyeDepth * worldViewDir * (1.0 / dot(cameraViewDir, worldViewDir))) + _WorldSpaceCameraPos);

                _wpos = wpos;
 
                //DECLARE_LIGHT_COORD(i, wpos);
                unityShadowCoord4 lightCoord = mul(unity_WorldToLight, unityShadowCoord4(wpos, 1));
                
                lightCoord.xy /= lightCoord.w;

                clip(lightCoord.z > 0.01 && lightCoord.z < 0.45 && abs(lightCoord.x) < 0.49 && lightCoord.y > -1 && lightCoord.y < 0.5 ? 1 : -1);

                float atten =  UNITY_SHADOW_ATTENUATION(i, wpos);
                //atten *= (lightCoord.z > 0);
                
                //UNITY_LIGHT_ATTENUATION(atten, i, wpos);
                
                //atten = lightCoord.z > 0.01 && lightCoord.z < 0.45 ? atten : 1;
                //atten = lightCoord.x > -0.49 && lightCoord.x < 0.49 ? atten : 1;
                //atten = lightCoord.y > -1 && lightCoord.y < 0.5 ? atten : 1;

                return lerp(_ShadowDensity + _ShadowColor, 1, saturate(atten));
#else
                return 1;
#endif          
            }
            ENDCG
        }
    }
}

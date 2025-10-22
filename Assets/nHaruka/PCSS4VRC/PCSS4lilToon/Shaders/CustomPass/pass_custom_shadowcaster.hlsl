#ifndef LIL_PASS_SHADOWCASTER_INCLUDED
#define LIL_PASS_SHADOWCASTER_INCLUDED

#include "Packages/jp.lilxyzw.liltoon/Shader/Includes/lil_common.hlsl"
#include "Packages/jp.lilxyzw.liltoon/Shader/Includes/lil_common_appdata.hlsl"

#define LIL_V2F_FORCE_TEXCOORD0
#define LIL_V2F_FORCE_POSITION_WS
#define LIL_CUSTOM_VERTEX_OS \
	unity_LightShadowBias.z = _ShadowNormalBias;
#define LIL_CUSTOM_V2F_MEMBER(id0,id1,id2,id3,id4,id5,id6,id7)\
    float3 normalWS : TEXCOORD ## id0;
#define LIL_CUSTOM_VERT_COPY  \
    output.normalWS = vertexNormalInput.normalWS;

//------------------------------------------------------------------------------------------------------------------------------
// Structure
#if !defined(LIL_CUSTOM_V2F_MEMBER)
#define LIL_CUSTOM_V2F_MEMBER(id0,id1,id2,id3,id4,id5,id6,id7)
#endif

#if defined(LIL_V2F_FORCE_TEXCOORD0) || (LIL_RENDER > 0)
#if defined(LIL_FUR)
#define LIL_V2F_TEXCOORD0
#else
#define LIL_V2F_PACKED_TEXCOORD01
#define LIL_V2F_PACKED_TEXCOORD23
#endif
#endif
#if defined(LIL_V2F_FORCE_POSITION_OS) || ((LIL_RENDER > 0) && !defined(LIL_LITE) && defined(LIL_FEATURE_DISSOLVE))
#define LIL_V2F_POSITION_OS
#endif
#if defined(LIL_V2F_FORCE_POSITION_WS) || (LIL_RENDER > 0) && defined(LIL_FEATURE_DISTANCE_FADE)
#define LIL_V2F_POSITION_WS
#endif
#define LIL_V2F_SHADOW_CASTER

struct v2f
{
    LIL_V2F_SHADOW_CASTER_OUTPUT
#if defined(LIL_V2F_TEXCOORD0)
        float2 uv0 : TEXCOORD1;
#endif
#if defined(LIL_V2F_PACKED_TEXCOORD01)
    float4 uv01 : TEXCOORD1;
#endif
#if defined(LIL_V2F_PACKED_TEXCOORD23)
    float4 uv23 : TEXCOORD2;
#endif
#if defined(LIL_V2F_POSITION_OS)
    float4 positionOSdissolve : TEXCOORD3;
#endif
#if defined(LIL_V2F_POSITION_WS)
    float3 positionWS : TEXCOORD4;
#endif
    LIL_CUSTOM_V2F_MEMBER(5, 6, 7, 8, 9, 10, 11, 12)
        LIL_VERTEX_INPUT_INSTANCE_ID
        LIL_VERTEX_OUTPUT_STEREO
};

uniform float _ShadowCasterBias;
uniform float _ShadowCasterBiasOffset;


float2x2 inv2(float2x2 M, out float det) {
    det = M._m00 * M._m11 - M._m01 * M._m10;
    float invDet = 1.0 / max(abs(det), 1e-20);
    return invDet * float2x2(M._m11, -M._m01, -M._m10, M._m00);
}

inline float3 curvatureInterpolation(float3 worldPos, float3 worldNormal)
{
    float3 normalNormalized = normalize(worldNormal);

    float originalNormalLength = length(worldNormal);

    float3 Ps = ddx(worldPos);
    float3 Pt = ddy(worldPos);
    Ps -= normalNormalized * dot(Ps, normalNormalized);
    Pt -= normalNormalized * dot(Pt, normalNormalized);

    float3 Ns = ddx(normalNormalized);
    float3 Nt = ddy(normalNormalized);
    Ns -= normalNormalized * dot(Ns, normalNormalized);
    Nt -= normalNormalized * dot(Nt, normalNormalized);

    // 第一基本量 I
    float a = dot(Ps, Ps);
    float b = dot(Ps, Pt);
    float c = dot(Pt, Pt);

    // 第二基本量 II
    float e = dot(Ns, Ps);
    float f = dot(Ns, Pt);
    float g = dot(Nt, Ps);
    float h2 = dot(Nt, Pt);

    float detI;
    float2x2 invI = inv2(float2x2(a, b, b, c), detI);

    float2x2 II = float2x2(e, f, g, h2);

    float2x2 S = mul(invI, II);

    S = 0.5 * (S + transpose(S));        // 数値対称化

    float H = 0.5 * (S._m00 + S._m11);   // 平均曲率（1/長さ）

    if (H < 1e-5) {
        return worldPos;
    }

    float R = 1.0 / H;  // 符号付き曲率半径
    float maxRadius = 100;  // メートル単位
    R = sign(R) * min(abs(R), maxRadius);  // 絶対値で制限、符号を保持

    float offset = R * (1.0 - originalNormalLength);
    float3 targetPos = worldPos + normalNormalized * offset;

    return lerp(worldPos, targetPos, _InterpolationStrength);
}

//------------------------------------------------------------------------------------------------------------------------------
// Shader
#include "Packages/jp.lilxyzw.liltoon/Shader/Includes/lil_common_vert.hlsl"
#include "Packages/jp.lilxyzw.liltoon/Shader/Includes/lil_common_frag.hlsl"

float4 frag(v2f input LIL_VFACE(facing), inout float depth: SV_Depth) : SV_Target
{
    LIL_SETUP_INSTANCE_ID(input);
    LIL_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    lilFragData fd = lilInitFragData();

    LIL_UNPACK_TEXCOORD0(input, fd);
    
    LIL_UNPACK_TEXCOORD1(input, fd); 
    
    float2 biasStrength = lerp(1, tex2D(_ShadowBiasMaskTexture, input.uv01.xy * _ShadowBiasMaskTexture_ST.xy + _ShadowBiasMaskTexture_ST.z).rg, _ShadowBiasMaskStrength);

    float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - input.positionWS);

    float NdotL = max(dot(normalize(input.normalWS), lightDir), 0.0);

    float offsetAmount = -saturate(NdotL - _ShadowCasterBiasOffset) * _ShadowCasterBias * biasStrength.r;

    //float3 interpolatedWpos = curvatureInterpolation(input.positionWS, input.normalWS);
    float3 interpolatedWpos = input.positionWS;

    float3 offsetPos = interpolatedWpos + normalize(input.normalWS) * offsetAmount;
    
    /*
    if (_ShadowNormalBias != 0.0)
    {
        float shadowCos = dot(input.normalWS, lightDir);
        float shadowSine = sqrt(1 - shadowCos * shadowCos);
        float normalBias = _ShadowNormalBias * shadowSine;

        offsetPos -= input.normalWS * normalBias * biasStrength.g;
    }
    */
    float4 clipPos = UnityWorldToClipPos(float4(offsetPos, 1.0));

    clipPos = UnityApplyLinearShadowBias(clipPos);

    depth = clipPos.z / clipPos.w;
    
    float castMask = lerp(1, tex2D(_CastMaskTex, input.uv01.xy * _CastMaskTex_ST.xy + _CastMaskTex_ST.zw).r, _CastMaskStrength) - 0.5f;

    depth = castMask < 0 ? 1 : depth;

    clip(castMask);

    BEFORE_UNPACK_V2F
    OVERRIDE_UNPACK_V2F
    LIL_COPY_VFACE(fd.facing);

    #include "Packages/jp.lilxyzw.liltoon/Shader/Includes/lil_common_frag_alpha.hlsl"

    LIL_SHADOW_CASTER_FRAGMENT(input);
}

#endif
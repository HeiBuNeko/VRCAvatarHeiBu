//----------------------------------------------------------------------------------------------------------------------
// Macro

#if defined(SPOT) && !defined(SHADOWS_SOFT)&& !defined(LIL_OUTLINE)
#define SHADOWS_SOFT
#endif

#include "UnityCG.cginc"

uniform  float3 _normal;
uniform  float3 _wpos;
uniform  float _InterpolationStrength; 
uniform  float _ShadowcoordzOffset;
uniform  float _MinusNormalOffset;
uniform  float _PlusNormalOffset;
uniform  float _ShadingThreshold;
uniform  float _ShadingCutOffThreshold;
uniform  float _ShadingCutOffBlurRadius;
uniform  float _ShadingBlurRadius;

#define LIL_CUSTOM_PROPERTIES \
	float _IsOn;\
	float NoShadowMode;\
	float _EnvLightStrength;\
	float _ShadowDistance;\
	int _BlendOpFA;\
	float3 _DropShadowColor;\
	float shadowArea;\
	float _EnableSurfaceSmoothing;\
	float _ShadowClamp;\
	float _ReceiveMaskStrength;\
	float _CastMaskStrength;\
	float _ShadowNormalBias;\
	float _ShadowBias;\
	float _ShadowDensity;\
	float _NormalMapStrength;\
	float _ShadowColorOverrideStrength;\
	float4 _ShadowColorOverrideTexture_ST;\
	float _ShadowBiasMaskStrength;\
	float4 _ShadowBiasMaskTexture_ST;\
	float4 _CastMaskTex_ST;\
	float4 _ReceiveMaskTex_ST;
	float _EnvLightAdjustLevel;

// Custom textures
#define LIL_CUSTOM_TEXTURES \
	Texture2D _ShadowColorOverrideTexture;\
	sampler2D _ShadowBiasMaskTexture;\
	sampler2D _CastMaskTex;\
	sampler2D _IgnoreCookieTexture;\
	Texture2D _ReceiveMaskTex;\	
	sampler2D _EnvLightLevelTexture;


#if defined(SPOT) && defined(SHADOWS_DEPTH) && !defined(LIL_OUTLINE)
/*
#undef DIRECTIONAL
#undef DIRECTIONAL_COOKIE
#undef POINT_COOKIE
#undef POINT
#undef UNITY_NO_SCREENSPACE_SHADOWS
#undef UNITY_LIGHT_PROBE_PROXY_VOLUME
#undef SHADOWS_SCREEN
#undef SHADOWS_CUBE
#undef LIGHTMAP_ON
#undef VERTEXLIGHT_ON
#undef DIRLIGHTMAP_COMBINED
#undef DYNAMICLIGHTMAP_ON
#undef SHADOWS_SHADOWMASK
#undef LIGHTMAP_SHADOW_MIXING
#undef LIGHTPROBE_SH
*/

#include "Assets/nHaruka/PCSS4VRC/PCSSLogic/AutoLight_mod.cginc"

inline float InvLerp(float from, float to, float value)
{
	return saturate(value - from) / saturate(to - from);
}

float2x2 inv2(float2x2 M, out float det) {
	det = M._m00 * M._m11 - M._m01 * M._m10;
	float invDet = 1.0 / max(abs(det), 1e-20);
	return invDet * float2x2(M._m11, -M._m01, -M._m10, M._m00);
}
/*
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

	if (H < 5) {
		return worldPos;
	}

	float R = 1.0 / H;  // 符号付き曲率半径
	float maxRadius = 1;  // メートル単位
	R = min(abs(R), maxRadius);  // 絶対値で制限、符号を保持

	float offset = R * (1.0 - originalNormalLength);
	float3 targetPos = worldPos + normalNormalized * offset;

	return lerp(worldPos, targetPos, _InterpolationStrength);
}
*/
inline float3 curvatureInterpolation(float3 worldPos, float3 worldNormal)
{
	float3 normalNormalized = normalize(worldNormal);
	float curvature = length(fwidth(normalNormalized)) / length(fwidth(worldPos));

	float3 interpolatedWorldPos = curvature < 0.01 ? worldPos : lerp(worldPos, worldPos + normalNormalized / (curvature) * (length(normalNormalized - worldNormal)), _InterpolationStrength);
	return interpolatedWorldPos;
}

/*
inline float3 GetWorldPosFromDepth(float4 ScreenUV, float3 worldPos)
{
	float4 screenPos = float4(ScreenUV.xyz, ScreenUV.w + 0.00000000001);

	float4 screenPosNorm = screenPos / screenPos.w;
	screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;

	float eyeDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenPosNorm.xy));
	float3 cameraViewDir = -UNITY_MATRIX_V._m20_m21_m22;
	float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
	float3 wpos = ((eyeDepth * worldViewDir * (1.0 / dot(cameraViewDir, worldViewDir))) + _WorldSpaceCameraPos);

	return wpos;
}
*/

#define LIL_V2F_FORCE_TANGENT
//#define LIL_V2F_FORCE_NORMAL
//#define LIL_V2F_FORCE_POSITION_OS
//#define LIL_V2F_FORCE_BITANGENT

#define BEFORE_UNPACK_V2F \
	float3 pre_normalmap = float3(0.0, 0.0, 1.0);\
	if(_IsOn)\
	{\ 
		NoShadowMode = max(max(_LightColor0.r, _LightColor0.g), _LightColor0.b) <= 0.02 && max(max(_LightColor0.r, _LightColor0.g), _LightColor0.b) > 0.01 ? 1 : 0; \
		NoShadowMode = saturate(NoShadowMode + step(_ShadowDistance, distance(_WorldSpaceLightPos0.xyz, input.positionWS))); \
		_LightColor0.rgb = max(max(_LightColor0.r, _LightColor0.g), _LightColor0.b) <= 0.01 ? _LightColor0.rgb * 100 : NoShadowMode == 1 ? (_LightColor0.rgb - 0.01) * 100 : 0; \
		LIL_UNPACK_TEXCOORD0(input, fd); \
		LIL_UNPACK_TEXCOORD1(input, fd); \
		LIL_COPY_VFACE(fd.facing); \
		COMBINENORMAL \
		CALC_TBN \
		_normal = lerp(input.normalWS, pre_normal, _NormalMapStrength); \
		input.positionWS = _InterpolationStrength > 0 ? curvatureInterpolation(input.positionWS, input.normalWS) : input.positionWS; \
		_wpos = input.positionWS; \
		_LightColor0.rgb = _LightColor0.rgb * lerp((tex2D(_EnvLightLevelTexture, half2(0.5, 0.5))).rgb, 1, 1 - _EnvLightAdjustLevel); \
		float biasStrength = lerp(1, tex2D(_ShadowBiasMaskTexture, fd.uv0 * _ShadowBiasMaskTexture_ST.xy + _ShadowBiasMaskTexture_ST.zw).b, _ShadowBiasMaskStrength); \
		_MinusNormalOffset *= biasStrength; \
		_PlusNormalOffset *= biasStrength; \
		_ShadowcoordzOffset *= biasStrength; \
	}\
	else \
	{\
		NoShadowMode = 1; \
		_normal = input.normalWS;\
		_wpos = input.positionWS; \
	}

#define BEFORE_SHADOW \
	if(_IsOn)\
	{\ 
		float rcvMask = lerp(1,_ReceiveMaskTex.Sample(sampler_MainTex, fd.uv0 * _ReceiveMaskTex_ST.xy + _ReceiveMaskTex_ST.zw).r, _ReceiveMaskStrength); \
		fd.attenuation = lerp(fd.attenuation, 1, 1 - rcvMask); \
		fd.attenuation = (_ShadowClamp > 0) ? step(_ShadowClamp, fd.attenuation) : fd.attenuation; \
		fd.attenuation = lerp(_ShadowDensity, 1, fd.attenuation); \
		fd.lightColor = min(LIL_MAINLIGHT_COLOR * fd.attenuation, _LightMaxLimit); \
		fd.lightColor = lerp(fd.lightColor, lilGray(fd.lightColor), _MonochromeLighting); \
		fd.lightColor = lerp(fd.lightColor, 0.0, _AsUnlit); \
		fd.lightColor = lerp(lerp(fd.lightColor, fd.lightColor * _DropShadowColor, (1 - fd.attenuation)), fd.lightColor, lerp(_ShadowDensity, 1, (1 - rcvMask))); \
		fd.lightColor = fd.lightColor * lerp(1, _ShadowColorOverrideTexture.Sample(sampler_MainTex, fd.uv0 * _ShadowColorOverrideTexture_ST.xy + _ShadowColorOverrideTexture_ST.zw).rgb, _ShadowColorOverrideStrength * (1 - fd.attenuation));\
	}

inline float3 OffsetWorldPos(float3 worldPos)
{
	float3 normalized = normalize(_normal);

	float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w);
	float NdotL = dot(normalized, lightDir) * 0.5 + 0.5;

	float4 shadowZRow = unity_WorldToShadow[0][2];
	float offsetScale = _ShadowcoordzOffset / dot(shadowZRow, float4(worldPos, 1));
	float3 zOffset = offsetScale * shadowZRow.xyz;

	float plusOffsetArea = (1 - step(NdotL, _ShadingThreshold)) * InvLerp(_ShadingThreshold, _ShadingCutOffBlurRadius, NdotL) * step(NdotL, _ShadingCutOffThreshold) * InvLerp(1 - _ShadingCutOffThreshold, _ShadingCutOffBlurRadius, 1 - NdotL);
	float minusOffsetArea = step(NdotL, _ShadingThreshold) * InvLerp(1 - _ShadingThreshold, _ShadingBlurRadius, 1 - NdotL);
	float zOffsetArea = step(NdotL, _ShadingCutOffThreshold) * InvLerp(1 - _ShadingCutOffThreshold, 1, 1 - NdotL);

	float3 worldPosOffset = -normalized * _MinusNormalOffset * minusOffsetArea + zOffset * zOffsetArea + normalized * _PlusNormalOffset * plusOffsetArea;

	return worldPos + worldPosOffset;
}

#undef UNITY_LIGHT_ATTENUATION
#define UNITY_LIGHT_ATTENUATION(destName, input, worldPos) \
	float3 OffsettedWorldPos = NoShadowMode > 0 ? worldPos : OffsetWorldPos(worldPos); \
	DECLARE_LIGHT_COORD(input, OffsettedWorldPos); \
	shadowArea = 1; \
	if (NoShadowMode < 1 ) { shadowArea = UNITY_SHADOW_ATTENUATION(input, OffsettedWorldPos); }\
	fixed destName = (lightCoord.z > 0) *  tex2D(_IgnoreCookieTexture,  lightCoord.xy / lightCoord.w + 0.5).w * UnitySpotAttenuate(lightCoord.xyz) * shadowArea;
#endif


/*
#if defined (UNITY_PASS_SHADOWCASTER) 
#define LIL_V2F_FORCE_NORMAL
#define LIL_V2F_FORCE_TEXCOORD0
	sampler2D _CastMaskTex;
	float4 _CastMaskTex_ST;
#define BEFORE_UNPACK_V2F \
    LIL_UNPACK_TEXCOORD0(input,fd); \
    LIL_UNPACK_TEXCOORD1(input,fd); \
	clip(lerp(1, tex2D(_CastMaskTex,fd.uv0 * _CastMaskTex_ST.xy + _CastMaskTex_ST.zw).r, _CastMaskStrength) - 0.5f);
#endif
*/


// Custom variables
//#define LIL_CUSTOM_PROPERTIES \
//    float _CustomVariable;



// Add vertex shader input
//#define LIL_REQUIRE_APP_POSITION
//#define LIL_REQUIRE_APP_TEXCOORD0
//#define LIL_REQUIRE_APP_TEXCOORD1
//#define LIL_REQUIRE_APP_TEXCOORD2
//#define LIL_REQUIRE_APP_TEXCOORD3
//#define LIL_REQUIRE_APP_TEXCOORD4
//#define LIL_REQUIRE_APP_TEXCOORD5
//#define LIL_REQUIRE_APP_TEXCOORD6
//#define LIL_REQUIRE_APP_TEXCOORD7
//#define LIL_REQUIRE_APP_COLOR
//#define LIL_REQUIRE_APP_NORMAL
//#define LIL_REQUIRE_APP_TANGENT
//#define LIL_REQUIRE_APP_VERTEXID

// Add vertex shader output
//#define LIL_V2F_FORCE_TEXCOORD0
//#define LIL_V2F_FORCE_TEXCOORD1
//#define LIL_V2F_FORCE_POSITION_OS
//#define LIL_V2F_FORCE_POSITION_WS
//#define LIL_V2F_FORCE_POSITION_SS
//#define LIL_V2F_FORCE_NORMAL
//#define LIL_V2F_FORCE_TANGENT
//#define LIL_V2F_FORCE_BITANGENT
//#define LIL_CUSTOM_V2F_MEMBER(id0,id1,id2,id3,id4,id5,id6,id7)\
	float4 positionOS : TEXCOORD ## id0;


// Add vertex copy
//#define LIL_CUSTOM_VERT_COPY \
	 output.positionOS = input.positionOS;

//#define LIL_CUSTOM_VERTEX_WS \

//#define LIL_CUSTOM_VERTEX_OS \

// Inserting a process into pixel shader
//#define BEFORE_xx
//#define OVERRIDE_xx


#if !defined(SPOT) || (defined(SPOT) && !defined(SHADOWS_DEPTH)) || defined(LIL_OUTLINE)

//#undef DIRECTIONAL
//#undef DIRECTIONAL_COOKIE
//#undef POINT_COOKIE
//#undef POINT
//#undef UNITY_NO_SCREENSPACE_SHADOWS
//#undef UNITY_LIGHT_PROBE_PROXY_VOLUME
#undef SHADOWS_SOFT
#undef SHADOWS_DEPTH
#undef SHADOWS_SCREEN
#undef SHADOWS_CUBE
//#undef LIGHTMAP_ON
//#undef VERTEXLIGHT_ON
//#undef DIRLIGHTMAP_COMBINED
//#undef DYNAMICLIGHTMAP_ON
//#undef SHADOWS_SHADOWMASK
//#undef LIGHTMAP_SHADOW_MIXING
//#undef LIGHTPROBE_SH

#define LIL_V2F_POSITION_WS
#define BEFORE_MAIN\
		if(_IsOn)\
		{\
			float3 overrideCol = lerp(1, _ShadowColorOverrideTexture.Sample(sampler_MainTex,fd.uv0 * _ShadowColorOverrideTexture_ST.xy + _ShadowColorOverrideTexture_ST.zw).rgb, _ShadowColorOverrideStrength);\
			fd.lightColor = (fd.lightColor.rgb  * _EnvLightStrength) * overrideCol;\
		}
#endif

/*
fd.indLightColor = (fd.indLightColor.rgb * _EnvLightStrength) * overrideCol; \
fd.addLightColor = (fd.addLightColor.rgb * _EnvLightStrength) * overrideCol; \
*/

//----------------------------------------------------------------------------------------------------------------------
// Information about variables
//----------------------------------------------------------------------------------------------------------------------

//----------------------------------------------------------------------------------------------------------------------
// Vertex shader inputs (appdata structure)
//
// Type     Name                    Description
// -------- ----------------------- --------------------------------------------------------------------
// float4   input.positionOS        POSITION
// float2   input.uv0               TEXCOORD0
// float2   input.uv1               TEXCOORD1
// float2   input.uv2               TEXCOORD2
// float2   input.uv3               TEXCOORD3
// float2   input.uv4               TEXCOORD4
// float2   input.uv5               TEXCOORD5
// float2   input.uv6               TEXCOORD6
// float2   input.uv7               TEXCOORD7
// float4   input.color             COLOR
// float3   input.normalOS          NORMAL
// float4   input.tangentOS         TANGENT
// uint     vertexID                SV_VertexID

//----------------------------------------------------------------------------------------------------------------------
// Vertex shader outputs or pixel shader inputs (v2f structure)
//
// The structure depends on the pass.
// Please check lil_pass_xx.hlsl for details.
//
// Type     Name                    Description
// -------- ----------------------- --------------------------------------------------------------------
// float4   output.positionCS       SV_POSITION
// float2   output.uv01             TEXCOORD0 TEXCOORD1
// float2   output.uv23             TEXCOORD2 TEXCOORD3
// float3   output.positionOS       object space position
// float3   output.positionWS       world space position
// float3   output.normalWS         world space normal
// float4   output.tangentWS        world space tangent




//----------------------------------------------------------------------------------------------------------------------
// Variables commonly used in the forward pass
//
// These are members of `lilFragData fd`
//
// Type     Name                    Description
// -------- ----------------------- --------------------------------------------------------------------
// float4   col                     lit color
// float3   albedo                  unlit color
// float3   emissionColor           color of emission
// -------- ----------------------- --------------------------------------------------------------------
// float3   lightColor              color of light
// float3   indLightColor           color of indirectional light
// float3   addLightColor           color of additional light
// float    attenuation             attenuation of light
// float3   invLighting             saturate((1.0 - lightColor) * sqrt(lightColor));
// -------- ----------------------- --------------------------------------------------------------------
// float2   uv0                     TEXCOORD0
// float2   uv1                     TEXCOORD1
// float2   uv2                     TEXCOORD2
// float2   uv3                     TEXCOORD3
// float2   uvMain                  Main UV
// float2   uvMat                   MatCap UV
// float2   uvRim                   Rim Light UV
// float2   uvPanorama              Panorama UV
// float2   uvScn                   Screen UV
// bool     isRightHand             input.tangentWS.w > 0.0;
// -------- ----------------------- --------------------------------------------------------------------
// float3   positionOS              object space position
// float3   positionWS              world space position
// float4   positionCS              clip space position
// float4   positionSS              screen space position
// float    depth                   distance from camera
// -------- ----------------------- --------------------------------------------------------------------
// float3x3 TBN                     tangent / bitangent / normal matrix
// float3   T                       tangent direction
// float3   B                       bitangent direction
// float3   N                       normal direction
// float3   V                       view direction
// float3   L                       light direction
// float3   origN                   normal direction without normal map
// float3   origL                   light direction without sh light
// float3   headV                   middle view direction of 2 cameras
// float3   reflectionN             normal direction for reflection
// float3   matcapN                 normal direction for reflection for MatCap
// float3   matcap2ndN              normal direction for reflection for MatCap 2nd
// float    facing                  VFACE
// -------- ----------------------- --------------------------------------------------------------------
// float    vl                      dot(viewDirection, lightDirection);
// float    hl                      dot(headDirection, lightDirection);
// float    ln                      dot(lightDirection, normalDirection);
// float    nv                      saturate(dot(normalDirection, viewDirection));
// float    nvabs                   abs(dot(normalDirection, viewDirection));
// -------- ----------------------- --------------------------------------------------------------------
// float4   triMask                 TriMask (for lite version)
// float3   parallaxViewDirection   mul(tbnWS, viewDirection);
// float2   parallaxOffset          parallaxViewDirection.xy / (parallaxViewDirection.z+0.5);
// float    anisotropy              strength of anisotropy
// float    smoothness              smoothness
// float    roughness               roughness
// float    perceptualRoughness     perceptual roughness
// float    shadowmix               this variable is 0 in the shadow area
// float    audioLinkValue          volume acquired by AudioLink
// -------- ----------------------- --------------------------------------------------------------------
// uint     renderingLayers         light layer of object (for URP / HDRP)
// uint     featureFlags            feature flags (for HDRP)
// uint2    tileIndex               tile index (for HDRP)
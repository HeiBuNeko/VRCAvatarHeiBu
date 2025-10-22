#ifndef UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED
#define UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED

// Shadowmap helpers.
#if defined( SHADOWS_SCREEN ) && defined( LIGHTMAP_ON )
    #define HANDLE_SHADOWS_BLENDING_IN_GI 1
#endif

#define unityShadowCoord float
#define unityShadowCoord2 float2
#define unityShadowCoord3 float3
#define unityShadowCoord4 float4
#define unityShadowCoord4x4 float4x4

half    UnitySampleShadowmap_PCF7x7(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (7x7 kernel)
half    UnitySampleShadowmap_PCF5x5(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (5x5 kernel)
half    UnitySampleShadowmap_PCF3x3(float4 coord, float3 receiverPlaneDepthBias);   // Samples the shadowmap based on PCF filtering (3x3 kernel)
float3  UnityGetReceiverPlaneDepthBias(float3 shadowCoord, float biasbiasMultiply); // Receiver plane depth bias


// sizes of cascade projections, relative to first one
float4 unity_ShadowCascadeScales;

/*
#if !defined(SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED) 
UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
float4 _ShadowMapTexture_TexelSize;
#define SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
#endif
*/


// declare shadowmap
#if !defined(SHADOWMAPSAMPLER_DEFINED)
UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
#define SHADOWMAPSAMPLER_DEFINED
#endif

// shadow sampling offsets and texel size
#if defined (SHADOWS_SOFT)
float4 _ShadowOffsets[4];
float4 _ShadowMapTexture_TexelSize;
#define SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
#endif


//
// Keywords based defines
//
#if defined (SHADOWS_SPLIT_SPHERES)
	#define GET_CASCADE_WEIGHTS(wpos, z)    getCascadeWeights_splitSpheres(wpos)
#else
	#define GET_CASCADE_WEIGHTS(wpos, z)	getCascadeWeights( wpos, z )
#endif

#if defined (SHADOWS_SINGLE_CASCADE)
	#define GET_SHADOW_COORDINATES(wpos,cascadeWeights)	getShadowCoord_SingleCascade(wpos)
#else
	#define GET_SHADOW_COORDINATES(wpos,cascadeWeights)	getShadowCoord(wpos,cascadeWeights)
#endif

// prototypes 
inline fixed4 getCascadeWeights(float3 wpos, float z);		// calculates the cascade weights based on the world position of the fragment and plane positions
inline fixed4 getCascadeWeights_splitSpheres(float3 wpos);	// calculates the cascade weights based on world pos and split spheres positions
inline float4 getShadowCoord_SingleCascade( float4 wpos );	// converts the shadow coordinates for shadow map using the world position of fragment (optimized for single fragment)
inline float4 getShadowCoord( float4 wpos, fixed4 cascadeWeights );// converts the shadow coordinates for shadow map using the world position of fragment

/*
 * Gets the cascade weights based on the world position of the fragment.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights(float3 wpos, float z)
{
	fixed4 zNear = float4( z >= _LightSplitsNear );
	fixed4 zFar = float4( z < _LightSplitsFar );
	fixed4 weights = zNear * zFar;
	return weights;
}

/*
 * Gets the cascade weights based on the world position of the fragment and the poisitions of the split spheres for each cascade.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights_splitSpheres(float3 wpos)
{
	float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
	float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
	float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
	float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
	float4 distances2 = float4(dot(fromCenter0,fromCenter0), dot(fromCenter1,fromCenter1), dot(fromCenter2,fromCenter2), dot(fromCenter3,fromCenter3));
	fixed4 weights = float4(distances2 < unity_ShadowSplitSqRadii);
	weights.yzw = saturate(weights.yzw - weights.xyz);
	return weights;
}

/*
 * Returns the shadowmap coordinates for the given fragment based on the world position and z-depth.
 * These coordinates belong to the shadowmap atlas that contains the maps for all cascades.
 */
inline float4 getShadowCoord( float4 wpos, fixed4 cascadeWeights )
{
	float3 sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
	float3 sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
	float3 sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
	float3 sc3 = mul (unity_WorldToShadow[3], wpos).xyz;
	float4 shadowMapCoordinate = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
#if defined(UNITY_REVERSED_Z)
	float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
	shadowMapCoordinate.z += noCascadeWeights;
#endif
	return shadowMapCoordinate;
}

/*
 * Same as the getShadowCoord; but optimized for single cascade
 */
inline float4 getShadowCoord_SingleCascade( float4 wpos )
{
	return float4( mul (unity_WorldToShadow[0], wpos).xyz, 0);
}

/*
 * Computes the receiver plane depth bias for the given shadow coord in screen space.
 * Inspirations: 
 *		http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/ 
 *		http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2012/10/Isidoro-ShadowMapping.pdf
 */
float2 getReceiverPlaneDepthBias (float3 shadowCoord)
{
	float2 biasUV;
	float3 dx = ddx (shadowCoord);
	float3 dy = ddy (shadowCoord);

	biasUV.x = dy.y * dx.z - dx.y * dy.z;
    biasUV.y = dx.x * dy.z - dy.x * dx.z;
    biasUV *= 1.0f / ((dx.x * dy.y) - (dx.y * dy.x));
    return biasUV;
}




//PCSS --------------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------

#define USE_FALLOFF

uniform float Blocker_Samples;
uniform float PCF_Samples;

uniform float Blocker_Rotation;
uniform float PCF_Rotation;

uniform float Softness;
uniform float SoftnessFalloff;
//uniform float NearPlane = .1;

uniform float Blocker_GradientBias;
uniform float PCF_GradientBias;
uniform float CascadeBlendDistance;

uniform float PenumbraWithMaxSamples;

//uniform float FalloffOffset;
uniform float SoftnessRange;
uniform float MaxDistance;


uniform sampler2D_float _ShadowMap;
float4 _ShadowMap_TexelSize;

uniform sampler2D _NoiseTexture;
uniform float4 NoiseCoords;

#if defined(POISSON_32)
static const float2 PoissonOffsets[32] = {
	float2(0.06407013, 0.05409927),
	float2(0.7366577, 0.5789394),
	float2(-0.6270542, -0.5320278),
	float2(-0.4096107, 0.8411095),
	float2(0.6849564, -0.4990818),
	float2(-0.874181, -0.04579735),
	float2(0.9989998, 0.0009880066),
	float2(-0.004920578, -0.9151649),
	float2(0.1805763, 0.9747483),
	float2(-0.2138451, 0.2635818),
	float2(0.109845, 0.3884785),
	float2(0.06876755, -0.3581074),
	float2(0.374073, -0.7661266),
	float2(0.3079132, -0.1216763),
	float2(-0.3794335, -0.8271583),
	float2(-0.203878, -0.07715034),
	float2(0.5912697, 0.1469799),
	float2(-0.88069, 0.3031784),
	float2(0.5040108, 0.8283722),
	float2(-0.5844124, 0.5494877),
	float2(0.6017799, -0.1726654),
	float2(-0.5554981, 0.1559997),
	float2(-0.3016369, -0.3900928),
	float2(-0.5550632, -0.1723762),
	float2(0.925029, 0.2995041),
	float2(-0.2473137, 0.5538505),
	float2(0.9183037, -0.2862392),
	float2(0.2469421, 0.6718712),
	float2(0.3916397, -0.4328209),
	float2(-0.03576927, -0.6220032),
	float2(-0.04661255, 0.7995201),
	float2(0.4402924, 0.3640312),
};

#else
static const float2 PoissonOffsets[64] = {
	float2(0.0617981, 0.07294159),
	float2(0.6470215, 0.7474022),
	float2(-0.5987766, -0.7512833),
	float2(-0.693034, 0.6913887),
	float2(0.6987045, -0.6843052),
	float2(-0.9402866, 0.04474335),
	float2(0.8934509, 0.07369385),
	float2(0.1592735, -0.9686295),
	float2(-0.05664673, 0.995282),
	float2(-0.1203411, -0.1301079),
	float2(0.1741608, -0.1682285),
	float2(-0.09369049, 0.3196758),
	float2(0.185363, 0.3213367),
	float2(-0.1493771, -0.3147511),
	float2(0.4452095, 0.2580113),
	float2(-0.1080467, -0.5329178),
	float2(0.1604507, 0.5460774),
	float2(-0.4037193, -0.2611179),
	float2(0.5947998, -0.2146744),
	float2(0.3276062, 0.9244621),
	float2(-0.6518704, -0.2503952),
	float2(-0.3580975, 0.2806469),
	float2(0.8587891, 0.4838005),
	float2(-0.1596546, -0.8791054),
	float2(-0.3096867, 0.5588146),
	float2(-0.5128918, 0.1448544),
	float2(0.8581337, -0.424046),
	float2(0.1562584, -0.5610626),
	float2(-0.7647934, 0.2709858),
	float2(-0.3090832, 0.9020988),
	float2(0.3935608, 0.4609676),
	float2(0.3929337, -0.5010948),
	float2(-0.8682281, -0.1990303),
	float2(-0.01973724, 0.6478714),
	float2(-0.3897587, -0.4665619),
	float2(-0.7416366, -0.4377831),
	float2(-0.5523247, 0.4272514),
	float2(-0.5325066, 0.8410385),
	float2(0.3085465, -0.7842533),
	float2(0.8400612, -0.200119),
	float2(0.6632416, 0.3067062),
	float2(-0.4462856, -0.04265022),
	float2(0.06892014, 0.812484),
	float2(0.5149567, -0.7502338),
	float2(0.6464897, -0.4666451),
	float2(-0.159861, 0.1038342),
	float2(0.6455986, 0.04419327),
	float2(-0.7445076, 0.5035095),
	float2(0.9430245, 0.3139912),
	float2(0.0349884, -0.7968109),
	float2(-0.9517487, 0.2963554),
	float2(-0.7304786, -0.01006928),
	float2(-0.5862702, -0.5531025),
	float2(0.3029106, 0.09497032),
	float2(0.09025345, -0.3503742),
	float2(0.4356628, -0.0710125),
	float2(0.4112572, 0.7500054),
	float2(0.3401214, -0.3047142),
	float2(-0.2192158, -0.6911137),
	float2(-0.4676369, 0.6570358),
	float2(0.6295372, 0.5629555),
	float2(0.1253822, 0.9892166),
	float2(-0.1154335, 0.8248222),
	float2(-0.4230408, -0.7129914),
};
#endif

float WhiteNoise(float2 position)
{
    float2 magic = float2(
        23.14069263277926, // e^pi (Gelfond's constant)
        2.665144142690225 // 2^sqrt(2) (Gelfondâ€“Schneider constant)
    );
    float noiseValue = frac(cos(dot(position, magic)) * 12345.6789);
    return noiseValue;
}

/*
=========================================================================================================================================
++++++++++++++++++++++++++++++++++++++++++++++++++++++    Helper Methods    +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
=========================================================================================================================================
*/

inline float ValueNoise(float3 pos)
{
	float3 Noise_skew = pos + 0.2127 + pos.x * pos.y * pos.z * 0.3713;
	float3 Noise_rnd = 4.789 * sin(489.123 * (Noise_skew));
	return frac(Noise_rnd.x * Noise_rnd.y * Noise_rnd.z * (1.0 + Noise_skew.x));
}

inline float2 Rotate(float2 pos, float2 rotationTrig)
{
	return float2(pos.x * rotationTrig.x - pos.y * rotationTrig.y, pos.y * rotationTrig.x + pos.x * rotationTrig.y);
}


SamplerState my_trilinear_clamp_smp;
inline float SampleShadowmapDepth(float2 uv)
{
    return _ShadowMapTexture.SampleLevel(my_trilinear_clamp_smp, uv, 0.0);
}
inline float SampleShadowmap_Soft(float4 coord)
{
	return UNITY_SAMPLE_SHADOW(_ShadowMapTexture, coord);
}

inline float SampleShadowmap(float4 coord)
{
	float depth = SampleShadowmapDepth(coord.xy);
	return step(depth, coord.z);
}

inline float GetScale(float4 cascadeWeights)
{
	float scale = 1.0;
	scale = (cascadeWeights.y > 0.0) ? 2.0 : scale;
	scale = (cascadeWeights.z > 0.0) ? 4.0 : scale;
	scale = (cascadeWeights.w > 0.0) ? 8.0 : scale;
	return 1.0 / scale;
}

float invLerp(float from, float to, float value) {
    return (value - from) / (to - from);
}

float2 VogelDiskSample(int sampleIndex, int samplesCount)
{
    float phi = 3.14159265359f;//UNITY_PI;
    float GoldenAngle = 2.4f;

    float r = sqrt(sampleIndex + 0.5f) / sqrt(samplesCount);
    float theta = sampleIndex * GoldenAngle + phi;

    float sine, cosine;
    sincos(theta, sine, cosine);

    return float2(r * cosine, r * sine);
}

float RadicalInverse_VdC(uint bits)
{
    bits = (bits << 16) | (bits >> 16);
    bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
    bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
    bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
    bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
    return float(bits) * 2.3283064365386963e-10; // 1.0 / (1 << 32)
}

float2 HammersleySample(uint i, uint N)
{
    float u = float(i) / float(N);
    float v = RadicalInverse_VdC(i);
    return float2(u, v);
}

float2 GenerateRandomOffset(float random)
{
    // 0から1の範囲のランダム値を生成
    float random1 = frac(float(random) * 0.328316f);
    float random2 = frac(float(random) * 0.123456f);

    return float2(random1, random2);
}

float2 GenerateSampleOffset(uint sampleIndex, uint totalSamples, float scale, float2 randomOffset)
{
    float2 xi = HammersleySample(sampleIndex, totalSamples);
    // 極座標への変換
    float r = sqrt(xi.x) * scale;
    float theta = xi.y * 2.0 * UNITY_PI;

    //theta += randomOffset.x * UNITY_TWO_PI;

    float sine, cosine;
    sincos(theta, sine, cosine);
    float2 sample = float2(r * cosine, r * sine);

    return sample;
}

float ComputeSearchRadius(float depth, float lightSize, float angle, float farplane, float nearplane)
{

    // 受光点からライトまでの距離を計算
    //float receiverDistance = length(worldPosition - _WorldSpaceLightPos0.xyz);

    //float depth = ShadowCoord.z / ShadowCoord.w; // 正規化デバイス座標系（NDC）の z 値
    float z_ndc = depth * 2.0 - 1.0;
    float receiverDistance = length(_wpos - _WorldSpaceLightPos0.xyz);

    float fov = radians(angle);
    float shadowMapWorldSize = 2.0 * farplane * tan(fov * 0.5);

    // シャドウマップのスケール調整
    float texelSize = shadowMapWorldSize * _ShadowMapTexture_TexelSize.x;

    // 検索半径を計算
    float searchRadius = (lightSize * (receiverDistance / (farplane - nearplane))) / texelSize;

    // 検索半径を制限
    float minSearchRadius = 0.00001f;

    float maxSearchRadius = 1.0f;

    searchRadius = clamp(searchRadius, minSearchRadius, maxSearchRadius);

    return searchRadius;
}

float GaussianWeight(float2 offset, float sigma) {
    float distanceSquared = dot(offset, offset); // x^2 + y^2
    float weight = exp(-distanceSquared / (2.0 * sigma * sigma));
    return weight;
}

/*
=========================================================================================================================================
++++++++++++++++++++++++++++++++++++++++++++++++++++++    Find Blocker    +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
=========================================================================================================================================
*/

float2 FindBlocker(float2 uv, float depth, float scale, float searchUV, float2 receiverPlaneDepthBias, float2 rotationTrig)
{
	float avgBlockerDepth = 0.0;
	float numBlockers = 0.0;
	float blockerSum = 0.0;

	for (int i = 0; i < Blocker_Samples; i++)
	{
        float2 offset = GenerateSampleOffset(i, Blocker_Samples, searchUV, rotationTrig);
		//float2 offset = PoissonOffsets[i] * searchUV * scale;
        //float2 offset = VogelDiskSample(i, Blocker_Samples) * searchUV * scale;

        //offset = smoothstep(0, searchUV, length(offset)) * offset;

//#if defined(ROTATE_SAMPLES)
		offset = Rotate(offset, rotationTrig);
//#endif

		float shadowMapDepth = SampleShadowmapDepth(saturate(uv + offset));

		float biasedDepth = depth;

//#if defined(USE_BLOCKER_BIAS)
		biasedDepth += receiverPlaneDepthBias * Blocker_GradientBias;
        
        //biasedDepth += 0.001 * smoothstep(0.5, 1, saturate(length(offset / searchUV)));
//#endif


#if defined(UNITY_REVERSED_Z)
		if (shadowMapDepth > biasedDepth)
#else
		if (shadowMapDepth < biasedDepth)
#endif
		{
            blockerSum += shadowMapDepth;
			numBlockers += 1;
		}
        
	}
    
	avgBlockerDepth = blockerSum / numBlockers;

#if defined(UNITY_REVERSED_Z)
	//avgBlockerDepth = 1.0 - avgBlockerDepth;
#endif

	return float2(avgBlockerDepth, numBlockers);
}

/*
=========================================================================================================================================
++++++++++++++++++++++++++++++++++++++++++++++++++++++    PCF Sampling    +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
=========================================================================================================================================
*/

float PCF_Filter(float2 uv, float depth, float scale, float filterRadiusUV, float2 receiverPlaneDepthBias, float penumbra, float distance, float2 rotationTrig)
{
	float sum = 0.0f;

    float weightSum = 0.0;
#if defined(UNITY_REVERSED_Z)
	//receiverPlaneDepthBias *= -1.0;
#endif

	//float penumbraPercent = saturate(penumbra / PenumbraWithMaxSamples);
	//int samples = ceil(penumbraPercent * PCF_Samples);
	////int samples = ceil((1.0 - (penumbraPercent * penumbraPercent)) * PCF_Samples);
	//samples = PCF_Samples;

    int minSamples = 4;   // 最小サンプル数
    int maxSamples = PCF_Samples;  // 最大サンプル数
    float normalizedPenumbra = saturate(penumbra / PenumbraWithMaxSamples);
    int sampleCount = lerp(minSamples, maxSamples, normalizedPenumbra);

    for (int i = 0; i < sampleCount; i++)
	{	
        float2 offset = PoissonOffsets[i] * filterRadiusUV * scale;
        //offset = smoothstep(0, filterRadiusUV, length(offset)) * offset;
        //float2 offset = VogelDiskSample(i, PCF_Samples) * filterRadiusUV * scale;

//#if defined(ROTATE_SAMPLES)
		offset = Rotate(offset, rotationTrig);
//#endif

		float biasedDepth = depth;

//#if defined(USE_PCF_BIAS)
		biasedDepth += receiverPlaneDepthBias * PCF_GradientBias;

        //biasedDepth +=  (distance / 30) * smoothstep(0.5, 1, saturate(length(offset / filterRadiusUV)));
//#endif
        float value = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float3(uv.xy + offset, biasedDepth));

        float weight = 1;// GaussianWeight(offset, 0.01);

		sum += value * weight;
        weightSum += weight;
    }

	//sum /= samples;
	sum /= weightSum;

	return sum;
}

#if defined (SHADOWS_DEPTH) && defined (SPOT)

// ------------------------------------------------------------------
// Spot light shadows
// ------------------------------------------------------------------


inline fixed UnitySampleShadowmap(float4 coords, float random)// , float2 receiverPlaneDepthBias, float random, float scale)
{
    float3 shadowcoord = coords.xyz / coords.w;

    float3 normal = normalize(_normal);  // 入力された法線（ワールド空間）
    float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - _wpos);

    float slopeScaleBias = length(_normal) == 0 ? 1 : abs(dot(normal, lightDir)); // 法線と光線の角度
    float receiverPlaneDepthBias = slopeScaleBias * (1.0 - saturate(shadowcoord.z)) / 5; // スケール係数

    //float2 receiverPlaneDepthBias = getReceiverPlaneDepthBias(shadowcoord);
    float2 uv = shadowcoord.xy;
    float depth = shadowcoord.z;
    float zAwareDepth = depth;

#if defined(UNITY_REVERSED_Z)
    zAwareDepth = 1.0 - depth;
#endif
   
    float rotationAngle = random * 6.283185307179586476925286766559;
    //float rotationAngle = random * 3.1415926;
    float2 rotationTrig = float2(cos(rotationAngle), sin(rotationAngle));

#if defined(UNITY_REVERSED_Z)
    //receiverPlaneDepthBias *= -1.0;
#endif

    float scale = 1;
    // STEP 1: blocker search
    //float searchSize = SoftnessRange * (depth - _LightShadowData.w) / depth;
    //float searchSize = SoftnessRange * saturate(depth - .02) / depth;
    
    float angle = 36;
    float farPlane = 30;
    float nearPlane = 0.2;

    float searchSize = ComputeSearchRadius(depth, (Softness + SoftnessFalloff) * SoftnessRange, angle, farPlane, nearPlane);

    //float searchSize = (1.0 - _LightShadowData.r) * lerp(0.00001, 0.5 * depth, SoftnessRange) ;
    //float searchSize = clamp(SoftnessRange * (depth), 0.05, 1.0);

    float2 blockerInfo = FindBlocker(uv, depth, scale, searchSize, receiverPlaneDepthBias, rotationTrig);

    if (blockerInfo.y < 1)
    {
        //There are no occluders so early out (this saves filtering)
        return 1.0;
    }
    else if (blockerInfo.y == Blocker_Samples)
    {
        return 0;
    }

    float dist = (blockerInfo.x - depth) / blockerInfo.x;

    // STEP 2: penumbra size
    //float penumbra = zAwareDepth * zAwareDepth - blockerInfo.x * blockerInfo.x;
    float penumbra = (zAwareDepth - blockerInfo.x) / blockerInfo.x;

#if defined(USE_FALLOFF)
    //penumbra = 1.0 - pow(1.0 - penumbra, SoftnessFalloff);
#endif

    //float filterRadiusUV = penumbra * Softness;
    //filterRadiusUV *= filterRadiusUV;

    float filterRadiusUV = Softness + searchSize * (lerp(0, SoftnessFalloff, clamp(dist, 0, MaxDistance)));

    // STEP 3: filtering
    float shadow = PCF_Filter(uv, depth, scale, filterRadiusUV, receiverPlaneDepthBias, penumbra, dist, rotationTrig);

    return lerp(_LightShadowData.r, 1.0f, shadow);
}

#endif // #if defined (SHADOWS_DEPTH) && defined (SPOT)

// ------------------------------------------------------------------
// Point light shadows
// ------------------------------------------------------------------

#if defined (SHADOWS_CUBE)

#if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
    UNITY_DECLARE_TEXCUBE_SHADOWMAP(_ShadowMapTexture);
#else
    UNITY_DECLARE_TEXCUBE(_ShadowMapTexture);
    inline float SampleCubeDistance (float3 vec)
    {
        return UnityDecodeCubeShadowDepth(UNITY_SAMPLE_TEXCUBE_LOD(_ShadowMapTexture, vec, 0));
    }

#endif

    
inline half UnitySampleShadowmap (float3 vec)
{
    #if defined(SHADOWS_CUBE_IN_DEPTH_TEX)
        float3 absVec = abs(vec);
        float dominantAxis = max(max(absVec.x, absVec.y), absVec.z); // TODO use max3() instead
        dominantAxis = max(0.00001, dominantAxis - _LightProjectionParams.z); // shadow bias from point light is apllied here.
        dominantAxis *= _LightProjectionParams.w; // bias
        float mydist = -_LightProjectionParams.x + _LightProjectionParams.y/dominantAxis; // project to shadow map clip space [0; 1]

        #if defined(UNITY_REVERSED_Z)
        mydist = 1.0 - mydist; // depth buffers are reversed! Additionally we can move this to CPP code!
        #endif
    #else
        float mydist = length(vec) * _LightPositionRange.w;
        mydist *= _LightProjectionParams.w; // bias
    #endif

    #if defined (SHADOWS_SOFT)
        float z = 1.0/128.0;
        float4 shadowVals;
        // No hardware comparison sampler (ie some mobile + xbox360) : simple 4 tap PCF
        #if defined (SHADOWS_CUBE_IN_DEPTH_TEX)
            shadowVals.x = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec+float3( z, z, z), mydist));
            shadowVals.y = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec+float3(-z,-z, z), mydist));
            shadowVals.z = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec+float3(-z, z,-z), mydist));
            shadowVals.w = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec+float3( z,-z,-z), mydist));
            half shadow = dot(shadowVals, 0.25);
            return lerp(_LightShadowData.r, 1.0, shadow);
        #else
            shadowVals.x = SampleCubeDistance (vec+float3( z, z, z));
            shadowVals.y = SampleCubeDistance (vec+float3(-z,-z, z));
            shadowVals.z = SampleCubeDistance (vec+float3(-z, z,-z));
            shadowVals.w = SampleCubeDistance (vec+float3( z,-z,-z));
            half4 shadows = (shadowVals < mydist.xxxx) ? _LightShadowData.rrrr : 1.0f;
            return dot(shadows, 0.25);
        #endif
    #else
        #if defined (SHADOWS_CUBE_IN_DEPTH_TEX)
            half shadow = UNITY_SAMPLE_TEXCUBE_SHADOW(_ShadowMapTexture, float4(vec, mydist));
            return lerp(_LightShadowData.r, 1.0, shadow);
        #else
            half shadowVal = UnityDecodeCubeShadowDepth(UNITY_SAMPLE_TEXCUBE(_ShadowMapTexture, vec));
            half shadow = shadowVal < mydist ? _LightShadowData.r : 1.0;
            return shadow;
        #endif
    #endif

}
#endif // #if defined (SHADOWS_CUBE)


// ------------------------------------------------------------------
// Baked shadows
// ------------------------------------------------------------------

#if UNITY_LIGHT_PROBE_PROXY_VOLUME

half4 LPPV_SampleProbeOcclusion(float3 worldPos)
{
    const float transformToLocal = unity_ProbeVolumeParams.y;
    const float texelSizeX = unity_ProbeVolumeParams.z;

    //The SH coefficients textures and probe occlusion are packed into 1 atlas.
    //-------------------------
    //| ShR | ShG | ShB | Occ |
    //-------------------------

    float3 position = (transformToLocal == 1.0f) ? mul(unity_ProbeVolumeWorldToObject, float4(worldPos, 1.0)).xyz : worldPos;

    //Get a tex coord between 0 and 1
    float3 texCoord = (position - unity_ProbeVolumeMin.xyz) * unity_ProbeVolumeSizeInv.xyz;

    // Sample fourth texture in the atlas
    // We need to compute proper U coordinate to sample.
    // Clamp the coordinate otherwize we'll have leaking between ShB coefficients and Probe Occlusion(Occ) info
    texCoord.x = max(texCoord.x * 0.25f + 0.75f, 0.75f + 0.5f * texelSizeX);

    return UNITY_SAMPLE_TEX3D_SAMPLER(unity_ProbeVolumeSH, unity_ProbeVolumeSH, texCoord);
}

#endif //#if UNITY_LIGHT_PROBE_PROXY_VOLUME

// ------------------------------------------------------------------
// Used by the forward rendering path
fixed UnitySampleBakedOcclusion (float2 lightmapUV, float3 worldPos)
{
    #if defined (SHADOWS_SHADOWMASK)
        #if defined(LIGHTMAP_ON)
            fixed4 rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
        #else
            fixed4 rawOcclusionMask = fixed4(1.0, 1.0, 1.0, 1.0);
            #if UNITY_LIGHT_PROBE_PROXY_VOLUME
                if (unity_ProbeVolumeParams.x == 1.0)
                    rawOcclusionMask = LPPV_SampleProbeOcclusion(worldPos);
                else
                    rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
            #else
                rawOcclusionMask = UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
            #endif
        #endif
        return saturate(dot(rawOcclusionMask, unity_OcclusionMaskSelector));

    #else

        //In forward dynamic objects can only get baked occlusion from LPPV, light probe occlusion is done on the CPU by attenuating the light color.
        fixed atten = 1.0f;
        #if defined(UNITY_INSTANCING_ENABLED) && defined(UNITY_USE_SHCOEFFS_ARRAYS)
            // ...unless we are doing instancing, and the attenuation is packed into SHC array's .w component.
            atten = unity_SHC.w;
        #endif

        #if UNITY_LIGHT_PROBE_PROXY_VOLUME && !defined(LIGHTMAP_ON) && !UNITY_STANDARD_SIMPLE
            fixed4 rawOcclusionMask = atten.xxxx;
            if (unity_ProbeVolumeParams.x == 1.0)
                rawOcclusionMask = LPPV_SampleProbeOcclusion(worldPos);
            return saturate(dot(rawOcclusionMask, unity_OcclusionMaskSelector));
        #endif

        return atten;
    #endif
}

// ------------------------------------------------------------------
// Used by the deferred rendering path (in the gbuffer pass)
fixed4 UnityGetRawBakedOcclusions(float2 lightmapUV, float3 worldPos)
{
    #if defined (SHADOWS_SHADOWMASK)
        #if defined(LIGHTMAP_ON)
            return UNITY_SAMPLE_TEX2D(unity_ShadowMask, lightmapUV.xy);
        #else
            half4 probeOcclusion = unity_ProbesOcclusion;

            #if UNITY_LIGHT_PROBE_PROXY_VOLUME
                if (unity_ProbeVolumeParams.x == 1.0)
                    probeOcclusion = LPPV_SampleProbeOcclusion(worldPos);
            #endif

            return probeOcclusion;
        #endif
    #else
        return fixed4(1.0, 1.0, 1.0, 1.0);
    #endif
}

// ------------------------------------------------------------------
// Used by both the forward and the deferred rendering path
half UnityMixRealtimeAndBakedShadows(half realtimeShadowAttenuation, half bakedShadowAttenuation, half fade)
{
    // -- Static objects --
    // FWD BASE PASS
    // ShadowMask mode          = LIGHTMAP_ON + SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = LIGHTMAP_ON + SHADOWS_SHADOWMASK
    // Subtractive mode         = LIGHTMAP_ON + LIGHTMAP_SHADOW_MIXING
    // Pure realtime direct lit = LIGHTMAP_ON

    // FWD ADD PASS
    // ShadowMask mode          = SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = SHADOWS_SHADOWMASK
    // Pure realtime direct lit = LIGHTMAP_ON

    // DEFERRED LIGHTING PASS
    // ShadowMask mode          = LIGHTMAP_ON + SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = LIGHTMAP_ON + SHADOWS_SHADOWMASK
    // Pure realtime direct lit = LIGHTMAP_ON

    // -- Dynamic objects --
    // FWD BASE PASS + FWD ADD ASS
    // ShadowMask mode          = LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = N/A
    // Subtractive mode         = LIGHTMAP_SHADOW_MIXING (only matter for LPPV. Light probes occlusion being done on CPU)
    // Pure realtime direct lit = N/A

    // DEFERRED LIGHTING PASS
    // ShadowMask mode          = SHADOWS_SHADOWMASK + LIGHTMAP_SHADOW_MIXING
    // Distance shadowmask mode = SHADOWS_SHADOWMASK
    // Pure realtime direct lit = N/A

    #if !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_CUBE)
        #if defined(LIGHTMAP_ON) && defined (LIGHTMAP_SHADOW_MIXING) && !defined (SHADOWS_SHADOWMASK)
            //In subtractive mode when there is no shadow we kill the light contribution as direct as been baked in the lightmap.
            return 0.0;
        #else
            return bakedShadowAttenuation;
        #endif
    #endif

    #if (SHADER_TARGET <= 20) || UNITY_STANDARD_SIMPLE
        //no fading nor blending on SM 2.0 because of instruction count limit.
        #if defined(SHADOWS_SHADOWMASK) || defined(LIGHTMAP_SHADOW_MIXING)
            return min(realtimeShadowAttenuation, bakedShadowAttenuation);
        #else
            return realtimeShadowAttenuation;
        #endif
    #endif

    #if defined(LIGHTMAP_SHADOW_MIXING)
        //Subtractive or shadowmask mode
        realtimeShadowAttenuation = saturate(realtimeShadowAttenuation + fade);
        return min(realtimeShadowAttenuation, bakedShadowAttenuation);
    #endif

    //In distance shadowmask or realtime shadow fadeout we lerp toward the baked shadows (bakedShadowAttenuation will be 1 if no baked shadows)
    return lerp(realtimeShadowAttenuation, bakedShadowAttenuation, fade);
}

// ------------------------------------------------------------------
// Shadow fade
// ------------------------------------------------------------------

float UnityComputeShadowFadeDistance(float3 wpos, float z)
{
    float sphereDist = distance(wpos, unity_ShadowFadeCenterAndType.xyz);
    return lerp(z, sphereDist, unity_ShadowFadeCenterAndType.w);
}

// ------------------------------------------------------------------
half UnityComputeShadowFade(float fadeDist)
{
    return saturate(fadeDist * _LightShadowData.z + _LightShadowData.w);
}


// ------------------------------------------------------------------
//  Bias
// ------------------------------------------------------------------

/*
* Computes the receiver plane depth bias for the given shadow coord in screen space.
* Inspirations:
*   http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*   http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2012/10/Isidoro-ShadowMapping.pdf
*/
float3 UnityGetReceiverPlaneDepthBias(float3 shadowCoord, float biasMultiply)
{
    // Should receiver plane bias be used? This estimates receiver slope using derivatives,
    // and tries to tilt the PCF kernel along it. However, when doing it in screenspace from the depth texture
    // (ie all light in deferred and directional light in both forward and deferred), the derivatives are wrong
    // on edges or intersections of objects, leading to shadow artifacts. Thus it is disabled by default.
    float3 biasUVZ = 0;

#if defined(UNITY_USE_RECEIVER_PLANE_BIAS) && defined(SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED)
    float3 dx = ddx(shadowCoord);
    float3 dy = ddy(shadowCoord);

    biasUVZ.x = dy.y * dx.z - dx.y * dy.z;
    biasUVZ.y = dx.x * dy.z - dy.x * dx.z;
    biasUVZ.xy *= biasMultiply / ((dx.x * dy.y) - (dx.y * dy.x));

    // Static depth biasing to make up for incorrect fractional sampling on the shadow map grid.
    const float UNITY_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR = 0.01f;
    float fractionalSamplingError = dot(_ShadowMapTexture_TexelSize.xy, abs(biasUVZ.xy));
    biasUVZ.z = -min(fractionalSamplingError, UNITY_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR);
    #if defined(UNITY_REVERSED_Z)
        biasUVZ.z *= -1;
    #endif
#endif

    return biasUVZ;
}

/*
* Combines the different components of a shadow coordinate and returns the final coordinate.
* See UnityGetReceiverPlaneDepthBias
*/
float3 UnityCombineShadowcoordComponents(float2 baseUV, float2 deltaUV, float depth, float3 receiverPlaneDepthBias)
{
    float3 uv = float3(baseUV + deltaUV, depth + receiverPlaneDepthBias.z);
    uv.z += dot(deltaUV, receiverPlaneDepthBias.xy);
    return uv;
}

// ------------------------------------------------------------------
//  PCF Filtering helpers
// ------------------------------------------------------------------

/*
* Assuming a isoceles rectangle triangle of height "triangleHeight" (as drawn below).
* This function return the area of the triangle above the first texel.
*
* |\      <-- 45 degree slop isosceles rectangle triangle
* | \
* ----    <-- length of this side is "triangleHeight"
* _ _ _ _ <-- texels
*/
float _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(float triangleHeight)
{
    return triangleHeight - 0.5;
}

/*
* Assuming a isoceles triangle of 1.5 texels height and 3 texels wide lying on 4 texels.
* This function return the area of the triangle above each of those texels.
*    |    <-- offset from -0.5 to 0.5, 0 meaning triangle is exactly in the center
*   / \   <-- 45 degree slop isosceles triangle (ie tent projected in 2D)
*  /   \
* _ _ _ _ <-- texels
* X Y Z W <-- result indices (in computedArea.xyzw and computedAreaUncut.xyzw)
*/
void _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(float offset, out float4 computedArea, out float4 computedAreaUncut)
{
    //Compute the exterior areas
    float offset01SquaredHalved = (offset + 0.5) * (offset + 0.5) * 0.5;
    computedAreaUncut.x = computedArea.x = offset01SquaredHalved - offset;
    computedAreaUncut.w = computedArea.w = offset01SquaredHalved;

    //Compute the middle areas
    //For Y : We find the area in Y of as if the left section of the isoceles triangle would
    //intersect the axis between Y and Z (ie where offset = 0).
    computedAreaUncut.y = _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(1.5 - offset);
    //This area is superior to the one we are looking for if (offset < 0) thus we need to
    //subtract the area of the triangle defined by (0,1.5-offset), (0,1.5+offset), (-offset,1.5).
    float clampedOffsetLeft = min(offset,0);
    float areaOfSmallLeftTriangle = clampedOffsetLeft * clampedOffsetLeft;
    computedArea.y = computedAreaUncut.y - areaOfSmallLeftTriangle;

    //We do the same for the Z but with the right part of the isoceles triangle
    computedAreaUncut.z = _UnityInternalGetAreaAboveFirstTexelUnderAIsocelesRectangleTriangle(1.5 + offset);
    float clampedOffsetRight = max(offset,0);
    float areaOfSmallRightTriangle = clampedOffsetRight * clampedOffsetRight;
    computedArea.z = computedAreaUncut.z - areaOfSmallRightTriangle;
}

/*
 * Assuming a isoceles triangle of 1.5 texels height and 3 texels wide lying on 4 texels.
 * This function return the weight of each texels area relative to the full triangle area.
 */
void _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(float offset, out float4 computedWeight)
{
    float4 dummy;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedWeight, dummy);
    computedWeight *= 0.44444;//0.44 == 1/(the triangle area)
}

/*
* Assuming a isoceles triangle of 2.5 texel height and 5 texels wide lying on 6 texels.
* This function return the weight of each texels area relative to the full triangle area.
*  /       \
* _ _ _ _ _ _ <-- texels
* 0 1 2 3 4 5 <-- computed area indices (in texelsWeights[])
*/
void _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(float offset, out float3 texelsWeightsA, out float3 texelsWeightsB)
{
    //See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
    float4 computedArea_From3texelTriangle;
    float4 computedAreaUncut_From3texelTriangle;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

    //Triangle slop is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
    //the 5 texel wide triangle can be seen as the 3 texel wide one but shifted up by one unit/texel.
    //0.16 is 1/(the triangle area)
    texelsWeightsA.x = 0.16 * (computedArea_From3texelTriangle.x);
    texelsWeightsA.y = 0.16 * (computedAreaUncut_From3texelTriangle.y);
    texelsWeightsA.z = 0.16 * (computedArea_From3texelTriangle.y + 1);
    texelsWeightsB.x = 0.16 * (computedArea_From3texelTriangle.z + 1);
    texelsWeightsB.y = 0.16 * (computedAreaUncut_From3texelTriangle.z);
    texelsWeightsB.z = 0.16 * (computedArea_From3texelTriangle.w);
}

/*
* Assuming a isoceles triangle of 3.5 texel height and 7 texels wide lying on 8 texels.
* This function return the weight of each texels area relative to the full triangle area.
*  /           \
* _ _ _ _ _ _ _ _ <-- texels
* 0 1 2 3 4 5 6 7 <-- computed area indices (in texelsWeights[])
*/
void _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(float offset, out float4 texelsWeightsA, out float4 texelsWeightsB)
{
    //See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
    float4 computedArea_From3texelTriangle;
    float4 computedAreaUncut_From3texelTriangle;
    _UnityInternalGetAreaPerTexel_3TexelsWideTriangleFilter(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

    //Triangle slop is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
    //the 7 texel wide triangle can be seen as the 3 texel wide one but shifted up by two unit/texel.
    //0.081632 is 1/(the triangle area)
    texelsWeightsA.x = 0.081632 * (computedArea_From3texelTriangle.x);
    texelsWeightsA.y = 0.081632 * (computedAreaUncut_From3texelTriangle.y);
    texelsWeightsA.z = 0.081632 * (computedAreaUncut_From3texelTriangle.y + 1);
    texelsWeightsA.w = 0.081632 * (computedArea_From3texelTriangle.y + 2);
    texelsWeightsB.x = 0.081632 * (computedArea_From3texelTriangle.z + 2);
    texelsWeightsB.y = 0.081632 * (computedAreaUncut_From3texelTriangle.z + 1);
    texelsWeightsB.z = 0.081632 * (computedAreaUncut_From3texelTriangle.z);
    texelsWeightsB.w = 0.081632 * (computedArea_From3texelTriangle.w);
}

// ------------------------------------------------------------------
//  PCF Filtering
// ------------------------------------------------------------------

/*
* PCF gaussian shadowmap filtering based on a 3x3 kernel (9 taps no PCF hardware support)
*/
half UnitySampleShadowmap_PCF3x3NoHardwareSupport(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
    // when we don't have hardware PCF sampling, then the above 5x5 optimized PCF really does not work.
    // Fallback to a simple 3x3 sampling with averaged results.
    float2 base_uv = coord.xy;
    float2 ts = _ShadowMapTexture_TexelSize.xy;
    shadow = 0;
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, -ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, 0), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(-ts.x, ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(0, ts.y), coord.z, receiverPlaneDepthBias));
    shadow += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(ts.x, ts.y), coord.z, receiverPlaneDepthBias));
    shadow /= 9.0;
#endif

    return shadow;
}

/*
* PCF tent shadowmap filtering based on a 3x3 kernel (optimized with 4 taps)
*/
half UnitySampleShadowmap_PCF3x3Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 3x3 base thus covering from 9 to 12 texels, thus we need 4 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based
    float4 texelsWeightsU, texelsWeightsV;
    _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU);
    _UnityInternalGetWeightPerTexel_3TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float2 fetchesWeightsU = texelsWeightsU.xz + texelsWeightsU.yw;
    float2 fetchesWeightsV = texelsWeightsV.xz + texelsWeightsV.yw;

    // move the PCF bilinear fetches to respect texels weights
    float2 fetchesOffsetsU = texelsWeightsU.yw / fetchesWeightsU.xy + float2(-1.5,0.5);
    float2 fetchesOffsetsV = texelsWeightsV.yw / fetchesWeightsV.xy + float2(-1.5,0.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow =  fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/*
* PCF tent shadowmap filtering based on a 5x5 kernel (optimized with 9 taps)
*/
half UnitySampleShadowmap_PCF5x5Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 5x5 base thus covering from 25 to 36 texels, thus we need 9 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based on the area of a 45 degree slop tent above each of them.
    float3 texelsWeightsU_A, texelsWeightsU_B;
    float3 texelsWeightsV_A, texelsWeightsV_B;
    _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    _UnityInternalGetWeightPerTexel_5TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float3 fetchesWeightsU = float3(texelsWeightsU_A.xz, texelsWeightsU_B.y) + float3(texelsWeightsU_A.y, texelsWeightsU_B.xz);
    float3 fetchesWeightsV = float3(texelsWeightsV_A.xz, texelsWeightsV_B.y) + float3(texelsWeightsV_A.y, texelsWeightsV_B.xz);

    // move the PCF bilinear fetches to respect texels weights
    float3 fetchesOffsetsU = float3(texelsWeightsU_A.y, texelsWeightsU_B.xz) / fetchesWeightsU.xyz + float3(-2.5,-0.5,1.5);
    float3 fetchesOffsetsV = float3(texelsWeightsV_A.y, texelsWeightsV_B.xz) / fetchesWeightsV.xyz + float3(-2.5,-0.5,1.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xxx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yyy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow  = fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/*
* PCF tent shadowmap filtering based on a 7x7 kernel (optimized with 16 taps)
*/
half UnitySampleShadowmap_PCF7x7Tent(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    // tent base is 7x7 base thus covering from 49 to 64 texels, thus we need 16 bilinear PCF fetches
    float2 tentCenterInTexelSpace = coord.xy * _ShadowMapTexture_TexelSize.zw;
    float2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    float2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based on the area of a 45 degree slop tent above each of them.
    float4 texelsWeightsU_A, texelsWeightsU_B;
    float4 texelsWeightsV_A, texelsWeightsV_B;
    _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    _UnityInternalGetWeightPerTexel_7TexelsWideTriangleFilter(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    float4 fetchesWeightsU = float4(texelsWeightsU_A.xz, texelsWeightsU_B.xz) + float4(texelsWeightsU_A.yw, texelsWeightsU_B.yw);
    float4 fetchesWeightsV = float4(texelsWeightsV_A.xz, texelsWeightsV_B.xz) + float4(texelsWeightsV_A.yw, texelsWeightsV_B.yw);

    // move the PCF bilinear fetches to respect texels weights
    float4 fetchesOffsetsU = float4(texelsWeightsU_A.yw, texelsWeightsU_B.yw) / fetchesWeightsU.xyzw + float4(-3.5,-1.5,0.5,2.5);
    float4 fetchesOffsetsV = float4(texelsWeightsV_A.yw, texelsWeightsV_B.yw) / fetchesWeightsV.xyzw + float4(-3.5,-1.5,0.5,2.5);
    fetchesOffsetsU *= _ShadowMapTexture_TexelSize.xxxx;
    fetchesOffsetsV *= _ShadowMapTexture_TexelSize.yyyy;

    // fetch !
    float2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * _ShadowMapTexture_TexelSize.xy;
    shadow  = fetchesWeightsU.x * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.x), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.y), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.z), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.x * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.x, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.y * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.y, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.z * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.z, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
    shadow += fetchesWeightsU.w * fetchesWeightsV.w * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(bilinearFetchOrigin, float2(fetchesOffsetsU.w, fetchesOffsetsV.w), coord.z, receiverPlaneDepthBias));
#endif

    return shadow;
}

/*
* PCF gaussian shadowmap filtering based on a 3x3 kernel (optimized with 4 taps)
*
* Algorithm: http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/
* Implementation example: http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*/
half UnitySampleShadowmap_PCF3x3Gaussian(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    const float2 offset = float2(0.5, 0.5);
    float2 uv = (coord.xy * _ShadowMapTexture_TexelSize.zw) + offset;
    float2 base_uv = (floor(uv) - offset) * _ShadowMapTexture_TexelSize.xy;
    float2 st = frac(uv);

    float2 uw = float2(3 - 2 * st.x, 1 + 2 * st.x);
    float2 u = float2((2 - st.x) / uw.x - 1, (st.x) / uw.y + 1);
    u *= _ShadowMapTexture_TexelSize.x;

    float2 vw = float2(3 - 2 * st.y, 1 + 2 * st.y);
    float2 v = float2((2 - st.y) / vw.x - 1, (st.y) / vw.y + 1);
    v *= _ShadowMapTexture_TexelSize.y;

    half sum = 0;

    sum += uw[0] * vw[0] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[0], v[0]), coord.z, receiverPlaneDepthBias));
    sum += uw[1] * vw[0] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[1], v[0]), coord.z, receiverPlaneDepthBias));
    sum += uw[0] * vw[1] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[0], v[1]), coord.z, receiverPlaneDepthBias));
    sum += uw[1] * vw[1] * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u[1], v[1]), coord.z, receiverPlaneDepthBias));

    shadow = sum / 16.0f;
#endif

    return shadow;
}

/*
* PCF gaussian shadowmap filtering based on a 5x5 kernel (optimized with 9 taps)
*
* Algorithm: http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/
* Implementation example: http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/
*/
half UnitySampleShadowmap_PCF5x5Gaussian(float4 coord, float3 receiverPlaneDepthBias)
{
    half shadow = 1;

#ifdef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED

    #ifndef SHADOWS_NATIVE
        // when we don't have hardware PCF sampling, fallback to a simple 3x3 sampling with averaged results.
        return UnitySampleShadowmap_PCF3x3NoHardwareSupport(coord, receiverPlaneDepthBias);
    #endif

    const float2 offset = float2(0.5, 0.5);
    float2 uv = (coord.xy * _ShadowMapTexture_TexelSize.zw) + offset;
    float2 base_uv = (floor(uv) - offset) * _ShadowMapTexture_TexelSize.xy;
    float2 st = frac(uv);

    float3 uw = float3(4 - 3 * st.x, 7, 1 + 3 * st.x);
    float3 u = float3((3 - 2 * st.x) / uw.x - 2, (3 + st.x) / uw.y, st.x / uw.z + 2);
    u *= _ShadowMapTexture_TexelSize.x;

    float3 vw = float3(4 - 3 * st.y, 7, 1 + 3 * st.y);
    float3 v = float3((3 - 2 * st.y) / vw.x - 2, (3 + st.y) / vw.y, st.y / vw.z + 2);
    v *= _ShadowMapTexture_TexelSize.y;

    half sum = 0.0f;

    half3 accum = uw * vw.x;
    sum += accum.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.x), coord.z, receiverPlaneDepthBias));
    sum += accum.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.x), coord.z, receiverPlaneDepthBias));
    sum += accum.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.x), coord.z, receiverPlaneDepthBias));

    accum = uw * vw.y;
    sum += accum.x *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.y), coord.z, receiverPlaneDepthBias));
    sum += accum.y *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.y), coord.z, receiverPlaneDepthBias));
    sum += accum.z *  UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.y), coord.z, receiverPlaneDepthBias));

    accum = uw * vw.z;
    sum += accum.x * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.x, v.z), coord.z, receiverPlaneDepthBias));
    sum += accum.y * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.y, v.z), coord.z, receiverPlaneDepthBias));
    sum += accum.z * UNITY_SAMPLE_SHADOW(_ShadowMapTexture, UnityCombineShadowcoordComponents(base_uv, float2(u.z, v.z), coord.z, receiverPlaneDepthBias));
    shadow = sum / 144.0f;

#endif

    return shadow;
}

half UnitySampleShadowmap_PCF3x3(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF3x3Tent(coord, receiverPlaneDepthBias);
}

half UnitySampleShadowmap_PCF5x5(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF5x5Tent(coord, receiverPlaneDepthBias);
}

half UnitySampleShadowmap_PCF7x7(float4 coord, float3 receiverPlaneDepthBias)
{
    return UnitySampleShadowmap_PCF7x7Tent(coord, receiverPlaneDepthBias);
}

#endif // UNITY_BUILTIN_SHADOW_LIBRARY_INCLUDED

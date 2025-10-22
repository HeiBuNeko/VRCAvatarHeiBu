#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_HIGH_RESOLUTION_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_HIGH_RESOLUTION_CGINC

#include "../Common/Samplers.cginc"

#ifndef LB_PREVIEW_MODE
static const float NUM_RINGS_SCALER = TARGET_RESOLUTION.y / 1080.0;
#else
static const float NUM_RINGS_SCALER = PREVIEW_TARGET_RESOLUTION.y / 1080.0;
#endif


Texture2D _GrabTexture;
float4 _GrabTexture_TexelSize;


//----------------------------------------------------------------------
// _CocTex
//----------------------------------------------------------------------

#ifndef LB_PREVIEW_MODE
static const float2 FULL_COC_TEXEL_SIZE = 1.0 / CAPTURE_RESOLUTION;
static const float2 HALF_COC_TEXEL_SIZE = 2.0 / CAPTURE_RESOLUTION;
#else
static const float2 FULL_COC_TEXEL_SIZE = 1.0 / PREVIEW_CAPTURE_RESOLUTION;
static const float2 HALF_COC_TEXEL_SIZE = 2.0 / PREVIEW_CAPTURE_RESOLUTION;
#endif

Texture2D _CocTex;


float fetchFullCoc(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = COC_TEXTURE_FULL_AREA;
#else
	const float4 area = COC_TEXTURE_PREVIEW_FULL_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(point_clamp_sampler, uv * scale + offset, 0);
}

float4 gatherFullCoc(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = COC_TEXTURE_FULL_AREA;
#else
	const float4 area = COC_TEXTURE_PREVIEW_FULL_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.Gather(point_clamp_sampler, uv * scale + offset);
}


float fetchBackgroundCoc(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = COC_TEXTURE_BACKGROUND_AREA;
#else
	const float4 area = COC_TEXTURE_PREVIEW_BACKGROUND_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(point_clamp_sampler, uv * scale + offset, 0);
}

float fetchBackgroundCocLinear(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = COC_TEXTURE_BACKGROUND_AREA;
#else
	const float4 area = COC_TEXTURE_PREVIEW_BACKGROUND_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(linear_clamp_sampler, uv * scale + offset, 0);
}

float4 gatherBackgroundCoc(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = COC_TEXTURE_BACKGROUND_AREA;
#else
	const float4 area = COC_TEXTURE_PREVIEW_BACKGROUND_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.Gather(point_clamp_sampler, uv * scale + offset);
}


float fetchForegroundCoc(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = COC_TEXTURE_FOREGROUND_AREA;
#else
	const float4 area = COC_TEXTURE_PREVIEW_FOREGROUND_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(point_clamp_sampler, uv * scale + offset, 0);
}

float fetchForegroundCocLinear(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = COC_TEXTURE_FOREGROUND_AREA;
#else
	const float4 area = COC_TEXTURE_PREVIEW_FOREGROUND_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(linear_clamp_sampler, uv * scale + offset, 0);
}

float4 gatherForegroundCoc(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = COC_TEXTURE_FOREGROUND_AREA;
#else
	const float4 area = COC_TEXTURE_PREVIEW_FOREGROUND_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.Gather(point_clamp_sampler, uv * scale + offset);
}


//----------------------------------------------------------------------
// _TileTex
//----------------------------------------------------------------------

Texture2D _TileTex;

float4 fetchTile(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = TILE_TEXTURE_NORMAL_AREA;
#else
	const float4 area = TILE_TEXTURE_PREVIEW_NORMAL_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _TileTex.Sample(point_clamp_sampler, uv * scale + offset);
}

float4 fetchDilatedTile(float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = TILE_TEXTURE_DILATED_AREA;
#else
	const float4 area = TILE_TEXTURE_PREVIEW_DILATED_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _TileTex.Sample(point_clamp_sampler, uv * scale + offset);
}


//----------------------------------------------------------------------
// Downsampled
//----------------------------------------------------------------------

#ifndef LB_PREVIEW_MODE
static const float2 DOWNSAMPLED_TEXEL_SIZE = 2.0 / CAPTURE_RESOLUTION;
#else
static const float2 DOWNSAMPLED_TEXEL_SIZE = 2.0 / PREVIEW_CAPTURE_RESOLUTION;
#endif

Texture2D _DownsampledTex;

float4 fetchPremultipliedFullSample(SamplerState s, float2 uv){
#ifndef LB_PREVIEW_MODE
	const float4 area = DOWNSAMPLED_TEXTURE_FULL_AREA;
#else
	const float4 area = DOWNSAMPLED_TEXTURE_PREVIEW_PREMULTIPLIED_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _DownsampledTex.SampleLevel(s, uv * scale + offset, 0);
}

float4 fetchDownsampledBackground(SamplerState s, float2 uv){
	const float2 texel_size = DOWNSAMPLED_TEXEL_SIZE;
	uv = clamp(uv, 0.5 * texel_size, 1.0 - 0.5 * texel_size);
#ifndef LB_PREVIEW_MODE
	const float4 area = DOWNSAMPLED_TEXTURE_BACKGROUND_AREA;
#else
	const float4 area = PREFILTER_TEXTURE_PREVIEW_BACKGROUND_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
#ifndef LB_PREVIEW_MODE
	return _DownsampledTex.SampleLevel(s, uv * scale + offset, 0);
#else
	return _GrabTexture.SampleLevel(s, uv * scale + offset, 0);
#endif
}

float4 fetchDownsampledForeground(SamplerState s, float2 uv){
	const float2 texel_size = DOWNSAMPLED_TEXEL_SIZE;
	uv = clamp(uv, 0.5 * texel_size, 1.0 - 0.5 * texel_size);
#ifndef LB_PREVIEW_MODE
	const float4 area = DOWNSAMPLED_TEXTURE_FOREGROUND_AREA;
#else
	const float4 area = PREFILTER_TEXTURE_PREVIEW_FOREGROUND_AREA;
#endif
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
#ifndef LB_PREVIEW_MODE
	return _DownsampledTex.SampleLevel(s, uv * scale + offset, 0);
#else
	return _GrabTexture.SampleLevel(s, uv * scale + offset, 0);
#endif
}


//----------------------------------------------------------------------
// Workarea
//----------------------------------------------------------------------

#ifndef LB_PREVIEW_MODE

Texture2D<float4> _VirtualLens2_WorkareaTex;

Texture2D rawWorkareaTexture(){
	return _VirtualLens2_WorkareaTex;
}

#define WORKAREA_TEXEL_SIZE     (2.0 / _ScreenParams.xy)
#define RAW_WORKAREA_TEXEL_SIZE (1.0 / _ScreenParams.xy)

float2 computePaddedUV(float2 uv){
	const float screen_aspect = _ScreenParams.x / _ScreenParams.y;
	const float target_aspect = TARGET_RESOLUTION.x / TARGET_RESOLUTION.y;
	float2 scale = TARGET_RESOLUTION / CAPTURE_RESOLUTION;
	if(screen_aspect < target_aspect){
		scale.x *= screen_aspect / target_aspect;
	}else{
		scale.y *= target_aspect / screen_aspect;
	}
	const float2 offset = 0.5 - 0.5 * scale;
	return uv * scale + offset;
}

float2 computeUnpaddedUV(float2 uv){
	const float screen_aspect = _ScreenParams.x / _ScreenParams.y;
	const float target_aspect = TARGET_RESOLUTION.x / TARGET_RESOLUTION.y;
	float2 scale = CAPTURE_RESOLUTION / TARGET_RESOLUTION;
	if(screen_aspect < target_aspect){
		scale.x *= target_aspect / screen_aspect;
	}else{
		scale.y *= screen_aspect / target_aspect;
	}
	const float2 offset = 0.5 - 0.5 * scale;
	return uv * scale + offset;
}

float2 rawBackgroundWorkareaUV(float2 uv){
	const float2 area = floor(_ScreenParams.xy * 0.5);
	const float2 texel_size = 1.0 / area;
	uv = clamp(computeUnpaddedUV(uv), 0.5 * texel_size, 1.0 - 0.5 * texel_size);
	const float2 minval = float2(0.0, 0.0);
	const float2 maxval = area / _ScreenParams.xy;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return uv * scale + offset;
}

float2 rawForegroundWorkareaUV(float2 uv){
	const float2 area = floor(_ScreenParams.xy * 0.5);
	const float2 texel_size = 1.0 / area;
	uv = clamp(computeUnpaddedUV(uv), 0.5 * texel_size, 1.0 - 0.5 * texel_size);
	const float2 minval = float2(1.0 - area.x / _ScreenParams.x, 0.0);
	const float2 maxval = float2(1.0, area.y / _ScreenParams.y);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return uv * scale + offset;
}

float4 fetchBackgroundWorkarea(SamplerState s, float2 uv){
	return _VirtualLens2_WorkareaTex.SampleLevel(s, rawBackgroundWorkareaUV(uv), 0);
}

float4 fetchForegroundWorkarea(SamplerState s, float2 uv){
	return _VirtualLens2_WorkareaTex.SampleLevel(s, rawForegroundWorkareaUV(uv), 0);
}

#else

Texture2D rawWorkareaTexture(){
	return _GrabTexture;
}

#define WORKAREA_TEXEL_SIZE     (2.0 / PREVIEW_CAPTURE_RESOLUTION)
#define RAW_WORKAREA_TEXEL_SIZE (1.0 / PREVIEW_CAPTURE_RESOLUTION)

float4 fetchBackgroundWorkarea(SamplerState s, float2 uv){
	const float4 area = WORKAREA_TEXTURE_PREVIEW_BACKGROUND_AREA;
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _GrabTexture.SampleLevel(s, uv * scale + offset, 0);
}

float4 fetchForegroundWorkarea(SamplerState s, float2 uv){
	const float4 area = WORKAREA_TEXTURE_PREVIEW_FOREGROUND_AREA;
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _GrabTexture.SampleLevel(s, uv * scale + offset, 0);
}

#endif

#endif

#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_1080P_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_1080P_CGINC

#include "../../Common/Constants.cginc"
#include "../Common/Samplers.cginc"

static const float2 CAPTURE_RESOLUTION = float2(2048.0, 1152.0);
static const float2 TARGET_RESOLUTION = float2(1920.0, 1080.0);

static const float2 PREVIEW_CAPTURE_RESOLUTION = CAPTURE_RESOLUTION;
static const float2 PREVIEW_TARGET_RESOLUTION  = TARGET_RESOLUTION;

static const float NUM_RINGS_SCALER = 1080.0 / 1080.0;


Texture2D _GrabTexture;
float4 _GrabTexture_TexelSize;


/* _CocTex layout
 *
 *   +---------------+---------------+
 *   |               |               |
 *   |     RT BG     |     RT FG     |
 *   |               |               |
 *   +---------------+---------------+
 *   |                               |
 *   |                               |
 *   |                               |
 *   |            RT Full            |
 *   |                               |
 *   |                               |
 *   |                               |
 *   +-------------------------------+
 *
 *  - RT Full: 2048 x 1152
 *  - RT BG:   1024 x  576
 *  - RT FG:   1024 x  576
 */

static const float2 COC_TEXTURE_RESOLUTION = float2(2048.0, 1728.0);

static const float4 COC_TEXTURE_FULL_AREA       = float4(0.0, 0.0,       1.0, 2.0 / 3.0);
static const float4 COC_TEXTURE_BACKGROUND_AREA = float4(0.0, 2.0 / 3.0, 0.5, 1.0);
static const float4 COC_TEXTURE_FOREGROUND_AREA = float4(0.5, 2.0 / 3.0, 1.0, 1.0);
static const float4 COC_TEXTURE_PREVIEW_FULL_AREA       = COC_TEXTURE_FULL_AREA;
static const float4 COC_TEXTURE_PREVIEW_BACKGROUND_AREA = COC_TEXTURE_BACKGROUND_AREA;
static const float4 COC_TEXTURE_PREVIEW_FOREGROUND_AREA = COC_TEXTURE_FOREGROUND_AREA;

static const float2 FULL_COC_TEXEL_SIZE = 1.0 / CAPTURE_RESOLUTION;
static const float2 HALF_COC_TEXEL_SIZE = 2.0 / CAPTURE_RESOLUTION;

Texture2D _CocTex;


float fetchFullCoc(float2 uv){
	const float2 minval = float2(0.0, 0.0);
	const float2 maxval = float2(1.0, 2.0 / 3.0);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(point_clamp_sampler, uv * scale + offset, 0);
}

float4 gatherFullCoc(float2 uv){
	const float2 minval = float2(0.0, 0.0);
	const float2 maxval = float2(1.0, 2.0 / 3.0);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.Gather(point_clamp_sampler, uv * scale + offset);
}


float fetchBackgroundCoc(float2 uv){
	const float2 minval = float2(0.0, 2.0 / 3.0);
	const float2 maxval = float2(0.5, 1.0);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(point_clamp_sampler, uv * scale + offset, 0);
}

float fetchBackgroundCocLinear(float2 uv){
	const float2 minval = float2(0.0, 2.0 / 3.0);
	const float2 maxval = float2(0.5, 1.0);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(linear_clamp_sampler, uv * scale + offset, 0);
}

float4 gatherBackgroundCoc(float2 uv){
	const float2 minval = float2(0.0, 2.0 / 3.0);
	const float2 maxval = float2(0.5, 1.0);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.Gather(point_clamp_sampler, uv * scale + offset);
}


float fetchForegroundCoc(float2 uv){
	const float2 minval = float2(0.5, 2.0 / 3.0);
	const float2 maxval = float2(1.0, 1.0);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(point_clamp_sampler, uv * scale + offset, 0);
}

float fetchForegroundCocLinear(float2 uv){
	const float2 minval = float2(0.5, 2.0 / 3.0);
	const float2 maxval = float2(1.0, 1.0);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.SampleLevel(linear_clamp_sampler, uv * scale + offset, 0);
}

float4 gatherForegroundCoc(float2 uv){
	const float2 minval = float2(0.5, 2.0 / 3.0);
	const float2 maxval = float2(1.0, 1.0);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _CocTex.Gather(point_clamp_sampler, uv * scale + offset);
}


/* _TileTex layout
 *
 *   +-------------+
 *   |             |
 *   |  RT Dilate  |
 *   |             |
 *   +-------------+
 *   |             |
 *   |     RT      |
 *   |             |
 *   +-------------+
 *
 * - RT: 128 x 72 (Dilated)
 * - RT: 128 x 72
 */

static const float2 TILE_TEXTURE_RESOLUTION = float2(128.0, 144.0);

static const float4 TILE_TEXTURE_NORMAL_AREA          = float4(0.0, 0.0, 1.0, 0.5);
static const float4 TILE_TEXTURE_DILATED_AREA         = float4(0.0, 0.5, 1.0, 1.0);
static const float4 TILE_TEXTURE_PREVIEW_NORMAL_AREA  = TILE_TEXTURE_NORMAL_AREA;
static const float4 TILE_TEXTURE_PREVIEW_DILATED_AREA = TILE_TEXTURE_DILATED_AREA;

Texture2D _TileTex;

float4 fetchTile(float2 uv){
	const float4 area = TILE_TEXTURE_NORMAL_AREA;
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _TileTex.Sample(point_clamp_sampler, uv * scale + offset);
}

float4 fetchDilatedTile(float2 uv){
	const float4 area = TILE_TEXTURE_DILATED_AREA;
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _TileTex.Sample(point_clamp_sampler, uv * scale + offset);
}

/* _PremultipliedFullTex layout
 *
 *   +---------------+
 *   |               |
 *   |    RT Full    |
 *   |               |
 *   +---------------+
 *
 *  - RT Full:    2048 x 1152
 */

static const float2 PREMULTIPLIED_FULL_TEXEL_SIZE = 1.0 / CAPTURE_RESOLUTION;

Texture2D _VirtualLens2_PremultipliedFullTex;

float4 fetchPremultipliedFullSample(SamplerState s, float2 uv){
	return _VirtualLens2_PremultipliedFullTex.SampleLevel(s, uv, 0);
}


/* _GrabTexture layout
 *
 *   +-------------------------------+
 *   |                               |
 *   |                               |
 *   |                               |
 *   +---------------+---------------+
 *   |               |               |
 *   |  Background   |  Foreground   |
 *   |               |               |
 *   +---------------+---------------+
 *
 *  - Background: 1024 x 576 
 *  - Foreground: 1024 x 576
 */

static const float2 DOWNSAMPLED_TEXEL_SIZE  = 2.0 / CAPTURE_RESOLUTION;
static const float2 WORKAREA_TEXEL_SIZE     = 2.0 / CAPTURE_RESOLUTION;
static const float2 RAW_WORKAREA_TEXEL_SIZE = 1.0 / CAPTURE_RESOLUTION;

static const float4 PREFILTER_TEXTURE_PREVIEW_BACKGROUND_AREA      = float4(0.0, 0.0,  0.5, 0.5);
static const float4 PREFILTER_TEXTURE_PREVIEW_FOREGROUND_AREA      = float4(0.5, 0.0,  1.0, 0.5);

static const float4 BLURRED_TEXTURE_PREVIEW_BACKGROUND_AREA        = float4(0.0, 0.0,  0.5, 0.5);
static const float4 BLURRED_TEXTURE_PREVIEW_FOREGROUND_AREA        = float4(0.5, 0.0,  1.0, 0.5);


float4 fetchDownsampledBackground(SamplerState s, float2 uv){
	const float2 texel_size = DOWNSAMPLED_TEXEL_SIZE;
	uv = clamp(uv, 0.5 * texel_size, 1.0 - 0.5 * texel_size);
	const float4 area = PREFILTER_TEXTURE_PREVIEW_BACKGROUND_AREA;
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _GrabTexture.SampleLevel(s, uv * scale + offset, 0);
}

float4 fetchDownsampledForeground(SamplerState s, float2 uv){
	const float2 texel_size = DOWNSAMPLED_TEXEL_SIZE;
	uv = clamp(uv, 0.5 * texel_size, 1.0 - 0.5 * texel_size);
	const float4 area = PREFILTER_TEXTURE_PREVIEW_FOREGROUND_AREA;
	const float2 minval = area.xy;
	const float2 maxval = area.zw;
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _GrabTexture.SampleLevel(s, uv * scale + offset, 0);
}


Texture2D rawWorkareaTexture(){
	return _GrabTexture;
}

float4 fetchBackgroundWorkarea(SamplerState s, float2 uv){
	const float2 texel_size = WORKAREA_TEXEL_SIZE;
	uv = clamp(uv, 0.5 * texel_size, 1.0 - 0.5 * texel_size);
	const float2 minval = float2(0.0, 0.0);
	const float2 maxval = float2(0.5, 0.5);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _GrabTexture.SampleLevel(s, uv * scale + offset, 0);
}

float4 fetchForegroundWorkarea(SamplerState s, float2 uv){
	const float2 texel_size = WORKAREA_TEXEL_SIZE;
	uv = clamp(uv, 0.5 * texel_size, 1.0 - 0.5 * texel_size);
	const float2 minval = float2(0.5, 0.0);
	const float2 maxval = float2(1.0, 0.5);
	const float2 scale  = maxval - minval;
	const float2 offset = minval;
	return _GrabTexture.SampleLevel(s, uv * scale + offset, 0);
}


#define FOREACH_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	[unroll(1)] \
	do { \
		int _tex_width, _tex_height, _tex_num_samples; \
		TEXTURE2DMS_GET_DIMENSIONS(_tex, _tex_width, _tex_height, _tex_num_samples); \
		const int2 _coord = (int2)floor((_uv) * float2(_tex_width, _tex_height)); \
		[unroll(8)] \
		for(int _i = 0; _i < _tex_num_samples && _i < 8; ++_i){ \
			const int _index = _i; \
			{ _block } \
		} \
	} while(false)

#define FOREACH_HALF_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	[unroll(1)] \
	do { \
		int _tex_width, _tex_height, _tex_num_samples; \
		TEXTURE2DMS_GET_DIMENSIONS(_tex, _tex_width, _tex_height, _tex_num_samples); \
		const int2 _coord0 = (int2)floor((_uv) * float2(_tex_width, _tex_height) - 0.5); \
		[unroll(2)] \
		for(int _dy = 0; _dy < 2; ++_dy){ \
			[unroll(2)] \
			for(int _dx = 0; _dx < 2; ++_dx){ \
				const int2 _coord = _coord0 + int2(_dx, _dy); \
				[unroll(8)] \
				for(int _i = 0; _i < _tex_num_samples && _i < 8; ++_i){ \
					const int _index = _i; \
					{ _block } \
				} \
			} \
		} \
	} while(false)

#define FOREACH_PREVIEW_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	FOREACH_SUBPIXELS(_tex, _uv, _coord, _index, _block)

#define FOREACH_HALF_PREVIEW_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	FOREACH_HALF_SUBPIXELS(_tex, _uv, _coord, _index, _block)

#endif

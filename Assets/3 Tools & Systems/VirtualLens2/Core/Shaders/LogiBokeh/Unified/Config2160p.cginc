#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_2160P_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_2160P_CGINC

static const float2 CAPTURE_RESOLUTION = float2(4096.0, 2304.0);
static const float2 TARGET_RESOLUTION = float2(3840.0, 2160.0);

static const float2 PREVIEW_CAPTURE_RESOLUTION = float2(2048.0, 1152.0);
static const float2 PREVIEW_TARGET_RESOLUTION  = float2(1920.0, 1080.0);


/* _CocTex layout
 *
 *   +---------------+-------+-------+
 *   |               | RT FG |       |
 *   |    RT Full    +-------+       |
 *   |               | RT BG |       |
 *   +---------------+-------+-------+
 *   |               |               |
 *   |     HR BG     |     HR FG     |
 *   |               |               |
 *   +---------------+---------------+
 *   |                               |
 *   |                               |
 *   |                               |
 *   |            HR Full            |
 *   |                               |
 *   |                               |
 *   |                               |
 *   +-------------------------------+
 *
 *  - HR Full: 4096 x 2304
 *  - HR BG:   2048 x 1152
 *  - HR FG:   2048 x 1152
 *  - RT Full: 2048 x 1152
 *  - RT BG:   1024 x  576
 *  - RT FG:   1024 x  576
 */

static const float2 COC_TEXTURE_RESOLUTION = float2(4096.0, 4608.0);

static const float4 COC_TEXTURE_FULL_AREA               = float4(0.0, 0.0,   1.0,  0.5);
static const float4 COC_TEXTURE_BACKGROUND_AREA         = float4(0.0, 0.5,   0.5,  0.75);
static const float4 COC_TEXTURE_FOREGROUND_AREA         = float4(0.5, 0.5,   1.0,  0.75);
static const float4 COC_TEXTURE_PREVIEW_FULL_AREA       = float4(0.0, 0.75,  0.5,  1.0);
static const float4 COC_TEXTURE_PREVIEW_BACKGROUND_AREA = float4(0.5, 0.75,  0.75, 0.875);
static const float4 COC_TEXTURE_PREVIEW_FOREGROUND_AREA = float4(0.5, 0.875, 0.75, 1.0);


/* _TileTex layout
 *
 *   +------------+------------+
 *   |     RT     | RT Dilated |
 *   +------------+------------+
 *   |                         |
 *   |       HR Dilated        |
 *   |                         |
 *   +-------------------------+
 *   |                         |
 *   |            HR           |
 *   |                         |
 *   +-------------------------+
 *
 *  - HR: 256 x 144 
 *  - RT: 128 x  72
 */

static const float2 TILE_TEXTURE_RESOLUTION = float2(256.0, 360.0);

static const float4 TILE_TEXTURE_NORMAL_AREA          = float4(0.0, 0.0 / 5.0, 1.0, 2.0 / 5.0);
static const float4 TILE_TEXTURE_DILATED_AREA         = float4(0.0, 2.0 / 5.0, 1.0, 4.0 / 5.0);
static const float4 TILE_TEXTURE_PREVIEW_NORMAL_AREA  = float4(0.0, 4.0 / 5.0, 0.5, 5.0 / 5.0);
static const float4 TILE_TEXTURE_PREVIEW_DILATED_AREA = float4(0.5, 4.0 / 5.0, 1.0, 5.0 / 5.0);


/* _DownsampledTex layout (full)
 *
 *   +---------------+---------------+
 *   |               |               |
 *   |    RT Full    |    RT Full    |
 *   |               |               |
 *   +---------------+---------------+
 *   |               |               |
 *   |  Background   |  Foreground   |
 *   |               |               |
 *   +---------------+---------------+
 *   |                               |
 *   |                               |
 *   |                               |
 *   |            HR Full            |
 *   |                               |
 *   |                               |
 *   |                               |
 *   +-------------------------------+
 *
 *  - RT Full:    2048 x 1152
 *  - RT Full:    2048 x 1152 (Premultiplied)
 *  - Background: 2048 x 1152
 *  - Foreground: 2048 x 1152
 *  - HR Full:    4096 x 2304 (Premultiplied)
 *
 *
 * _GrabTexture layout (preview)
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
 * - Background: 1024 x 576
 * - Foreground: 1024 x 576
 */

static const float2 DOWNSAMPLED_TEXTURE_RESOLUTION = float2(4096.0, 4608.0);

static const float4 DOWNSAMPLED_TEXTURE_FULL_AREA                  = float4(0.0, 0.0,  1.0, 0.5);
static const float4 DOWNSAMPLED_TEXTURE_BACKGROUND_AREA            = float4(0.0, 0.5,  0.5, 0.75);
static const float4 DOWNSAMPLED_TEXTURE_FOREGROUND_AREA            = float4(0.5, 0.5,  1.0, 0.75);
static const float4 DOWNSAMPLED_TEXTURE_PREVIEW_AREA               = float4(0.0, 0.75, 0.5, 1.0);
static const float4 DOWNSAMPLED_TEXTURE_PREVIEW_PREMULTIPLIED_AREA = float4(0.5, 0.75, 1.0, 1.0);

static const float4 PREFILTER_TEXTURE_PREVIEW_BACKGROUND_AREA      = float4(0.0, 0.0,  0.5, 0.5);
static const float4 PREFILTER_TEXTURE_PREVIEW_FOREGROUND_AREA      = float4(0.5, 0.0,  1.0, 0.5);

static const float4 BLURRED_TEXTURE_PREVIEW_BACKGROUND_AREA        = float4(0.0, 0.0,  0.5, 0.5);
static const float4 BLURRED_TEXTURE_PREVIEW_FOREGROUND_AREA        = float4(0.5, 0.0,  1.0, 0.5);


/*
 * _GrabTexture layout (preview)
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
 * - Background: 1024 x 576
 * - Foreground: 1024 x 576
 */

static const float4 WORKAREA_TEXTURE_PREVIEW_BACKGROUND_AREA = float4(0.0, 0.0,  0.5, 0.5);
static const float4 WORKAREA_TEXTURE_PREVIEW_FOREGROUND_AREA = float4(0.5, 0.0,  1.0, 0.5);


/*
 * _GrabTexture layout (full)
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
 * - Background: floor((_ScreenParams.xy) / 2)
 * - Foreground: floor((_ScreenParams.xy) / 2)
 */

#define BLURRED_TEXTURE_BACKGROUND_AREA (float4( \
	0.0, \
	0.0, \
	floor(_ScreenParams.x * 0.5) / _ScreenParams.x, \
	floor(_ScreenParams.y * 0.5) / _ScreenParams.y))

#define BLURRED_TEXTURE_FOREGROUND_AREA (float4( \
	1.0 - floor(_ScreenParams.x * 0.5) / _ScreenParams.x, \
	0.0, \
	1.0, \
	floor(_ScreenParams.y * 0.5) / _ScreenParams.y))
	

//----------------------------------------------------------------------
// Enumerate multisampled pixels
//----------------------------------------------------------------------

#define FOREACH_PREVIEW_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	[unroll(1)] \
	do { \
		int _tex_width, _tex_height, _tex_num_samples; \
		TEXTURE2DMS_GET_DIMENSIONS(_tex, _tex_width, _tex_height, _tex_num_samples); \
		const int2 _coord0 = (int2)floor((_uv) * float2(uint2(_tex_width, _tex_height) / 2)) * 2; \
		[unroll(2)] \
		for(int _dy = 0; _dy < 2; ++_dy){ \
			[unroll(2)] \
			for(int _dx = 0; _dx < 2; ++_dx){ \
				const int2 _coord = _coord0 + int2(_dx, _dy); \
				const int _index = 0; \
				{ _block } \
			} \
		} \
	} while(false)

#define FOREACH_HALF_PREVIEW_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	[unroll(1)] \
	do { \
		int _tex_width, _tex_height, _tex_num_samples; \
		TEXTURE2DMS_GET_DIMENSIONS(_tex, _tex_width, _tex_height, _tex_num_samples); \
		const int2 _coord0 = (int2)floor((_uv) * float2(uint2(_tex_width, _tex_height) / 4)) * 4; \
		[unroll(4)] \
		for(int _dy = 0; _dy < 4; ++_dy){ \
			[unroll(4)] \
			for(int _dx = 0; _dx < 4; ++_dx){ \
				const int2 _coord = _coord0 + int2(_dx, _dy); \
				const int _index = 0; \
				{ _block } \
			} \
		} \
	} while(false)

#ifndef LB_PREVIEW_MODE

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

#else

#define FOREACH_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	FOREACH_PREVIEW_SUBPIXELS(_tex, _uv, _coord, _index, _block)
#define FOREACH_HALF_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	FOREACH_HALF_PREVIEW_SUBPIXELS(_tex, _uv, _coord, _index, _block)

#endif

#include "ConfigHighResolution.cginc"

#endif

#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_1440P_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_1440P_CGINC

#include "../Common/Samplers.cginc"

static const float2 CAPTURE_RESOLUTION = float2(2752.0, 1536.0);
static const float2 TARGET_RESOLUTION = float2(2560.0, 1440.0);

static const float2 PREVIEW_CAPTURE_RESOLUTION = float2(2048.0, 1152.0);
static const float2 PREVIEW_TARGET_RESOLUTION  = float2(1920.0, 1080.0);


/* _CocTex layout
 *
 *   +-----------+-----------+-------+
 *   |           |           |       |
 *   |   RT BG   |   RT FG   |       |
 *   +-----------+-----------+       |
 *   |                       |       |
 *   |                       |       |
 *   |         RT Full       |       |
 *   |                       |       |
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
 *  - HR Full: 2752 x 1536
 *  - HR BG:   1376 x  768
 *  - HR FG:   1376 x  768
 *  - RT Full: 2048 x 1152
 *  - RT BG:   1024 x  576
 *  - RT FG:   1024 x  576
 */

static const float2 COC_TEXTURE_RESOLUTION = float2(2752.0, 4032.0);

static const float4 COC_TEXTURE_FULL_AREA               = float4(   0.0,             0.0 / 4032.0,    1.0,          1536.0 / 4032.0);
static const float4 COC_TEXTURE_BACKGROUND_AREA         = float4(   0.0,          1536.0 / 4032.0,    0.5,          2304.0 / 4032.0);
static const float4 COC_TEXTURE_FOREGROUND_AREA         = float4(   0.5,          1536.0 / 4032.0,    1.0,          2304.0 / 4032.0);
static const float4 COC_TEXTURE_PREVIEW_FULL_AREA       = float4(   0.0 / 2752.0, 2304.0 / 4032.0, 2048.0 / 2752.0, 3456.0 / 4032.0);
static const float4 COC_TEXTURE_PREVIEW_BACKGROUND_AREA = float4(   0.0 / 2752.0, 3456.0 / 4032.0, 1024.0 / 2752.0, 4032.0 / 4032.0);
static const float4 COC_TEXTURE_PREVIEW_FOREGROUND_AREA = float4(1024.0 / 2752.0, 3456.0 / 4032.0, 2048.0 / 2752.0, 4032.0 / 4032.0);


/* _TileTex layout
 *
 *   +-------------------+-----+
 *   |                   |     |
 *   |    RT Dilated     |     |
 *   +-------------------+     |
 *   |                   |     |
 *   |        RT         |     |
 *   +-------------------+-----+
 *   |                         |
 *   |       HR Dilated        |
 *   |                         |
 *   +-------------------------+
 *   |                         |
 *   |            HR           |
 *   |                         |
 *   +-------------------------+
 *
 *  - HR: 172 x 96
 *  - RT: 128 x 72
 */

static const float2 TILE_TEXTURE_RESOLUTION = float2(172.0, 336.0);

static const float4 TILE_TEXTURE_NORMAL_AREA          = float4(0.0,   0.0 / 336.0,   1.0,          96.0 / 336.0);
static const float4 TILE_TEXTURE_DILATED_AREA         = float4(0.0,  96.0 / 336.0,   1.0,         192.0 / 336.0);
static const float4 TILE_TEXTURE_PREVIEW_NORMAL_AREA  = float4(0.0, 192.0 / 336.0, 128.0 / 172.0, 264.0 / 336.0);
static const float4 TILE_TEXTURE_PREVIEW_DILATED_AREA = float4(0.0, 264.0 / 336.0, 128.0 / 172.0, 336.0 / 336.0);


/* _DownsampledTex layout
 *
 *   +------------------------+------+
 *   |                        |      |
 *   |                        |      |
 *   |         RT Full        |      |
 *   |                        |      |
 *   +------------------------+------+
 *   |                        |      |
 *   |                        |      |
 *   |         RT Full        |      |
 *   |                        |      |
 *   +---------------+--------+------+
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
 *  - Background: 1376 x  768
 *  - Foreground: 1376 x  768
 *  - HR Full:    2752 x 1536 (Premultiplied)
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

static const float2 DOWNSAMPLED_TEXTURE_RESOLUTION = float2(2752.0, 4608.0);

static const float4 DOWNSAMPLED_TEXTURE_FULL_AREA                  = float4(0.0,    0.0 / 4608.0, 2752.0 / 2752.0, 1536.0 / 4608.0);
static const float4 DOWNSAMPLED_TEXTURE_BACKGROUND_AREA            = float4(0.0, 1536.0 / 4608.0, 1376.0 / 2752.0, 2304.0 / 4608.0);
static const float4 DOWNSAMPLED_TEXTURE_FOREGROUND_AREA            = float4(0.5, 1536.0 / 4608.0, 2752.0 / 2752.0, 2304.0 / 4608.0);
static const float4 DOWNSAMPLED_TEXTURE_PREVIEW_AREA               = float4(0.0, 2304.0 / 4608.0, 2048.0 / 2752.0, 3456.0 / 4608.0);
static const float4 DOWNSAMPLED_TEXTURE_PREVIEW_PREMULTIPLIED_AREA = float4(0.0, 3456.0 / 4608.0, 2048.0 / 2752.0, 4608.0 / 4608.0);

static const float4 PREFILTER_TEXTURE_PREVIEW_BACKGROUND_AREA = float4(0.0, 0.0, 0.5, 0.5);
static const float4 PREFILTER_TEXTURE_PREVIEW_FOREGROUND_AREA = float4(0.5, 0.0, 1.0, 0.5);

static const float4 BLURRED_TEXTURE_PREVIEW_BACKGROUND_AREA = float4(0.0, 0.0, 0.5, 0.5);
static const float4 BLURRED_TEXTURE_PREVIEW_FOREGROUND_AREA = float4(0.5, 0.0, 1.0, 0.5);


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
		const float _scale = CAPTURE_RESOLUTION.y / PREVIEW_CAPTURE_RESOLUTION.y; \
		const float _width_sp = PREVIEW_CAPTURE_RESOLUTION.x * _scale; \
		const float _offset_sp = (CAPTURE_RESOLUTION.x - _width_sp) * 0.5; \
		const float2 _uv_copy = (_uv); \
		const float2 _lo_uv = _uv_copy - 0.5 * rcp(PREVIEW_CAPTURE_RESOLUTION); \
		const float2 _hi_uv = _uv_copy + 0.5 * rcp(PREVIEW_CAPTURE_RESOLUTION); \
		const int2 _lo_sp = (int2)floor(float2(_lo_uv.x * _width_sp + _offset_sp, _lo_uv.y * CAPTURE_RESOLUTION.y)); \
		const int2 _hi_sp = (int2)floor(float2(_hi_uv.x * _width_sp + _offset_sp, _hi_uv.y * CAPTURE_RESOLUTION.y)); \
		[unroll(3)] \
		for(int _y = _lo_sp.y; _y <= _hi_sp.y; ++_y){ \
			[unroll(3)] \
			for(int _x = _lo_sp.x; _x <= _hi_sp.x; ++_x){ \
				const int2 coord = int2(_x, _y); \
				[unroll(8)] \
				for(int _i = 0; _i < _tex_num_samples && _i < 8; ++_i){ \
					const int _index = _i; \
					const float2 _sp = TEXTURE2DMS_GET_SAMPLE_POSITION(_tex, _i); \
					const float2 _sp_uv = float2( \
						((float)_x + 0.5 + _sp.x - _offset_sp) * rcp(_width_sp), \
						((float)_y + 0.5 + _sp.y)              * rcp(CAPTURE_RESOLUTION.y)); \
					if(_lo_uv.x <= _sp_uv.x && _sp_uv.x < _hi_uv.x && _lo_uv.y <= _sp_uv.y && _sp_uv.y < _hi_uv.y){ \
						_block \
					} \
				} \
			} \
		} \
	} while(false)

#define FOREACH_HALF_PREVIEW_SUBPIXELS(_tex, _uv, _coord, _index, _block) \
	[unroll(1)] \
	do { \
		int _tex_width, _tex_height, _tex_num_samples; \
		TEXTURE2DMS_GET_DIMENSIONS(_tex, _tex_width, _tex_height, _tex_num_samples); \
		const float _scale = CAPTURE_RESOLUTION.y / PREVIEW_CAPTURE_RESOLUTION.y; \
		const float _width_sp = PREVIEW_CAPTURE_RESOLUTION.x * _scale; \
		const float _offset_sp = (CAPTURE_RESOLUTION.x - _width_sp) * 0.5; \
		const float2 _uv_copy = (_uv); \
		const float2 _lo_uv = _uv_copy - rcp(PREVIEW_CAPTURE_RESOLUTION); \
		const float2 _hi_uv = _uv_copy + rcp(PREVIEW_CAPTURE_RESOLUTION); \
		const int2 _lo_sp = (int2)floor(float2(_lo_uv.x * _width_sp + _offset_sp, _lo_uv.y * CAPTURE_RESOLUTION.y)); \
		const int2 _hi_sp = (int2)floor(float2(_hi_uv.x * _width_sp + _offset_sp, _hi_uv.y * CAPTURE_RESOLUTION.y)); \
		[unroll(4)] \
		for(int _y = _lo_sp.y; _y <= _hi_sp.y; ++_y){ \
			[unroll(4)] \
			for(int _x = _lo_sp.x; _x <= _hi_sp.x; ++_x){ \
				const int2 coord = int2(_x, _y); \
				[unroll(8)] \
				for(int _i = 0; _i < _tex_num_samples && _i < 8; ++_i){ \
					const int _index = _i; \
					const float2 _sp = TEXTURE2DMS_GET_SAMPLE_POSITION(_tex, _i); \
					const float2 _sp_uv = float2( \
						((float)_x + 0.5 + _sp.x - _offset_sp) * rcp(_width_sp), \
						((float)_y + 0.5 + _sp.y)              * rcp(CAPTURE_RESOLUTION.y)); \
					if(_lo_uv.x <= _sp_uv.x && _sp_uv.x < _hi_uv.x && _lo_uv.y <= _sp_uv.y && _sp_uv.y < _hi_uv.y){ \
						_block \
					} \
				} \
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

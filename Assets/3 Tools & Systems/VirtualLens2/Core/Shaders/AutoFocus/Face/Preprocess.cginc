//---------------------------------------------------------------------------
// Preprocess for YOLOX
//
// Parameters:
// - BUFFER_WIDTH
// - BUFFER_HEIGHT
// - DESTINATION_OFFSET
// - INPUT_WIDTH
// - INPUT_HEIGHT
// - OUTPUT_WIDTH
// - OUTPUT_HEIGHT
// - INV_SCALE
// - INPUT_TEXTURE
//---------------------------------------------------------------------------

#ifndef NN4VRC_YOLOX_PREPROCESS_CGINC
#define NN4VRC_YOLOX_PREPROCESS_CGINC

#include "UnityCG.cginc"
#include "../Common/Utility.cginc"
#include "../../Common/MultiSamplingHelper.cginc"


//---------------------------------------------------------------------------
// Parameter declarations
//---------------------------------------------------------------------------

int _Use_Linear_Input;

static const uint LOG_OUTPUT_WIDTH  = ceil_log2(OUTPUT_WIDTH);
static const uint LOG_OUTPUT_HEIGHT = ceil_log2(OUTPUT_HEIGHT);

static const uint CEIL_OUTPUT_WIDTH  = 1u << LOG_OUTPUT_WIDTH;
static const uint CEIL_OUTPUT_HEIGHT = 1u << LOG_OUTPUT_HEIGHT;

TEXTURE2DMS<float3> INPUT_TEXTURE;


//---------------------------------------------------------------------------
// Vertex shader
//---------------------------------------------------------------------------

v2f vertex(appdata v){
	const uint n_out = CEIL_OUTPUT_WIDTH * CEIL_OUTPUT_HEIGHT * 8u;
	return vertex_impl(v, n_out);
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

float4 fragment(v2f i) : SV_TARGET {
	const uint raw_index = compute_raw_index(i);

	uint width = 0, height = 0, num_samples = 0;
	TEXTURE2DMS_GET_DIMENSIONS(INPUT_TEXTURE, width, height, num_samples);

	const uint input_width  = width;
	const uint input_height = height;
	const float inv_scale    = (float)height / (9 * 32);

	const uint input_offset_x = (uint)round(input_width  - OUTPUT_WIDTH  * inv_scale) / 2u;
	const uint input_offset_y = (uint)round(input_height - OUTPUT_HEIGHT * inv_scale) / 2u;

	const uint x_width = LOG_OUTPUT_WIDTH;
	const uint y_width = LOG_OUTPUT_HEIGHT;
	const uint x_mask = make_mask(x_width);
	const uint y_mask = make_mask(y_width);

	const uint out_x = raw_index &  x_mask;
	const uint out_y = raw_index >> x_width;
	if(out_x >= OUTPUT_WIDTH || out_y >= OUTPUT_HEIGHT){ return 0; }

	const uint in_x0 = (uint)round(out_x * inv_scale) + input_offset_x;
	const uint in_y0 = (uint)round(out_y * inv_scale) + input_offset_y;

	const uint sample_count = 4u;
	const float step = inv_scale / sample_count;
	float3 acc = 0;
	[unroll]
	for(int dy = 0; dy < (int)sample_count; ++dy){
		[unroll]
		for(int dx = 0; dx < (int)sample_count; ++dx){
			const int x = in_x0 + (int)round(dx * step);
			const int y = in_y0 + (int)round(dy * step);
			acc += INPUT_TEXTURE.Load(
				TEXTURE2DMS_COORD(int2(x, input_height - 1 - y)), 0);
		}
	}
	acc /= sample_count * sample_count;
	if(_Use_Linear_Input){
		acc = LinearToGammaSpace(acc);
	}

	const float3 mean = float3(0.485, 0.456, 0.406);
	const float3 std  = float3(0.229, 0.224, 0.225);
	acc = (acc - mean) * rcp(std);

	return float4(acc, 0.0);
}


#endif

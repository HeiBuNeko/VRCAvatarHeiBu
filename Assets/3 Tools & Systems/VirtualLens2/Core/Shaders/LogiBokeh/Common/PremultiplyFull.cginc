#ifndef VIRTUALLENS2_LOGIBOKEH_COMMON_PREMULTIPLY_FULL_CGINC
#define VIRTUALLENS2_LOGIBOKEH_COMMON_PREMULTIPLY_FULL_CGINC

#include "../../Common/MultiSamplingHelper.cginc"
#include "ComputeCoc.cginc"

//---------------------------------------------------------------------------
// Utility functions
//---------------------------------------------------------------------------

float4 premultiplyFull(
	TEXTURE2DMS<float3> color_tex,
	TEXTURE2DMS<float>  depth_tex,
	float2 uv,
	float3 params)
{
	float4 acc = 0.0;
	float num_samples = 0.0;
	FOREACH_SUBPIXELS(color_tex, uv, coord, i, {
		const float3 rgb   = max(color_tex.Load(TEXTURE2DMS_COORD(coord), i), 0.0);
		const float  depth = depth_tex.Load(TEXTURE2DMS_COORD(coord), i);
		const float  coc   = computeCoc(depth, params);
		const float  w     = saturate(1.5 - abs(coc));
		acc += float4(rgb * w, w);
		num_samples += 1.0;
	});
	return acc * rcp(num_samples);
}

#endif

#ifndef VIRTUALLENS2_LOGIBOKEH_COMMON_FOREGROUND_PREFILTER_CGINC
#define VIRTUALLENS2_LOGIBOKEH_COMMON_FOREGROUND_PREFILTER_CGINC

#include "../../Common/MultiSamplingHelper.cginc"
#include "Samplers.cginc"
#include "ComputeCoc.cginc"

float4 downsampleForeground(
	TEXTURE2DMS<float3> color_tex,
	TEXTURE2DMS<float>  depth_tex,
	float2 uv,
	float3 params)
{
	const float tile_max = fetchTile(uv).w;
	if(tile_max < 0.5){ return 0.0; }

	const float pixel_coc = fetchForegroundCoc(uv);
	if(pixel_coc < 0.5){ return 0.0; }

	float4 acc = 0.0;
	FOREACH_HALF_SUBPIXELS(color_tex, uv, coord, i, {
		const float3 color = max(color_tex.Load(TEXTURE2DMS_COORD(coord), i), 0.0);
		const float  coc   = -computeCoc(depth_tex.Load(TEXTURE2DMS_COORD(coord), i), params);
		const float  w     = saturate(1.0 - abs(pixel_coc - coc));
		acc += float4(color * w, w);
	});
	if(acc.a <= 0.0){
		return float4(0, 0, 0, 0);
	}else{
		const float3 rgb = acc.rgb * rcp(acc.a);
		const float  a   = max(pixel_coc - 0.5, 0.0);
		return float4(rgb * a, a);
	}
}

#endif

#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_COMPUTE_COC_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_COMPUTE_COC_CGINC

#include "UnityCG.cginc"
#include "../../Common/Constants.cginc"
#include "../../Common/TargetDetector.cginc"
#include "../../Common/MultiSamplingHelper.cginc"
#include "../Common/Samplers.cginc"

#include "RingKernel.cginc"
#include "../Common/ComputeCoc.cginc"
#include "Utility.cginc"


//---------------------------------------------------------------------------
// Parameter declarations
//---------------------------------------------------------------------------

float _Blurring;

TEXTURE2DMS<float> _DepthTex;


//---------------------------------------------------------------------------
// Structure definitions
//---------------------------------------------------------------------------

struct appdata {
	float4 vertex : POSITION;
	float2 uv     : TEXCOORD0;
};

struct v2f {
	float4 vertex  : SV_POSITION;
	float2 uv      : TEXCOORD0;
	float3 params0 : TEXCOORD1;
	float3 params1 : TEXCOORD2;
};


//---------------------------------------------------------------------------
// Vertex shader
//---------------------------------------------------------------------------

v2f vertex(appdata v){
	v2f o;

	if(_Blurring == 0.0 || !isVirtualLensCustomComputeCamera((int2)COC_TEXTURE_RESOLUTION)){
		o.vertex  = 0;
		o.uv      = 0;
		o.params0 = 0;
		o.params1 = 0;
		return o;
	}

	const float x = v.uv.x;
	const float y = v.uv.y;

	const float proj_x = (x * 2 - 1);
	const float proj_y = (y * 2 - 1) * _ProjectionParams.x;
	o.vertex  = float4(proj_x, proj_y, 0, 1);
	o.uv      = float2(x, y);
	o.params0 = computeCocParameters(_DepthTex, TARGET_RESOLUTION);
	o.params1 = computeCocParameters(_DepthTex, PREVIEW_TARGET_RESOLUTION);
	return o;
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

float computeFull(float2 uv, float3 params){
	float coc_sum = 0.0, w_sum = 0.0;
	FOREACH_SUBPIXELS(_DepthTex, uv, coord, i, {
		const float coc = computeCoc(_DepthTex.Load(TEXTURE2DMS_COORD(coord), i), params);
		const float w   = saturate(1.5 - abs(coc));
		coc_sum += w * coc;
		w_sum   += w;
	});
	if(w_sum == 0.0){
		return 0.0;
	}else{
		return coc_sum / w_sum;
	}
}


float computeHalf(float2 uv, float3 params, float s){
	float best_coc = -1e3, best_score = 1e30;
	FOREACH_HALF_SUBPIXELS(_DepthTex, uv, coord, i, {
		const float coc = s * computeCoc(_DepthTex.Load(TEXTURE2DMS_COORD(coord), i), params);
		const float score = (coc - 1.5) * (coc - 1.5);
		if(coc >= 0.0 && score < best_score){
			best_coc = coc;
			best_score = score;
		}
	});
	return best_coc;
}


float computePreviewFull(float2 uv, float3 params){
	float coc_sum = 0.0, w_sum = 0.0;
	FOREACH_PREVIEW_SUBPIXELS(_DepthTex, uv, coord, i, {
		const float coc = computeCoc(_DepthTex.Load(TEXTURE2DMS_COORD(coord), i), params);
		const float w   = saturate(1.5 - abs(coc));
		coc_sum += w * coc;
		w_sum   += w;
	});
	if(w_sum == 0.0){
		return 0.0;
	}else{
		return coc_sum / w_sum;
	}
}


float computePreviewHalf(float2 uv, float3 params, float s){
	float best_coc = -1e3, best_score = 1e30;
	FOREACH_HALF_PREVIEW_SUBPIXELS(_DepthTex, uv, coord, i, {
		const float coc = s * computeCoc(_DepthTex.Load(TEXTURE2DMS_COORD(coord), i), params);
		const float score = (coc - 1.5) * (coc - 1.5);
		if(coc >= 0.0 && score < best_score){
			best_coc = coc;
			best_score = score;
		}
	});
	return best_coc;
}


float fragment(v2f i) : SV_Target {
	if(inArea(i.uv, COC_TEXTURE_FULL_AREA)){
		return computeFull(transformUV(i.uv, COC_TEXTURE_FULL_AREA), i.params0);
	}else if(inArea(i.uv, COC_TEXTURE_BACKGROUND_AREA)){
		return computeHalf(transformUV(i.uv, COC_TEXTURE_BACKGROUND_AREA), i.params0,  1.0);
	}else if(inArea(i.uv, COC_TEXTURE_FOREGROUND_AREA)){
		return computeHalf(transformUV(i.uv, COC_TEXTURE_FOREGROUND_AREA), i.params0, -1.0);
	}else if(inArea(i.uv, COC_TEXTURE_PREVIEW_FULL_AREA)){
		return computePreviewFull(transformUV(i.uv, COC_TEXTURE_PREVIEW_FULL_AREA), i.params1);
	}else if(inArea(i.uv, COC_TEXTURE_PREVIEW_BACKGROUND_AREA)){
		return computePreviewHalf(transformUV(i.uv, COC_TEXTURE_PREVIEW_BACKGROUND_AREA), i.params1,  1.0);
	}else if(inArea(i.uv, COC_TEXTURE_PREVIEW_FOREGROUND_AREA)){
		return computePreviewHalf(transformUV(i.uv, COC_TEXTURE_PREVIEW_FOREGROUND_AREA), i.params1, -1.0);
	}
	discard;
	return 0; // to suppress warnings
}

#endif

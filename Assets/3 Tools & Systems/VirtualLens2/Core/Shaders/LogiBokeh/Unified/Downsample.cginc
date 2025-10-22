#ifndef VIRTUALLENS2_LOGIBOKEH_Unified_DOWNSAMPLE_CGINC
#define VIRTUALLENS2_LOGIBOKEH_Unified_DOWNSAMPLE_CGINC

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

TEXTURE2DMS<float3> _MainTex;
TEXTURE2DMS<float>  _DepthTex;


//---------------------------------------------------------------------------
// Structure definitions
//---------------------------------------------------------------------------

struct appdata {
	float4 vertex : POSITION;
	float2 uv     : TEXCOORD0;
};

struct v2f {
	float4 vertex : SV_POSITION;
	float2 uv     : TEXCOORD0;
	float3 params : TEXCOORD1;
};


//---------------------------------------------------------------------------
// Vertex shader
//---------------------------------------------------------------------------

v2f vertex(appdata v){
	v2f o;

	if(!isVirtualLensCustomComputeCamera((int2)DOWNSAMPLED_TEXTURE_RESOLUTION)){
		o.vertex = 0;
		o.uv     = 0;
		o.params = 0;
		return o;
	}

	const float x = v.uv.x;
	const float y = v.uv.y;

	const float proj_x = (x * 2 - 1);
	const float proj_y = (y * 2 - 1) * _ProjectionParams.x;
	o.vertex = float4(proj_x, proj_y, 0, 1);
	o.uv     = v.uv;
	o.params = computeCocParameters(_DepthTex, PREVIEW_TARGET_RESOLUTION);
	return o;
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

float4 computePreview(float2 uv, float3 params){
	float3 acc = 0.0;
	float num_samples = 0.0;
	FOREACH_PREVIEW_SUBPIXELS(_MainTex, uv, coord, i, {
		const float3 rgb = max(_MainTex.Load(TEXTURE2DMS_COORD(coord), i), 0.0);
		acc += rgb;
		num_samples += 1.0;
	});
	return float4(acc * rcp(num_samples), 1.0);
}


float4 computePreviewPremultiplied(float2 uv, float3 params){
	float4 acc = 0.0;
	float num_samples = 0.0;
	FOREACH_PREVIEW_SUBPIXELS(_MainTex, uv, coord, i, {
		const float3 rgb   = max(_MainTex.Load(TEXTURE2DMS_COORD(coord), i), 0.0);
		const float  depth = _DepthTex.Load(TEXTURE2DMS_COORD(coord), i);
		const float  coc   = computeCoc(depth, params);
		const float  w     = saturate(1.5 - abs(coc));
		acc += float4(rgb * w, w);
		num_samples += 1.0;
	});
	return acc * rcp(num_samples);
}


float4 fragment(v2f i) : SV_Target {
	if(inArea(i.uv, DOWNSAMPLED_TEXTURE_FULL_AREA)){
	}else if(inArea(i.uv, DOWNSAMPLED_TEXTURE_BACKGROUND_AREA)){
	}else if(inArea(i.uv, DOWNSAMPLED_TEXTURE_FOREGROUND_AREA)){
	}else if(inArea(i.uv, DOWNSAMPLED_TEXTURE_PREVIEW_AREA)){
		return computePreview(transformUV(i.uv, DOWNSAMPLED_TEXTURE_PREVIEW_AREA), i.params);
	}else if(inArea(i.uv, DOWNSAMPLED_TEXTURE_PREVIEW_PREMULTIPLIED_AREA)){
		return computePreviewPremultiplied(transformUV(i.uv, DOWNSAMPLED_TEXTURE_PREVIEW_PREMULTIPLIED_AREA), i.params);
	}
	discard;
	return 0; // to suppress warnings
}


#endif

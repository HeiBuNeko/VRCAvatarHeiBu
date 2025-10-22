#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_COPY_PREVIEW_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_COPY_PREVIEW_CGINC

#include "UnityCG.cginc"
#include "../../Common/Constants.cginc"
#include "../../Common/TargetDetector.cginc"
#include "../../Common/MultiSamplingHelper.cginc"
#include "../Common/Samplers.cginc"

#include "RingKernel.cginc"


//---------------------------------------------------------------------------
// Parameter declarations
//---------------------------------------------------------------------------

Texture2D<float4> _ResultTex;


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
};


//---------------------------------------------------------------------------
// Vertex shader
//---------------------------------------------------------------------------

v2f vertex(appdata v){
	v2f o;

#ifdef FORCE_COPY_RESULT
	if(!isVRChatCamera() && !isVRChatCameraHighResolution()){
#else
	if(!isVRChatCamera() || isVRChatCameraHighResolution()){
#endif
		o.vertex = 0;
		o.uv     = 0;
		return o;
	}

	const float x = (v.uv.x * 2 - 1);
	const float y = (v.uv.y * 2 - 1) * _ProjectionParams.x;
#ifdef UNITY_REVERSED_Z
	const float z = 1.0;
#else
	const float z = UNITY_NEAR_CLIP_VALUE;
#endif
	const float internal_aspect = ASPECT;
	const float target_aspect = _ScreenParams.x / _ScreenParams.y;
	float x_scale = INV_SCREEN_ENLARGEMENT.x;
	float y_scale = INV_SCREEN_ENLARGEMENT.y;
	if(target_aspect > internal_aspect){
		y_scale *= internal_aspect / target_aspect;
	}else{
		x_scale *= target_aspect / internal_aspect;
	}
	o.vertex = float4(x, y, z, 1);
	o.uv.x = v.uv.x * x_scale + (1.0 - x_scale) * 0.5;
	o.uv.y = v.uv.y * y_scale + (1.0 - y_scale) * 0.5;
	return o;
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

float4 fragment(v2f i) : SV_Target {
	return _ResultTex.Sample(linear_clamp_sampler, i.uv);
}


#endif

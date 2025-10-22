#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_PREVIEW_BLUR_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_PREVIEW_BLUR_CGINC

#include "UnityCG.cginc"
#include "../../Common/Constants.cginc"
#include "../../Common/TargetDetector.cginc"
#include "../../Common/MultiSamplingHelper.cginc"
#include "../Common/Samplers.cginc"

#include "RingKernel.cginc"
#include "../Common/BackgroundBlur.cginc"
#include "../Common/ForegroundBlur.cginc"
#include "Utility.cginc"


//---------------------------------------------------------------------------
// Parameter declarations
//---------------------------------------------------------------------------

float _Blurring;
int _MaxNumRings;


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

	if(_Blurring == 0.0 || !isVirtualLensCustomComputeCamera((int2)PREVIEW_CAPTURE_RESOLUTION)){
		o.vertex = 0;
		o.uv     = 0;
		return o;
	}

	const float x = v.uv.x;
	const float y = v.uv.y;

	const float proj_x = (x * 2 - 1);
	const float proj_y = (y * 2 - 1) * _ProjectionParams.x;
	o.vertex = float4(proj_x, proj_y, 0, 1);
	o.uv     = v.uv;
	return o;
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

float4 fragment(v2f i) : SV_Target {
	if(inArea(i.uv, BLURRED_TEXTURE_PREVIEW_BACKGROUND_AREA)){
		// TODO: discard unnecessary pixels
		return blurBackground(_MaxNumRings, transformUV(i.uv, BLURRED_TEXTURE_PREVIEW_BACKGROUND_AREA));
	}else if(inArea(i.uv, BLURRED_TEXTURE_PREVIEW_FOREGROUND_AREA)){
		// TODO: discard unnecessary pixels
		return blurForeground(_MaxNumRings, transformUV(i.uv, BLURRED_TEXTURE_PREVIEW_FOREGROUND_AREA));
	}
	discard;
	return 0; // to suppress warnings
}


#endif

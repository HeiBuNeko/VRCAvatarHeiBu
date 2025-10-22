#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_PREVIEW_COMPOSITION_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_PREVIEW_COMPOSITION_CGINC

#include "UnityCG.cginc"
#include "../../Common/Constants.cginc"
#include "../../Common/TargetDetector.cginc"
#include "../../Common/MultiSamplingHelper.cginc"
#include "../Common/Samplers.cginc"

#include "RingKernel.cginc"
#include "../Common/Composition.cginc"
#include "Utility.cginc"


//---------------------------------------------------------------------------
// Parameter declarations
//---------------------------------------------------------------------------

TEXTURE2DMS<float4> _MainTex;

float _Blurring;
float _Exposure;
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
	float2 params : TEXCOORD1;
};


//---------------------------------------------------------------------------
// Vertex shader
//---------------------------------------------------------------------------

v2f vertex(appdata v){
	v2f o;

	if(!isVirtualLensCustomComputeCamera((int2)PREVIEW_CAPTURE_RESOLUTION)){
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
	o.params = float2(exp(_Exposure), 0);
	return o;
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

float4 fragment(v2f i) : SV_Target {
	// TODO: discard unnecessary pixels
	const float exposure = i.params.x;
	float3 rgb = float3(0, 0, 0);
	if(_Blurring > 0.0){
		rgb = exposure * composition(_MaxNumRings, i.uv);
	}else{
		float count = 0.0;
		FOREACH_PREVIEW_SUBPIXELS(_MainTex, i.uv, coord, i, {
			rgb += _MainTex.Load(TEXTURE2DMS_COORD(coord), i).xyz;
			count += 1.0;
		});
		rgb *= exposure / count;
	}
	return prepareAntiAliasing(rgb);
}


#endif

#ifndef VIRTUALLENS2_LOGIBOKEH_HIGH_RESOLUTION_DILATE_TILES_CGINC
#define VIRTUALLENS2_LOGIBOKEH_HIGH_RESOLUTION_DILATE_TILES_CGINC

#include "UnityCG.cginc"
#include "../../Common/Constants.cginc"
#include "../../Common/TargetDetector.cginc"

#include "RingKernel.cginc"
#include "../Common/TileDilator.cginc"
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
	o.vertex = 0;
	o.uv     = 0;

	if(_Blurring == 0.0 || !isVirtualLensCustomComputeCamera(TILE_TEXTURE_RESOLUTION)){
		return o;
	}

#ifndef LB_PREVIEW_MODE
	const float4 area = TILE_TEXTURE_DILATED_AREA;
#else
	if(all(TILE_TEXTURE_DILATED_AREA == TILE_TEXTURE_PREVIEW_DILATED_AREA)){ return o; }
	const float4 area = TILE_TEXTURE_PREVIEW_DILATED_AREA;
#endif

	const float x = (area.z - area.x) * v.uv.x + area.x;
	const float y = (area.w - area.y) * v.uv.y + area.y;

	const float proj_x = (x * 2 - 1);
	const float proj_y = (y * 2 - 1) * _ProjectionParams.x;
	o.vertex = float4(proj_x, proj_y, 0, 1);
	o.uv     = float2(x, y);
	return o;
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

float4 fragment(v2f i) : SV_Target {
#ifndef LB_PREVIEW_MODE
	const float4 src_area = TILE_TEXTURE_NORMAL_AREA;
	const float4 dst_area = TILE_TEXTURE_DILATED_AREA;
#else
	const float4 src_area = TILE_TEXTURE_PREVIEW_NORMAL_AREA;
	const float4 dst_area = TILE_TEXTURE_PREVIEW_DILATED_AREA;
#endif
	return dilateTile(
		NUM_RINGS_SCALER * _MaxNumRings,
		transformUV(i.uv, dst_area), src_area.xy, src_area.zw);
}


#endif

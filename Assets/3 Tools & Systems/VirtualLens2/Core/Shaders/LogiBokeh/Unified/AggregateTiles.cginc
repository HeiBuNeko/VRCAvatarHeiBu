#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_AGGREGATE_TILES_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_AGGREGATE_TILES_CGINC

#include "UnityCG.cginc"
#include "../../Common/Constants.cginc"
#include "../../Common/TargetDetector.cginc"

#include "../Common/TileAggregator.cginc"
#include "Utility.cginc"


//---------------------------------------------------------------------------
// Parameter declarations
//---------------------------------------------------------------------------

float _Blurring;


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
	o.uv = 0;

	if(_Blurring == 0.0 || !isVirtualLensCustomComputeCamera(TILE_TEXTURE_RESOLUTION)){
		return o;
	}

#ifndef LB_PREVIEW_MODE
	const float4 area = TILE_TEXTURE_NORMAL_AREA;
#else
	if(all(TILE_TEXTURE_NORMAL_AREA == TILE_TEXTURE_PREVIEW_NORMAL_AREA)){ return o; }
	const float4 area = TILE_TEXTURE_PREVIEW_NORMAL_AREA;
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
	const float4 area = TILE_TEXTURE_NORMAL_AREA;
#else
	const float4 area = TILE_TEXTURE_PREVIEW_NORMAL_AREA;
#endif
	if(inArea(i.uv, area)){
		return aggregateTile(transformUV(i.uv, area));
	}
	discard;
	return 0; // to suppress warnings
}


#endif

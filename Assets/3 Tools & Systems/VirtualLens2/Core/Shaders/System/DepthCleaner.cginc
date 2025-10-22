#ifndef VIRTUALLENS2_SYSTEM_DEPTH_CLEANER_CGINC
#define VIRTUALLENS2_SYSTEM_DEPTH_CLEANER_CGINC

#include "UnityCG.cginc"
#include "../Common/Constants.cginc"
#include "../Common/TargetDetector.cginc"


//---------------------------------------------------------------------------
// Parameter declarations
//---------------------------------------------------------------------------

float _EnableShadowCaster;
float _ShadowCasterDepth;


//---------------------------------------------------------------------------
// Structure definitions
//---------------------------------------------------------------------------

struct appdata {
	float4 vertex : POSITION;
	float2 uv     : TEXCOORD0;
};

struct v2f {
	float4 vertex : SV_POSITION;
};


//---------------------------------------------------------------------------
// Vertex shader
//---------------------------------------------------------------------------

v2f vertex(appdata v){
	v2f o;

	if(!isVRChatCamera() && !isVRChatCameraHighResolution()){
		o.vertex = 0;
		return o;
	}

#ifdef IS_SHADOW_CASTER
	if(_EnableShadowCaster < 0.5){
		o.vertex = 0;
		return o;
	}
#endif

	const float x = (v.uv.x * 2 - 1);
	const float y = (v.uv.y * 2 - 1) * _ProjectionParams.x;

#ifdef UNITY_REVERSED_Z
#define NEAR (1.0)
#define FAR  (0.0)
#else
#define NEAR (UNITY_NEAR_CLIP_VALUE)
#define FAR  (1.0)
#endif

#ifdef IS_SHADOW_CASTER
	const float d = _ShadowCasterDepth;
	const float z = NEAR * (1.0 - d) + FAR * d;
#else
	const float z = NEAR;
#endif

	o.vertex = float4(x, y, z, 1);
	return o;
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

// Write depth only
void fragment(v2f i){ }

#endif

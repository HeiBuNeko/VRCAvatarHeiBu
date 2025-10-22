#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_UTILITY_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_UTILITY_CGINC

bool inArea(float2 uv, float4 area){
	return area.x <= uv.x && uv.x < area.z && area.y <= uv.y && uv.y < area.w;
}

float2 transformUV(float2 uv, float4 area){
	return (uv - area.xy) / (area.zw - area.xy);
}

#endif


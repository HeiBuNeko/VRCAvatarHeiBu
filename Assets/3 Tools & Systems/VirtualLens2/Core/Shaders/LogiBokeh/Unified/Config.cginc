#ifndef VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_CGINC
#define VIRTUALLENS2_LOGIBOKEH_UNIFIED_CONFIG_CGINC

#if defined(LB_TARGET_1080P)
#include "Config1080p.cginc"
#elif defined(LB_TARGET_1440P)
#include "Config1440p.cginc"
#elif defined(LB_TARGET_2160P)
#include "Config2160p.cginc"
#elif defined(LB_TARGET_4320P)
#include "Config4320p.cginc"
#endif

#endif

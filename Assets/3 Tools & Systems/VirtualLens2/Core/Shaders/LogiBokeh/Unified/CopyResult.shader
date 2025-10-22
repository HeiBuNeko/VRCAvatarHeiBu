Shader "VirtualLens2/LogiBokeh/Unified/CopyResult"
{
	Properties
	{
		[NoScaleOffset] _ResultTex("ResultTex", 2D) = "black" {}
		[Toggle] _IsDesktopMode("Is desktop mode", Float) = 0.0
	}

	SubShader
	{
		Tags
		{
			"RenderType"      = "Transparent"
			"Queue"           = "Overlay+1000"
			"DisableBatching" = "True"
			"IgnoreProjector" = "True"
		}

		LOD    100
		Blend  Off
		Cull   Back
		ZWrite Off
		ZTest  Always

		Pass
		{
			Tags { "LightMode" = "Always" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#define FORCE_COPY_RESULT
			#include "CopyResult.cginc"
			ENDCG
		}
	}
}

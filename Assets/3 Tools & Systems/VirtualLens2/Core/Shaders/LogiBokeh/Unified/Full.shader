Shader "VirtualLens2/LogiBokeh/Unified/Full"
{
	Properties
	{
		[NoScaleOffset] _MainTex("MainTex", 2D) = "black" {}
		[NoScaleOffset] _DownsampledTex("DownsampledTex", 2D) = "black" {}
		[NoScaleOffset] _DepthTex("DepthTex", 2D) = "black" {}
		[NoScaleOffset] _CocTex("CocTex", 2D) = "black" {}
		[NoScaleOffset] _TileTex("TileTex", 2D) = "black" {}
		[NoScaleOffset] _ResultTex("ResultTex", 2D) = "black" {}
		_Blurring("Blur flag", Float) = 0.0
		_Exposure("Exposure Value", Float) = 0.0
		_MaxNumRings("Maximum Number of Rings", int) = 12
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
			#pragma shader_feature_local WITH_MULTI_SAMPLING
			#pragma multi_compile_local LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#include "Config.cginc"
			#include "FullBlur.cginc"
			ENDCG
		}

		GrabPass { "_VirtualLens2_WorkareaTex" }

		Pass
		{
			Tags { "LightMode" = "Always" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#pragma shader_feature_local WITH_MULTI_SAMPLING
			#pragma multi_compile_local LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#define FUSED_POSTFILTERING
			#include "Config.cginc"
			#include "FullComposition.cginc"
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "Always" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#include "CopyResult.cginc"
			ENDCG
		}
	}
}

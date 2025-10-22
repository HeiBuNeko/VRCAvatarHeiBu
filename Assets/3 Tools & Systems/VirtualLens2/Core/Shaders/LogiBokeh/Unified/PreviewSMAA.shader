Shader "VirtualLens2/LogiBokeh/Unified/PreviewSMAA"
{
	Properties
	{
		[NoScaleOffset] _MainTex("MainTex", 2D) = "black" {}
		[NoScaleOffset] _DownsampledTex("DownsampledTex", 2D) = "black" {}
		[NoScaleOffset] _DepthTex("DepthTex", 2D) = "black" {}
		[NoScaleOffset] _CocTex("CocTex", 2D) = "black" {}
		[NoScaleOffset] _TileTex("TileTex", 2D) = "black" {}
		[NoScaleOffset] _StateTex("StateTex", 2D) = "black" {}
		[NoScaleOffset] _SMAAAreaTex("SMAAAreaTex", 2D) = "black" {}
		[NoScaleOffset] _SMAASearchTex("SMAASearchTex", 2D) = "black" {}
		_Near("Near", Float) = 0.05
		_Far("Far", Float) = 1000.0
		_FieldOfView("Field of View", Float) = 60.0
		_LogFNumber("F Number (log)", Float) = 2.0
		_LogFocusDistance("Manual Focus Distance (log)", Float) = 2.0
		_BlurringThresh("Maximum F Number for Blurring", Float) = 0.0
		_FocusingThresh("Minimum Distance for Manual Focusing", Float) = 0.0
		_Blurring("Blur flag", Float) = 0.0
		_Exposure("Exposure Value", Float) = 0.0
		_MaxNumRings("Maximum Number of Rings", int) = 12
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
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma vertex   vertex
			#pragma fragment fragment
			#include "../../Common/EmptyPass.cginc"
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#pragma shader_feature_local WITH_MULTI_SAMPLING
			#pragma multi_compile_local LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#define LB_PREVIEW_MODE
			#include "Config.cginc"
			#include "PreviewPrefilter.cginc"
			ENDCG
		}

		GrabPass
		{
			Tags { "LightMode" = "Vertex" }
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#pragma shader_feature_local WITH_MULTI_SAMPLING
			#pragma multi_compile_local LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#define LB_PREVIEW_MODE
			#include "Config.cginc"
			#include "PreviewBlur.cginc"
			ENDCG
		}

		GrabPass
		{
			Tags { "LightMode" = "Vertex" }
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#pragma shader_feature_local WITH_MULTI_SAMPLING
			#pragma multi_compile_local LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#define LB_PREVIEW_MODE
			#include "Config.cginc"
			#include "PreviewPostfilter.cginc"
			ENDCG
		}

		GrabPass
		{
			Tags { "LightMode" = "Vertex" }
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#pragma shader_feature_local WITH_MULTI_SAMPLING
			#pragma multi_compile_local LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#define LB_PREVIEW_MODE
			#define USE_SMAA
			#include "Config.cginc"
			#include "PreviewComposition.cginc"
			ENDCG
		}

		GrabPass
		{
			"_VirtualLens2_BlurredTex"
			Tags { "LightMode" = "Vertex" }
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			HLSLPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#define REALTIME_SMAA
			#include "RealtimeSMAA.cginc"
			#include "SMAAEdgeDetection.cginc"
			ENDHLSL
		}

		GrabPass
		{
			Tags { "LightMode" = "Vertex" }
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			HLSLPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#define REALTIME_SMAA
			#include "RealtimeSMAA.cginc"
			#include "SMAABlendingWeightCalculation.cginc"
			ENDHLSL
		}

		GrabPass
		{
			Tags { "LightMode" = "Vertex" }
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			HLSLPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#define REALTIME_SMAA
			#include "RealtimeSMAA.cginc"
			#include "SMAANeighborhoodBlending.cginc"
			ENDHLSL
		}
	}
}

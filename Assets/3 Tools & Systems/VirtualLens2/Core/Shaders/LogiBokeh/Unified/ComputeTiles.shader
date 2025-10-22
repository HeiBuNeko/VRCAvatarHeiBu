Shader "VirtualLens2/LogiBokeh/Unified/ComputeTiles"
{
	Properties
	{
		[NoScaleOffset] _CocTex("CocTex", 2D) = "black" {}
		_Blurring("Blur flag", Float) = 0.0
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
			#pragma multi_compile_local LB_TARGET_1080P LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#include "Config.cginc"
			#include "AggregateTiles.cginc"
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#pragma multi_compile_local LB_TARGET_1080P LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#define LB_PREVIEW_MODE
			#include "Config.cginc"
			#include "AggregateTiles.cginc"
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
			#pragma multi_compile_local LB_TARGET_1080P LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#include "Config.cginc"
			#include "DilateTiles.cginc"
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "Vertex" }
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vertex
			#pragma fragment fragment
			#pragma multi_compile_local LB_TARGET_1080P LB_TARGET_1440P LB_TARGET_2160P LB_TARGET_4320P
			#define LB_PREVIEW_MODE
			#include "Config.cginc"
			#include "DilateTiles.cginc"
			ENDCG
		}
	}
}

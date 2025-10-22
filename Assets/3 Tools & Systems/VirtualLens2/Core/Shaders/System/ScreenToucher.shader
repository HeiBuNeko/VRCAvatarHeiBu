Shader "VirtualLens2/System/ScreenToucher"
{
	Properties
	{
		_HorizontalFlip("Horizontal Flip", Float) = 0.0
	}

	SubShader
	{
		Tags
		{
			"RenderType"      = "Transparent"
			"Queue"           = "Overlay+1000"
			"IgnoreProjector" = "True"
		}

		LOD    100
		Blend  Off
		Cull   Back
		ZWrite On
		ZTest  Always

		Pass
		{
			CGPROGRAM
			#pragma vertex   vertex
			#pragma fragment fragment
			#include "ScreenToucher.cginc"
			ENDCG
		}
	}
}

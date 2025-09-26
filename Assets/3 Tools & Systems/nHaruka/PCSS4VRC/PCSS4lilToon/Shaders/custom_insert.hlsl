#if defined(SPOT) && defined(SHADOWS_DEPTH) && !defined(LIL_OUTLINE) && !defined(LIL_LITE)

	#define LIL_CUSTOM_V2F_MEMBER(id0,id1,id2,id3,id4,id5,id6,id7)\
		float4 positionOS : TEXCOORD ## id0;
	// Add vertex copy
	#define LIL_CUSTOM_VERT_COPY \
		 output.positionOS = input.positionOS;

		#if defined(LIL_FEATURE_NORMAL_1ST) && defined(LIL_FEATURE_NORMAL_2ND)
			
			//#if defined(LIL_FEATURE_BumpMap) && defined(LIL_FEATURE_Bump2ndMap)

				#define COMBINENORMAL \	
					if (_UseBumpMap)\
					{\
					float4 normalTex = LIL_SAMPLE_2D_ST(_BumpMap, sampler_MainTex, fd.uv0); \
					pre_normalmap = lilUnpackNormalScale(normalTex, _BumpScale); \
					}\
					if (_UseBump2ndMap)\
					{\
					float2 uvBump2nd = fd.uv0; \
					if (_Bump2ndMap_UVMode == 1) uvBump2nd = fd.uv1; \
					if (_Bump2ndMap_UVMode == 2) uvBump2nd = fd.uv2; \
					if (_Bump2ndMap_UVMode == 3) uvBump2nd = fd.uv3; \
					float4 normal2ndTex = LIL_SAMPLE_2D_ST(_Bump2ndMap, sampler_MainTex, uvBump2nd); \
					float bump2ndScale = _Bump2ndScale; \
					bump2ndScale *= LIL_SAMPLE_2D_ST(_Bump2ndScaleMask, sampler_MainTex, fd.uv0).r; \
					pre_normalmap = lilBlendNormal(pre_normalmap, lilUnpackNormalScale(normal2ndTex, bump2ndScale)); \
					}


				//#define OVERRIDE_NORMAL_1ST \
					//normalmap = pre_normalmap;

				//#define OVERRIDE_NORMAL_2ND \
					//normalmap = pre_normalmap;

			//#endif

		#elif defined(LIL_FEATURE_NORMAL_1ST) 

			#if defined(LIL_FEATURE_BumpMap)

				#define COMBINENORMAL \	
					if (_UseBumpMap)\
					{\
					float4 normalTex = LIL_SAMPLE_2D_ST(_BumpMap, sampler_MainTex, fd.uv0); \
					pre_normalmap = lilUnpackNormalScale(normalTex, _BumpScale); \
					}

				#define OVERRIDE_NORMAL_1ST \
					normalmap = pre_normalmap;

			#endif

		#elif defined(LIL_FEATURE_NORMAL_2ND)

			#if defined(LIL_FEATURE_Bump2ndMap)

				#define COMBINENORMAL \	
					if (_UseBump2ndMap)\
					{\
					float2 uvBump2nd = fd.uv0; \
					if (_Bump2ndMap_UVMode == 1) uvBump2nd = fd.uv1; \
					if (_Bump2ndMap_UVMode == 2) uvBump2nd = fd.uv2; \
					if (_Bump2ndMap_UVMode == 3) uvBump2nd = fd.uv3; \
					float4 normal2ndTex = LIL_SAMPLE_2D_ST(_Bump2ndMap, sampler_MainTex, uvBump2nd); \
					float bump2ndScale = _Bump2ndScale; \
					bump2ndScale *= LIL_SAMPLE_2D_ST(_Bump2ndScaleMask, sampler_MainTex, fd.uv0).r; \
					pre_normalmap = lilBlendNormal(pre_normalmap, lilUnpackNormalScale(normal2ndTex, bump2ndScale)); \
					}

				#define OVERRIDE_NORMAL_2ND \
					normalmap = pre_normalmap;

			#endif
		#else
			#define COMBINENORMAL 
		#endif

	#if defined(LIL_V2F_FORCE_TANGENT) || defined(LIL_SHOULD_TBN)
		#define CALC_TBN \
			float3 bitangentWS_pre = cross(input.normalWS, input.tangentWS.xyz) * (input.tangentWS.w * LIL_NEGATIVE_SCALE); \
			fd.TBN = float3x3(input.tangentWS.xyz, bitangentWS_pre, input.normalWS); \
			float3 pre_normal = normalize(mul(pre_normalmap, fd.TBN));\
			pre_normal = fd.facing < (_FlipNormal - 1.0) ? -pre_normal : pre_normal;
	#else
		#define CALC_TBN float3 pre_normal = input.normalWS;
	#endif
#else
	#define COMBINENORMAL
	#define CALC_TBN float3 pre_normal = input.normalWS;
#endif
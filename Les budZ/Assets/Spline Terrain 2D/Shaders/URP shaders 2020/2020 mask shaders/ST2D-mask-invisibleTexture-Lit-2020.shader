Shader "SplineTerrain2D/mask-invisibleTexture-Lit-2020"
{
	Properties
	{
		[NoScaleOffset]_MainTex("_MainTex", 2D) = "white" {}
		_Color("_Color", Color) = (1, 1, 1, 1)
		[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{

		Stencil
		{
			Ref 0
			Comp NotEqual
			Pass keep
		}

		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"UniversalMaterialType" = "Lit"
			"Queue" = "Transparent"
		}
		Pass
		{
			Name "Sprite Lit"
			Tags
			{
				"LightMode" = "Universal2D"
			}

		// Render State
		Cull Off
	Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
	ZTest LEqual
	ZWrite Off

		// Debug
		// <None>

		// --------------------------------------------------
		// Pass

		HLSLPROGRAM

		// Pragmas
		#pragma target 2.0
	#pragma exclude_renderers d3d11_9x
	#pragma vertex vert
	#pragma fragment frag

		// DotsInstancingOptions: <None>
		// HybridV1InjectedBuiltinProperties: <None>

		// Keywords
		#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_0
	#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_1
	#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_2
	#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_3
		// GraphKeywords: <None>

		// Defines
		#define _SURFACE_TYPE_TRANSPARENT 1
		#define ATTRIBUTES_NEED_NORMAL
		#define ATTRIBUTES_NEED_TANGENT
		#define ATTRIBUTES_NEED_TEXCOORD0
		#define ATTRIBUTES_NEED_COLOR
		#define VARYINGS_NEED_TEXCOORD0
		#define VARYINGS_NEED_COLOR
		#define VARYINGS_NEED_SCREENPOSITION
		#define FEATURES_GRAPH_VERTEX
		/* WARNING: $splice Could not find named fragment 'PassInstancing' */
		#define SHADERPASS SHADERPASS_SPRITELIT
		/* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

		// Includes
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

		// --------------------------------------------------
		// Structs and Packing

		struct Attributes
	{
		float3 positionOS : POSITION;
		float3 normalOS : NORMAL;
		float4 tangentOS : TANGENT;
		float4 uv0 : TEXCOORD0;
		float4 color : COLOR;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : INSTANCEID_SEMANTIC;
		#endif
	};
	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		float4 texCoord0;
		float4 color;
		float4 screenPosition;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : CUSTOM_INSTANCE_ID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
		#endif
	};
	struct SurfaceDescriptionInputs
	{
		float4 uv0;
	};
	struct VertexDescriptionInputs
	{
		float3 ObjectSpaceNormal;
		float3 ObjectSpaceTangent;
		float3 ObjectSpacePosition;
	};
	struct PackedVaryings
	{
		float4 positionCS : SV_POSITION;
		float4 interp0 : TEXCOORD0;
		float4 interp1 : TEXCOORD1;
		float4 interp2 : TEXCOORD2;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : CUSTOM_INSTANCE_ID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
		#endif
	};

		PackedVaryings PackVaryings(Varyings input)
	{
		PackedVaryings output;
		output.positionCS = input.positionCS;
		output.interp0.xyzw = input.texCoord0;
		output.interp1.xyzw = input.color;
		output.interp2.xyzw = input.screenPosition;
		#if UNITY_ANY_INSTANCING_ENABLED
		output.instanceID = input.instanceID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		output.cullFace = input.cullFace;
		#endif
		return output;
	}
	Varyings UnpackVaryings(PackedVaryings input)
	{
		Varyings output;
		output.positionCS = input.positionCS;
		output.texCoord0 = input.interp0.xyzw;
		output.color = input.interp1.xyzw;
		output.screenPosition = input.interp2.xyzw;
		#if UNITY_ANY_INSTANCING_ENABLED
		output.instanceID = input.instanceID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		output.cullFace = input.cullFace;
		#endif
		return output;
	}

	// --------------------------------------------------
	// Graph

	// Graph Properties
	CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
CBUFFER_END

// Object and Global properties
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
SAMPLER(SamplerState_Linear_Repeat);

// Graph Functions

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
	RGBA = float4(R, G, B, A);
	RGB = float3(R, G, B);
	RG = float2(R, G);
}

void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
{
	Out = A * B;
}

void Unity_Multiply_float(float A, float B, out float Out)
{
	Out = A * B;
}

// Graph Vertex
struct VertexDescription
{
	float3 Position;
	float3 Normal;
	float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
	VertexDescription description = (VertexDescription)0;
	description.Position = IN.ObjectSpacePosition;
	description.Normal = IN.ObjectSpaceNormal;
	description.Tangent = IN.ObjectSpaceTangent;
	return description;
}

// Graph Pixel
struct SurfaceDescription
{
	float3 BaseColor;
	float Alpha;
	float4 SpriteMask;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
	SurfaceDescription surface = (SurfaceDescription)0;
	UnityTexture2D _Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0 = UnityBuildTexture2DStructNoScale(_MainTex);
	float4 _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0.tex, _Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0.samplerstate, IN.uv0.xy);
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_R_4 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.r;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_G_5 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.g;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_B_6 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.b;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_A_7 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.a;
	float4 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGBA_4;
	float3 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5;
	float2 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RG_6;
	Unity_Combine_float(_SampleTexture2D_6a2647baa5594014a551a41af6002692_R_4, _SampleTexture2D_6a2647baa5594014a551a41af6002692_G_5, _SampleTexture2D_6a2647baa5594014a551a41af6002692_B_6, 0, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGBA_4, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RG_6);
	float4 _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0 = _Color;
	float _Split_123cbfa7501c4efd909e3d6470ee4508_R_1 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[0];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_G_2 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[1];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_B_3 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[2];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_A_4 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[3];
	float4 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGBA_4;
	float3 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5;
	float2 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RG_6;
	Unity_Combine_float(_Split_123cbfa7501c4efd909e3d6470ee4508_R_1, _Split_123cbfa7501c4efd909e3d6470ee4508_G_2, _Split_123cbfa7501c4efd909e3d6470ee4508_B_3, 0, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGBA_4, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RG_6);
	float3 _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2;
	Unity_Multiply_float(_Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5, _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2);
	float _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2;
	Unity_Multiply_float(_SampleTexture2D_6a2647baa5594014a551a41af6002692_A_7, _Split_123cbfa7501c4efd909e3d6470ee4508_A_4, _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2);
	surface.BaseColor = _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2;
	surface.Alpha = _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2;
	surface.SpriteMask = IsGammaSpace() ? float4(1, 1, 1, 1) : float4 (SRGBToLinear(float3(1, 1, 1)), 1);
	return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
	VertexDescriptionInputs output;
	ZERO_INITIALIZE(VertexDescriptionInputs, output);

	output.ObjectSpaceNormal = input.normalOS;
	output.ObjectSpaceTangent = input.tangentOS;
	output.ObjectSpacePosition = input.positionOS;

	return output;
}
	SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
	SurfaceDescriptionInputs output;
	ZERO_INITIALIZE(SurfaceDescriptionInputs, output);





	output.uv0 = input.texCoord0;
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

	return output;
}

	// --------------------------------------------------
	// Main

	#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteLitPass.hlsl"

	ENDHLSL
}
Pass
{
	Name "Sprite Normal"
	Tags
	{
		"LightMode" = "NormalsRendering"
	}

		// Render State
		Cull Off
	Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
	ZTest LEqual
	ZWrite Off

		// Debug
		// <None>

		// --------------------------------------------------
		// Pass

		HLSLPROGRAM

		// Pragmas
		#pragma target 2.0
	#pragma exclude_renderers d3d11_9x
	#pragma vertex vert
	#pragma fragment frag

		// DotsInstancingOptions: <None>
		// HybridV1InjectedBuiltinProperties: <None>

		// Keywords
		// PassKeywords: <None>
		// GraphKeywords: <None>

		// Defines
		#define _SURFACE_TYPE_TRANSPARENT 1
		#define ATTRIBUTES_NEED_NORMAL
		#define ATTRIBUTES_NEED_TANGENT
		#define ATTRIBUTES_NEED_TEXCOORD0
		#define VARYINGS_NEED_NORMAL_WS
		#define VARYINGS_NEED_TANGENT_WS
		#define VARYINGS_NEED_TEXCOORD0
		#define FEATURES_GRAPH_VERTEX
		/* WARNING: $splice Could not find named fragment 'PassInstancing' */
		#define SHADERPASS SHADERPASS_SPRITENORMAL
		/* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

		// Includes
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

		// --------------------------------------------------
		// Structs and Packing

		struct Attributes
	{
		float3 positionOS : POSITION;
		float3 normalOS : NORMAL;
		float4 tangentOS : TANGENT;
		float4 uv0 : TEXCOORD0;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : INSTANCEID_SEMANTIC;
		#endif
	};
	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		float3 normalWS;
		float4 tangentWS;
		float4 texCoord0;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : CUSTOM_INSTANCE_ID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
		#endif
	};
	struct SurfaceDescriptionInputs
	{
		float3 TangentSpaceNormal;
		float4 uv0;
	};
	struct VertexDescriptionInputs
	{
		float3 ObjectSpaceNormal;
		float3 ObjectSpaceTangent;
		float3 ObjectSpacePosition;
	};
	struct PackedVaryings
	{
		float4 positionCS : SV_POSITION;
		float3 interp0 : TEXCOORD0;
		float4 interp1 : TEXCOORD1;
		float4 interp2 : TEXCOORD2;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : CUSTOM_INSTANCE_ID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
		#endif
	};

		PackedVaryings PackVaryings(Varyings input)
	{
		PackedVaryings output;
		output.positionCS = input.positionCS;
		output.interp0.xyz = input.normalWS;
		output.interp1.xyzw = input.tangentWS;
		output.interp2.xyzw = input.texCoord0;
		#if UNITY_ANY_INSTANCING_ENABLED
		output.instanceID = input.instanceID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		output.cullFace = input.cullFace;
		#endif
		return output;
	}
	Varyings UnpackVaryings(PackedVaryings input)
	{
		Varyings output;
		output.positionCS = input.positionCS;
		output.normalWS = input.interp0.xyz;
		output.tangentWS = input.interp1.xyzw;
		output.texCoord0 = input.interp2.xyzw;
		#if UNITY_ANY_INSTANCING_ENABLED
		output.instanceID = input.instanceID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		output.cullFace = input.cullFace;
		#endif
		return output;
	}

	// --------------------------------------------------
	// Graph

	// Graph Properties
	CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
CBUFFER_END

// Object and Global properties
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
SAMPLER(SamplerState_Linear_Repeat);

// Graph Functions

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
	RGBA = float4(R, G, B, A);
	RGB = float3(R, G, B);
	RG = float2(R, G);
}

void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
{
	Out = A * B;
}

void Unity_Multiply_float(float A, float B, out float Out)
{
	Out = A * B;
}

// Graph Vertex
struct VertexDescription
{
	float3 Position;
	float3 Normal;
	float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
	VertexDescription description = (VertexDescription)0;
	description.Position = IN.ObjectSpacePosition;
	description.Normal = IN.ObjectSpaceNormal;
	description.Tangent = IN.ObjectSpaceTangent;
	return description;
}

// Graph Pixel
struct SurfaceDescription
{
	float3 BaseColor;
	float Alpha;
	float3 NormalTS;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
	SurfaceDescription surface = (SurfaceDescription)0;
	UnityTexture2D _Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0 = UnityBuildTexture2DStructNoScale(_MainTex);
	float4 _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0.tex, _Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0.samplerstate, IN.uv0.xy);
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_R_4 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.r;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_G_5 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.g;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_B_6 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.b;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_A_7 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.a;
	float4 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGBA_4;
	float3 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5;
	float2 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RG_6;
	Unity_Combine_float(_SampleTexture2D_6a2647baa5594014a551a41af6002692_R_4, _SampleTexture2D_6a2647baa5594014a551a41af6002692_G_5, _SampleTexture2D_6a2647baa5594014a551a41af6002692_B_6, 0, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGBA_4, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RG_6);
	float4 _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0 = _Color;
	float _Split_123cbfa7501c4efd909e3d6470ee4508_R_1 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[0];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_G_2 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[1];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_B_3 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[2];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_A_4 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[3];
	float4 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGBA_4;
	float3 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5;
	float2 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RG_6;
	Unity_Combine_float(_Split_123cbfa7501c4efd909e3d6470ee4508_R_1, _Split_123cbfa7501c4efd909e3d6470ee4508_G_2, _Split_123cbfa7501c4efd909e3d6470ee4508_B_3, 0, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGBA_4, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RG_6);
	float3 _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2;
	Unity_Multiply_float(_Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5, _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2);
	float _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2;
	Unity_Multiply_float(_SampleTexture2D_6a2647baa5594014a551a41af6002692_A_7, _Split_123cbfa7501c4efd909e3d6470ee4508_A_4, _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2);
	surface.BaseColor = _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2;
	surface.Alpha = _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2;
	surface.NormalTS = IN.TangentSpaceNormal;
	return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
	VertexDescriptionInputs output;
	ZERO_INITIALIZE(VertexDescriptionInputs, output);

	output.ObjectSpaceNormal = input.normalOS;
	output.ObjectSpaceTangent = input.tangentOS;
	output.ObjectSpacePosition = input.positionOS;

	return output;
}
	SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
	SurfaceDescriptionInputs output;
	ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



	output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


	output.uv0 = input.texCoord0;
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

	return output;
}

	// --------------------------------------------------
	// Main

	#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteNormalPass.hlsl"

	ENDHLSL
}
Pass
{
	Name "Sprite Forward"
	Tags
	{
		"LightMode" = "UniversalForward"
	}

		// Render State
		Cull Off
	Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
	ZTest LEqual
	ZWrite Off

		// Debug
		// <None>

		// --------------------------------------------------
		// Pass

		HLSLPROGRAM

		// Pragmas
		#pragma target 2.0
	#pragma exclude_renderers d3d11_9x
	#pragma vertex vert
	#pragma fragment frag

		// DotsInstancingOptions: <None>
		// HybridV1InjectedBuiltinProperties: <None>

		// Keywords
		// PassKeywords: <None>
		// GraphKeywords: <None>

		// Defines
		#define _SURFACE_TYPE_TRANSPARENT 1
		#define ATTRIBUTES_NEED_NORMAL
		#define ATTRIBUTES_NEED_TANGENT
		#define ATTRIBUTES_NEED_TEXCOORD0
		#define ATTRIBUTES_NEED_COLOR
		#define VARYINGS_NEED_TEXCOORD0
		#define VARYINGS_NEED_COLOR
		#define FEATURES_GRAPH_VERTEX
		/* WARNING: $splice Could not find named fragment 'PassInstancing' */
		#define SHADERPASS SHADERPASS_SPRITEFORWARD
		/* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */

		// Includes
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

		// --------------------------------------------------
		// Structs and Packing

		struct Attributes
	{
		float3 positionOS : POSITION;
		float3 normalOS : NORMAL;
		float4 tangentOS : TANGENT;
		float4 uv0 : TEXCOORD0;
		float4 color : COLOR;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : INSTANCEID_SEMANTIC;
		#endif
	};
	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		float4 texCoord0;
		float4 color;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : CUSTOM_INSTANCE_ID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
		#endif
	};
	struct SurfaceDescriptionInputs
	{
		float3 TangentSpaceNormal;
		float4 uv0;
	};
	struct VertexDescriptionInputs
	{
		float3 ObjectSpaceNormal;
		float3 ObjectSpaceTangent;
		float3 ObjectSpacePosition;
	};
	struct PackedVaryings
	{
		float4 positionCS : SV_POSITION;
		float4 interp0 : TEXCOORD0;
		float4 interp1 : TEXCOORD1;
		#if UNITY_ANY_INSTANCING_ENABLED
		uint instanceID : CUSTOM_INSTANCE_ID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
		#endif
	};

		PackedVaryings PackVaryings(Varyings input)
	{
		PackedVaryings output;
		output.positionCS = input.positionCS;
		output.interp0.xyzw = input.texCoord0;
		output.interp1.xyzw = input.color;
		#if UNITY_ANY_INSTANCING_ENABLED
		output.instanceID = input.instanceID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		output.cullFace = input.cullFace;
		#endif
		return output;
	}
	Varyings UnpackVaryings(PackedVaryings input)
	{
		Varyings output;
		output.positionCS = input.positionCS;
		output.texCoord0 = input.interp0.xyzw;
		output.color = input.interp1.xyzw;
		#if UNITY_ANY_INSTANCING_ENABLED
		output.instanceID = input.instanceID;
		#endif
		#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
		output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
		#endif
		#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
		output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
		#endif
		#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
		output.cullFace = input.cullFace;
		#endif
		return output;
	}

	// --------------------------------------------------
	// Graph

	// Graph Properties
	CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
CBUFFER_END

// Object and Global properties
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
SAMPLER(SamplerState_Linear_Repeat);

// Graph Functions

void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
	RGBA = float4(R, G, B, A);
	RGB = float3(R, G, B);
	RG = float2(R, G);
}

void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
{
	Out = A * B;
}

void Unity_Multiply_float(float A, float B, out float Out)
{
	Out = A * B;
}

// Graph Vertex
struct VertexDescription
{
	float3 Position;
	float3 Normal;
	float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
	VertexDescription description = (VertexDescription)0;
	description.Position = IN.ObjectSpacePosition;
	description.Normal = IN.ObjectSpaceNormal;
	description.Tangent = IN.ObjectSpaceTangent;
	return description;
}

// Graph Pixel
struct SurfaceDescription
{
	float3 BaseColor;
	float Alpha;
	float3 NormalTS;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
	SurfaceDescription surface = (SurfaceDescription)0;
	UnityTexture2D _Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0 = UnityBuildTexture2DStructNoScale(_MainTex);
	float4 _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0.tex, _Property_5b183496f1d545b884a49a7c8aaf76cb_Out_0.samplerstate, IN.uv0.xy);
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_R_4 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.r;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_G_5 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.g;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_B_6 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.b;
	float _SampleTexture2D_6a2647baa5594014a551a41af6002692_A_7 = _SampleTexture2D_6a2647baa5594014a551a41af6002692_RGBA_0.a;
	float4 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGBA_4;
	float3 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5;
	float2 _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RG_6;
	Unity_Combine_float(_SampleTexture2D_6a2647baa5594014a551a41af6002692_R_4, _SampleTexture2D_6a2647baa5594014a551a41af6002692_G_5, _SampleTexture2D_6a2647baa5594014a551a41af6002692_B_6, 0, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGBA_4, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5, _Combine_dec8cb12f4dd4b9c852f803bf01930dc_RG_6);
	float4 _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0 = _Color;
	float _Split_123cbfa7501c4efd909e3d6470ee4508_R_1 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[0];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_G_2 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[1];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_B_3 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[2];
	float _Split_123cbfa7501c4efd909e3d6470ee4508_A_4 = _Property_9ca5e0a2a58c4e099ec8a7d870fbcc8e_Out_0[3];
	float4 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGBA_4;
	float3 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5;
	float2 _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RG_6;
	Unity_Combine_float(_Split_123cbfa7501c4efd909e3d6470ee4508_R_1, _Split_123cbfa7501c4efd909e3d6470ee4508_G_2, _Split_123cbfa7501c4efd909e3d6470ee4508_B_3, 0, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGBA_4, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RG_6);
	float3 _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2;
	Unity_Multiply_float(_Combine_dec8cb12f4dd4b9c852f803bf01930dc_RGB_5, _Combine_120c52e0042e4eebbb9a8a2cc1c4e7cf_RGB_5, _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2);
	float _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2;
	Unity_Multiply_float(_SampleTexture2D_6a2647baa5594014a551a41af6002692_A_7, _Split_123cbfa7501c4efd909e3d6470ee4508_A_4, _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2);
	surface.BaseColor = _Multiply_0abf62dcf70c4e809aa18d34477bf0ad_Out_2;
	surface.Alpha = _Multiply_0a7c3f957e5a49b39145f7b04938c82c_Out_2;
	surface.NormalTS = IN.TangentSpaceNormal;
	return surface;
}

// --------------------------------------------------
// Build Graph Inputs

VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
	VertexDescriptionInputs output;
	ZERO_INITIALIZE(VertexDescriptionInputs, output);

	output.ObjectSpaceNormal = input.normalOS;
	output.ObjectSpaceTangent = input.tangentOS;
	output.ObjectSpacePosition = input.positionOS;

	return output;
}
	SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
	SurfaceDescriptionInputs output;
	ZERO_INITIALIZE(SurfaceDescriptionInputs, output);



	output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);


	output.uv0 = input.texCoord0;
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

	return output;
}

	// --------------------------------------------------
	// Main

	#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SpriteForwardPass.hlsl"

	ENDHLSL
}
	}
		FallBack "Hidden/Shader Graph/FallbackError"
}
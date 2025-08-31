// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/Sprite/Diffuse-Layers"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
	[PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
	}

		SubShader
	{
		Tags
	{
		"Queue" = "Transparent"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"PreviewType" = "Plane"
		//"CanUseSpriteAtlas" = "True"
	}

		ZTest Off
		Cull Off
		Lighting Off
		ZWrite Off
		//ZWrite On
		//Blend One OneMinusSrcAlpha
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
#pragma surface surf Lambert vertex:vert nofog nolightmap nodynlightmap keepalpha noinstancing
#pragma multi_compile _ PIXELSNAP_ON
#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
#include "UnitySprites.cginc"

		struct Input
	{
		float2 uv_MainTex;
		fixed4 color;
	};

	void vert(inout appdata_full v, out Input o)
	{
		v.vertex.xy *= _Flip.xy;

#if defined(PIXELSNAP_ON)
		v.vertex = UnityPixelSnap(v.vertex);
#endif

		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color * _Color * _RendererColor;
	}

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = SampleSpriteTexture(IN.uv_MainTex) * IN.color;
		o.Albedo = c.rgb * c.a;
		o.Alpha = c.a;
	}
	ENDCG
	}

		Fallback "Transparent/VertexLit"
}



//////////////////////////////
//
//{
//Properties{
//	_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
//	_MainTex("Particle Texture", 2D) = "white" {}
//_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
//}
//
//Category{
//	Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
//	Blend SrcAlpha OneMinusSrcAlpha
//	ColorMask RGB
//	Cull Off Lighting Off ZWrite Off ZTest Off
//
//	SubShader{
//	Pass{
//
//	CGPROGRAM
//#pragma vertex vert
//#pragma fragment frag
//#pragma target 2.0
//#pragma multi_compile_particles
//#pragma multi_compile_fog
//
//#include "UnityCG.cginc"
//
//	sampler2D _MainTex;
//fixed4 _TintColor;
//
//struct appdata_t
//{
//	float4 vertex : POSITION;
//	fixed4 color : COLOR;
//	float2 texcoord : TEXCOORD0;
//	UNITY_VERTEX_INPUT_INSTANCE_ID
//};
//
//struct v2f
//{
//	float4 vertex : SV_POSITION;
//	fixed4 color : COLOR;
//	float2 texcoord : TEXCOORD0;
//	UNITY_FOG_COORDS(1)
//#ifdef SOFTPARTICLES_ON
//		float4 projPos : TEXCOORD2;
//#endif
//	UNITY_VERTEX_OUTPUT_STEREO
//};
//
//float4 _MainTex_ST;
//
//v2f vert(appdata_t v)
//{
//	v2f o;
//	UNITY_SETUP_INSTANCE_ID(v);
//	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
//	o.vertex = UnityObjectToClipPos(v.vertex);
//#ifdef SOFTPARTICLES_ON
//	o.projPos = ComputeScreenPos(o.vertex);
//	COMPUTE_EYEDEPTH(o.projPos.z);
//#endif
//	o.color = v.color * _TintColor;
//	o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
//	UNITY_TRANSFER_FOG(o,o.vertex);
//	return o;
//}
//
//UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
//float _InvFade;
//
//fixed4 frag(v2f i) : SV_Target
//{
//#ifdef SOFTPARTICLES_ON
//	float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
//float partZ = i.projPos.z;
//float fade = saturate(_InvFade * (sceneZ - partZ));
//i.color.a *= fade;
//#endif
//
//fixed4 col = 2.0f * i.color * tex2D(_MainTex, i.texcoord);
//UNITY_APPLY_FOG(i.fogCoord, col);
//return col;
//}
//ENDCG
//}
//}
//}
//}



Shader "Custom/WaterMask" {		//	WRITER  -before BG (reader )


// ORDER		:	write <= read
// Render queue :	write < read

//show values to edit in inspector
	Properties{
		//[IntRange] _StencilWriteMask("Stencil Mask ID", Range(0,255)) = 0
		[IntRange] _StencilRef("Stencil Reference Value", Range(0,255)) = 0
	}

		SubShader{
		//the material is completely non-transparent and is rendered at the same time as the other opaque geometry
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry-1"}

		//	WRITER
		Stencil{
			Ref[_StencilRef]
			//WriteMask[_StencilWriteMask]
			Comp Always
			Pass Replace
		}

		Pass{
		//don't draw color or depth
		Blend Zero One
		ZWrite Off

		CGPROGRAM
		#include "UnityCG.cginc"

		#pragma vertex vert
		#pragma fragment frag

		struct appdata
{
float4 vertex : POSITION;
};

struct v2f
{
float4 position : SV_POSITION;
};

v2f vert(appdata v)
{
v2f o;
//calculate the position in clip space to render the object
o.position = UnityObjectToClipPos(v.vertex);
return o;
}

fixed4 frag(v2f i) : SV_TARGET{
	return 0;
}

ENDCG
}
	}
}


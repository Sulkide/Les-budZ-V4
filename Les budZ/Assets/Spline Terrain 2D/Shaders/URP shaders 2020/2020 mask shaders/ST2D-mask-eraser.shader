Shader "SplineTerrain2D/mask-eraser"
{
	SubShader
	{
		Zwrite off
		ColorMask 0
		Cull off

		Stencil
		{
			Ref 0
			Comp always
			Pass replace
		}

		Pass
		{
		}
	}
		FallBack "Diffuse"
}

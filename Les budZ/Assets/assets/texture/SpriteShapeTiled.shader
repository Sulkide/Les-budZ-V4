Shader "Custom/SpriteShapeTiled"
{
    Properties {
        _MainTex    ("Texture", 2D) = "white" {}
        _TileSize   ("Tile Size (X=largeur,Y=hauteur)", Vector) = (1,1,0,0)
    }
    SubShader {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        LOD 100
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };
            struct v2f {
                float4 vertex  : SV_POSITION;
                float2 worldXY : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4   _TileSize;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // On garde la position locale XY
                o.worldXY = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                // UV = position / taille de tuile
                float2 uv = i.worldXY / _TileSize.xy;
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}

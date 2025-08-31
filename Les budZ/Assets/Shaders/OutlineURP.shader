Shader "Custom/OutlineURP"
{
    Properties
    {
        // Surface (reçoit lumière/ombres)
        [MainColor] _Color("Couleur de base", Color) = (1,1,1,1)
        [MainTexture] _MainTex("Texture de base", 2D) = "white" {}

        // Silhouette outline (inverted hull)
        _OutlineColor("Couleur du contour", Color) = (0,0,0,1)
        _OutlineWidth("Epaisseur contour (monde)", Float) = 0.06

        // Crease lines (lignes d'angle, screen-space)
        _CreaseColor("Couleur des arêtes", Color) = (0,0,0,1)
        _CreaseThreshold("Seuil normales (0-1)", Range(0.02, 0.6)) = 0.22
        _CreaseWidthPx("Echantillonnage (px)", Range(0.5, 3.0)) = 1.0
        _CreaseOpacity("Opacité arêtes", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" }
        LOD 200

        // ===============================
        // PASS 1 : SURFACE LIT + OMBRES
        // ===============================
        Pass
        {
            Name "SurfaceLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP core + lighting
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _OutlineColor;
                float  _OutlineWidth;

                half4  _CreaseColor;
                float  _CreaseThreshold;
                float  _CreaseWidthPx;
                float  _CreaseOpacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = nrmInputs.normalWS;

                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

                // Coordonnées d'ombre pour la lumière principale
                OUT.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Albedo
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                // Normal / lumière
                half3 N = normalize(IN.normalWS);

                // Lumière principale + ombres
                Light mainLight = GetMainLight(IN.shadowCoord);
                half NdotL = saturate(dot(N, mainLight.direction));
                half3 litCol = albedo.rgb * (NdotL * mainLight.color.rgb * mainLight.shadowAttenuation);

                // Lumières additionnelles
                uint addCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < addCount; i++)
                {
                    Light l = GetAdditionalLight(i, IN.positionWS);
                    half ndl = saturate(dot(N, l.direction));
                    litCol += albedo.rgb * (ndl * l.color.rgb * l.distanceAttenuation * l.shadowAttenuation);
                }

                return half4(litCol, albedo.a);
            }
            ENDHLSL
        }

        // =========================================
        // PASS 2 : SILHOUETTE (INVERTED HULL OUTLINE)
        // =========================================
        Pass
        {
            Name "OutlineSilhouette"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _OutlineColor;
                float  _OutlineWidth;

                half4  _CreaseColor;
                float  _CreaseThreshold;
                float  _CreaseWidthPx;
                float  _CreaseOpacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;
                // Extrusion monde le long de la normale objet (approx ok si échelle uniforme)
                float3 posOS = IN.positionOS.xyz + IN.normalOS * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(posOS);
                return OUT;
            }

            half4 fragOutline(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }


        // =========================================
        // PASS 3 : CREASE LINES (écran, normales + profondeur)
        // =========================================
        Pass
        {
            Name "CreaseLines"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vertCrease
            #pragma fragment fragCrease

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Accès aux textures profondeur & normales écran
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _OutlineColor;
                float  _OutlineWidth;

                half4  _CreaseColor;
                float  _CreaseThreshold;
                float  _CreaseWidthPx;
                float  _CreaseOpacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
            };

            Varyings vertCrease(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            float3 TryGetSceneNormal(float2 uv, out bool hasNormals)
            {
                // Essaie d'échantillonner les normales écran ; si non dispo, renvoie (0,0,0)
                float3 n = SampleSceneNormals(uv);
                hasNormals = any(abs(n) > 1e-5);
                // Normalise si valide
                if (hasNormals) n = normalize(n);
                return n;
            }

            half4 fragCrease(Varyings IN) : SV_Target
            {
                float2 uv = IN.screenPos.xy / IN.screenPos.w;

                // --- Profondeur scène (raw) -> linéarisée [0..1]
                float rawScene = SampleSceneDepth(uv);
                float scene01  = Linear01Depth(rawScene, _ZBufferParams);

                // --- Profondeur du fragment courant : clip -> raw -> linéarisée [0..1]
                float clipZ    = IN.positionHCS.z / IN.positionHCS.w;
                float rawMy    = UNITY_Z_0_FAR_FROM_CLIPSPACE(clipZ);
                float my01     = Linear01Depth(rawMy, _ZBufferParams);

                // Si ce pixel ne correspond pas à la surface visible à l'écran, on ne dessine pas
                // (tolérance ajustable selon tes meshes/plateformes)
                if (abs(scene01 - my01) > 0.002) discard;

                // Taille d'un texel écran * facteur utilisateur (largeur en px)
                float2 texel = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * _CreaseWidthPx;

                // ---------- Détection par NORMALES (si dispo) ----------
                bool hasNormals = false;
                float3 nC = TryGetSceneNormal(uv, hasNormals);
                float edgeN = 0.0;

                if (hasNormals)
                {
                    float3 nR = TryGetSceneNormal(uv + float2(texel.x, 0), hasNormals);
                    float3 nL = TryGetSceneNormal(uv - float2(texel.x, 0), hasNormals);
                    float3 nU = TryGetSceneNormal(uv + float2(0, texel.y), hasNormals);
                    float3 nD = TryGetSceneNormal(uv - float2(0, texel.y), hasNormals);

                    float dH = length(nR - nL);
                    float dV = length(nU - nD);
                    edgeN = max(dH, dV);
                }

                // ---------- Détection par PROFONDEUR (toujours dispo) ----------
                float zR01 = Linear01Depth(SampleSceneDepth(uv + float2(texel.x, 0)), _ZBufferParams);
                float zL01 = Linear01Depth(SampleSceneDepth(uv - float2(texel.x, 0)), _ZBufferParams);
                float zU01 = Linear01Depth(SampleSceneDepth(uv + float2(0, texel.y)), _ZBufferParams);
                float zD01 = Linear01Depth(SampleSceneDepth(uv - float2(0, texel.y)), _ZBufferParams);

                float dZH = abs(zR01 - zL01);
                float dZV = abs(zU01 - zD01);
                float edgeZ = max(dZH, dZV);

                // Combine : les normales (si présentes) + un soupçon de profondeur
                float edgeStrength = max(edgeN, edgeZ * 2.0);

                // Seuillage doux
                float a = saturate((edgeStrength - _CreaseThreshold) / max(_CreaseThreshold * 0.5, 1e-4));
                if (a <= 0.0) discard;

                return half4(_CreaseColor.rgb, _CreaseOpacity * a);
            }
            ENDHLSL
        }

        
    }
}

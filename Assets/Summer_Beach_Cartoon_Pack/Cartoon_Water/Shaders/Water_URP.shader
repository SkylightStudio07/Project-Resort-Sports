// Converted from Built-in RP (Amplify Shader Editor) to URP
// Matches Built-in "Water" shader visuals: Standard PBR lighting (Metallic=0, Smoothness=0),
// fullforwardshadows, fresnel emission using non-saturated NdotV.
Shader "URP/Water"
{
    Properties
    {
        _WaterColor("Water Color", Color) = (0,0,0,0)
        _WaterTexture("Water Texture", 2D) = "white" {}
        _WaterTiling("Water Tiling", Float) = 1
        _WaterSpeed("Water Speed", Vector) = (0.02,0.01,0,0)
        _DistortionTexture("Distortion Texture", 2D) = "white" {}
        _DistortionTiling("Distortion Tiling", Float) = 1
        _DistortionSpeed("Distortion Speed", Vector) = (-0.03,-0.01,0,0)
        _DistortionIntensity("Distortion Intensity", Range(0 , 1)) = 0.5
        _FresnelColor("Fresnel Color", Color) = (1,1,1,0)
        _FresnelIntensity("Fresnel Intensity", Float) = 0.5
        _FresnelPower("Fresnel Power", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }
        LOD 200
        Cull Back

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _WaterTexture_ST;
            float4 _DistortionTexture_ST;
            float4 _WaterColor;
            float4 _FresnelColor;
            float2 _WaterSpeed;
            float2 _DistortionSpeed;
            float _WaterTiling;
            float _DistortionTiling;
            float _DistortionIntensity;
            float _FresnelIntensity;
            float _FresnelPower;
        CBUFFER_END

        TEXTURE2D(_WaterTexture);       SAMPLER(sampler_WaterTexture);
        TEXTURE2D(_DistortionTexture);  SAMPLER(sampler_DistortionTexture);
        ENDHLSL

        // -------------------- Forward Lit Pass --------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP keywords (mirrors built-in 'fullforwardshadows' coverage)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float3 positionWS : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float4 shadowCoord: TEXCOORD4;
                float  fogCoord   : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = nrmInputs.normalWS;
                OUT.uv         = IN.uv;
                OUT.fogCoord   = ComputeFogFactor(posInputs.positionCS.z);
                OUT.shadowCoord = GetShadowCoord(posInputs);

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // ---- Albedo (matches built-in surf) -------------------------------------
                float2 distUV   = _DistortionTiling * IN.uv + _Time.y * _DistortionSpeed;
                float2 distSamp = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, distUV).rg;

                float2 waterUV  = _WaterTiling * IN.uv + _Time.y * _WaterSpeed;
                float2 finalUV  = lerp(waterUV, distSamp + waterUV, _DistortionIntensity);

                half4 waterTex  = SAMPLE_TEXTURE2D(_WaterTexture, sampler_WaterTexture, finalUV);
                half3 albedo    = (_WaterColor + waterTex).rgb;

                // ---- Fresnel emission (matches built-in: NdotV is NOT saturated) --------
                float3 N        = normalize(IN.normalWS);
                float3 V        = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  NdotV    = dot(N, V);                      // raw (matches ASE FresnelNode)
                float  fres     = pow(1.0 - NdotV, _FresnelPower);
                half3  emission = _FresnelColor.rgb * (_FresnelIntensity * fres);

                // ---- Build InputData / SurfaceData matching built-in Standard surf ------
                // Built-in surf only sets Albedo and Emission. Metallic=0, Smoothness=0,
                // Normal=vertex normal, Occlusion=1 (Unity's default for unset Occlusion).
                InputData inputData         = (InputData)0;
                inputData.positionWS        = IN.positionWS;
                inputData.normalWS          = N;
                inputData.viewDirectionWS   = V;
                inputData.shadowCoord       = IN.shadowCoord;
                inputData.fogCoord          = IN.fogCoord;
                inputData.vertexLighting    = half3(0, 0, 0);
                inputData.bakedGI           = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask        = SAMPLE_SHADOWMASK(IN.lightmapUV);

                SurfaceData surfaceData     = (SurfaceData)0;
                surfaceData.albedo          = albedo;
                surfaceData.metallic        = 0.0;
                surfaceData.specular        = half3(0, 0, 0);
                surfaceData.smoothness      = 0.0;
                surfaceData.normalTS        = half3(0, 0, 1);
                surfaceData.emission        = emission;
                surfaceData.occlusion       = 1.0;
                surfaceData.alpha           = 1.0;
                surfaceData.clearCoatMask        = 0.0;
                surfaceData.clearCoatSmoothness  = 0.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb   = MixFog(color.rgb, IN.fogCoord);
                color.a     = 1.0;
                return color;
            }
            ENDHLSL
        }

        // -------------------- Shadow Caster --------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = GetShadowPositionHClip(IN);
                return OUT;
            }

            half4 ShadowPassFragment(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // -------------------- DepthOnly --------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // -------------------- DepthNormals (used by SSAO etc.) --------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = p.positionCS;
                OUT.normalWS   = n.normalWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(normalize(IN.normalWS) * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}

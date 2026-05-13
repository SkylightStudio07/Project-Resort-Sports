// Converted from Built-in RP (Amplify Shader Editor) to URP
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

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
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
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Distortion UV pan
                float2 distUV   = _DistortionTiling * IN.uv + _Time.y * _DistortionSpeed;
                float2 distSamp = SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, distUV).rg;

                // Water UV pan
                float2 waterUV  = _WaterTiling * IN.uv + _Time.y * _WaterSpeed;

                // Lerp between clean and distorted UVs by intensity
                float2 finalUV  = lerp(waterUV, distSamp + waterUV, _DistortionIntensity);

                half4 waterTex  = SAMPLE_TEXTURE2D(_WaterTexture, sampler_WaterTexture, finalUV);
                half3 albedo    = (_WaterColor + waterTex).rgb;

                // Fresnel
                float3 N        = normalize(IN.normalWS);
                float3 V        = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  NdotV    = saturate(dot(N, V));
                float  fres     = pow(1.0 - NdotV, _FresnelPower);
                half3  emission = (_FresnelColor.rgb * (_FresnelIntensity * fres));

                // Simple URP lighting (Lambert + ambient) to mimic Standard surf output
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 lighting  = mainLight.color * (mainLight.shadowAttenuation * mainLight.distanceAttenuation) * saturate(dot(N, mainLight.direction));
                lighting       += SampleSH(N);

                half3 color = albedo * lighting + emission;
                color = MixFog(color, IN.fogCoord);
                return half4(color, 1.0);
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
    }
    FallBack "Universal Render Pipeline/Lit"
}

Shader "Universal Render Pipeline/Foam_Shader"
{
    Properties
    {
        _FoamTint("Foam Tint", Color) = (0.4745891, 0.5197171, 0.9779412, 0)
        _Cutoff("Cutoff", Range(0, 1)) = 0
        _BorderWidth("Border Width", Range(0, 5)) = 0
        _NoiseA("Noise A", 2D) = "white" {}
        _TileNoiseA("Tile Noise A", Vector) = (2, 1.5, 0, 0)
        _NoiseBSpeed("Noise B Speed", Float) = 0.023
        _NoiseAMaskClip("Noise A Mask Clip", Float) = 0
        _NoiseB("Noise B", 2D) = "white" {}
        _TileNoiseB("Tile Noise B", Vector) = (1, 1, 0, 0)
        _NoiseASpeed("Noise A Speed", Float) = 0.023
        _NoiseBMaskClip("Noise B Mask Clip", Float) = 1
        [HideInInspector]_TextureSample0("Texture Sample 0", 2D) = "white" {}
        [HideInInspector] _texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "AlphaTest+0"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Back
        Blend One One
        ZWrite Off

        // ---------------------------------------------------------------
        // Forward Pass (URP)
        // ---------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_NoiseA);          SAMPLER(sampler_NoiseA);
            TEXTURE2D(_NoiseB);          SAMPLER(sampler_NoiseB);
            TEXTURE2D(_TextureSample0);  SAMPLER(sampler_TextureSample0);

            CBUFFER_START(UnityPerMaterial)
                float4 _FoamTint;
                float  _Cutoff;
                float  _BorderWidth;
                float4 _TextureSample0_ST;
                float  _NoiseASpeed;
                float  _NoiseBSpeed;
            CBUFFER_END

            // Per-instance properties (replaces UNITY_INSTANCING_BUFFER in built-in)
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float2, _TileNoiseA)
                UNITY_DEFINE_INSTANCED_PROP(float2, _TileNoiseB)
                UNITY_DEFINE_INSTANCED_PROP(float,  _NoiseAMaskClip)
                UNITY_DEFINE_INSTANCED_PROP(float,  _NoiseBMaskClip)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv = IN.uv;

                // Border falloff: pow(clamp(uv.y), _BorderWidth)
                float clampedY = saturate(uv.y);
                float borderTerm = pow(clampedY, _BorderWidth);

                // Per-instance values
                float2 tileA      = UNITY_ACCESS_INSTANCED_PROP(Props, _TileNoiseA);
                float2 tileB      = UNITY_ACCESS_INSTANCED_PROP(Props, _TileNoiseB);
                float  maskClipA  = UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseAMaskClip);
                float  maskClipB  = UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseBMaskClip);

                // Noise A scrolling UVs: tiled UV + time * (0, _NoiseASpeed)
                float2 uvA      = uv * tileA;
                float2 pannerA  = uvA + _Time.y * float2(0.0, _NoiseASpeed);

                // Noise B scrolling UVs: tiled UV + time * (0, _NoiseBSpeed)
                float2 uvB      = uv * tileB;
                float2 pannerB  = uvB + _Time.y * float2(0.0, _NoiseBSpeed);

                // Sample everything
                float4 noiseA = SAMPLE_TEXTURE2D(_NoiseA, sampler_NoiseA, pannerA);
                float4 noiseB = SAMPLE_TEXTURE2D(_NoiseB, sampler_NoiseB, pannerB);

                float2 uvTex0 = uv * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
                float4 tex0   = SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, uvTex0);

                // Combine: (noiseB * maskClipB) * (noiseA * maskClipA)
                float4 noiseCombined = (noiseB * maskClipB) * (noiseA * maskClipA);
                float4 masked        = tex0 * noiseCombined;

                // Final clip value (uses .r as in the original)
                float clipValue = (borderTerm + masked.r) - _Cutoff;
                clip(clipValue);

                // Emission-only output (additive blend), matches original behavior
                half4 col = half4(_FoamTint.rgb, 1.0);
                return col;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        // Shadow Caster Pass
        // ---------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_NoiseA);          SAMPLER(sampler_NoiseA);
            TEXTURE2D(_NoiseB);          SAMPLER(sampler_NoiseB);
            TEXTURE2D(_TextureSample0);  SAMPLER(sampler_TextureSample0);

            CBUFFER_START(UnityPerMaterial)
                float4 _FoamTint;
                float  _Cutoff;
                float  _BorderWidth;
                float4 _TextureSample0_ST;
                float  _NoiseASpeed;
                float  _NoiseBSpeed;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float2, _TileNoiseA)
                UNITY_DEFINE_INSTANCED_PROP(float2, _TileNoiseB)
                UNITY_DEFINE_INSTANCED_PROP(float,  _NoiseAMaskClip)
                UNITY_DEFINE_INSTANCED_PROP(float,  _NoiseBMaskClip)
            UNITY_INSTANCING_BUFFER_END(Props)

            float3 _LightDirection;

            float4 GetShadowPositionHClip(ShadowAttributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            ShadowVaryings shadowVert(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS = GetShadowPositionHClip(IN);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 shadowFrag(ShadowVaryings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv = IN.uv;

                float clampedY  = saturate(uv.y);
                float borderTerm = pow(clampedY, _BorderWidth);

                float2 tileA      = UNITY_ACCESS_INSTANCED_PROP(Props, _TileNoiseA);
                float2 tileB      = UNITY_ACCESS_INSTANCED_PROP(Props, _TileNoiseB);
                float  maskClipA  = UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseAMaskClip);
                float  maskClipB  = UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseBMaskClip);

                float2 pannerA = uv * tileA + _Time.y * float2(0.0, _NoiseASpeed);
                float2 pannerB = uv * tileB + _Time.y * float2(0.0, _NoiseBSpeed);

                float4 noiseA = SAMPLE_TEXTURE2D(_NoiseA, sampler_NoiseA, pannerA);
                float4 noiseB = SAMPLE_TEXTURE2D(_NoiseB, sampler_NoiseB, pannerB);

                float2 uvTex0 = uv * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
                float4 tex0   = SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, uvTex0);

                float4 noiseCombined = (noiseB * maskClipB) * (noiseA * maskClipA);
                float4 masked        = tex0 * noiseCombined;

                clip((borderTerm + masked.r) - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        // DepthOnly Pass (used by URP for depth prepass / SSAO / etc.)
        // ---------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_NoiseA);          SAMPLER(sampler_NoiseA);
            TEXTURE2D(_NoiseB);          SAMPLER(sampler_NoiseB);
            TEXTURE2D(_TextureSample0);  SAMPLER(sampler_TextureSample0);

            CBUFFER_START(UnityPerMaterial)
                float4 _FoamTint;
                float  _Cutoff;
                float  _BorderWidth;
                float4 _TextureSample0_ST;
                float  _NoiseASpeed;
                float  _NoiseBSpeed;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float2, _TileNoiseA)
                UNITY_DEFINE_INSTANCED_PROP(float2, _TileNoiseB)
                UNITY_DEFINE_INSTANCED_PROP(float,  _NoiseAMaskClip)
                UNITY_DEFINE_INSTANCED_PROP(float,  _NoiseBMaskClip)
            UNITY_INSTANCING_BUFFER_END(Props)

            DepthVaryings depthVert(DepthAttributes IN)
            {
                DepthVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 depthFrag(DepthVaryings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv = IN.uv;
                float clampedY   = saturate(uv.y);
                float borderTerm = pow(clampedY, _BorderWidth);

                float2 tileA     = UNITY_ACCESS_INSTANCED_PROP(Props, _TileNoiseA);
                float2 tileB     = UNITY_ACCESS_INSTANCED_PROP(Props, _TileNoiseB);
                float  maskClipA = UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseAMaskClip);
                float  maskClipB = UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseBMaskClip);

                float2 pannerA = uv * tileA + _Time.y * float2(0.0, _NoiseASpeed);
                float2 pannerB = uv * tileB + _Time.y * float2(0.0, _NoiseBSpeed);

                float4 noiseA = SAMPLE_TEXTURE2D(_NoiseA, sampler_NoiseA, pannerA);
                float4 noiseB = SAMPLE_TEXTURE2D(_NoiseB, sampler_NoiseB, pannerB);

                float2 uvTex0 = uv * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
                float4 tex0   = SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, uvTex0);

                float4 noiseCombined = (noiseB * maskClipB) * (noiseA * maskClipA);
                float4 masked        = tex0 * noiseCombined;

                clip((borderTerm + masked.r) - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}

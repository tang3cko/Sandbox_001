Shader "Prism/DitherTransparency"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _DitherAlpha ("Dither Alpha", Range(0, 1)) = 1
        _HoleAlpha ("Hole Alpha", Range(0, 1)) = 0.25
        _HoleRadius ("Hole Radius (viewport)", Range(0, 1)) = 0
        _HoleSoftness ("Hole Softness", Range(0, 0.5)) = 0.1
        _HoleCenter ("Hole Center (viewport)", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _DitherAlpha;
            half _HoleAlpha;
            half _HoleRadius;
            half _HoleSoftness;
            float4 _HoleCenter;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // Ordered 4x4 Bayer matrix, normalized to thresholds in (0, 1).
        // Screen-space pixel position selects a threshold; pixels whose
        // threshold exceeds _DitherAlpha are discarded, so coverage scales
        // with alpha while depth/sorting stay opaque-correct.
        static const half BAYER_4X4[16] =
        {
             0.5 / 16,  8.5 / 16,  2.5 / 16, 10.5 / 16,
            12.5 / 16,  4.5 / 16, 14.5 / 16,  6.5 / 16,
             3.5 / 16, 11.5 / 16,  1.5 / 16,  9.5 / 16,
            15.5 / 16,  7.5 / 16, 13.5 / 16,  5.5 / 16
        };

        void ApplyDither(float4 positionCS, half alpha)
        {
            uint2 pixel = uint2(positionCS.xy);
            uint index = (pixel.y % 4) * 4 + (pixel.x % 4);
            clip(alpha - BAYER_4X4[index]);
        }

        // Camera-view alpha: whole-object fade (_DitherAlpha) combined with an
        // optional screen-space circular hole around _HoleCenter (viewport UV).
        // _HoleRadius <= 0 disables the hole. View-dependent, so the shadow
        // caster pass must NOT use this.
        half ComputeCameraDitherAlpha(float4 positionCS)
        {
            half alpha = _DitherAlpha;
            if (_HoleRadius > 0)
            {
                float2 uv = GetNormalizedScreenSpaceUV(positionCS);
                float2 offset = uv - _HoleCenter.xy;
                offset.x *= _ScreenParams.x / _ScreenParams.y;
                half edge = smoothstep(_HoleRadius - _HoleSoftness, _HoleRadius, length(offset));
                alpha = min(alpha, lerp(_HoleAlpha, 1.0, edge));
            }
            return alpha;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                ApplyDither(input.positionCS, ComputeCameraDitherAlpha(input.positionCS));

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 diffuse = mainLight.color * mainLight.shadowAttenuation
                    * saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);

                return half4(baseColor.rgb * (diffuse + ambient), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Dither the shadow too so a faded occluder darkens less.
                ApplyDither(input.positionCS, _DitherAlpha);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                ApplyDither(input.positionCS, ComputeCameraDitherAlpha(input.positionCS));
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
}

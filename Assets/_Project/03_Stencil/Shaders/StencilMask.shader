Shader "Prism/Stencil/Mask"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry-1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StencilMask"
            Tags { "LightMode" = "UniversalForward" }

            // Write to stencil bit 1 everywhere this mesh covers.
            // Mask 2 avoids URP deferred bits 4-7, stencil LOD bits 2-3,
            // and the XR motion-vector bit 0 path.
            Stencil
            {
                Ref 2
                WriteMask 2
                Comp Always
                Pass Replace
            }

            ColorMask 0    // Write nothing to the color buffer - fully invisible
            ZWrite Off     // Write nothing to depth - does not occlude other geometry
            Cull Off       // Both faces write stencil (useful for cone shapes)

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            // Fragment output is discarded by ColorMask 0.
            // This function exists only to satisfy the shader compiler.
            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}

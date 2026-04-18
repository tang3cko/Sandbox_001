Shader "Prism/Rain"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.3, 0.35, 0.4, 1)
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.02
        _LitAlpha ("Lit Alpha", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Rain"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // =================================================================
            // Data Structures (must match Rain.compute)
            // =================================================================

            struct RainDrop
            {
                float3 position;
                float size;
                float3 velocity;
                float alpha;
                float sparklePhase;
                float sparkleSpeed;
                float litContribution;
                float sparkleIntensity;
                float3 litColor;
                float padding;
            };

            // =================================================================
            // Buffers
            // =================================================================

            StructuredBuffer<RainDrop> _RainBuffer;

            // =================================================================
            // Material Properties
            // =================================================================

            float4 _BaseColor;
            float _BaseAlpha;
            float _LitAlpha;

            // GPU Sync overrides (from RainRenderPass)
            float _RainBaseAlpha;
            float _RainLitAlpha;

            // =================================================================
            // Quad Geometry
            // =================================================================

            // Billboard quad vertices (4:1 aspect ratio for rain streaks)
            static const float2 QuadPositions[6] =
            {
                float2(-0.5, -2.0), float2(0.5, -2.0), float2(-0.5, 2.0),
                float2(0.5, -2.0), float2(0.5, 2.0), float2(-0.5, 2.0)
            };

            static const float2 QuadTexCoords[6] =
            {
                float2(0, 0), float2(1, 0), float2(0, 1),
                float2(1, 0), float2(1, 1), float2(0, 1)
            };

            // =================================================================
            // Vertex Output
            // =================================================================

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float alpha : TEXCOORD1;
                float rimLight : TEXCOORD2;
                float sparkle : TEXCOORD3;
                float3 color : TEXCOORD4;
            };

            // =================================================================
            // Helper Functions
            // =================================================================

            float GetBaseAlpha()
            {
                return _RainBaseAlpha > 0 ? _RainBaseAlpha : _BaseAlpha;
            }

            float GetLitAlpha()
            {
                return _RainLitAlpha > 0 ? _RainLitAlpha : _LitAlpha;
            }

            // =================================================================
            // Vertex Shader
            // Lighting is precomputed in Rain.compute — no light loop here.
            // =================================================================

            void setup() {}

            VertexOutput vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                RainDrop drop = _RainBuffer[instanceID];

                // Billboard: face camera but keep vertical
                float3 cameraRight = UNITY_MATRIX_V[0].xyz;
                float3 cameraUp = float3(0, 1, 0);

                float2 quadPos = QuadPositions[vertexID] * drop.size;
                float3 worldPos = drop.position + cameraRight * quadPos.x + cameraUp * quadPos.y;

                VertexOutput o;
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = QuadTexCoords[vertexID];
                o.alpha = drop.alpha;
                o.rimLight = drop.litContribution;
                o.sparkle = drop.sparkleIntensity;
                o.color = drop.litColor;

                return o;
            }

            // =================================================================
            // Fragment Shader
            // =================================================================

            float4 frag(VertexOutput i) : SV_Target
            {
                // Rain drop shape (elongated with soft edges)
                float2 uv = i.uv * 2.0 - 1.0;
                float dist = length(float2(uv.x, uv.y * 0.25));
                float shape = 1.0 - smoothstep(0.3, 1.0, dist);

                // Base color
                float3 baseColor = _BaseColor.rgb;

                // Mix with lit color
                float3 color = lerp(baseColor, i.color, i.rimLight);

                // Add sparkle
                color += i.color * i.sparkle;

                // Alpha: dim when dark, bright when lit
                float alpha = shape * i.alpha * lerp(GetBaseAlpha(), GetLitAlpha(), i.rimLight + i.sparkle * 0.5);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}

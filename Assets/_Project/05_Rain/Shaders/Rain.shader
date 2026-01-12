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
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // =================================================================
            // Data Structures (must match C# structs)
            // =================================================================

            struct RainDrop
            {
                float3 position;
                float size;
                float3 velocity;
                float alpha;
                float lifetime;
                float sparklePhase;
                float sparkleSpeed;
                float padding;
            };

            // RainLightData: 64 bytes (must match RainLightData.cs)
            struct RainLightData
            {
                float3 position;
                float range;
                float3 direction;
                float spotAngle;
                float3 color;
                float intensity;
                float innerSpotAngle;
                int lightType;      // 0 = Point, 1 = Spot
                int lightIndex;     // URP additional light index for shadow lookup
                float padding;
            };

            // =================================================================
            // Buffers
            // =================================================================

            StructuredBuffer<RainDrop> _RainBuffer;
            StructuredBuffer<RainLightData> _LightBuffer;
            int _LightCount;

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

            // Point light attenuation (cubic falloff)
            float CalculatePointLightAttenuation(float3 pos, RainLightData light)
            {
                float3 toLight = light.position - pos;
                float dist = length(toLight);
                float distAtten = saturate(1.0 - dist / light.range);
                distAtten *= distAtten * distAtten;
                return distAtten;
            }

            // Spot light attenuation (distance + cone)
            float CalculateSpotLightAttenuation(float3 pos, RainLightData light)
            {
                float3 toLight = light.position - pos;
                float dist = length(toLight);

                // Distance attenuation
                float distAtten = saturate(1.0 - dist / light.range);
                distAtten *= distAtten * distAtten;

                // Cone attenuation
                float3 fromLightDir = normalize(pos - light.position);
                float3 lightForward = normalize(light.direction);
                float cosAngle = dot(fromLightDir, lightForward);
                float outerCos = cos(radians(light.spotAngle * 0.5));
                float innerCos = cos(radians(light.innerSpotAngle * 0.5));
                float coneAtten = smoothstep(outerCos, innerCos, cosAngle);

                return distAtten * coneAtten;
            }

            // Shadow sampling for additional lights
            half GetAdditionalLightShadowAttenuation(int lightIndex, float3 positionWS, half3 lightDirection)
            {
                #if defined(ADDITIONAL_LIGHT_CALCULATE_SHADOWS)
                    return AdditionalLightRealtimeShadow(lightIndex, positionWS, lightDirection);
                #else
                    return half(1.0);
                #endif
            }

            // Calculate contribution from a single light
            void CalculateLightContribution(
                float3 dropPos,
                float3 viewDir,
                RainLightData light,
                inout float totalContribution,
                inout float3 totalColor)
            {
                float3 toLightDir = normalize(light.position - dropPos);
                float atten;

                if (light.lightType == 1)
                {
                    // Spot light
                    atten = CalculateSpotLightAttenuation(dropPos, light);
                }
                else
                {
                    // Point light
                    atten = CalculatePointLightAttenuation(dropPos, light);
                }

                // Shadow attenuation
                half shadowAtten = GetAdditionalLightShadowAttenuation(light.lightIndex, dropPos, toLightDir);

                // Height factor (rain above light is less visible)
                float heightDiff = dropPos.y - light.position.y;
                float heightFactor = saturate(1.0 - heightDiff * 0.5);
                heightFactor *= heightFactor;

                float influence = atten * heightFactor * shadowAtten;

                // Rim lighting effect
                float rim = 1.0 - saturate(dot(viewDir, -toLightDir));
                rim = pow(rim, 2.0);

                float contribution = rim * influence * light.intensity;
                totalContribution += contribution;
                totalColor += light.color * contribution;
            }

            // =================================================================
            // Vertex Shader
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

                // View direction
                float3 viewDir = normalize(_WorldSpaceCameraPos - drop.position);

                // Accumulate light contributions
                float totalContribution = 0.0;
                float3 totalColor = float3(0, 0, 0);

                for (int i = 0; i < _LightCount; i++)
                {
                    RainLightData light = _LightBuffer[i];
                    CalculateLightContribution(drop.position, viewDir, light, totalContribution, totalColor);
                }

                // Sparkle effect (only when lit)
                float sparkleIntensity = 0.0;
                if (totalContribution > 0.1)
                {
                    float sparkle = sin(drop.sparklePhase) * 0.5 + 0.5;
                    sparkle = pow(sparkle, 8.0);
                    sparkleIntensity = sparkle * totalContribution * 2.0;
                }

                VertexOutput o;
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = QuadTexCoords[vertexID];
                o.alpha = drop.alpha;
                o.rimLight = saturate(totalContribution);
                o.sparkle = sparkleIntensity;
                o.color = totalColor;

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

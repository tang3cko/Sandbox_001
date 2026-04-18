using UnityEngine;
using System.Runtime.InteropServices;

namespace Prism.Rain
{
    /// <summary>
    /// Light data structure for GPU.
    /// Memory layout must match Rain.compute and Rain.shader RainLightData structs.
    /// Values are precomputed on CPU to avoid per-vertex trig and division on GPU.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RainLightData
    {
        public Vector3 position;
        public float invRange;          // 1.0 / range (precomputed, avoids GPU division)
        public Vector3 direction;       // normalized, (0,0,0) for point lights
        public float cosOuterAngle;     // cos(spotAngle * 0.5 * Deg2Rad), precomputed
        public Vector3 color;
        public float intensity;
        public float cosInnerAngle;     // cos(innerSpotAngle * 0.5 * Deg2Rad), precomputed
        public int lightType;           // 0 = Point, 1 = Spot
        public int lightIndex;          // URP additional light index (reserved for future shadow support)
        public float padding;

        /// <summary>
        /// Size in bytes (must be multiple of 16 for GPU alignment).
        /// 16 floats = 64 bytes
        /// </summary>
        public static int Stride => sizeof(float) * 16;

        public static RainLightData FromLight(Light light, int additionalLightIndex)
        {
            var data = new RainLightData
            {
                position = light.transform.position,
                invRange = 1f / Mathf.Max(light.range, 0.0001f),
                color = new Vector3(light.color.r, light.color.g, light.color.b),
                intensity = light.intensity,
                lightType = light.type == LightType.Spot ? 1 : 0,
                lightIndex = additionalLightIndex
            };

            if (light.type == LightType.Spot)
            {
                data.direction = light.transform.forward;
                data.cosOuterAngle = Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad);
                data.cosInnerAngle = Mathf.Cos(light.innerSpotAngle * 0.5f * Mathf.Deg2Rad);
            }

            return data;
        }
    }
}

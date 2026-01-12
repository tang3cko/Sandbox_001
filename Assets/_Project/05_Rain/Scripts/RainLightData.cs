using UnityEngine;
using System.Runtime.InteropServices;

namespace Prism.Rain
{
    /// <summary>
    /// Light data structure for GPU.
    /// Memory layout must match Rain.shader's RainLightData struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RainLightData
    {
        public Vector3 position;
        public float range;
        public Vector3 direction;   // normalized, (0,0,0) for point lights
        public float spotAngle;     // degrees, 0 for point lights
        public Vector3 color;
        public float intensity;
        public float innerSpotAngle; // degrees, 0 for point lights
        public int lightType;        // 0 = Point, 1 = Spot
        public int lightIndex;       // URP additional light index for shadow lookup
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
                range = light.range,
                color = new Vector3(light.color.r, light.color.g, light.color.b),
                intensity = light.intensity,
                lightType = light.type == LightType.Spot ? 1 : 0,
                lightIndex = additionalLightIndex
            };

            if (light.type == LightType.Spot)
            {
                data.direction = light.transform.forward;
                data.spotAngle = light.spotAngle;
                data.innerSpotAngle = light.innerSpotAngle;
            }
            else
            {
                data.direction = Vector3.zero;
                data.spotAngle = 0f;
                data.innerSpotAngle = 0f;
            }

            return data;
        }
    }
}

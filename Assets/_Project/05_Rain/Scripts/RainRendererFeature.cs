using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Prism.Rain
{
    /// <summary>
    /// URP Renderer Feature for GPU-based rain rendering.
    /// Add this to the URP Renderer asset to enable rain effects.
    /// </summary>
    [DisallowMultipleRendererFeature("Rain")]
    public sealed class RainRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class RainSettings
        {
            [Header("Particle Count")]
            [Tooltip("Number of rain particles")]
            public int rainDropCount = 20000;

            [Header("Spawn Area")]
            [Tooltip("Horizontal spawn radius from camera")]
            public float spawnRadius = 8f;

            [Tooltip("Height offset below camera")]
            public float heightMin = -1f;

            [Tooltip("Height offset above camera")]
            public float heightMax = 12f;

            [Header("Physics")]
            [Tooltip("Gravity acceleration")]
            public float gravity = 12f;

            [Header("Culling")]
            [Tooltip("Maximum distance from camera to render rain")]
            public float cullDistance = 50f;

            [Tooltip("Scale multiplier for rain drops")]
            public float dropScale = 1f;

            [Header("Alpha")]
            [Tooltip("Base alpha when not lit")]
            [Range(0f, 1f)]
            public float baseAlpha = 0.02f;

            [Tooltip("Alpha when lit by light")]
            [Range(0f, 1f)]
            public float litAlpha = 0.9f;

            [Header("Rendering")]
            [Tooltip("When to render rain in the pipeline")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

            [Tooltip("Render bounds size for frustum culling")]
            public float renderBoundsSize = 100f;
        }

        [Header("Resources")]
        [SerializeField] private ComputeShader computeShader;
        [SerializeField] private Material rainMaterial;

        [Header("Settings")]
        [SerializeField] private RainSettings settings = new RainSettings();

        private RainRenderPass rainRenderPass;

        public override void Create()
        {
            if (computeShader == null || rainMaterial == null)
            {
                return;
            }

            rainRenderPass = new RainRenderPass(computeShader, rainMaterial, settings);
            rainRenderPass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (rainRenderPass == null)
            {
                return;
            }

            // Skip when not playing
            if (!Application.isPlaying)
            {
                return;
            }

            // Skip for non-game cameras (preview, reflection, scene view)
            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game)
            {
                return;
            }

            renderer.EnqueuePass(rainRenderPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            rainRenderPass?.Dispose();
        }
    }
}

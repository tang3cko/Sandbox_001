using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Prism.RenderingTest
{
    /// <summary>
    /// Switches between different URP Pipeline Assets at runtime.
    /// Each asset should be configured with a different rendering mode (Forward/Forward+/Deferred).
    /// </summary>
    public sealed class RenderPipelineSwitcher : MonoBehaviour
    {
        public enum RenderingMode
        {
            Forward = 0,
            ForwardPlus = 1,
            Deferred = 2
        }

        [System.Serializable]
        public class PipelineConfig
        {
            public string name;
            public RenderingMode mode;
            public UniversalRenderPipelineAsset asset;
        }

        [Header("Pipeline Assets")]
        [Tooltip("Assign URP Assets for each rendering mode")]
        [SerializeField] private PipelineConfig[] pipelineConfigs = new PipelineConfig[]
        {
            new PipelineConfig { name = "Forward", mode = RenderingMode.Forward },
            new PipelineConfig { name = "Forward+", mode = RenderingMode.ForwardPlus },
            new PipelineConfig { name = "Deferred", mode = RenderingMode.Deferred }
        };

        private int currentIndex;
        private RenderPipelineAsset originalAsset;

        public RenderingMode CurrentMode => pipelineConfigs[currentIndex].mode;
        public string CurrentModeName => pipelineConfigs[currentIndex].name;

        private void Awake()
        {
            originalAsset = GraphicsSettings.defaultRenderPipeline;

            // Find current mode based on active pipeline
            for (int i = 0; i < pipelineConfigs.Length; i++)
            {
                if (pipelineConfigs[i].asset == originalAsset)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Cycles to the next rendering mode.
        /// </summary>
        public void CycleMode()
        {
            int nextIndex = (currentIndex + 1) % pipelineConfigs.Length;
            SetMode(nextIndex);
        }

        /// <summary>
        /// Sets the rendering mode by index.
        /// </summary>
        public void SetMode(int index)
        {
            if (index < 0 || index >= pipelineConfigs.Length)
                return;

            var config = pipelineConfigs[index];
            if (config.asset == null)
            {
                Debug.LogWarning($"[RenderPipelineSwitcher] No asset assigned for {config.name}");
                return;
            }

            currentIndex = index;
            GraphicsSettings.defaultRenderPipeline = config.asset;
            QualitySettings.renderPipeline = config.asset;

            Debug.Log($"[RenderPipelineSwitcher] Switched to {config.name} ({config.mode})");
        }

        /// <summary>
        /// Sets the rendering mode by enum.
        /// </summary>
        public void SetMode(RenderingMode mode)
        {
            for (int i = 0; i < pipelineConfigs.Length; i++)
            {
                if (pipelineConfigs[i].mode == mode)
                {
                    SetMode(i);
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            // Restore original pipeline on exit
            if (originalAsset != null)
            {
                GraphicsSettings.defaultRenderPipeline = originalAsset;
                QualitySettings.renderPipeline = originalAsset;
            }
        }
    }
}

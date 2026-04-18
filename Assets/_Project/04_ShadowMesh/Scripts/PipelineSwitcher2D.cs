using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Prism.ShadowMesh
{
    /// <summary>
    /// Switches to the 2D URP pipeline on scene load and restores the
    /// original pipeline when the scene is unloaded.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PipelineSwitcher2D : MonoBehaviour
    {
        [Header("Pipeline")]
        [Tooltip("The 2D URP Asset to activate for this scene")]
        [SerializeField] private UniversalRenderPipelineAsset pipeline2DAsset;

        private RenderPipelineAsset originalDefaultPipeline;
        private RenderPipelineAsset originalQualityPipeline;
        private bool hasSwitchedPipeline;

        private void Awake()
        {
            if (pipeline2DAsset == null)
            {
                Debug.LogWarning($"[{GetType().Name}] pipeline2DAsset not assigned on {gameObject.name}.", this);
                return;
            }

            originalDefaultPipeline = GraphicsSettings.defaultRenderPipeline;
            originalQualityPipeline = QualitySettings.renderPipeline;

            GraphicsSettings.defaultRenderPipeline = pipeline2DAsset;
            QualitySettings.renderPipeline = pipeline2DAsset;
            hasSwitchedPipeline = true;
        }

        private void OnDestroy()
        {
            if (!hasSwitchedPipeline)
            {
                return;
            }

            GraphicsSettings.defaultRenderPipeline = originalDefaultPipeline;
            QualitySettings.renderPipeline = originalQualityPipeline;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (pipeline2DAsset == null)
            {
                Debug.LogWarning($"[{GetType().Name}] pipeline2DAsset not assigned on {gameObject.name}.", this);
            }
        }
#endif
    }
}

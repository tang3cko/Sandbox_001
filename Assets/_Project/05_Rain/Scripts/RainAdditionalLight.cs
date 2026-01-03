using UnityEngine;

namespace Prism.Rain
{
    /// <summary>
    /// Marker component to indicate that a light should affect rain rendering.
    /// Attach to any Point or Spot light that should illuminate rain drops.
    /// Light parameters (position, color, range, etc.) are read directly from the Light component.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class RainAdditionalLight : MonoBehaviour
    {
        // This component acts as a marker.
        // The RainRenderPass will collect all lights with this component
        // and use their Light data to illuminate rain particles.
        //
        // Future: Add per-light rain parameters here if needed, e.g.:
        // - Influence multiplier
        // - Custom scattering
        // - etc.
    }
}

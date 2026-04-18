using System.Collections.Generic;
using UnityEngine;

namespace Prism.Rain
{
    /// <summary>
    /// Marker component to indicate that a light should affect rain rendering.
    /// Attach to any Point or Spot light that should illuminate rain drops.
    /// Light parameters are read directly from the cached Light component.
    /// Registers itself in a static list for O(1) lookup by RainRenderPass.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class RainAdditionalLight : MonoBehaviour
    {
        // Fields
        private static readonly List<RainAdditionalLight> activeInstances = new();

        private Light cachedLight;

        // Properties
        public static IReadOnlyList<RainAdditionalLight> ActiveInstances => activeInstances;

        public Light Light => cachedLight;

        // Unity Lifecycle
        private void Awake()
        {
            cachedLight = GetComponent<Light>();
        }

        private void OnEnable()
        {
            activeInstances.Add(this);
        }

        private void OnDisable()
        {
            activeInstances.Remove(this);
        }
    }
}

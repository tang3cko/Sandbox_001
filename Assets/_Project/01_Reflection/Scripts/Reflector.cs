using UnityEngine;

namespace Prism.Reflection
{
    /// <summary>
    /// Marker component to indicate that an object can reflect lasers.
    /// Attach to any object with a Collider that should reflect laser beams.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Reflector : MonoBehaviour
    {
        // This component acts as a marker.
        // The LaserEmitter will check for this component on raycast hits
        // to determine if the laser should reflect or stop.
        //
        // Future: Add per-reflector parameters here if needed, e.g.:
        // - Reflectivity (0-1)
        // - Tint color
        // - Scatter angle
    }
}

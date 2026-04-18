using System;
using UnityEngine;

namespace Prism.Reflection
{
    /// <summary>
    /// Emits a laser that reflects off surfaces using Physics.Raycast and Vector3.Reflect.
    /// Requires a LineRenderer component to visualize the laser path.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LaserEmitter : MonoBehaviour
    {
        [Header("Laser Settings")]
        [Tooltip("Maximum distance the laser can travel per segment")]
        [SerializeField] private float maxDistance = 100f;

        [Tooltip("Maximum number of reflections")]
        [SerializeField] private int maxReflections = 10;

        [Tooltip("Layer mask for raycast detection")]
        [SerializeField] private LayerMask layerMask = ~0;

        [Header("Visual Settings")]
        [Tooltip("Width of the laser beam")]
        [SerializeField] private float laserWidth = 0.05f;

        [Tooltip("Color of the laser beam")]
        [SerializeField] private Color laserColor = Color.red;

        [Header("Debug")]
        [Tooltip("Draw debug gizmos in Scene view")]
        [SerializeField] private bool drawGizmos = true;

        public event Action OnUpdated;

        private LineRenderer lineRenderer;
        private readonly Vector3[] points = new Vector3[12]; // maxReflections + 2
        private int pointCount;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
            CastLaser();
        }

        /// <summary>
        /// Recalculates the laser path. Call when emitter or reflectors change.
        /// </summary>
        public void RequestUpdate()
        {
            CastLaser();
            OnUpdated?.Invoke();
        }

        /// <summary>
        /// Configures the LineRenderer with current visual settings.
        /// </summary>
        private void ConfigureLineRenderer()
        {
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth;
            lineRenderer.startColor = laserColor;
            lineRenderer.endColor = laserColor;
            lineRenderer.useWorldSpace = true;
        }

        /// <summary>
        /// Casts the laser and calculates all reflection points.
        /// </summary>
        private void CastLaser()
        {
            pointCount = 0;
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;

            // Add starting point
            AddPoint(origin);

            for (int i = 0; i < maxReflections + 1; i++)
            {
                if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
                {
                    // Add hit point
                    AddPoint(hit.point);

                    // Check if hit object can reflect
                    if (!hit.collider.TryGetComponent<Reflector>(out _))
                    {
                        // Hit non-reflective surface - stop laser
                        break;
                    }

                    // Calculate reflection direction
                    direction = Vector3.Reflect(direction, hit.normal);
                    origin = hit.point + direction * 0.001f; // Small offset to avoid self-intersection
                }
                else
                {
                    // No hit - add endpoint at max distance
                    AddPoint(origin + direction * maxDistance);
                    break;
                }
            }

            // Update LineRenderer
            lineRenderer.positionCount = pointCount;
            lineRenderer.SetPositions(points);
        }

        /// <summary>
        /// Adds a point to the laser path.
        /// </summary>
        private void AddPoint(Vector3 point)
        {
            if (pointCount < points.Length)
            {
                points[pointCount] = point;
                pointCount++;
            }
        }

        private void OnValidate()
        {
            if (lineRenderer != null)
            {
                ConfigureLineRenderer();
            }

            // Ensure points array is large enough
            if (maxReflections + 2 > points.Length)
            {
                maxReflections = points.Length - 2;
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            if (!Application.isPlaying) return;

            // Draw reflection points
            Gizmos.color = Color.yellow;
            for (int i = 1; i < pointCount - 1; i++)
            {
                Gizmos.DrawWireSphere(points[i], 0.1f);
            }

            // Draw direction at emitter
            Gizmos.color = laserColor;
            Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
        }
    }
}

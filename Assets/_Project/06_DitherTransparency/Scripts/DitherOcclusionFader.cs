using System.Collections.Generic;
using UnityEngine;

namespace Prism.DitherTransparency
{
    /// <summary>
    /// Fades out occluders between the camera and a target using dither transparency.
    /// Casts a sphere from the camera to the target each frame; renderers whose
    /// material exposes _DitherAlpha are faded down while occluding and restored after.
    /// Uses MaterialPropertyBlock so each occluder fades independently.
    /// </summary>
    [ExecuteAlways]
    public sealed class DitherOcclusionFader : MonoBehaviour
    {
        /// <summary>How an occluder is made see-through.</summary>
        public enum FadeMode
        {
            /// <summary>Dither the whole renderer uniformly.</summary>
            WholeObject,
            /// <summary>Dither only a screen-space circle around the target.</summary>
            CircularHole,
        }

        private static readonly int DitherAlphaId = Shader.PropertyToID("_DitherAlpha");
        private static readonly int HoleAlphaId = Shader.PropertyToID("_HoleAlpha");
        private static readonly int HoleRadiusId = Shader.PropertyToID("_HoleRadius");
        private static readonly int HoleSoftnessId = Shader.PropertyToID("_HoleSoftness");
        private static readonly int HoleCenterId = Shader.PropertyToID("_HoleCenter");

        [Header("Occlusion")]
        [Tooltip("Target that should stay visible (e.g. the player)")]
        [SerializeField] private Transform target;
        [Tooltip("Layers checked for occluders between camera and target")]
        [SerializeField] private LayerMask occluderMask = ~0;
        [SerializeField] private float castRadius = 0.3f;

        [Header("Fade")]
        [SerializeField] private FadeMode fadeMode = FadeMode.WholeObject;
        [Range(0f, 1f)]
        [SerializeField] private float fadedAlpha = 0.25f;
        [SerializeField] private float fadeSpeed = 8f;

        [Header("Circular Hole")]
        [Tooltip("Hole radius in viewport height units when fully open")]
        [Range(0f, 1f)]
        [SerializeField] private float holeRadius = 0.25f;
        [Range(0f, 0.5f)]
        [SerializeField] private float holeSoftness = 0.08f;

        private readonly Dictionary<Renderer, float> alphaByRenderer = new();
        private readonly HashSet<Renderer> occludedThisFrame = new();
        private readonly List<Renderer> fadingRenderers = new();
        private MaterialPropertyBlock propertyBlock;
        private Camera cachedCamera;

        private void OnDisable()
        {
            foreach (var renderer in alphaByRenderer.Keys)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }
            alphaByRenderer.Clear();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            propertyBlock ??= new MaterialPropertyBlock();

            CollectOccluders();
            UpdateFades();
        }

        private void CollectOccluders()
        {
            occludedThisFrame.Clear();

            // Outside play mode the physics scene is not ticked, so newly
            // moved or created colliders must be synced before querying.
            if (!Application.isPlaying)
            {
                Physics.SyncTransforms();
            }

            Vector3 origin = transform.position;
            Vector3 toTarget = target.position - origin;
            float distance = toTarget.magnitude;
            if (distance < Mathf.Epsilon) return;

            var hits = Physics.SphereCastAll(
                origin, castRadius, toTarget / distance, distance, occluderMask);

            foreach (var hit in hits)
            {
                if (hit.transform == target || hit.transform.IsChildOf(target)) continue;

                var renderer = hit.collider.GetComponentInChildren<Renderer>();
                if (renderer == null || renderer.sharedMaterial == null) continue;
                if (!renderer.sharedMaterial.HasProperty(DitherAlphaId)) continue;

                occludedThisFrame.Add(renderer);
                alphaByRenderer.TryAdd(renderer, 1f);
            }
        }

        private void UpdateFades()
        {
            // Snapshot keys: the dictionary is mutated while iterating
            fadingRenderers.Clear();
            fadingRenderers.AddRange(alphaByRenderer.Keys);

            float delta = Application.isPlaying ? Time.deltaTime : 1f;

            foreach (var renderer in fadingRenderers)
            {
                if (renderer == null)
                {
                    alphaByRenderer.Remove(renderer);
                    continue;
                }

                float targetAlpha = occludedThisFrame.Contains(renderer) ? fadedAlpha : 1f;
                float newAlpha = Mathf.MoveTowards(alphaByRenderer[renderer], targetAlpha, fadeSpeed * delta);

                if (Mathf.Approximately(newAlpha, 1f))
                {
                    renderer.SetPropertyBlock(null);
                    alphaByRenderer.Remove(renderer);
                }
                else
                {
                    alphaByRenderer[renderer] = newAlpha;
                    ApplyFade(renderer, newAlpha);
                }
            }
        }

        private void ApplyFade(Renderer renderer, float alpha)
        {
            if (fadeMode == FadeMode.WholeObject)
            {
                propertyBlock.SetFloat(DitherAlphaId, alpha);
                propertyBlock.SetFloat(HoleRadiusId, 0f);
            }
            else
            {
                // Animate the hole radius with fade progress; the shadow pass
                // ignores the hole, so _DitherAlpha stays at 1
                cachedCamera = cachedCamera != null ? cachedCamera : GetComponent<Camera>();
                Vector3 viewportPos = cachedCamera != null
                    ? cachedCamera.WorldToViewportPoint(target.position)
                    : new Vector3(0.5f, 0.5f, 0f);

                float progress = Mathf.InverseLerp(1f, fadedAlpha, alpha);
                propertyBlock.SetFloat(DitherAlphaId, 1f);
                propertyBlock.SetFloat(HoleAlphaId, fadedAlpha);
                propertyBlock.SetFloat(HoleRadiusId, holeRadius * progress);
                propertyBlock.SetFloat(HoleSoftnessId, holeSoftness);
                propertyBlock.SetVector(HoleCenterId, new Vector4(viewportPos.x, viewportPos.y, 0f, 0f));
            }
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace Prism.RenderingTest
{
    /// <summary>
    /// Spawns and manages multiple point lights for rendering performance testing.
    /// </summary>
    public sealed class LightSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Initial number of lights to spawn")]
        [SerializeField] private int initialLightCount = 10;

        [Tooltip("Maximum number of lights")]
        [SerializeField] private int maxLightCount = 200;

        [Header("Light Properties")]
        [Tooltip("Light intensity")]
        [SerializeField] private float lightIntensity = 1f;

        [Tooltip("Light range")]
        [SerializeField] private float lightRange = 5f;

        [Tooltip("Enable shadows on spawned lights")]
        [SerializeField] private bool enableShadows = false;

        [Header("Spawn Area")]
        [Tooltip("Size of the spawn area")]
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(20f, 5f, 20f);

        [Tooltip("Center offset of spawn area")]
        [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;

        [Header("Animation")]
        [Tooltip("Animate light positions")]
        [SerializeField] private bool animateLights = true;

        [Tooltip("Animation speed")]
        [SerializeField] private float animationSpeed = 1f;

        private readonly List<Light> spawnedLights = new List<Light>();
        private readonly List<Vector3> lightOffsets = new List<Vector3>();

        public int LightCount => spawnedLights.Count;
        public int MaxLightCount => maxLightCount;

        public bool AnimateLights
        {
            get => animateLights;
            set => animateLights = value;
        }

        private void Start()
        {
            SpawnLights(initialLightCount);
        }

        private void Update()
        {
            UpdateLightAnimation();
        }

        private void UpdateLightAnimation()
        {
            if (!animateLights) return;

            float time = Time.time * animationSpeed;

            for (int i = 0; i < spawnedLights.Count; i++)
            {
                if (spawnedLights[i] == null) continue;

                Vector3 basePos = lightOffsets[i];
                float offset = Mathf.Sin(time + i * 0.5f) * 2f;
                spawnedLights[i].transform.position = basePos + new Vector3(0, offset, 0);
            }
        }

        /// <summary>
        /// Spawns a specific number of lights.
        /// </summary>
        public void SpawnLights(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (spawnedLights.Count >= maxLightCount)
                    break;

                CreateLight();
            }
        }

        /// <summary>
        /// Adds lights to the scene.
        /// </summary>
        public void AddLights(int count)
        {
            SpawnLights(count);
            Debug.Log($"[LightSpawner] Added lights. Total: {spawnedLights.Count}");
        }

        /// <summary>
        /// Removes lights from the scene.
        /// </summary>
        public void RemoveLights(int count)
        {
            for (int i = 0; i < count && spawnedLights.Count > 0; i++)
            {
                int lastIndex = spawnedLights.Count - 1;
                if (spawnedLights[lastIndex] != null)
                {
                    Destroy(spawnedLights[lastIndex].gameObject);
                }
                spawnedLights.RemoveAt(lastIndex);
                lightOffsets.RemoveAt(lastIndex);
            }

            Debug.Log($"[LightSpawner] Removed lights. Total: {spawnedLights.Count}");
        }

        /// <summary>
        /// Sets the exact number of lights.
        /// </summary>
        public void SetLightCount(int count)
        {
            count = Mathf.Clamp(count, 0, maxLightCount);

            while (spawnedLights.Count > count)
            {
                RemoveLights(1);
            }

            while (spawnedLights.Count < count)
            {
                AddLights(1);
            }
        }

        private void CreateLight()
        {
            Vector3 position = GetRandomPosition();

            GameObject lightObj = new GameObject($"Light_{spawnedLights.Count}");
            lightObj.transform.parent = transform;
            lightObj.transform.position = position;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = lightIntensity;
            light.range = lightRange;
            light.color = GetRandomColor();
            light.shadows = enableShadows ? LightShadows.Soft : LightShadows.None;

            spawnedLights.Add(light);
            lightOffsets.Add(position);
        }

        private Vector3 GetRandomPosition()
        {
            return spawnAreaCenter + new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                Random.Range(0, spawnAreaSize.y),
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );
        }

        private Color GetRandomColor()
        {
            return Color.HSVToRGB(Random.value, 0.7f, 1f);
        }

        private void OnDestroy()
        {
            foreach (var light in spawnedLights)
            {
                if (light != null)
                {
                    Destroy(light.gameObject);
                }
            }
            spawnedLights.Clear();
            lightOffsets.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
        }
    }
}

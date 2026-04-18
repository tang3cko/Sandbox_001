using UnityEngine;

namespace Prism.ShadowMesh
{
    /// <summary>
    /// Generates a visibility polygon mesh from radial raycasts and updates
    /// a PolygonCollider2D to act as a physical shadow boundary.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class ShadowMeshGenerator : MonoBehaviour
    {
        private const int MIN_RAY_COUNT = 12;
        private const int RAYCAST_HIT_BUFFER_SIZE = 16;

        [Header("Raycast")]
        [Tooltip("Number of rays cast in a full circle")]
        [Min(MIN_RAY_COUNT)]
        [SerializeField] private int rayCount = 180;

        [Tooltip("Maximum reach of each ray")]
        [SerializeField] private float maxDistance = 10f;

        [Tooltip("Layers treated as shadow-casting obstacles")]
        [SerializeField] private LayerMask obstacleLayers;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private PolygonCollider2D polygonCollider;

        private Vector3[] vertices;
        private int[] triangles;
        private Vector2[] colliderPoints;
        private RaycastHit2D[] raycastHits;
        private ContactFilter2D obstacleFilter;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            polygonCollider = GetComponent<PolygonCollider2D>();

            mesh = new Mesh { name = "ShadowMesh" };
            meshFilter.mesh = mesh;

            Initialize();
        }

        private void Update()
        {
            GenerateMesh();
        }

        private void OnDestroy()
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }
        }

        private void Initialize()
        {
            InitializeArrays();
            ConfigureObstacleFilter();
        }

        private void InitializeArrays()
        {
            vertices = new Vector3[rayCount + 1];
            colliderPoints = new Vector2[rayCount];
            raycastHits = new RaycastHit2D[RAYCAST_HIT_BUFFER_SIZE];

            triangles = new int[rayCount * 3];
            for (int i = 0; i < rayCount; i++)
            {
                int ti = i * 3;
                triangles[ti] = 0;
                triangles[ti + 1] = i + 1;
                triangles[ti + 2] = i + 2 <= rayCount ? i + 2 : 1;
            }
        }

        private void ConfigureObstacleFilter()
        {
            obstacleFilter = new ContactFilter2D();
            obstacleFilter.SetLayerMask(obstacleLayers);
            obstacleFilter.useTriggers = false;
        }

        private void GenerateMesh()
        {
            if (vertices == null || vertices.Length != rayCount + 1)
            {
                InitializeArrays();
            }

            ConfigureObstacleFilter();

            Vector2 origin = transform.position;
            float angleStep = 360f / rayCount;

            vertices[0] = Vector3.zero;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                Vector2 endPoint = GetRayEndPoint(origin, direction);

                Vector3 localPoint = transform.InverseTransformPoint(endPoint);
                vertices[i + 1] = localPoint;
                colliderPoints[i] = localPoint;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            polygonCollider.pathCount = 1;
            polygonCollider.SetPath(0, colliderPoints);
        }

        private Vector2 GetRayEndPoint(Vector2 origin, Vector2 direction)
        {
            int hitCount = Physics2D.Raycast(origin, direction, obstacleFilter, raycastHits, maxDistance);
            float nearestDistance = float.PositiveInfinity;
            Vector2 endPoint = origin + direction * maxDistance;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = raycastHits[i];
                if (hit.collider == null || hit.collider == polygonCollider)
                {
                    continue;
                }

                if (hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                endPoint = hit.point;
            }

            return endPoint;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rayCount < MIN_RAY_COUNT)
            {
                rayCount = MIN_RAY_COUNT;
                Debug.LogWarning($"[{GetType().Name}] rayCount clamped to minimum {MIN_RAY_COUNT} on {gameObject.name}.", this);
            }

            if (maxDistance <= 0f)
            {
                maxDistance = 1f;
                Debug.LogWarning($"[{GetType().Name}] maxDistance must be positive on {gameObject.name}.", this);
            }
        }
#endif
    }
}

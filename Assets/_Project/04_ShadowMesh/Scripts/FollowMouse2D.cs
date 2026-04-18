using UnityEngine;
using UnityEngine.InputSystem;

namespace Prism.ShadowMesh
{
    /// <summary>
    /// Moves this object to follow the mouse cursor in 2D world space.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FollowMouse2D : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip("Camera used to convert the cursor position to world space")]
        [SerializeField] private Camera targetCamera;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (targetCamera == null || Mouse.current == null)
            {
                return;
            }

            Vector3 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = targetCamera.ScreenToWorldPoint(mousePosition);
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetCamera == null && Camera.main == null)
            {
                Debug.LogWarning($"[{GetType().Name}] targetCamera not assigned and no MainCamera found on {gameObject.name}.", this);
            }
        }
#endif
    }
}

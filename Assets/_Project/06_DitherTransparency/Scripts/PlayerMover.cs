using UnityEngine;
using UnityEngine.InputSystem;

namespace Prism.DitherTransparency
{
    /// <summary>
    /// Minimal WASD player movement for the dither transparency demo.
    /// Polls the keyboard directly via the Input System and moves a
    /// CharacterController relative to the main camera's orientation.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController controller;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;

            Vector3 move = Vector3.zero;
            if (input.sqrMagnitude > 0f)
            {
                input.Normalize();
                Transform cam = Camera.main != null ? Camera.main.transform : transform;
                Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
                move = forward * input.y + right * input.x;

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(move),
                    rotationSpeed * Time.deltaTime);
            }

            verticalVelocity = controller.isGrounded
                ? -1f
                : verticalVelocity + gravity * Time.deltaTime;

            controller.Move((move * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }
    }
}

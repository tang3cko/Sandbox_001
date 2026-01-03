using UnityEngine;
using UnityEngine.InputSystem;

namespace Prism.Common
{
    /// <summary>
    /// Simple fly camera controller using InputActionAsset with PlayerInput component.
    /// Attach to a camera with PlayerInput component configured to use Send Messages.
    /// </summary>
    public class FlyCameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float sprintMultiplier = 3f;
        [SerializeField] private float smoothTime = 0.1f;

        [Header("Look Settings")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float verticalLookLimit = 89f;

        [Header("Cursor Settings")]
        [SerializeField] private bool lockCursorOnStart = true;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _isSprinting;
        private float _verticalMovement;

        private Vector3 _currentVelocity;
        private Vector3 _targetVelocity;

        private float _pitch;
        private float _yaw;

        private void Start()
        {
            _yaw = transform.eulerAngles.y;
            _pitch = transform.eulerAngles.x;

            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleCursorToggle();
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            _yaw += _lookInput.x * lookSensitivity;
            _pitch -= _lookInput.y * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -verticalLookLimit, verticalLookLimit);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleMovement()
        {
            float currentSpeed = _isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            _targetVelocity = (forward * _moveInput.y + right * _moveInput.x) * currentSpeed;
            _targetVelocity.y += _verticalMovement * currentSpeed;

            _currentVelocity = Vector3.Lerp(_currentVelocity, _targetVelocity, Time.deltaTime / smoothTime);

            transform.position += _currentVelocity * Time.deltaTime;
        }

        private void HandleCursorToggle()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // Input callbacks for PlayerInput component (Send Messages mode)
        private void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        private void OnLook(InputValue value)
        {
            _lookInput = value.Get<Vector2>();
        }

        private void OnSprint(InputValue value)
        {
            _isSprinting = value.isPressed;
        }

        private void OnAscend(InputValue value)
        {
            _verticalMovement = value.isPressed ? 1f : 0f;
        }

        private void OnDescend(InputValue value)
        {
            _verticalMovement = value.isPressed ? -1f : 0f;
        }
    }
}

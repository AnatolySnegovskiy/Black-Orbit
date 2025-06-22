using Unity.Mathematics;
using UnityEngine;

namespace Black_Orbit.Scripts.FPS
{
    [RequireComponent(typeof(Rigidbody))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [Range(0f, 1f)]
        [SerializeField] private float movementInertia = 0.5f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float jumpKickAngle = 8f;
        [SerializeField] private float jumpKickSpeed = 6f;
        [SerializeField] private float strafeTiltAngle = 5f;
        [SerializeField] private float strafeTiltSpeed = 5f;

        private float _currentTilt; // Текущий угол поворота камеры по Z
        private float _targetTilt;  // Целевой угол
        private float _lookX; // вращение вверх/вниз (вокруг X)
        private float _lookY; // вращение вбок (вокруг Y)
        private float _jumpKickOffset;
        private float _jumpKickTarget;

        [Header("Camera")]
        [SerializeField] private Transform cameraHolder;
        [Range(0.1f, 1f)]
        [SerializeField] private float lookSensitivity = 0.2f;
        [Range(0f, 1f)]
        [SerializeField] private float cameraInertia = 0.5f;
        [SerializeField] private float maxLookX = 80f;
        [SerializeField] private float minLookX = -80f;

        [Header("Crouch")]
        [SerializeField] private float crouchHeight = 1f;
        private float _originalHeight;

        private Rigidbody _rb;
        private CapsuleCollider _col;
        private PlayerControls _input;
        private Vector2 _moveInput;
        private Vector2 _rawLookInput;
        private Vector2 _smoothLookInput;

        private bool _isGrounded;
        private bool _isCrouching;
        private float _currentXRotation;

        void Awake()
        {
            _input = new PlayerControls();
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _originalHeight = _col.height;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void OnEnable()
        {
            _input.Player.Enable();
            _input.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
            _input.Player.Move.canceled += _ => _moveInput = Vector2.zero;
            _input.Player.Look.performed += ctx => _rawLookInput = ctx.ReadValue<Vector2>();
            _input.Player.Look.canceled += _ => _rawLookInput = Vector2.zero;
            _input.Player.Jump.performed += _ => Jump();
            _input.Player.Crouch.performed += _ => CrouchToggle();
        }

        void OnDisable()
        {
            _input.Player.Disable();
        }

        void FixedUpdate()
        {
            Move();
            CheckGrounded();
        }

        void Update()
        {
            Look();
        }

        void Move()
        {
            Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            float speed = _isCrouching ? crouchSpeed : moveSpeed;

            Vector3 targetVelocity = new Vector3(move.x * speed, _rb.linearVelocity.y, move.z * speed);
            float lerpFactor = math.clamp(1f - math.pow(movementInertia, Time.deltaTime * 10), 0.001f, 1f);

            _rb.linearVelocity = math.lerp(_rb.linearVelocity, targetVelocity, lerpFactor);
            // Наклон камеры при боковом движении
            _targetTilt = -_moveInput.x * strafeTiltAngle;
        }

        void Jump()
        {
            if (_isGrounded)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                // Отдача камеры вниз
                _jumpKickTarget = jumpKickAngle;
            }
        }

        void CrouchToggle()
        {
            _isCrouching = !_isCrouching;
            _col.height = _isCrouching ? crouchHeight : _originalHeight;
            Vector3 camPos = cameraHolder.localPosition;
            camPos.y = _isCrouching ? crouchHeight - 0.5f : _originalHeight - 0.5f;
            cameraHolder.localPosition = camPos;
        }

        void CheckGrounded()
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            _isGrounded = Physics.Raycast(ray, (_col.height / 2f) + 0.1f);
        }

        void Look()
        {
            float lerpFactor = math.clamp(1f - math.pow(cameraInertia, Time.deltaTime * 10), 0.001f, 1f);
            _smoothLookInput = math.lerp(_smoothLookInput, _rawLookInput, lerpFactor);

            float mouseX = _smoothLookInput.x * lookSensitivity * 100f * Time.deltaTime;
            float mouseY = _smoothLookInput.y * lookSensitivity * 100f * Time.deltaTime;

            _lookY += mouseX;
            _lookX -= mouseY;
            _lookX = math.clamp(_lookX, minLookX, maxLookX);

            // Обновляем эффект прыжка
            _jumpKickOffset = math.lerp(_jumpKickOffset, _jumpKickTarget, math.max(Time.deltaTime * jumpKickSpeed, 0.01f));
            if (math.abs(_jumpKickOffset - _jumpKickTarget) < 0.01f)
            {
                _jumpKickTarget = 0f; // сбрасываем цель
            }

            // Плавный наклон камеры вбок (roll)
            _currentTilt = math.lerp(_currentTilt, _targetTilt, math.max(Time.deltaTime * strafeTiltSpeed, 0.01f));

            // Применяем итоговое вращение
            cameraHolder.localRotation = Quaternion.Euler(_lookX + _jumpKickOffset, 0f, _currentTilt);
            transform.rotation = Quaternion.Euler(0f, _lookY, 0f);
        }
    }
}

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Black_Orbit.Scripts.FPS
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
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
        private float _targetTilt; // Целевой угол
        private float _lookX; // вращение вверх/вниз (вокруг X)
        private float _lookY; // вращение вбок (вокруг Y)
        private float _jumpKickOffset;
        private float _jumpKickTarget;

        [Header("Camera")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Transform camera;
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
        private PlayerInput _input;
        private Vector2 _moveInput;
        private Vector2 _rawLookInput;
        private Vector2 _smoothLookInput;

        private bool _isGrounded;
        private bool _isCrouching;
        private float _currentXRotation;
        private InputActionMap _currentMap;
        CharacterAnimator _animator;

        void Awake()
        {
            _animator = new CharacterAnimator(GetComponent<Animator>());
            _input = GetComponent<PlayerInput>();
            _currentMap = _input.currentActionMap;
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _originalHeight = _col.height;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void OnEnable()
        {
            _currentMap.Enable();
            _currentMap["Move"].performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
            _currentMap["Move"].canceled += _ => _moveInput = Vector2.zero;
            _currentMap["Look"].performed += ctx => _rawLookInput = ctx.ReadValue<Vector2>();
            _currentMap["Look"].canceled += _ => _rawLookInput = Vector2.zero;
            _currentMap["Jump"].performed += _ => Jump();
            _currentMap["Crouch"].performed += _ => CrouchToggle();
        }

        void OnDisable()
        {
            _currentMap.Disable();
        }

        void FixedUpdate()
        {
            Move();
            CheckGrounded();
        }

        void LateUpdate()
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

            _animator.Move(transform.InverseTransformDirection(_rb.linearVelocity));
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
            Vector3 camPos = camera.localPosition;
            camPos.y = _isCrouching ? crouchHeight - 0.5f : _originalHeight - 0.5f;
            camera.localPosition = camPos;
        }

        void CheckGrounded()
        {
            Ray ray = new Ray(transform.position + Vector3.up * 0.05f, Vector3.down);
            _isGrounded = Physics.Raycast(ray,  0.1f);
            Debug.DrawRay(transform.position + Vector3.up * 0.05f, Vector3.down * 0.1f, _isGrounded ? Color.green : Color.red);
        }

        void Look()
        {
            camera.position = cameraHolder.position;
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
            _currentTilt = math.lerp(_currentTilt, _targetTilt, math.max(Time.smoothDeltaTime * strafeTiltSpeed, 0.01f));
            // Применяем итоговое вращение
            camera.localRotation = Quaternion.Euler(_lookX + _jumpKickOffset, 0f, _currentTilt);

            _rb.MoveRotation(Quaternion.Euler(0, _lookY, 0));
        }
    }
}

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

        private float currentTilt = 0f; // Текущий угол поворота камеры по Z
        private float targetTilt = 0f;  // Целевой угол
        private float lookX; // вращение вверх/вниз (вокруг X)
        private float lookY; // вращение вбок (вокруг Y)
        private float jumpKickOffset = 0f;
        private float jumpKickTarget = 0f;

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
        private float originalHeight;

        private Rigidbody rb;
        private CapsuleCollider col;
        private PlayerControls input;
        private Vector2 moveInput;
        private Vector2 rawLookInput;
        private Vector2 smoothLookInput;

        private bool isGrounded;
        private bool isCrouching;
        private float currentXRotation;

        void Awake()
        {
            input = new PlayerControls();
            rb = GetComponent<Rigidbody>();
            col = GetComponent<CapsuleCollider>();
            originalHeight = col.height;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void OnEnable()
        {
            input.Player.Enable();
            input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
            input.Player.Look.performed += ctx => rawLookInput = ctx.ReadValue<Vector2>();
            input.Player.Look.canceled += ctx => rawLookInput = Vector2.zero;
            input.Player.Jump.performed += ctx => Jump();
            input.Player.Crouch.performed += ctx => CrouchToggle();
        }

        void OnDisable()
        {
            input.Player.Disable();
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
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            float speed = isCrouching ? crouchSpeed : moveSpeed;

            Vector3 targetVelocity = new Vector3(move.x * speed, rb.linearVelocity.y, move.z * speed);
            float lerpFactor = math.clamp(1f - math.pow(movementInertia, Time.deltaTime * 10), 0.001f, 1f);

            rb.linearVelocity = math.lerp(rb.linearVelocity, targetVelocity, lerpFactor);
            // Наклон камеры при боковом движении
            targetTilt = -moveInput.x * strafeTiltAngle;
        }

        void Jump()
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                // Отдача камеры вниз
                jumpKickTarget = jumpKickAngle;
            }
        }

        void CrouchToggle()
        {
            isCrouching = !isCrouching;
            col.height = isCrouching ? crouchHeight : originalHeight;
            Vector3 camPos = cameraHolder.localPosition;
            camPos.y = isCrouching ? crouchHeight - 0.5f : originalHeight - 0.5f;
            cameraHolder.localPosition = camPos;
        }

        void CheckGrounded()
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            isGrounded = Physics.Raycast(ray, (col.height / 2f) + 0.1f);
        }

        void Look()
        {
            float lerpFactor = math.clamp(1f - math.pow(cameraInertia, Time.deltaTime * 10), 0.001f, 1f);
            smoothLookInput = math.lerp(smoothLookInput, rawLookInput, lerpFactor);

            float mouseX = smoothLookInput.x * lookSensitivity * 100f * Time.deltaTime;
            float mouseY = smoothLookInput.y * lookSensitivity * 100f * Time.deltaTime;

            lookY += mouseX;
            lookX -= mouseY;
            lookX = math.clamp(lookX, minLookX, maxLookX);

            // Обновляем эффект прыжка
            jumpKickOffset = math.lerp(jumpKickOffset, jumpKickTarget, math.max(Time.deltaTime * jumpKickSpeed, 0.01f));
            if (math.abs(jumpKickOffset - jumpKickTarget) < 0.01f)
            {
                jumpKickTarget = 0f; // сбрасываем цель
            }

            // Плавный наклон камеры вбок (roll)
            currentTilt = math.lerp(currentTilt, targetTilt, math.max(Time.deltaTime * strafeTiltSpeed, 0.01f));

            // Применяем итоговое вращение
            cameraHolder.localRotation = Quaternion.Euler(lookX + jumpKickOffset, 0f, currentTilt);
            transform.rotation = Quaternion.Euler(0f, lookY, 0f);
        }
    }
}

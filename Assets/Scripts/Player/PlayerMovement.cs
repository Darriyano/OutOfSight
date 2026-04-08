using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Input")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode sneakKey = KeyCode.LeftControl;

    [Header("Mouse")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform cameraHolder;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Noise")]
    [SerializeField] private bool emitMovementNoise = true;
    [SerializeField] private float movementNoiseThreshold = 0.15f;
    [SerializeField] private float walkStepDistance = 2f;
    [SerializeField] private float walkNoiseRadius = 8f;
    [SerializeField] private float walkNoiseStrength = 0.45f;
    [SerializeField] private float sprintStepDistance = 1.3f;
    [SerializeField] private float sprintNoiseRadius = 14f;
    [SerializeField] private float sprintNoiseStrength = 1f;
    [SerializeField, Min(0.05f)] private float sprintNoisePulseInterval = 0.35f;
    [SerializeField] private float sprintNoisePulseRadius = 18f;
    [SerializeField] private float sprintNoisePulseStrength = 1.25f;
    [SerializeField] private float sneakStepDistance = 2.4f;
    [SerializeField] private float sneakNoiseRadius = 2.5f;
    [SerializeField] private float sneakNoiseStrength = 0.08f;
    [SerializeField] private float noiseSourceHeight = 0.1f;
    [SerializeField] private int maxNoiseBounces = 2;
    [SerializeField, Range(0f, 1f)] private float noiseBounceDamping = 0.72f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private float distanceUntilNextStep;
    private float sprintPulseTimer;

    public bool IsSneaking { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsMoving { get; private set; }
    public float HorizontalSpeed { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        Look();
        Move();
    }

    private void Look()
    {
        if (cameraHolder == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        move = Vector3.ClampMagnitude(move, 1f);

        IsSneaking = Input.GetKey(sneakKey);
        IsSprinting = !IsSneaking && Input.GetKey(sprintKey) && move.sqrMagnitude > 0.001f;

        float speed = IsSneaking
            ? crouchSpeed
            : IsSprinting
                ? sprintSpeed
                : walkSpeed;

        controller.Move(move * speed * Time.deltaTime);

        ApplyGravity();
        UpdateMovementNoise();
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateMovementNoise()
    {
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;

        HorizontalSpeed = horizontalVelocity.magnitude;
        IsMoving = controller.isGrounded && HorizontalSpeed > movementNoiseThreshold;

        if (!emitMovementNoise || !IsMoving)
        {
            distanceUntilNextStep = 0f;
            sprintPulseTimer = 0f;
            return;
        }

        float stepDistance = GetCurrentStepDistance();
        if (stepDistance <= 0.01f)
            return;

        distanceUntilNextStep += HorizontalSpeed * Time.deltaTime;

        while (distanceUntilNextStep >= stepDistance)
        {
            distanceUntilNextStep -= stepDistance;
            EmitMovementNoise();
        }

        UpdateSprintPulseNoise();
    }

    private float GetCurrentStepDistance()
    {
        if (IsSneaking)
            return sneakStepDistance;

        if (IsSprinting)
            return sprintStepDistance;

        return walkStepDistance;
    }

    private void EmitMovementNoise()
    {
        float radius;
        float strength;

        if (IsSneaking)
        {
            radius = sneakNoiseRadius;
            strength = sneakNoiseStrength;
        }
        else if (IsSprinting)
        {
            radius = sprintNoiseRadius;
            strength = sprintNoiseStrength;
        }
        else
        {
            radius = walkNoiseRadius;
            strength = walkNoiseStrength;
        }

        if (radius <= 0f || strength <= 0f)
            return;

        Vector3 noisePosition = transform.position + Vector3.up * noiseSourceHeight;
        NoiseSystem.Emit(new NoiseEventData(
            noisePosition,
            radius,
            strength,
            gameObject,
            maxNoiseBounces,
            noiseBounceDamping));
    }

    private void UpdateSprintPulseNoise()
    {
        if (!IsSprinting)
        {
            sprintPulseTimer = 0f;
            return;
        }

        sprintPulseTimer -= Time.deltaTime;
        if (sprintPulseTimer > 0f)
            return;

        sprintPulseTimer = sprintNoisePulseInterval;
        EmitNoisePulse(sprintNoisePulseRadius, sprintNoisePulseStrength);
    }

    private void EmitNoisePulse(float radius, float strength)
    {
        if (radius <= 0f || strength <= 0f)
            return;

        Vector3 noisePosition = transform.position + Vector3.up * noiseSourceHeight;
        NoiseSystem.Emit(new NoiseEventData(
            noisePosition,
            radius,
            strength,
            gameObject,
            maxNoiseBounces,
            noiseBounceDamping));
    }
}

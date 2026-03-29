using UnityEngine;

public class SimpleFpsController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;

    [Header("Move")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookUp = 85f;

    private float _yVelocity;
    private float _pitch;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main) cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!controller || !cameraTransform) return;

        // --- Look (мышь) ---
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mx, 0f);

        _pitch -= my;
        _pitch = Mathf.Clamp(_pitch, -maxLookUp, maxLookUp);
        cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // --- Move (WASD) ---
        float x = Input.GetAxis("Horizontal"); // A/D
        float z = Input.GetAxis("Vertical");   // W/S

        Vector3 move = (transform.right * x + transform.forward * z) * speed;

        // --- Gravity ---
        if (controller.isGrounded && _yVelocity < 0f)
            _yVelocity = -2f; // небольшое прижатие к земле

        _yVelocity += gravity * Time.deltaTime;
        move.y = _yVelocity;

        controller.Move(move * Time.deltaTime);
    }
}
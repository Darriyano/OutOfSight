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

    private float yVelocity;
    private float pitch;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();

        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (controller == null || cameraTransform == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);

        pitch = Mathf.Clamp(pitch - mouseY, -maxLookUp, maxLookUp);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * horizontal + transform.forward * vertical) * speed;

        if (controller.isGrounded && yVelocity < 0f)
            yVelocity = -2f;

        yVelocity += gravity * Time.deltaTime;
        move.y = yVelocity;

        controller.Move(move * Time.deltaTime);
    }
}

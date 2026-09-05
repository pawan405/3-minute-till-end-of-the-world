using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    public float distance = 6f;
    public float height = 3f;

    public float mouseSensitivity = 3f;

    private float yaw = 0f;
    private float pitch = 15f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Mouse movement
        if (Mouse.current != null)
        {
            yaw += Mouse.current.delta.x.ReadValue() * mouseSensitivity * 0.1f;
            pitch -= Mouse.current.delta.y.ReadValue() * mouseSensitivity * 0.1f;
        }

        pitch = Mathf.Clamp(pitch, -20f, 60f);

        // Camera rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // Camera position
        Vector3 targetPosition =
            target.position + Vector3.up * height;

        Vector3 cameraPosition =
            targetPosition - rotation * Vector3.forward * distance;

        transform.position = cameraPosition;
        transform.rotation = rotation;
    }
}
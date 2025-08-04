using UnityEngine;

public class RTSCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;
    public float zoomSpeed = 500f;
    public float rotationSpeed = 5f;
    public float dragSpeed = 2f;

    [Header("Zoom")]
    public float minZoom = 3f;
    public float maxZoom = 150f;

    private Vector3 lastMousePosition;

    void Update()
    {
        HandleMovementInput();
        HandleZoom();
        HandleMouseDrag();
        HandleRotation();
    }

    void HandleMovementInput()
    {
        Vector3 direction = new Vector3();

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) direction += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) direction -= transform.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) direction -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) direction += transform.right;

        direction.y = 0; // Keep it level
        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Vector3 pos = transform.position;
        pos += transform.forward * scroll * zoomSpeed * Time.deltaTime;

        float dist = Vector3.Distance(pos, Vector3.zero);
        if (dist > minZoom && dist < maxZoom)
        {
            transform.position = pos;
        }
    }

    void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(2)) // Middle mouse
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * dragSpeed * Time.deltaTime;

            transform.Translate(move, Space.Self);

            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            float yaw = delta.x * rotationSpeed * Time.deltaTime;
            float pitch = -delta.y * rotationSpeed * Time.deltaTime;

            // Rotate around global Y axis (yaw)
            transform.Rotate(Vector3.up, yaw, Space.World);

            // Rotate around local X axis (pitch)
            transform.Rotate(Vector3.right, pitch, Space.Self);

            // Clamp pitch manually (see below)
            Vector3 currentAngles = transform.eulerAngles;
            currentAngles.z = 0; // prevent roll
            float clampedX = ClampAngle(currentAngles.x, 20f, 80f); // adjust limits as needed
            transform.eulerAngles = new Vector3(clampedX, currentAngles.y, 0);

            lastMousePosition = Input.mousePosition;
        }
    }

    float ClampAngle(float angle, float min, float max)
    {
        if (angle < 90 || angle > 270)
        {
            // Clamp between 0-180
            if (angle > 180) angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }
        // Clamp between 180-360
        return angle;
    }
}

using UnityEngine;

public class MouseCameraMovement: MonoBehaviour
{
    [Header("Pan Settings")]
    public float panSpeed = 2f;
    public bool invertPanX = false;
    public bool invertPanY = false;

    [Header("Zoom Settings")]
    public float zoomSpeed = 1f;
    public float minZoom = 1f;
    public float maxZoom = 20f;

    [Header("Mouse Settings")]
    public int panMouseButton = 0; // 0 = Left, 1 = Right, 2 = Middle

    private Camera cam;
    private Vector3 lastMousePosition;
    private bool isPanning = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Ensure camera is orthographic
        if (!cam.orthographic)
        {
            Debug.LogWarning("Camera is not orthographic. Setting to orthographic mode.");
            cam.orthographic = true;
        }
    }

    void Update()
    {
        HandlePanning();
        HandleZooming();
    }

    void HandlePanning()
    {
        // Start panning
        if (Input.GetMouseButtonDown(panMouseButton))
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }

        // Continue panning
        if (isPanning && Input.GetMouseButton(panMouseButton))
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 deltaMousePosition = currentMousePosition - lastMousePosition;

            // Only pan if there's actual mouse movement
            if (deltaMousePosition.magnitude > 0.01f)
            {
                // Convert screen delta to world delta based on orthographic size
                float worldUnitsPerPixel = (cam.orthographicSize * 2f) / cam.pixelHeight;

                float panX = deltaMousePosition.x * worldUnitsPerPixel * panSpeed * (invertPanX ? 1f : -1f);
                float panY = deltaMousePosition.y * worldUnitsPerPixel * panSpeed * (invertPanY ? 1f : -1f);

                // Move camera in local space
                transform.Translate(-panX, -panY, 0, Space.Self);
            }

            lastMousePosition = currentMousePosition;
        }

        // Stop panning
        if (Input.GetMouseButtonUp(panMouseButton))
        {
            isPanning = false;
        }
    }
    void HandleZooming()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            // Calculate new orthographic size
            float newSize = cam.orthographicSize - (scrollInput * zoomSpeed);

            // Clamp the size within min/max bounds
            newSize = Mathf.Clamp(newSize, minZoom, maxZoom);

            cam.orthographicSize = newSize;
        }
    }

    // Optional: Reset camera to default position and zoom
    public void ResetCamera()
    {
        transform.position = new Vector3(0, 0, transform.position.z);
        cam.orthographicSize = 5f; // Default Unity orthographic size
    }
}
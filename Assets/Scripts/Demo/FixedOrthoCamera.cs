using UnityEngine;

public class FixedOrthoCamera : MonoBehaviour
{
    [SerializeField] private Camera orthoCamera;
    [SerializeField] private float targetWidth = 20f; // Your desired world units width

    private void Start()
    {
        if (orthoCamera == null)
            orthoCamera = GetComponent<Camera>();

        UpdateCameraSize();
    }

    private void Update()
    {
        UpdateCameraSize();
    }

    private void UpdateCameraSize()
    {
        float targetAspect = targetWidth / (targetWidth / orthoCamera.aspect);
        orthoCamera.orthographicSize = targetWidth / (2f * orthoCamera.aspect);
    }
}
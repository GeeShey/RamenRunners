using UnityEngine;

public class Clicker : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float rayDistance = 100f;
    public LayerMask layerMask = -1; // Cast against all layers by default

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("No camera component found on " + gameObject.name);
        }
    }

    void Update()
    {
        // Cast ray on mouse click
        if (Input.GetMouseButtonDown(0))
        {
            CastRayFromMouse();
        }
    }

    void CastRayFromMouse()
    {
        Vector3 mousePos = Input.mousePosition;
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, layerMask))
        {
            if (hit.collider.CompareTag("Worker"))
            {
                OnWorkerHit(hit.collider.gameObject);
            }
        }
    }

    // Called when a Worker is hit
    void OnWorkerHit(GameObject worker)
    {
        string workerName = worker.name;
        BaseWorker clickedWorker = KitchenManager.instance.workers.Find(worker => worker.gameObject.name == workerName);
        clickedWorker.ReceiveBonusReduction();
    }

    // Public method to cast ray from any screen position
    public bool CastRayAtScreenPosition(Vector2 screenPos, out GameObject hitWorker)
    {
        hitWorker = null;
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, layerMask))
        {
            if (hit.collider.CompareTag("Worker"))
            {
                hitWorker = hit.collider.gameObject;
                return true;
            }
        }
        return false;
    }
}
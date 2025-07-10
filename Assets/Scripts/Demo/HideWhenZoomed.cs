using UnityEngine;

public class HideWhenZoomed : MonoBehaviour
{
    private Camera cam;
    private MeshRenderer meshRenderer;
    void Start()
    {
        cam = Camera.main;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (cam.orthographicSize < 8)
        {
            meshRenderer.enabled = false;
        }
        else 
        {
            if(!meshRenderer.enabled)
            meshRenderer.enabled = true;
        }
    }
}

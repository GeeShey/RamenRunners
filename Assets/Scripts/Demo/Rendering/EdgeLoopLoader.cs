using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class EdgeLoopLoader : MonoBehaviour
{
    [Header("Text file containing vertex positions")]
    public TextAsset vertexDataFile;

    private LineRenderer lineRenderer;  

    void Start()
    {
        loadVertices();
    }

    public void loadVertices()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (vertexDataFile == null || lineRenderer == null)
        {
            Debug.LogError("Missing reference: Please assign both TextAsset and LineRenderer.");
            return;
        }

        List<Vector3> positions = new List<Vector3>();
        string[] lines = vertexDataFile.text.Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Trim().Split(',');
            if (parts.Length == 3 &&
                float.TryParse(parts[0], out float x) &&
                float.TryParse(parts[1], out float y) &&
                float.TryParse(parts[2], out float z))
            {
                positions.Add(new Vector3(x, y, z));
            }
        }

        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }
}

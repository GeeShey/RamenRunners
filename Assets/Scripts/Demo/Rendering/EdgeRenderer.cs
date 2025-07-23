using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent (typeof(LineRenderer))]
public class EdgeRenderer : MonoBehaviour
{

    Mesh mesh;
    Vector3[] vertices;
    LineRenderer lineRenderer;
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = vertices.Length+1;
        List<Vector3> vertexList = vertices.ToList();
        vertexList.Add(vertices[0]);
        SwapThirdAndFourthVertices(vertexList);
        lineRenderer.SetPositions(vertexList.ToArray());


    }

    public static void SwapThirdAndFourthVertices(List<Vector3> vertices)
    {
        // Process vertices in groups of 4
        for (int i = 0; i < vertices.Count; i += 4)
        {
            // Make sure we have at least 4 vertices remaining
            if (i + 3 < vertices.Count)
            {
                // Swap 3rd and 4th elements in current group (indices i+2 and i+3)
                Vector3 temp = vertices[i + 2];
                vertices[i + 2] = vertices[i + 3];
                vertices[i + 3] = temp;
            }
        }
    }
}
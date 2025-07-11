using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Splines;

public class MovementManager : MonoBehaviour
{
    public static MovementManager instance;
    public List<Vector3> allLocations; // this will store the locations in worldspace

    [Header("Special Stations")]
    public Transform orderInStation;
    public Transform checkoutStation;

    [Header("Debug Settings")]
    public bool debugMode = false;
    public Material debugCubeMaterial;
    public float cubeSize = 1f;

    [Header("Path Settings")]
    public Material pathMaterial;
    public float pathWidth = 0.1f;

    [Header("Generated Paths (Debug)")]
    [SerializeField] private List<string> generatedPaths = new List<string>();

    public Dictionary<Vector3, Dictionary<Vector3, GameObject>> pathObjects; // stores path gameobjects
    private List<Transform> allStations;
    private List<GameObject> debugCubes;

    void Start()
    {
        instance = this;
        pathObjects = new Dictionary<Vector3, Dictionary<Vector3, GameObject>>();
        allStations = new List<Transform>();
        allLocations = new List<Vector3>();
        debugCubes = new List<GameObject>();
    }

    public void RegisterLocations(Transform[] locations)
    {
        for (int i = 0; i < locations.Length; i++)
        {
            allLocations.Add(locations[i].position);
            allStations.Add(locations[i]);
        }

        if (debugMode)
        {
            CreateDebugCubes();
        }
        GenerateAllPaths();

    }

    public void RegisterLocations(Transform location)
    {
        allLocations.Add(location.position);
        allStations.Add(location);

        if (debugMode)
        {
            CreateDebugCubes();
        }
        GenerateAllPaths();
    }

    private void GenerateAllPaths()
    {
        // Clear existing paths
        ClearAllPaths();

        // Clear the debug list
        generatedPaths.Clear();

        // Generate paths from order-in station to all other stations
        if (orderInStation != null)
        {
            GeneratePathsFromStation(orderInStation);
        }

        // Generate paths from checkout station to all other stations
        if (checkoutStation != null)
        {
            GeneratePathsFromStation(checkoutStation);
        }

        // Generate paths from every station to every other station
        for (int i = 0; i < allStations.Count; i++)
        {
            for (int j = 0; j < allStations.Count; j++)
            {
                if (i != j) // Don't create path to itself
                {
                    CreatePathBetweenStations(allStations[i], allStations[j]);
                }
            }
        }
    }

    private void GeneratePathsFromStation(Transform startStation)
    {
        for (int i = 0; i < allStations.Count; i++)
        {
            if (allStations[i] != startStation)
            {
                CreatePathBetweenStations(startStation, allStations[i]);
            }
        }
    }

    private void CreatePathBetweenStations(Transform startStation, Transform endStation)
    {
        Vector3 startPos = startStation.position;
        Vector3 endPos = endStation.position;

        // Check if path already exists
        if (pathObjects.ContainsKey(startPos) && pathObjects[startPos].ContainsKey(endPos) ||
            pathObjects.ContainsKey(endPos) && pathObjects[endPos].ContainsKey(startPos))
        {
            return; // Path already exists
        }

        // Add to debug list
        string pathName = $"{startStation.name} -> {endStation.name}";
        generatedPaths.Add(pathName);

        // Create path gameobject
        GameObject pathObj = new GameObject($"Path_{startStation.name}_to_{endStation.name}");
        pathObj.transform.SetParent(this.transform);

        // Add Unity Spline Container
        SplineContainer splineContainer = pathObj.AddComponent<SplineContainer>();
        CreateSplinePath(splineContainer, startPos, endPos);

        // Add line renderer for visualization
        if (debugMode)
        {
            LineRenderer lineRenderer = pathObj.AddComponent<LineRenderer>();
            SetupLineRenderer(lineRenderer, splineContainer);
        }

        // Store path object in dictionary
        if (!pathObjects.ContainsKey(startPos))
        {
            pathObjects[startPos] = new Dictionary<Vector3, GameObject>();
        }
        pathObjects[startPos][endPos] = pathObj;
    }

    private void CreateSplinePath(SplineContainer splineContainer, Vector3 startPos, Vector3 endPos)
    {
        // Create a new spline
        Spline spline = splineContainer.Spline;

        // Clear existing knots
        spline.Clear();

        // Add only two knots for a straight line
        spline.Add(new BezierKnot(startPos));
        spline.Add(new BezierKnot(endPos));

        // Set the spline to closed = false for paths
        spline.Closed = false;
    }

    private void SetupLineRenderer(LineRenderer lineRenderer, SplineContainer splineContainer)
    {
        lineRenderer.material = pathMaterial;
        lineRenderer.startWidth = pathWidth;
        lineRenderer.endWidth = pathWidth;
        lineRenderer.useWorldSpace = true;

        // Sample points along the spline for visualization
        int sampleCount = 20; // Number of points to sample along the spline
        lineRenderer.positionCount = sampleCount;

        Spline spline = splineContainer.Spline;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float3 position = spline.EvaluatePosition(t);
            lineRenderer.SetPosition(i, position);
        }
    }

    private void ClearAllPaths()
    {
        foreach (var startPaths in pathObjects.Values)
        {
            foreach (var pathObj in startPaths.Values)
            {
                if (pathObj != null)
                {
                    DestroyImmediate(pathObj);
                }
            }
        }
        pathObjects.Clear();
    }

    private void CreateDebugCubes()
    {
        // Clear existing debug cubes
        ClearDebugCubes();

        // Create cube for each location using the stored Vector3 positions
        for (int i = 0; i < allLocations.Count; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Get the station name if available, otherwise use index
            string stationName = i < allStations.Count ? allStations[i].name : $"Station{i}";
            cube.name = $"DebugCube_{stationName}";

            // Use the stored world position from allLocations (not the current transform position)
            cube.transform.position = allLocations[i];
            cube.transform.localScale = Vector3.one * cubeSize;

            // Don't parent to avoid any transform issues
            // cube.transform.SetParent(this.transform, true);

            // Set material if provided
            if (debugCubeMaterial != null)
            {
                cube.GetComponent<Renderer>().material = debugCubeMaterial;
            }

            debugCubes.Add(cube);
        }
    }

    private void ClearDebugCubes()
    {
        for (int i = 0; i < debugCubes.Count; i++)
        {
            if (debugCubes[i] != null)
            {
                DestroyImmediate(debugCubes[i]);
            }
        }
        debugCubes.Clear();
    }

    public GameObject GetPathObject(StationId from, StationId to, ref bool isReversed)
    {
        //get the free slot from both the stations
        Station fromStation = KitchenManager.instance.GetStation(from);
        Station toStation = KitchenManager.instance.GetStation(to);

        Vector3 startPos = fromStation.GetAvailableStandingLocation().position;
        Vector3 endPos = toStation.GetAvailableStandingLocation().position; // Fixed: was using fromStation twice

        //get the path
        if (pathObjects.ContainsKey(startPos) && pathObjects[startPos].ContainsKey(endPos))
        {
            isReversed = false;
            return pathObjects[startPos][endPos];
        }
        else if (pathObjects.ContainsKey(endPos) && pathObjects[endPos].ContainsKey(startPos))
        {
            isReversed = true;
            return pathObjects[endPos][startPos]; // Fixed: was using wrong key
        }
        else
        {
            return null;
        }
    }

    // Helper method to get position along a spline at parameter t (0 to 1)
    public Vector3 GetPositionOnSpline(GameObject pathObject, float t)
    {
        SplineContainer splineContainer = pathObject.GetComponent<SplineContainer>();
        if (splineContainer != null)
        {
            return splineContainer.Spline.EvaluatePosition(t);
        }
        return Vector3.zero;
    }

    // Helper method to get tangent direction along a spline at parameter t (0 to 1)
    public Vector3 GetTangentOnSpline(GameObject pathObject, float t)
    {
        SplineContainer splineContainer = pathObject.GetComponent<SplineContainer>();
        if (splineContainer != null)
        {
            return splineContainer.Spline.EvaluateTangent(t);
        }
        return Vector3.forward;
    }

    // Helper method to get the total length of a spline
    public float GetSplineLength(GameObject pathObject)
    {
        SplineContainer splineContainer = pathObject.GetComponent<SplineContainer>();
        if (splineContainer != null)
        {
            return splineContainer.Spline.GetLength();
        }
        return 0f;
    }

    void OnDestroy()
    {
        ClearAllPaths();
        ClearDebugCubes();
    }

    // Method to toggle debug mode at runtime
    public void ToggleDebugMode()
    {
        debugMode = !debugMode;

        if (debugMode)
        {
            ClearAllPaths();
            CreateDebugCubes();
        }
        else
        {
            ClearDebugCubes();
            GenerateAllPaths();
        }
    }

    // Method to clear everything (useful for inspector button)
    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        ClearAllPaths();
        ClearDebugCubes();
        generatedPaths.Clear();
    }
}
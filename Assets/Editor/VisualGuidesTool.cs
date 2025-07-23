using UnityEngine;
using UnityEditor;

public class VisualGuidesTool : EditorWindow
{
    private bool showVisualGuides = true;
    private int visualGuidesCount = 0;
    private GameObject[] visualGuideObjects;
    private int visualGuidesLayer;

    [MenuItem("Tools/Visual Guides Toggle &g")]
    public static void ShowWindow()
    {
        GetWindow<VisualGuidesTool>("Visual Guides");
    }

    private void OnEnable()
    {
        FindAllVisualGuides();
    }

    private void OnGUI()
    {
        GUILayout.Label("Visual Guides Control", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // Info section
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Found objects: {visualGuidesCount}");
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            FindAllVisualGuides();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Toggle controls
        EditorGUILayout.BeginHorizontal();

        bool newShowState = EditorGUILayout.Toggle("Show Visual Guides", showVisualGuides);
        if (newShowState != showVisualGuides)
        {
            showVisualGuides = newShowState;
            SetVisualGuidesVisibility(showVisualGuides);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Button controls
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Show All"))
        {
            showVisualGuides = true;
            SetVisualGuidesVisibility(true);
        }

        if (GUILayout.Button("Hide All"))
        {
            showVisualGuides = false;
            SetVisualGuidesVisibility(false);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Help text
        EditorGUILayout.HelpBox("Keyboard shortcut: Alt+G to toggle\nMake sure objects are on the 'VisualGuides' layer", MessageType.Info);
    }

    private void FindAllVisualGuides()
    {
        visualGuidesLayer = LayerMask.NameToLayer("VisualGuides");

        if (visualGuidesLayer == -1)
        {
            Debug.LogError("Layer 'VisualGuides' not found! Please create this layer in the Layer settings.");
            visualGuidesCount = 0;
            return;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        System.Collections.Generic.List<GameObject> visualGuides = new System.Collections.Generic.List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == visualGuidesLayer)
            {
                visualGuides.Add(obj);
            }
        }

        visualGuideObjects = visualGuides.ToArray();
        visualGuidesCount = visualGuideObjects.Length;

        Debug.Log($"Found {visualGuidesCount} objects with VisualGuides layer");
    }

    private void ToggleVisualGuides()
    {
        showVisualGuides = !showVisualGuides;
        SetVisualGuidesVisibility(showVisualGuides);
        Repaint(); // Update the window UI
    }

    private void SetVisualGuidesVisibility(bool visible)
    {
        if (visualGuideObjects == null)
        {
            FindAllVisualGuides();
        }

        foreach (GameObject obj in visualGuideObjects)
        {
            if (obj != null)
            {
                obj.GetComponent<MeshRenderer>().enabled = visible;
            }
        }

        Debug.Log($"Visual guides {(visible ? "shown" : "hidden")} - {visualGuidesCount} objects affected");
    }
}
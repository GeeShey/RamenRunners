using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineVisualizer))]
public class SplineVisualizerEditor : Editor
{
    private SplineVisualizer visualizer;

    private void OnEnable()
    {
        visualizer = (SplineVisualizer)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // Timed Animation Section (only show in play mode)
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Timed Animation (Play Mode Only)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Animate 2s", GUILayout.Height(25)))
            {
                visualizer.AnimateAlongPath(2f, () => Debug.Log("Animation completed!"));
            }

            if (GUILayout.Button("Animate 5s", GUILayout.Height(25)))
            {
                visualizer.AnimateAlongPath(5f, () => Debug.Log("Animation completed!"));
            }

            if (GUILayout.Button("Animate 10s", GUILayout.Height(25)))
            {
                visualizer.AnimateAlongPath(10f, () => Debug.Log("Animation completed!"));
            }

            EditorGUILayout.EndHorizontal();

            // Custom duration input
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Custom Duration:", GUILayout.Width(100));

            if (!EditorPrefs.HasKey("SplineVisualizerCustomDuration"))
                EditorPrefs.SetFloat("SplineVisualizerCustomDuration", 3f);

            float customDuration = EditorPrefs.GetFloat("SplineVisualizerCustomDuration");
            customDuration = EditorGUILayout.FloatField(customDuration, GUILayout.Width(60));
            EditorPrefs.SetFloat("SplineVisualizerCustomDuration", customDuration);

            if (GUILayout.Button($"Animate {customDuration}s", GUILayout.Height(25)))
            {
                visualizer.AnimateAlongPath(customDuration, () => Debug.Log($"Custom animation ({customDuration}s) completed!"));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }
        else
        {
            EditorGUILayout.HelpBox("Timed Animation features are only available in Play Mode. Use the controls above for Edit Mode visualization.", MessageType.Info);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Editor Visualization Controls", EditorStyles.boldLabel);

        // Show current mode
        string mode = Application.isPlaying ? "Play Mode" : "Edit Mode";
        EditorGUILayout.LabelField($"Current Mode: {mode}", EditorStyles.helpBox);

        EditorGUILayout.Space(5);

        // Create a horizontal layout for the main buttons
        EditorGUILayout.BeginHorizontal();

        // Start/Stop button
        if (visualizer.IsMoving)
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Stop", GUILayout.Height(30)))
            {
                visualizer.StopVisualization();
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start", GUILayout.Height(30)))
            {
                visualizer.StartVisualization();
            }
            GUI.backgroundColor = Color.white;
        }

        // Reset button
        if (GUILayout.Button("Reset", GUILayout.Height(30)))
        {
            visualizer.ResetVisualization();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Progress slider
        EditorGUILayout.LabelField($"Progress: {visualizer.Progress:P1}");
        EditorGUI.BeginChangeCheck();
        float newProgress = EditorGUILayout.Slider("Manual Control", visualizer.Progress, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            visualizer.SetProgress(newProgress);
            visualizer.StopVisualization(); // Stop automatic movement when manually controlling
            SceneView.RepaintAll(); // Update scene view immediately
        }

        EditorGUILayout.Space(5);

        // Runtime property controls
        EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        float newSpeed = EditorGUILayout.FloatField("Speed", visualizer.Speed);
        if (EditorGUI.EndChangeCheck())
        {
            visualizer.Speed = newSpeed;
        }

        EditorGUI.BeginChangeCheck();
        bool newLoop = EditorGUILayout.Toggle("Loop", visualizer.Loop);
        if (EditorGUI.EndChangeCheck())
        {
            visualizer.Loop = newLoop;
        }

        EditorGUI.BeginChangeCheck();
        bool newLookForward = EditorGUILayout.Toggle("Look Forward", visualizer.LookForward);
        if (EditorGUI.EndChangeCheck())
        {
            visualizer.LookForward = newLookForward;
        }

        EditorGUILayout.Space(10);

        // Info section
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Is Moving: {(visualizer.IsMoving ? "Yes" : "No")}");

        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"Is Animating: {(visualizer.IsAnimating ? "Yes" : "No")}");
        }

        EditorGUILayout.LabelField($"Current Progress: {visualizer.Progress:F3}");

        // Quick setup buttons
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Test Cube Visualizer"))
        {
            CreateTestVisualizer();
        }

        // Force repaint to keep UI updated
        if (visualizer.IsMoving)
        {
            Repaint();
        }
    }

    private void CreateTestVisualizer()
    {
        // Create a simple cube prefab for testing
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = Vector3.one * 0.3f;

        // Give it a bright color
        var renderer = cube.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.red;
        mat.SetFloat("_Metallic", 0.5f);
        mat.SetFloat("_Smoothness", 0.8f);
        renderer.material = mat;

        // Create as prefab asset
        string path = "Assets/SplineVisualizerCube.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cube, path);

        // Assign to visualizer
        SerializedObject so = new SerializedObject(visualizer);
        so.FindProperty("visualizerPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedProperties();

        // Clean up temporary object
        DestroyImmediate(cube);

        Debug.Log($"Created test visualizer prefab at {path} and assigned to SplineVisualizer");
    }
}
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class HierarchyHighlighterTool : EditorWindow
{
    private List<HierarchyHighlights> highlightElements = new List<HierarchyHighlights>();
    private Vector2 scrollPosition;
    private string saveFilePath;


    [MenuItem("Tools/Hierarchy Highlights Editor")]
    public static void ShowWindow()
    {
        GetWindow<HierarchyHighlighterTool>("Hierarchy Highlights");
    }

    private void OnEnable()
    {
        saveFilePath = Path.Combine(Application.dataPath, "Editor", "HierarchyHighlights.json");
        LoadHighlights();
        ApplyToHierarchy();
    }
    private void SaveHighlights()
    {
        try
        {
            // Ensure the Editor directory exists
            string directory = Path.GetDirectoryName(saveFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            HighlightElementsData data = new HighlightElementsData();
            data.elements = new List<SerializableHighlight>();

            foreach (var element in highlightElements)
            {
                data.elements.Add(new SerializableHighlight(element));
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveFilePath, json);

            Debug.Log($"Hierarchy highlights saved to: {saveFilePath}");
            EditorUtility.DisplayDialog("Save Successful", "Highlight elements have been saved successfully!", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save highlights: {e.Message}");
            EditorUtility.DisplayDialog("Save Failed", $"Failed to save highlights:\n{e.Message}", "OK");
        }
    }
    private void LoadHighlights()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                HighlightElementsData data = JsonUtility.FromJson<HighlightElementsData>(json);

                highlightElements.Clear();
                foreach (var serializableElement in data.elements)
                {
                    highlightElements.Add(serializableElement.ToHierarchyHighlight());
                }

                Debug.Log("Hierarchy highlights loaded successfully!");
                Repaint();
            }
            else
            {
                Debug.Log("No saved highlights file found. Starting with empty list.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load highlights: {e.Message}");
            EditorUtility.DisplayDialog("Load Failed", $"Failed to load highlights:\n{e.Message}", "OK");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Hierarchy Highlights Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Add new element button
        if (GUILayout.Button("Add New Highlight Element"))
        {
            highlightElements.Add(new HierarchyHighlights
            {
                label = "New Element",
                backgroundColor = Color.yellow,
                textColor = Color.black
            });
        }

        EditorGUILayout.Space();

        // Scroll view for elements
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < highlightElements.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Element {i + 1}", EditorStyles.boldLabel);

            // Create a temporary struct to modify
            HierarchyHighlights element = highlightElements[i];

            element.label = EditorGUILayout.TextField("Object Name:", element.label);
            element.backgroundColor = EditorGUILayout.ColorField("Background Color:", element.backgroundColor);
            element.textColor = EditorGUILayout.ColorField("Text Color:", element.textColor);

            // Update the list with modified values
            highlightElements[i] = element;

            EditorGUILayout.Space();

            // Remove button
            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                highlightElements.RemoveAt(i);
                i--; // Adjust index after removal
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Save/Load buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Highlights"))
        {
            SaveHighlights();
        }

        if (GUILayout.Button("Load Highlights"))
        {
            LoadHighlights();
        }

        if (GUILayout.Button("Apply to Hierarchy"))
        {
            ApplyToHierarchy();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Save Location: {saveFilePath}", EditorStyles.miniLabel);
    }

    private void ApplyToHierarchy()
    {
        HierarchyColor.higlightElements = new List<HierarchyHighlights>(highlightElements);
        EditorApplication.RepaintHierarchyWindow();
        Debug.Log($"Applied {highlightElements.Count} highlight elements to hierarchy.");
        //EditorUtility.DisplayDialog("Applied Successfully", $"Applied {highlightElements.Count} highlight elements to the hierarchy!", "OK");
    }


}


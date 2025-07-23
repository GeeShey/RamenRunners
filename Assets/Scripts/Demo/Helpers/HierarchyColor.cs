using System.Collections.Generic;
using UnityEngine;

using System.IO;


#if UNITY_EDITOR
using UnityEditor;

public struct HierarchyHighlights
{
    public string label;
    public Color backgroundColor;
    public Color textColor;
}

/// <summary> Sets a background color for game objects in the Hierarchy tab</summary>
[UnityEditor.InitializeOnLoad]
#endif
public class HierarchyColor
{
    private static Vector2 offset = new Vector2(20, 1);
    public static List<HierarchyHighlights> higlightElements;
    public static HierarchyColor instance;
    static string saveFilePath;


    static HierarchyColor()
    {
        higlightElements = new List<HierarchyHighlights>();
        EditorApplication.delayCall += LoadAndApplyHighlights;
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;

    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        //get all the 
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj != null)
        {
            Color backgroundColor = Color.white;
            Color textColor = Color.white;
            Texture2D texture = null;

            for (int i = 0; i < higlightElements.Count; i++)
            {
                if (obj.name == higlightElements[i].label)
                {
                    backgroundColor = higlightElements[i].backgroundColor;
                    textColor = higlightElements[i].textColor;
                }
            }

            if (backgroundColor != Color.white)
            {
                Rect offsetRect = new Rect(selectionRect.position + offset, selectionRect.size);
                Rect bgRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width + 50, selectionRect.height);

                EditorGUI.DrawRect(bgRect, backgroundColor);
                EditorGUI.LabelField(offsetRect, obj.name, new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = textColor },
                    fontStyle = FontStyle.Bold
                }
                );

                if (texture != null)
                    EditorGUI.DrawPreviewTexture(new Rect(selectionRect.position, new Vector2(selectionRect.height, selectionRect.height)), texture);
            }
        }
    }

    private static void LoadAndApplyHighlights()
    {
        saveFilePath = Path.Combine(Application.dataPath, "Editor", "HierarchyHighlights.json");

        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                HighlightElementsData data = JsonUtility.FromJson<HighlightElementsData>(json);

                List<HierarchyHighlights> loaded = new List<HierarchyHighlights>();
                foreach (var serializableElement in data.elements)
                {
                    loaded.Add(serializableElement.ToHierarchyHighlight());
                }

                HierarchyColor.higlightElements = loaded;
                EditorApplication.RepaintHierarchyWindow();

                Debug.Log($"[HierarchyHighlightAutoLoader] Reapplied {loaded.Count} highlights after script reload.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HierarchyHighlightAutoLoader] Failed to load highlights after reload: {e.Message}");
            }
        }
    }
}


[System.Serializable]
public class HighlightElementsData
{
    public List<SerializableHighlight> elements = new List<SerializableHighlight>();
}

[System.Serializable]
public class SerializableHighlight
{
    public string label;
    public float backgroundColorR;
    public float backgroundColorG;
    public float backgroundColorB;
    public float backgroundColorA;
    public float textColorR;
    public float textColorG;
    public float textColorB;
    public float textColorA;

    public SerializableHighlight() { }

    public SerializableHighlight(HierarchyHighlights highlight)
    {
        label = highlight.label;
        backgroundColorR = highlight.backgroundColor.r;
        backgroundColorG = highlight.backgroundColor.g;
        backgroundColorB = highlight.backgroundColor.b;
        backgroundColorA = highlight.backgroundColor.a;
        textColorR = highlight.textColor.r;
        textColorG = highlight.textColor.g;
        textColorB = highlight.textColor.b;
        textColorA = highlight.textColor.a;
    }

    public HierarchyHighlights ToHierarchyHighlight()
    {
        return new HierarchyHighlights
        {
            label = label,
            backgroundColor = new Color(backgroundColorR, backgroundColorG, backgroundColorB, backgroundColorA),
            textColor = new Color(textColorR, textColorG, textColorB, textColorA)
        };
    }
}
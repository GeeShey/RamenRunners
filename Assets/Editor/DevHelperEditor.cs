using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DevHelper))]

public class DevHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Add some space
        EditorGUILayout.Space();

        // Get reference to the target script
        DevHelper script = (DevHelper)target;

        // Create the button
        if (GUILayout.Button("Init All Workers"))
        {
            script.initializeAllWorkers();
        }
    }
}

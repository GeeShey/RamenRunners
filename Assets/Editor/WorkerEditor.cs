using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Worker))]
public class WorkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Add some space
        EditorGUILayout.Space();

        // Get reference to the target script
        Worker worker = (Worker)target;

        // Create the button
        if (GUILayout.Button("Init Worker"))
        {
            worker.InitializeWorker();
        }
    }
}

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EdgeLoopLoader))]
public class EdgeLoopLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EdgeLoopLoader loader = (EdgeLoopLoader)target;

        if (GUILayout.Button("Load Vertices Into Line Renderer"))
        {
            loader.loadVertices();
        }
    }
}

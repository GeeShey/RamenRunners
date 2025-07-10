using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OrderManager))]
public class OrderManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Add some space
        EditorGUILayout.Space();

        // Get reference to the target script
        OrderManager orderManager = (OrderManager)target;

        // Create the button
        if (GUILayout.Button("Place Order"))
        {
            orderManager.PlaceOrder();
        }
    }
}
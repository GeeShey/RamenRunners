using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(CarManager))]
public class CarManagerEditor : Editor
{
    private CarManager carManager;
    private GUIStyle readOnlyStyle;
    private GUIStyle headerStyle;
    private GUIStyle slotStyle;

    public override void OnInspectorGUI()
    {

        InitializeStyles();
        // Draw the default inspector
        DrawDefaultInspector();

        // Add some space
        EditorGUILayout.Space();

        // Get reference to the target script
        carManager = (CarManager)target;

        // Create the button
        if (GUILayout.Button("Initialize Car with Order"))
        {
            carManager.InitializeNewCar();
        }

        // Add some space
        EditorGUILayout.Space(10);

        // Draw custom car slots visualization
        DrawCarSlotsVisualization();

        // Repaint to keep the inspector updated in real-time
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void InitializeStyles()
    {
        if (readOnlyStyle == null)
        {
            readOnlyStyle = new GUIStyle(GUI.skin.textField);
            readOnlyStyle.normal.textColor = Color.gray;
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.normal.textColor = Color.white;
        }

        if (slotStyle == null)
        {
            slotStyle = new GUIStyle(GUI.skin.label);
            slotStyle.fontSize = 11;
        }
    }

    private void DrawCarSlotsVisualization()
    {
        // Header
        EditorGUILayout.LabelField("Car Slots Visualization", headerStyle);

        EditorGUI.BeginDisabledGroup(true); // Make everything read-only

        // Draw Order Slots
        if (carManager.orderSlots != null && carManager.orderSlots.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Order Slots:", EditorStyles.boldLabel);

            for (int i = 0; i < carManager.orderSlots.Count; i++)
            {
                DrawSlotInfo($"Order Slot {i + 1}", carManager.orderSlots[i]);
            }
        }

        // Draw Pickup Slots
        if (carManager.pickupSlots != null && carManager.pickupSlots.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Pickup Slots:", EditorStyles.boldLabel);

            for (int i = 0; i < carManager.pickupSlots.Count; i++)
            {
                DrawSlotInfo($"Pickup Slot {i + 1}", carManager.pickupSlots[i]);
            }
        }

        // Draw waiting cars count
        if (carManager.carsWaitingToOrder != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Waiting Cars:", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Cars in Queue", carManager.carsWaitingToOrder.Count);
        }

        EditorGUI.EndDisabledGroup();
    }

    private void DrawSlotInfo(string slotName, CarSlot slot)
    {
        if (slot == null) return;

        EditorGUILayout.BeginHorizontal();

        // Slot name
        EditorGUILayout.LabelField(slotName, slotStyle, GUILayout.Width(100));

        // Occupied status with color coding
        Color originalColor = GUI.color;
        GUI.color = slot.occupied ? Color.red : Color.green;

        string statusText = slot.occupied ? "Occupied" : "Free";
        EditorGUILayout.TextField(statusText, readOnlyStyle, GUILayout.Width(80));

        GUI.color = originalColor;

        // Position info
        EditorGUILayout.Vector3Field("", slot.position);

        EditorGUILayout.EndHorizontal();
    }
}

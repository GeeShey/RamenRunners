using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemsHandler))]
public class ItemsHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ItemsHandler handler = (ItemsHandler)target;

        if (Application.isPlaying)
        {
            if (ItemFactory.Instance == null)
            {
                EditorGUILayout.HelpBox("ItemFactory.Instance is null", MessageType.Warning);
                return;
            }

            var itemList = ItemFactory.Instance.AllItemTypes;

            if (itemList == null || itemList.Count == 0)
            {
                EditorGUILayout.HelpBox("ItemFactory.AllItemTypes is empty. Did you call LoadItemDictionary()?", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Equip Items", EditorStyles.boldLabel);

            foreach (var itemName in itemList)
            {
                if (GUILayout.Button($"Add {itemName}"))
                {
                    handler.EquipItem(itemName);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to equip items.", MessageType.Info);
        }
    }
}

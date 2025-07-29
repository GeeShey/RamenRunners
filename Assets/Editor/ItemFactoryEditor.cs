using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemFactory))]
public class ItemFactoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ItemFactory factory = (ItemFactory)target;

        if (GUILayout.Button("Load Dictionary"))
        {
            factory.LoadDictionary();
        }
    }
}

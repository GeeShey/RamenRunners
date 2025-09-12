// Step 2: Create this in an "Editor" folder - "DictionaryPropertyDrawer.cs"
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(StringTransformDict))]
public class StringTransformDictDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get the keys and values properties
        var keysProperty = property.FindPropertyRelative("keys");
        var valuesProperty = property.FindPropertyRelative("values");

        // Draw the foldout
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                                               property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // Draw size field
            Rect sizeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
            int newSize = EditorGUI.IntField(sizeRect, "Size", keysProperty.arraySize);

            if (newSize != keysProperty.arraySize)
            {
                keysProperty.arraySize = newSize;
                valuesProperty.arraySize = newSize;
            }

            // Draw each key-value pair
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                float yPos = position.y + (i + 2) * EditorGUIUtility.singleLineHeight;
                Rect elementRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);

                // Split the rect for key and value
                float keyWidth = position.width * 0.4f;
                float valueWidth = position.width * 0.6f;

                Rect keyRect = new Rect(elementRect.x, elementRect.y, keyWidth - 5, elementRect.height);
                Rect valueRect = new Rect(elementRect.x + keyWidth, elementRect.y, valueWidth, elementRect.height);

                // Draw key field
                EditorGUI.PropertyField(keyRect, keysProperty.GetArrayElementAtIndex(i), GUIContent.none);

                // Draw value field  
                EditorGUI.PropertyField(valueRect, valuesProperty.GetArrayElementAtIndex(i), GUIContent.none);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        var keysProperty = property.FindPropertyRelative("keys");
        return EditorGUIUtility.singleLineHeight * (keysProperty.arraySize + 2);
    }
}

// You can create similar drawers for other dictionary types
[CustomPropertyDrawer(typeof(StringGameObjectDict))]
public class StringGameObjectDictDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var keysProperty = property.FindPropertyRelative("keys");
        var valuesProperty = property.FindPropertyRelative("values");

        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                                               property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            Rect sizeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
            int newSize = EditorGUI.IntField(sizeRect, "Size", keysProperty.arraySize);

            if (newSize != keysProperty.arraySize)
            {
                keysProperty.arraySize = newSize;
                valuesProperty.arraySize = newSize;
            }

            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                float yPos = position.y + (i + 2) * EditorGUIUtility.singleLineHeight;
                Rect elementRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);

                float keyWidth = position.width * 0.4f;
                float valueWidth = position.width * 0.6f;

                Rect keyRect = new Rect(elementRect.x, elementRect.y, keyWidth - 5, elementRect.height);
                Rect valueRect = new Rect(elementRect.x + keyWidth, elementRect.y, valueWidth, elementRect.height);

                EditorGUI.PropertyField(keyRect, keysProperty.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUI.PropertyField(valueRect, valuesProperty.GetArrayElementAtIndex(i), GUIContent.none);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        var keysProperty = property.FindPropertyRelative("keys");
        return EditorGUIUtility.singleLineHeight * (keysProperty.arraySize + 2);
    }
}
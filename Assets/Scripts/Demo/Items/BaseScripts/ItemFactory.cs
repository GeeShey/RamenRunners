using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class ItemFactory : MonoBehaviour
{
    public List<string> AllItemTypes = new List<string>();
    public Dictionary<string, Type> itemTypeDictionary = new Dictionary<string, Type>();
    public static ItemFactory instance;

    private void Start()
    {
        instance = this;
        LoadDictionary();
    }

    public void LoadDictionary()
    {
        itemTypeDictionary.Clear();
        AllItemTypes.Clear();

        // Scan all loaded assemblies for classes that inherit from Item
        var allItemTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Item)));

        foreach (var type in allItemTypes)
        {
            string key = type.Name; // You can customize this key if needed (e.g., add attribute)

            if (!itemTypeDictionary.ContainsKey(key))
            {
                itemTypeDictionary[key] = type;
                AllItemTypes.Add(key);
                Debug.Log($"Registered item type: {key}");
            }
        }
    }

    public Component AddItemTo(GameObject target, string itemName)
    {
        if (itemTypeDictionary.TryGetValue(itemName, out Type type))
        {
            if (target.GetComponent(type) == null)
            {
                Debug.Log($"Added {itemName} to {target.name}");
                return target.AddComponent(type);
            }
            else
            {
                Debug.LogWarning($"{target.name} already has a {itemName} component.");
            }
        }
        else
        {
            Debug.LogError($"Item type '{itemName}' not found in dictionary.");
        }
        return null;
    }
}

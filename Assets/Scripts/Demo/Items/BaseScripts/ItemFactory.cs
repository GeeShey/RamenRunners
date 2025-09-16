using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Factory class responsible for creating and managing item instances.
/// Uses reflection to automatically discover and register item types.
/// </summary>
public class ItemFactory : MonoBehaviour
{
    #region Inspector Fields
    [Header("Factory Configuration")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoLoadOnStart = true;
    #endregion

    #region Private Fields
    private Dictionary<string, Type> itemTypeDictionary = new Dictionary<string, Type>();
    private List<string> allItemTypes = new List<string>();
    #endregion

    #region Properties
    public static ItemFactory Instance { get; private set; }
    public IReadOnlyList<string> AllItemTypes => allItemTypes.AsReadOnly();
    public int RegisteredItemCount => itemTypeDictionary.Count;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        if (autoLoadOnStart)
        {
            LoadItemDictionary();
        }
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            LogDebug("Multiple ItemFactory instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public void LoadItemDictionary()
    {
        itemTypeDictionary.Clear();
        allItemTypes.Clear();

        // Scan all loaded assemblies for classes that inherit from Item
        var discoveredItemTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => IsValidItemType(type));

        foreach (var type in discoveredItemTypes)
        {
            RegisterItemType(type);
        }

        LogDebug($"Loaded {itemTypeDictionary.Count} item types");
    }

    private bool IsValidItemType(Type type)
    {
        return type.IsClass && 
               !type.IsAbstract && 
               type.IsSubclassOf(typeof(Item)) &&
               !type.IsGenericType;
    }

    private void RegisterItemType(Type type)
    {
        string key = GetItemTypeKey(type);

        if (!itemTypeDictionary.ContainsKey(key))
        {
            itemTypeDictionary[key] = type;
            allItemTypes.Add(key);
            LogDebug($"Registered item type: {key}");
        }
        else
        {
            LogDebug($"Item type {key} already registered, skipping duplicate.");
        }
    }

    private string GetItemTypeKey(Type type)
    {
        // Use the class name as the key
        // Could be extended to use custom attributes for different naming schemes
        return type.Name;
    }
    #endregion

    #region Item Creation
    public Item CreateItem(GameObject target, string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            LogDebug("Item name cannot be null or empty");
            return null;
        }

        if (target == null)
        {
            LogDebug("Target GameObject cannot be null");
            return null;
        }

        if (!itemTypeDictionary.TryGetValue(itemName, out Type itemType))
        {
            LogDebug($"Item type '{itemName}' not found in dictionary. Available types: {string.Join(", ", allItemTypes)}");
            return null;
        }

        return CreateItemComponent(target, itemType, itemName);
    }

    private Item CreateItemComponent(GameObject target, Type itemType, string itemName)
    {
        // Check if component already exists
        if (target.GetComponent(itemType) != null)
        {
            LogDebug($"{target.name} already has a {itemName} component.");
            return target.GetComponent(itemType) as Item;
        }

        // Add the component
        Component newComponent = target.AddComponent(itemType);
        Item newItem = newComponent as Item;

        if (newItem != null)
        {
            LogDebug($"Successfully created {itemName} on {target.name}");
        }
        else
        {
            LogDebug($"Failed to create {itemName} - component is not an Item");
            Destroy(newComponent);
        }

        return newItem;
    }
    #endregion

    #region Utility Methods
    public bool IsItemTypeRegistered(string itemName)
    {
        return itemTypeDictionary.ContainsKey(itemName);
    }

    public Type GetItemType(string itemName)
    {
        itemTypeDictionary.TryGetValue(itemName, out Type type);
        return type;
    }

    public List<string> GetAvailableItemTypes()
    {
        return new List<string>(allItemTypes);
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ItemFactory] {message}");
        }
    }
    #endregion
}

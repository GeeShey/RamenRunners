using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a utensil that can be equipped by workers.
/// Provides basic utensil functionality and properties.
/// </summary>
[Serializable]
public class Utensil
{
    #region Fields
    [SerializeField] private string utensilName;
    [SerializeField] private UtensilType utensilType;
    [SerializeField] private float efficiencyBonus = 0f;
    [SerializeField] private int durability = 100;
    [SerializeField] private int maxDurability = 100;
    #endregion

    #region Properties
    public string Name => utensilName;
    public UtensilType Type => utensilType;
    public float EfficiencyBonus => efficiencyBonus;
    public int Durability => durability;
    public int MaxDurability => maxDurability;
    public float DurabilityPercentage => (float)durability / maxDurability;
    public bool IsBroken => durability <= 0;
    #endregion

    #region Constructors
    public Utensil(string name, UtensilType type, float efficiencyBonus = 0f, int maxDurability = 100)
    {
        this.utensilName = name;
        this.utensilType = type;
        this.efficiencyBonus = efficiencyBonus;
        this.maxDurability = maxDurability;
        this.durability = maxDurability;
    }
    #endregion

    #region Durability Management
    public void UseUtensil(int wearAmount = 1)
    {
        durability = Mathf.Max(0, durability - wearAmount);
    }

    public void RepairUtensil(int repairAmount)
    {
        durability = Mathf.Min(maxDurability, durability + repairAmount);
    }

    public void RepairFully()
    {
        durability = maxDurability;
    }
    #endregion
}

/// <summary>
/// Types of utensils available in the game.
/// </summary>
public enum UtensilType
{
    Knife,
    Spatula,
    Tongs,
    Ladle,
    Whisk,
    CuttingBoard,
    Pan,
    Pot,
    Other
}

/// <summary>
/// Dictionary class for managing item-related data and configurations.
/// Provides centralized access to item definitions and utilities.
/// </summary>
public class ItemDictionary
{
    #region Static Fields
    private static ItemDictionary instance;
    private Dictionary<string, ItemBasicDetailsSO> itemDefinitions;
    private Dictionary<UtensilType, List<Utensil>> utensilTemplates;
    #endregion

    #region Properties
    public static ItemDictionary Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ItemDictionary();
            }
            return instance;
        }
    }
    #endregion

    #region Initialization
    private ItemDictionary()
    {
        InitializeItemDefinitions();
        InitializeUtensilTemplates();
    }

    private void InitializeItemDefinitions()
    {
        itemDefinitions = new Dictionary<string, ItemBasicDetailsSO>();
        
        // Load all ItemBasicDetailsSO assets from Resources
        var itemAssets = Resources.LoadAll<ItemBasicDetailsSO>("Items");
        
        foreach (var itemAsset in itemAssets)
        {
            if (!string.IsNullOrEmpty(itemAsset.ID))
            {
                itemDefinitions[itemAsset.ID] = itemAsset;
            }
        }
        
        Debug.Log($"[ItemDictionary] Loaded {itemDefinitions.Count} item definitions");
    }

    private void InitializeUtensilTemplates()
    {
        utensilTemplates = new Dictionary<UtensilType, List<Utensil>>();
        
        // Initialize utensil templates for each type
        foreach (UtensilType type in Enum.GetValues(typeof(UtensilType)))
        {
            utensilTemplates[type] = new List<Utensil>();
        }
        
        // Add some default utensil templates
        AddUtensilTemplate(new Utensil("Chef's Knife", UtensilType.Knife, 0.1f, 200));
        AddUtensilTemplate(new Utensil("Spatula", UtensilType.Spatula, 0.05f, 150));
        AddUtensilTemplate(new Utensil("Tongs", UtensilType.Tongs, 0.05f, 100));
    }
    #endregion

    #region Item Definitions
    public ItemBasicDetailsSO GetItemDefinition(string itemId)
    {
        itemDefinitions.TryGetValue(itemId, out ItemBasicDetailsSO definition);
        return definition;
    }

    public bool HasItemDefinition(string itemId)
    {
        return itemDefinitions.ContainsKey(itemId);
    }

    public List<string> GetAllItemIds()
    {
        return new List<string>(itemDefinitions.Keys);
    }
    #endregion

    #region Utensil Management
    public void AddUtensilTemplate(Utensil utensil)
    {
        if (utensilTemplates.ContainsKey(utensil.Type))
        {
            utensilTemplates[utensil.Type].Add(utensil);
        }
    }

    public List<Utensil> GetUtensilTemplates(UtensilType type)
    {
        utensilTemplates.TryGetValue(type, out List<Utensil> templates);
        return templates ?? new List<Utensil>();
    }

    public Utensil CreateUtensil(UtensilType type, string name = null)
    {
        var templates = GetUtensilTemplates(type);
        if (templates.Count > 0)
        {
            var template = templates[0]; // Use first template as default
            return new Utensil(
                name ?? template.Name,
                template.Type,
                template.EfficiencyBonus,
                template.MaxDurability
            );
        }
        
        // Fallback if no templates exist
        return new Utensil(name ?? type.ToString(), type);
    }
    #endregion
}

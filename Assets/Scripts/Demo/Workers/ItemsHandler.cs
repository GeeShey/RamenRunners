using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Handles item equipment and management for workers.
/// Provides a clean interface for equipping, unequipping, and managing items.
/// </summary>
public class ItemsHandler : MonoBehaviour
{
    #region Inspector Fields
    [Header("Item Management")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private int maxEquippedItems = 10;
    #endregion

    #region Private Fields
    private BaseWorker worker;
    private Dictionary<string, Item> equippedItems = new Dictionary<string, Item>();
    #endregion

    #region Properties
    public BaseWorker Worker => worker;
    public IReadOnlyDictionary<string, Item> EquippedItems => equippedItems;
    public int EquippedItemCount => equippedItems.Count;
    public bool IsAtCapacity => equippedItems.Count >= maxEquippedItems;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        worker = GetComponent<BaseWorker>();
        if (worker == null)
        {
            LogDebug("ItemsHandler requires a BaseWorker component!");
        }
    }
    #endregion

    #region Item Equipment
    public bool EquipItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            LogDebug("Item ID cannot be null or empty");
            return false;
        }

        if (IsAtCapacity && !HasItem(itemId))
        {
            LogDebug($"Cannot equip {itemId} - at capacity ({maxEquippedItems})");
            return false;
        }

        if (HasItem(itemId))
        {
            return AddToExistingItem(itemId);
        }
        else
        {
            return CreateNewItem(itemId);
        }
    }

    private bool AddToExistingItem(string itemId)
    {
        Item existingItem = equippedItems[itemId];
        
        if (existingItem.CanAddToStack(1))
        {
            bool success = existingItem.OnStackModified(1);
            if (success)
            {
                LogDebug($"Added to existing {itemId} stack. New count: {existingItem.CurrentStackCount}");
            }
            return success;
        }
        else
        {
            LogDebug($"Cannot add to {itemId} stack - at maximum capacity");
            return false;
        }
    }

    private bool CreateNewItem(string itemId)
    {
        if (ItemFactory.Instance == null)
        {
            LogDebug("ItemFactory instance not available");
            return false;
        }

        Item newItem = ItemFactory.Instance.CreateItem(gameObject, itemId);
        
        if (newItem != null)
        {
            newItem.PreLoad(worker);
            equippedItems[itemId] = newItem;
            
            bool stackSuccess = newItem.OnStackModified(1);
            if (stackSuccess)
            {
                LogDebug($"Successfully equipped {itemId}");
                OnItemEquipped(newItem);
            }
            else
            {
                LogDebug($"Failed to initialize stack for {itemId}");
                UnequipItem(itemId);
            }
            
            return stackSuccess;
        }
        else
        {
            LogDebug($"Failed to create item: {itemId}");
            return false;
        }
    }
    #endregion

    #region Item Management
    public bool UnequipItem(string itemId)
    {
        if (!HasItem(itemId))
        {
            LogDebug($"Item {itemId} not equipped");
            return false;
        }

        Item item = equippedItems[itemId];
        equippedItems.Remove(itemId);
        
        OnItemUnequipped(item);
        Destroy(item);
        
        LogDebug($"Unequipped {itemId}");
        return true;
    }

    public bool HasItem(string itemId)
    {
        return equippedItems.ContainsKey(itemId);
    }

    public Item GetItem(string itemId)
    {
        equippedItems.TryGetValue(itemId, out Item item);
        return item;
    }

    public bool UseItem(string itemId)
    {
        Item item = GetItem(itemId);
        if (item == null)
        {
            LogDebug($"Cannot use {itemId} - not equipped");
            return false;
        }

        bool success = item.UseItem();
        if (success)
        {
            LogDebug($"Used {itemId}");
            OnItemUsed(item);
        }

        return success;
    }

    public void ClearAllItems()
    {
        var itemsToRemove = equippedItems.Keys.ToList();
        foreach (string itemId in itemsToRemove)
        {
            UnequipItem(itemId);
        }
        LogDebug("Cleared all equipped items");
    }
    #endregion

    #region Events
    protected virtual void OnItemEquipped(Item item)
    {
        // Override in derived classes for custom behavior
    }

    protected virtual void OnItemUnequipped(Item item)
    {
        // Override in derived classes for custom behavior
    }

    protected virtual void OnItemUsed(Item item)
    {
        // Override in derived classes for custom behavior
    }
    #endregion

    #region Utility
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ItemsHandler] {message}");
        }
    }
    #endregion
}

using System;
using UnityEngine;

/// <summary>
/// Abstract base class for all items that can be equipped by workers.
/// Handles item ownership, stacking, and basic item functionality.
/// </summary>
public abstract class Item : MonoBehaviour
{
    #region Inspector Fields
    [Header("Item Configuration")]
    [SerializeField] protected ItemBasicDetailsSO itemBasicDetails;
    [SerializeField] protected int maxStackCount = 1;
    [SerializeField] protected bool isStackable = true;
    [SerializeField] protected bool isConsumable = false;
    
    [Header("Debug")]
    [SerializeField] protected bool enableDebugLogs = false;
    #endregion

    #region Protected Fields
    protected BaseWorker itemOwner;
    protected int currentStackCount = 0;
    #endregion

    #region Properties
    public ItemBasicDetailsSO ItemBasicDetails => itemBasicDetails;
    public BaseWorker ItemOwner => itemOwner;
    public int CurrentStackCount => currentStackCount;
    public int MaxStackCount => maxStackCount;
    public bool IsStackable => isStackable;
    public bool IsConsumable => isConsumable;
    public bool IsEmpty => currentStackCount <= 0;
    public bool IsFull => currentStackCount >= maxStackCount;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        InitializeItem();
    }
    #endregion

    #region Initialization
    protected virtual void InitializeItem()
    {
        if (itemBasicDetails == null)
        {
            LogDebug("ItemBasicDetails not assigned!");
        }
    }

    public virtual void PreLoad(BaseWorker itemOwner)
    {
        this.itemOwner = itemOwner;
        LogDebug($"Item {name} loaded for worker {itemOwner.name}");
    }
    #endregion

    #region Stack Management
    public virtual bool OnStackModified(int count)
    {
        if (!isStackable && count > 0 && currentStackCount > 0)
        {
            LogDebug("Cannot stack non-stackable item");
            return false;
        }

        int newCount = currentStackCount + count;
        
        if (newCount < 0)
        {
            LogDebug("Cannot reduce stack below zero");
            return false;
        }

        if (newCount > maxStackCount)
        {
            LogDebug($"Cannot exceed max stack count of {maxStackCount}");
            return false;
        }

        currentStackCount = newCount;
        OnStackCountChanged(currentStackCount);
        
        if (currentStackCount == 0)
        {
            OnItemDepleted();
        }

        LogDebug($"Stack count changed to {currentStackCount}");
        return true;
    }

    public virtual bool CanAddToStack(int amount)
    {
        if (!isStackable) return currentStackCount == 0;
        return currentStackCount + amount <= maxStackCount;
    }

    public virtual int GetAvailableStackSpace()
    {
        return isStackable ? maxStackCount - currentStackCount : (currentStackCount == 0 ? 1 : 0);
    }
    #endregion

    #region Item Actions
    public virtual bool UseItem()
    {
        if (IsEmpty)
        {
            LogDebug("Cannot use empty item");
            return false;
        }

        bool success = OnItemUsed();
        
        if (success && isConsumable)
        {
            OnStackModified(-1);
        }

        return success;
    }

    protected virtual bool OnItemUsed()
    {
        LogDebug($"Item {name} used");
        return true;
    }

    protected virtual void OnStackCountChanged(int newCount)
    {
        // Override in derived classes for custom behavior
    }

    protected virtual void OnItemDepleted()
    {
        LogDebug($"Item {name} depleted");
        // Could trigger item removal or other cleanup
    }
    #endregion

    #region Utility
    protected void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] {message}");
        }
    }
    #endregion
}
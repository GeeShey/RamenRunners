using UnityEngine;
using System;

/// <summary>
/// Energy Drink item that provides movement speed bonuses to the worker.
/// The bonus scales with the stack count of the energy drink.
/// </summary>
public class Item_EnergyDrink : Item
{
    #region Inspector Fields
    [Header("Energy Drink Configuration")]
    [SerializeField] private float speedBonusPercentage = 0.25f;
    #endregion

    #region Private Fields
    private float originalMovementSpeed;
    private bool hasAppliedBonus = false;
    #endregion

    #region Override Methods
    public override void PreLoad(BaseWorker itemOwner)
    {
        base.PreLoad(itemOwner);
        StoreOriginalSpeed();
    }

    protected override bool OnItemUsed()
    {
        LogDebug($"Energy Drink {name} used - providing speed boost");
        return true;
    }

    protected override void OnStackCountChanged(int newCount)
    {
        LogDebug($"Energy Drink stack changed to: {newCount}");
        UpdateMovementSpeedBonus();
    }

    protected override void OnItemDepleted()
    {
        LogDebug($"Energy Drink {name} depleted - removing speed bonus");
        RestoreOriginalSpeed();
    }
    #endregion

    #region Speed Management
    private void StoreOriginalSpeed()
    {
        if (ItemOwner != null && !hasAppliedBonus)
        {
            originalMovementSpeed = ItemOwner.CurrentMovementSpeed;
            hasAppliedBonus = true;
            LogDebug($"Stored original movement speed: {originalMovementSpeed}");
        }
    }

    private void UpdateMovementSpeedBonus()
    {
        if (ItemOwner == null) return;

        // Calculate the bonus based on stack count
        float speedBonus = originalMovementSpeed * speedBonusPercentage * CurrentStackCount;
        float newSpeed = originalMovementSpeed + speedBonus;
        
        // Apply the new speed (this would need to be implemented in BaseWorker)
        // For now, we'll use the old method but with proper calculation
        ItemOwner.CurrentMovementSpeed = newSpeed;
        
        LogDebug($"Updated movement speed: {originalMovementSpeed} + {speedBonus} = {newSpeed} (stacks: {CurrentStackCount})");
    }

    private void RestoreOriginalSpeed()
    {
        if (ItemOwner != null && hasAppliedBonus)
        {
            ItemOwner.CurrentMovementSpeed = originalMovementSpeed;
            hasAppliedBonus = false;
            LogDebug($"Restored original movement speed: {originalMovementSpeed}");
        }
    }
    #endregion

    #region Cleanup
    private void OnDestroy()
    {
        RestoreOriginalSpeed();
    }
    #endregion

    #region Utility
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[EnergyDrink] {message}");
        }
    }
    #endregion
}

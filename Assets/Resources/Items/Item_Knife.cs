using UnityEngine;
using System;

/// <summary>
/// Knife item that provides station efficiency bonuses to the worker.
/// The bonus scales with the stack count of the knife.
/// </summary>
public class Item_Knife : Item
{
    #region Inspector Fields
    [Header("Knife Configuration")]
    [SerializeField] private float efficiencyBonusPercentage = 0.5f;
    #endregion

    #region Private Fields
    private float originalStationEfficiency;
    private bool hasAppliedBonus = false;
    #endregion

    #region Override Methods
    public override void PreLoad(BaseWorker itemOwner)
    {
        base.PreLoad(itemOwner);
        StoreOriginalEfficiency();
    }

    protected override bool OnItemUsed()
    {
        LogDebug($"Knife {name} used - providing efficiency boost");
        return true;
    }

    protected override void OnStackCountChanged(int newCount)
    {
        LogDebug($"Knife stack changed to: {newCount}");
        UpdateStationEfficiencyBonus();
    }

    protected override void OnItemDepleted()
    {
        LogDebug($"Knife {name} depleted - removing efficiency bonus");
        RestoreOriginalEfficiency();
    }
    #endregion

    #region Efficiency Management
    private void StoreOriginalEfficiency()
    {
        if (ItemOwner != null && !hasAppliedBonus)
        {
            originalStationEfficiency = ItemOwner.CurrentStationEfficiency;
            hasAppliedBonus = true;
            LogDebug($"Stored original station efficiency: {originalStationEfficiency}");
        }
    }

    private void UpdateStationEfficiencyBonus()
    {
        if (ItemOwner == null) return;

        // Calculate the bonus based on stack count
        float efficiencyBonus = originalStationEfficiency * efficiencyBonusPercentage * CurrentStackCount;
        float newEfficiency = originalStationEfficiency + efficiencyBonus;
        
        // Apply the new efficiency
        ItemOwner.CurrentStationEfficiency = newEfficiency;
        
        LogDebug($"Updated station efficiency: {originalStationEfficiency} + {efficiencyBonus} = {newEfficiency} (stacks: {CurrentStackCount})");
    }

    private void RestoreOriginalEfficiency()
    {
        if (ItemOwner != null && hasAppliedBonus)
        {
            ItemOwner.CurrentStationEfficiency = originalStationEfficiency;
            hasAppliedBonus = false;
            LogDebug($"Restored original station efficiency: {originalStationEfficiency}");
        }
    }
    #endregion

    #region Cleanup
    private void OnDestroy()
    {
        RestoreOriginalEfficiency();
    }
    #endregion

    #region Utility
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[Knife] {message}");
        }
    }
    #endregion
}

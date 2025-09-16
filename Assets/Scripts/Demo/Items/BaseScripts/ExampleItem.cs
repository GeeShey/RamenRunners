using UnityEngine;

/// <summary>
/// Example implementation of an Item to demonstrate the new item system.
/// This shows how to create custom items that inherit from the base Item class.
/// </summary>
public class ExampleItem : Item
{
    #region Inspector Fields
    [Header("Example Item Properties")]
    [SerializeField] private float customEffectValue = 1.0f;
    [SerializeField] private bool hasSpecialAbility = false;
    #endregion

    #region Override Methods
    protected override bool OnItemUsed()
    {
        LogDebug($"Example item {name} was used with effect value: {customEffectValue}");
        
        if (hasSpecialAbility)
        {
            TriggerSpecialAbility();
        }
        
        return true;
    }

    protected override void OnStackCountChanged(int newCount)
    {
        LogDebug($"Example item stack changed to: {newCount}");
        
        // Example: Update visual representation based on stack count
        UpdateVisualRepresentation(newCount);
    }

    protected override void OnItemDepleted()
    {
        LogDebug($"Example item {name} has been depleted");
        
        // Example: Could trigger special effects when item is depleted
        TriggerDepletionEffect();
    }
    #endregion

    #region Custom Behavior
    private void TriggerSpecialAbility()
    {
        LogDebug($"Special ability triggered for {name}!");
        
        // Example: Could modify worker stats or trigger special effects
        if (ItemOwner != null)
        {
            // Example: Increase movement speed temporarily
            // This would need to be implemented in BaseWorker
            LogDebug($"Applying special effect to worker {ItemOwner.name}");
        }
    }

    private void UpdateVisualRepresentation(int stackCount)
    {
        // Example: Update UI, particle effects, or other visual elements
        // based on the current stack count
        LogDebug($"Updating visual representation for stack count: {stackCount}");
    }

    private void TriggerDepletionEffect()
    {
        // Example: Could spawn particles, play sound, or trigger other effects
        LogDebug($"Triggering depletion effect for {name}");
    }
    #endregion
}

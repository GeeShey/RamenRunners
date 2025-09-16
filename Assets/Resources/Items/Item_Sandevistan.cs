using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Sandevistan item that provides teleportation abilities to workers.
/// Has a chance to teleport instead of normal movement, with probability scaling with stack count.
/// </summary>
public class Item_Sandevistan : Item
{
    #region Inspector Fields
    [Header("Sandevistan Configuration")]
    [SerializeField] private float baseTeleportProbability = 1.0f;
    [SerializeField] private float probabilityPerStack = 1.0f;
    #endregion

    #region Private Fields
    private BaseWorker.MovementMethod previousMovementMethod;
    private float currentTeleportProbability = 0f;
    private bool isSubscribedToEvents = false;
    #endregion

    #region Properties
    public float CurrentTeleportProbability => currentTeleportProbability;
    #endregion

    #region Override Methods
    public override void PreLoad(BaseWorker itemOwner)
    {
        base.PreLoad(itemOwner);
        LogDebug($"Sandevistan PreLoad called for worker: {itemOwner.name}");
        SubscribeToEvents();
        UpdateTeleportProbability();
    }

    protected override bool OnItemUsed()
    {
        LogDebug($"Sandevistan {name} used - attempting teleport");
        return true;
    }

    protected override void OnStackCountChanged(int newCount)
    {
        LogDebug($"Sandevistan stack changed to: {newCount}");
        UpdateTeleportProbability();
    }

    protected override void OnItemDepleted()
    {
        LogDebug($"Sandevistan {name} depleted - removing teleport ability");
        UnsubscribeFromEvents();
    }
    #endregion

    #region Event Handling
    private void SubscribeToEvents()
    {
        if (ItemOwner != null && !isSubscribedToEvents)
        {
            // Store the original movement method and set our teleport method
            previousMovementMethod = ItemOwner.InitializeMovementMethod;
            ItemOwner.InitializeMovementMethod = OnAboutToStartMoving;
            isSubscribedToEvents = true;
            LogDebug("Sandevistan movement method activated");
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (ItemOwner != null && isSubscribedToEvents)
        {
            // Restore the original movement method
            ItemOwner.InitializeMovementMethod = previousMovementMethod;
            isSubscribedToEvents = false;
            LogDebug("Sandevistan movement method deactivated");
        }
    }

    private IEnumerator OnAboutToStartMoving(StationId destinationStationId)
    {
        bool shouldTeleport = RollForTeleport();
        if (shouldTeleport)
        {
            LogDebug("Teleport activated!");
            yield return StartCoroutine(SandevistanTeleport(destinationStationId));
        }
        else
        {
            LogDebug("Teleport failed, using normal movement");
            // Use the original movement method or fallback to normal movement
            if (previousMovementMethod != null)
            {
                yield return StartCoroutine(previousMovementMethod(destinationStationId));
            }
            else
            {
                // Fallback: temporarily restore normal movement and call it
                ItemOwner.InitializeMovementMethod = null;
                yield return StartCoroutine(ItemOwner.MoveToStation(destinationStationId));
                // Restore our method
                ItemOwner.InitializeMovementMethod = OnAboutToStartMoving;
            }
        }
    }
    #endregion

    #region Teleport Logic
    private void UpdateTeleportProbability()
    {
        currentTeleportProbability = baseTeleportProbability + (probabilityPerStack * CurrentStackCount);
        currentTeleportProbability = Mathf.Clamp01(currentTeleportProbability);
        LogDebug($"Updated teleport probability to: {currentTeleportProbability} (stacks: {CurrentStackCount})");
    }

    private bool RollForTeleport()
    {
        float rollValue = UnityEngine.Random.value;
        
        if (currentTeleportProbability >= 1f)
        {
            LogDebug("Guaranteed teleport (100% probability)");
            return true;
        }

        bool teleportSuccess = rollValue < currentTeleportProbability;
        
        if (teleportSuccess)
        {
            LogDebug($"Teleport successful! (rolled {rollValue:F3} < {currentTeleportProbability:F3})");
            currentTeleportProbability = baseTeleportProbability; // Reset on success
        }
        else
        {
            // Increase probability for next attempt
            currentTeleportProbability += baseTeleportProbability;
            currentTeleportProbability = Mathf.Clamp01(currentTeleportProbability);
            LogDebug($"Teleport failed! (rolled {rollValue:F3} >= {currentTeleportProbability - baseTeleportProbability:F3}), increased probability to: {currentTeleportProbability:F3}");
        }

        return teleportSuccess;
    }

    private IEnumerator SandevistanTeleport(StationId destinationStationId)
    {
        LogDebug($"Teleporting to {destinationStationId}");

        // Check if already at destination
        if (destinationStationId == ItemOwner.CurrentStationId)
        {
            LogDebug("Already at destination station");
            yield break;
        }

        // Release current station if occupied
        if (ItemOwner.Status == WorkerStatus.AtStation)
        {
            Station currentStation = KitchenManager.instance?.GetStation(ItemOwner.CurrentStationId);
            currentStation?.ReleaseStandingLocation(ItemOwner as Worker);
        }

        // Get destination station
        Station destinationStation = KitchenManager.instance?.GetStation(destinationStationId);
        if (destinationStation == null)
        {
            LogDebug($"Destination station {destinationStationId} not found!");
            yield break;
        }

        // Try to get an available slot
        Transform availableSlot = destinationStation.ReserveAvailableStandingLocation(ItemOwner as Worker);

        if (availableSlot != null)
        {
            // Direct teleport to available slot
            ItemOwner.transform.position = availableSlot.position;
            ItemOwner.UpdateCurrentStation(destinationStationId, destinationStation);
            ItemOwner.UpdateStatus(WorkerStatus.AtStation);
            ItemOwner.TriggerMovementStarted(destinationStationId);
            LogDebug($"Teleported directly to {destinationStationId}");
        }
        else
        {
            // Wait for slot to become available
            yield return StartCoroutine(WaitForSlotAndTeleport(destinationStation, destinationStationId));
        }

        // Restore original movement method
        ItemOwner.InitializeMovementMethod = previousMovementMethod;
    }

    private IEnumerator WaitForSlotAndTeleport(Station destinationStation, StationId destinationStationId)
    {
        Transform freedSlot = null;
        Action<Transform> onSlotFreed = (slot) => freedSlot = slot;

        destinationStation.ReserveUnavailableStandingLocation(onSlotFreed);
        ItemOwner.UpdateStatus(WorkerStatus.Waiting);

        // Move to halfway point while waiting
        Vector3 startPos = ItemOwner.transform.position;
        Vector3 halfwayPoint = Vector3.Lerp(startPos, destinationStation.transform.position, 0.5f);
        ItemOwner.transform.position = halfwayPoint;

        LogDebug($"Waiting for slot at {destinationStationId}");

        // Wait for slot to become available
        yield return new WaitUntil(() => freedSlot != null);

        // Teleport to the freed slot
        ItemOwner.transform.position = freedSlot.position;
        ItemOwner.UpdateCurrentStation(destinationStationId, destinationStation);
        ItemOwner.UpdateStatus(WorkerStatus.AtStation);
        ItemOwner.TriggerMovementStarted(destinationStationId);

        LogDebug($"Teleported to freed slot at {destinationStationId}");
    }
    #endregion

    #region Cleanup
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Utility
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[Sandevistan] {message}");
        }
    }
    #endregion
}
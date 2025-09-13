using System;
using System.Collections;
using UnityEngine;

//example of an item
public class Item_Sandevistan : Item
{
    public BaseWorker.MovementMethod PreviousMovementMethod; // Updated reference
    public float BaseTeleportProbability = 0.025f;
    public float CurrentTeleportProbability = 0f;

    public override void PreLoad(BaseWorker _ItemOwner) // Changed parameter type
    {
        base.PreLoad(_ItemOwner);
        _ItemOwner.initializeMovementMethod += AboutToStartMoving;
    }

    public override void OnStackModified(int count)
    {
        currentStackCount += count;
        BaseTeleportProbability = 1.0f * currentStackCount;
        CurrentTeleportProbability = BaseTeleportProbability;
    }

    public IEnumerator AboutToStartMoving()
    {
        bool goodDiceRoll = DiceRoll();
        if (goodDiceRoll)
        {
            PreviousMovementMethod = ItemOwner.CurrentMovementMethod;
            ItemOwner.CurrentMovementMethod = SandevistanTeleport;
        }
        yield break;
    }

    public bool DiceRoll()
    {
        if (CurrentTeleportProbability >= 1f)
            return true;

        bool goodDiceRoll = false;
        if (UnityEngine.Random.value < CurrentTeleportProbability)
        {
            goodDiceRoll = true;
            CurrentTeleportProbability = BaseTeleportProbability;
        }
        else
        {
            goodDiceRoll = false;
            CurrentTeleportProbability += BaseTeleportProbability;
            CurrentTeleportProbability = Mathf.Clamp(CurrentTeleportProbability, 0f, 1f); // Fixed the clamp
        }
        Debug.Log("CurrentTeleportProbability changed to: " + CurrentTeleportProbability);
        return goodDiceRoll;
    }

    private IEnumerator SandevistanTeleport(StationId destinationStationId)
    {
        if (destinationStationId == ItemOwner.currentStationId)
        {
            yield return null;
        }

        // Release current station if at one
        if (ItemOwner.currentStatus == WorkerStatus.AtStation)
        {
            KitchenManager.instance.GetStation(ItemOwner.currentStationId).ReleaseStandingLocation(ItemOwner as Worker);
        }

        // Do the teleport logic
        Vector3 startingPos = ItemOwner.transform.position;
        Station destinationStation = KitchenManager.instance.GetStation(destinationStationId);
        Transform slotTransform = destinationStation.ReserveAvailableStandingLocation(ItemOwner as Worker);

        if (slotTransform != null)
        {
            ItemOwner.transform.position = slotTransform.position;
            ItemOwner.onMovementStarted?.Invoke(Enum.GetName(typeof(StationId), ItemOwner.currentStationId));
        }
        else
        {
            // Set up waiting mechanism for slot
            Transform freedSlot = null;
            Action<Transform> onSlotFreed = (Transform slot) => {
                freedSlot = slot;
            };
            destinationStation.ReserveUnavailableStandingLocation(onSlotFreed);

            // Move to halfway point
            ItemOwner.transform.position = Vector3.Lerp(startingPos, destinationStation.transform.position, 0.5f);

            // Wait for slot to be free
            ItemOwner.currentStatus = WorkerStatus.Waiting;
            yield return new WaitUntil(() => freedSlot != null);

            // Move to slot
            ItemOwner.transform.position = freedSlot.position;
            ItemOwner.currentStatus = WorkerStatus.AtStation;
        }

        ItemOwner.currentStationId = destinationStationId;
        ItemOwner.currentStatus = WorkerStatus.AtStation;
        ItemOwner.CurrentMovementMethod = PreviousMovementMethod;
    }
}
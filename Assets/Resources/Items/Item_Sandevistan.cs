using System;
using System.Collections;
using UnityEngine;
using static Worker;

public class Item_Sandevistan : Item
{
    public MovementMethod PreviousMovementMethod;
    public float BaseTeleportProbability = 0.025f;
    public float CurrentTeleportProbability = 0f;

    // 20% chance to teleport

    public override void PreLoad(Worker _ItemOwner)
    {
        base.PreLoad(_ItemOwner);
        _ItemOwner.initializeMovementMethod += AboutToStartMoving;
    }
    public override void OnStackModified(int count)
    {
        //changing stack count will reset current probability
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
        if(UnityEngine.Random.value < CurrentTeleportProbability)
        {
            //good roll
            goodDiceRoll = true;
            CurrentTeleportProbability = BaseTeleportProbability;
        }
        else
        {
            //bad roll
            goodDiceRoll = false;
            CurrentTeleportProbability += BaseTeleportProbability;
            Mathf.Clamp(CurrentTeleportProbability, 0f, 1f);


        }
        Debug.Log("CurrentTeleportProbability changed to: "+ CurrentTeleportProbability);
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
            KitchenManager.instance.GetStation(ItemOwner.currentStationId).ReleaseStandingLocation(ItemOwner);
        }

        //do the telport logic
        Vector3 startingPos = ItemOwner.transform.position;
        Station destinationStation = KitchenManager.instance.GetStation(destinationStationId);
        Transform slotTransform = destinationStation.ReserveAvailableStandingLocation(ItemOwner);
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


            //move to halfway point
            ItemOwner.transform.position = Vector3.Lerp(startingPos, destinationStation.transform.position, 0.5f);

            //wait for slot to be free
            ItemOwner.currentStatus = WorkerStatus.Waiting;
            yield return new WaitUntil(() => freedSlot != null);

            //move to slot
            ItemOwner.transform.position = freedSlot.position;
            ItemOwner.currentStatus = WorkerStatus.AtStation;

        }
        ItemOwner.currentStationId = destinationStationId;
        ItemOwner.currentStatus = WorkerStatus.AtStation;
        ItemOwner.CurrentMovementMethod = PreviousMovementMethod;

    }
}

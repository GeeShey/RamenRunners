using System;
using System.Collections;
using UnityEngine;
using static Worker;

public class Item_Sandevistan : Item
{
    Worker worker;
    public MovementMethod PreviousMovementMethod;
    public float BaseTeleportProbability = 0.025f;
    public float CurrentTeleportProbability = 0f;
    public int currentStackCount = 0;

    // 20% chance to teleport

    public override void PreLoad()
    {
        worker = GetComponent<Worker>();
    }

    public override void OnStackModified(int count)
    {
        //changing stack count will reset current probability
        currentStackCount += count;
        BaseTeleportProbability = 1.0f * currentStackCount;
        CurrentTeleportProbability = BaseTeleportProbability;

    }

    public void AboutToStartMoving()
    {
        bool goodDiceRoll = DiceRoll();
        if (goodDiceRoll)
        {
            PreviousMovementMethod = worker.CurrentMovementMethod;
            worker.CurrentMovementMethod = SandevistanTeleport;

        }

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

        if (destinationStationId == worker.currentStationId)
        {
            yield return null;
        }

        // Release current station if at one
        if (worker.currentStatus == WorkerStatus.AtStation)
        {
            KitchenManager.instance.GetStation(worker.currentStationId).ReleaseStandingLocation(worker);
        }

        //do the telport logic
        Vector3 startingPos = worker.transform.position;
        Station destinationStation = KitchenManager.instance.GetStation(destinationStationId);
        Transform slotTransform = destinationStation.ReserveAvailableStandingLocation(worker);
        if (slotTransform != null)
        {
            worker.transform.position = slotTransform.position;
            worker.onMovementStarted?.Invoke(Enum.GetName(typeof(StationId), worker.currentStationId));

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
            worker.transform.position = Vector3.Lerp(startingPos, destinationStation.transform.position, 0.5f);

            //wait for slot to be free
            worker.currentStatus = WorkerStatus.Waiting;
            yield return new WaitUntil(() => freedSlot != null);

            //move to slot
            worker.transform.position = freedSlot.position;
            worker.currentStatus = WorkerStatus.AtStation;

        }
        worker.currentStationId = destinationStationId;
        worker.currentStatus = WorkerStatus.AtStation;
        worker.CurrentMovementMethod = PreviousMovementMethod;

    }
}

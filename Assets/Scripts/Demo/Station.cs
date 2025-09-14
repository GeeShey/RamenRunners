using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using static UnityEngine.Rendering.DebugUI;


public class Station : MonoBehaviour
{
    public int slots;
    public Transform[] standingLocations;
    public float stationTime;
    public StationId StationId;
    public Action<BaseWorker> SomeoneStartedWorkAtStation;

    private Dictionary<Transform, BaseWorker> occupiedSlots = new Dictionary<Transform, BaseWorker>();
    private List<Action<Transform>> waitingQueue = new List<Action<Transform>>();


    void Start()
    {
        KitchenManager.instance.RegisterStation(this);

    }

    public List<BaseWorker> GetAllWorkers()
    {
        return occupiedSlots.Values.ToList();
    }

    public Transform ReserveAvailableStandingLocation( BaseWorker worker)
    {
        foreach (Transform location in standingLocations)
        {
            if (!occupiedSlots.ContainsKey(location))
            {
                occupiedSlots[location] = worker;
                return location;
            }
        }
        Debug.Log("no standing location available");
        return null; // No available spots
    }

    public void ReserveUnavailableStandingLocation(Action<Transform> onSlotFreed)
    {
        waitingQueue.Add(onSlotFreed);
    }

    //THIS IS USED BY THE BOTS TO WAIT
    public Transform GetDefaultSlot()
    {
        return standingLocations[0];
    }

    public void ReleaseStandingLocation(BaseWorker worker)
    {
        if (occupiedSlots.ContainsValue(worker))
        {
            Transform slotToRemove = occupiedSlots.FirstOrDefault(x => x.Value == worker).Key;
            occupiedSlots.Remove(slotToRemove);

            if (waitingQueue.Count > 0)
            {
                //tell this ItemOwner that the slot is free
                Action<Transform> firstAction= waitingQueue[0];
                waitingQueue.RemoveAt(0);
                firstAction?.Invoke(slotToRemove);
            }

        }
    }

    public bool IsLocationOccupied(Transform location)
    {
        return occupiedSlots.ContainsKey(location);
    }

    public string getName()
    {
        return Enum.GetName(typeof(StationId), StationId);
    }
    public int GetAvailableStandingLocationCount()
    {
        return standingLocations.Length - occupiedSlots.Count;
    }
}
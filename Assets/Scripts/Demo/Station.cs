using System.Collections.Generic;
using UnityEngine;

public class Station : MonoBehaviour
{
    public int slots;
    public Transform[] standingLocations;
    public float stationTime;
    public StationId StationId;

    private Dictionary<Transform, Worker> occupiedSlots = new Dictionary<Transform, Worker>();

    void Start()
    {
        KitchenManager.instance.RegisterStation(this);
        MovementManager.instance.RegisterLocations(standingLocations);

    }

    public Transform GetAvailableStandingLocation()
    {
        foreach (Transform location in standingLocations)
        {
            if (!occupiedSlots.ContainsKey(location))
            {
                return location;
            }
        }
        return null; // No available spots
    }

    public void ReserveStandingLocation(Transform location, Worker worker)
    {
        if (!occupiedSlots.ContainsKey(location))
        {
            occupiedSlots[location] = worker;
        }
    }

    public void ReleaseStandingLocation(Transform location)
    {
        if (occupiedSlots.ContainsKey(location))
        {
            occupiedSlots.Remove(location);
        }
    }

    public bool IsLocationOccupied(Transform location)
    {
        return occupiedSlots.ContainsKey(location);
    }

    public int GetAvailableStandingLocationCount()
    {
        return standingLocations.Length - occupiedSlots.Count;
    }
}
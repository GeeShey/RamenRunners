using System.Collections.Generic;
using UnityEngine;

public class Worker : MonoBehaviour
{
    public float movementSpeed;
    public float stationEfficiency;

    public StationId currentStation;
    public WorkerStatus currentStatus;

    public List<Utensil> equippedUtensils;


    void Start()
    {
        KitchenManager.instance.addWorker(this);
        currentStatus = WorkerStatus.Free;
    }

    public void startOrder(Order order)
    {


    }



}

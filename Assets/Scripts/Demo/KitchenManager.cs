using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public enum WorkerStatus
{
    Running,AtStation,Free
}
public class KitchenManager : MonoBehaviour
{
    public static KitchenManager instance;

    List<Worker> workers;
    List<Order> ordersInProgress;
    List<Order> ordersToBeTaken;


    void Start()
    {
        instance = this;
        workers = new List<Worker>();
        ordersInProgress = new List<Order>();
        ordersToBeTaken = new List<Order>();
    }

    void Update()
    {
        
    }

    public void addWorker(Worker _worker)
    {
        workers.Add(_worker);

    }

    public void EnqueOrder(Order order, Action orderStarted)
    {

        bool freeWorkerAvailable = workers.Any(worker => worker.currentStatus == WorkerStatus.Free);

        if(freeWorkerAvailable)
        {
            Worker availableWorker = workers.First(worker => worker.currentStatus == WorkerStatus.Free);
            availableWorker.startOrder(order);
            orderStarted?.Invoke();
            ordersInProgress.Add(order);
        }
        else
        {
            ordersToBeTaken.Add(order);
            //TODO keep track of the event orderstarted as well
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KitchenManager : MonoBehaviour
{
    public static KitchenManager instance;
    List<Worker> workers;
    List<Order> orders;
    List<Station> stations;

    void Start()
    {
        instance = this;
        workers = new List<Worker>();
        orders = new List<Order>();
        stations = new List<Station>();
    }

    public void RegisterStation(Station station)
    {
        stations.Add(station);
    }

    public Station GetStation(StationId stationId)
    {
        return stations.FirstOrDefault(s => s.StationId == stationId);
    }

    public void addWorker(Worker _worker)
    {
        workers.Add(_worker);
    }

    public void EnqueOrder(Order order)
    {
        bool freeWorkerAvailable = workers.Any(worker => worker.isFree());

        if (freeWorkerAvailable)
        {
            Worker availableWorker = workers.First(worker => worker.isFree());
            availableWorker.startOrder(order);

        }
        orders.Add(order);

    }

    public bool GiveMeOrder(Worker worker)
    {
        bool orderAvailable = orders.Any(order => order.status == OrderStatus.NotStarted);
        if (orderAvailable)
        {
            Order pendingOrder = orders.First(order => order.status == OrderStatus.NotStarted);
            orders.Remove(pendingOrder);
            worker.startOrder(pendingOrder);
            return true;
        }
        else
        {
            return false;
        }
    }
}
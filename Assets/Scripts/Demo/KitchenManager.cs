using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KitchenManager : MonoBehaviour
{
    public static KitchenManager instance;
    public List<BaseWorker> workers;
    public List<Order> orders;
    public List<Station> stations;

    public bool DEBUG;

    void Start()
    {
        instance = this;
        workers = new List<BaseWorker>();
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

    public void addWorker(BaseWorker _worker)
    {
        workers.Add(_worker);
    }

    public void EnqueOrder(Order order)
    {
        bool freeWorkerAvailable = workers.Any(worker => worker.isFree());

        if (freeWorkerAvailable)
        {
            Worker availableWorker = workers.First(worker => worker.isFree()) as Worker;
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
            CarManager.instance.InitializeNewCar();
            return false;
        }
    }

    public Worker getWorker()
    {
        return null;

    }

    public StationId[] getAvailableStations()
    {
        List<StationId> availableStations = new List<StationId>();

        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i].StationId != StationId.CheckIn || stations[i].StationId != StationId.CheckOut)
            {
                availableStations.Add(stations[i].StationId);
            }
        }

        return availableStations.ToArray();

    }
}
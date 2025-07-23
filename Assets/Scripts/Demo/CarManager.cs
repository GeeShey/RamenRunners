using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;


public class CarSlot
{
    public Vector3 position;
    public bool occupied;
    public CarSlot(Vector3 _position)
    {
        position = _position;
    }
}

public class CarManager : MonoBehaviour
{

    public static CarManager instance;

    public List<Transform> orderSlotLocations;
    public List<Transform> pickupSlotLocations;
    public Transform exitSlotLocation;


    public List<CarSlot> pickupSlots;
    public List<CarSlot> orderSlots;

    public List<Car> carsWaitingToOrder;

    public Transform carSpawnPoint;
    public GameObject carModel;

    public Action orderSlotJustOpened;

    public void InitializeNewCar(Order order)
    {

        GameObject car = Instantiate(carModel);
        car.transform.position = carSpawnPoint.position;
        car.GetComponent<Car>().order = order;
    }

    //DEBUG
    public void InitializeNewCar()
    {

        GameObject car = Instantiate(carModel);
        car.transform.position = carSpawnPoint.position;
        Car CarComponent = car.GetComponent<Car>();
        CarComponent.order = OrderManager.instance.createRandomOrder();
        CarComponent.StartOrder();

    }
    private void InitializeCarSlots()
    {

        pickupSlots = pickupSlotLocations.Select(transform => new CarSlot(transform.position)).ToList();
        orderSlots = orderSlotLocations.Select(transform => new CarSlot(transform.position)).ToList();

    }

    public void AddCarToWaitingList(Car car)
    {
        carsWaitingToOrder.Add(car);
    }

    private void Start()
    {
        instance = this;

        InitializeAllLists();
        InitializeCarSlots();
    }

    private void InitializeAllLists()
    {
        pickupSlots = new List<CarSlot>();
        orderSlots = new List<CarSlot>();

        carsWaitingToOrder = new List<Car>();
    }

    public CarSlot GetAvailablePickupSlot()
    {
        CarSlot freeSlot = pickupSlots.Find((carSlot) => carSlot.occupied == false);
        freeSlot.occupied = true;
        return freeSlot;
    }
    public CarSlot GetAvailableOrderSlot()
    {
        CarSlot freeSlot = orderSlots.Find((carSlot) => carSlot.occupied == false);
        freeSlot.occupied = true;
        return freeSlot;
    }
    public void ResetSlot(CarSlot freeSlot)
    {
        if (orderSlots.Any(slot => slot == freeSlot))
        {
            orderSlots.Find(slot => slot == freeSlot).occupied = false;
            SendWaitingCar();
        }
        else if (pickupSlots.Any(slot => slot == freeSlot))
        {
            pickupSlots.Find(slot => slot == freeSlot).occupied = false;
        }
    }

    private void SendWaitingCar(){

        if (carsWaitingToOrder.Count <= 0)
            return;

        carsWaitingToOrder[0].StartOrder();
        carsWaitingToOrder.RemoveAt(0);

    }

    public bool PickupSlotAvailable()
    {
        return pickupSlots.Any((carSlot) => carSlot.occupied == false);
    }
    public bool OrderSlotAvailable()
    {
        bool freeSlotAvailable = orderSlots.Any((carSlot) => carSlot.occupied == false);
        return freeSlotAvailable;

    }

    public Transform GetExitSlot()
    {
        return exitSlotLocation;

    }



}

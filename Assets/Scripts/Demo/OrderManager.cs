using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum StationId
{
    Pantry, Cold, Hot, Frier, Assembly, CheckIn, CheckOut, Rest
}

public enum OrderStatus
{
    NotStarted, InProgress, Completed
}

public class Order
{
    public string id;
    public int orderValue;
    public DishSo dish;
    public float orderStartTime;
    public OrderStatus status;
    public float completionPercentage;
    public Worker assignedWorker;
    public Car assignedCar;


    //EVENTS
    public Action orderRequested;//this is invoked when the ItemOwner has finished taking the order from the customer
    public Action orderStarted;
    public Action orderPrepared;
    public Action orderHanded;



}

//This script will take care of orders coming in to the restaurant and going out
public class OrderManager : MonoBehaviour
{
    public static OrderManager instance;
    DishSo[] DishDefinitions;
    private KitchenManager kitchenManager;

    void Start()
    {
        instance = this;
        kitchenManager = KitchenManager.instance;
        //Load all the dish definitions
        DishDefinitions = Resources.LoadAll<DishSo>("Dishes");
        //create a random order, give it to the kitchen manager
    }

    public void SpawnCarWithOrder()
    {
        //create a random order
        //give it to the car
        //let car go and place the order
        //let car wait for the order


    }

    public Order createRandomOrder()
    {
        Order order = new Order();
        order.id = "OD001";
        DishSo dish = FoodMap.instance.getRandomDish(KitchenManager.instance.GetAvailableStations());        
        order.dish = dish;
        order.orderValue = dish.itemPrice;
        order.status = OrderStatus.NotStarted;
        order.completionPercentage = 0f;
        return order;
    }

    public void PlaceOrder()
    {
        Order newOrder = createRandomOrder();

        KitchenManager.instance.EnqueOrder(newOrder);
    }

    public void PlaceOrder(Order order)
    {
        KitchenManager.instance.EnqueOrder(order);
    }
}
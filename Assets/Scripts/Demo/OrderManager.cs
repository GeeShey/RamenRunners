using System;
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
    public StationId[] requiredStations;
    public float orderStartTime;
    public float customerWaitLimit = 40.0f;
    public float customerWaitTime;
    public OrderStatus status;
    public float completionPercentage;


    /*
     * Using events and flags because we need to bind actions to events. As opposed to continuously checking for flags
     */

    //EVENTS
    public Action orderRequested;//this is invoked when the worker has finished taking the order from the customer
    public Action orderStarted;
    public Action orderPrepared;
    public Action orderHanded;


}

//This script will take care of orders coming in to the restaurant and going out
public class OrderManager : MonoBehaviour
{
    public static OrderManager instance;
    DishSO[] DishDefinitions;
    private KitchenManager kitchenManager;

    void Start()
    {
        instance = this;
        kitchenManager = KitchenManager.instance;
        //Load all the dish definitions
        DishDefinitions = Resources.LoadAll<DishSO>("Dishes");
        //create a random order, give it to the kitchen manager
    }

    // Update is called once per frame
    void Update()
    {

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
        order.requiredStations = new StationId[] { StationId.CheckIn, StationId.Frier,  StationId.CheckOut };
        order.customerWaitTime = 300;
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
using System;
using UnityEngine;

public enum StationId
{
    Pantry, Cold, Hot, Frier, Assembly, CheckIn, CheckOut
}

public enum OrderStatus
{
    NotStarted, InProgress, Completed
}

public class Order
{
    public string id;
    public StationId[] requiredStations;
    public float customerWaitTime;
    public OrderStatus status;
    public float completionPercentage;


    /*
     * Using events and flags because we need to bind actions to events. As opposed to continuously checking for flags
     */

    //EVENTS
    public Action orderRequested;
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

    Order createRandomOrder()
    {
        Order order = new Order();
        order.id = "OD001";
        order.requiredStations = new StationId[] { StationId.CheckIn, StationId.Frier, StationId.Cold, StationId.Pantry, StationId.Assembly, StationId.CheckOut };
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
}
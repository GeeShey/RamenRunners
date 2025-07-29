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

    private List<StationId> getRandomStations()
    {
        StationId[] availableStations = { StationId.Pantry, StationId.Cold, StationId.Hot, StationId.Frier, StationId.Assembly };
        // Create a random number generator
        System.Random random = new System.Random();
        // Determine how many middle stations to include (1 to all available stations)
        int middleStationCount = random.Next(1, availableStations.Length + 1);
        // Shuffle the available stations and take the required number
        StationId[] shuffledStations = availableStations.OrderBy(x => random.Next()).Take(middleStationCount).ToArray();
        // Build the final station list: CheckIn + random middle stations + CheckOut
        List<StationId> finalStations = new List<StationId>();
        finalStations.Add(StationId.CheckIn);           // Always first
        finalStations.AddRange(shuffledStations);       // Random middle stations
        finalStations.Add(StationId.CheckOut);          // Always last
        return finalStations;
    }

    public Order createRandomOrder()
    {
        Order order = new Order();
        order.id = "OD001";
        order.requiredStations = getRandomStations().ToArray();
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
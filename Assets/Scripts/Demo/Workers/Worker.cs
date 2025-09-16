using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main worker class that handles order processing and station workflow
/// </summary>
public class Worker : BaseWorker
{
    #region Inspector Fields
    [Header("Worker Equipment")]
    [SerializeField] private List<Utensil> equippedUtensils = new List<Utensil>();

    [Header("Debug Info")]
    [SerializeField] private DishSo currentDish;
    #endregion

    #region Private Fields
    private Order currentOrder;
    private ItemsHandler itemsHandler;
    #endregion

    #region Properties
    public List<Utensil> EquippedUtensils => equippedUtensils;
    public int FinishedOrdersCount { get; private set; }
    public Order CurrentOrder => currentOrder;
    public ItemsHandler ItemsHandler => itemsHandler;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        itemsHandler = GetComponent<ItemsHandler>();
    }

    protected override void Start()
    {
        base.Start();
        InitializeWorkerSystems();
    }

    #endregion

    #region Initialization
    private void InitializeWorkerSystems()
    {
        RegisterWithKitchenManager();
        InitializeExternalSystems();
    }

    private void RegisterWithKitchenManager()
    {
        KitchenManager.instance?.AddWorker(this);
    }

    private void InitializeExternalSystems()
    {
        CarManager.instance?.InitializeNewCar();
    }
    #endregion

    #region Order Management
    public void StartOrder(Order orderToStart)
    {
        if (orderToStart == null) return;

        if (ShouldInterruptCurrentMovement())
        {
            HandleMovementInterruption(() => BeginOrder(orderToStart));
        }
        else
        {
            BeginOrder(orderToStart);
        }
    }

    private bool ShouldInterruptCurrentMovement()
    {
        return CurrentStationId == StationId.Rest && Status == WorkerStatus.Running;
    }

    private void HandleMovementInterruption(Action callback)
    {
        Debug.Log("Movement interrupted for new order");
        InterruptMovement(callback);
    }

    internal void OnOrderCompleted()
    {
        FinishedOrdersCount++;
        AssignNextOrderOrRest();
    }

    private void AssignNextOrderOrRest()
    {
        // Try to get an order once; if none, rest without retry loop
        if (KitchenManager.instance.GiveMeOrder(this)) return;
        Rest();
    }
    #endregion

    #region Order Workflow
    private void BeginOrder(Order order)
    {
        currentOrder = order;
        currentOrder.assignedWorker = this;
        currentOrder.status = OrderStatus.InProgress;
        currentOrder.orderStartTime = Time.time;
        SetCurrentDish(order.dish);

        StartCoroutine(RunOrder(order));
    }

    private IEnumerator RunOrder(Order order)
    {
        if (order?.dish == null || order.dish.requiredStations == null || order.dish.requiredStations.Count == 0)
        {
            yield break;
        }

        int requiredStationsCount = order.dish.requiredStations.Count;
        for (int stationIndex = 0; stationIndex < requiredStationsCount; stationIndex++)
        {
            StationId stationId = order.dish.requiredStations[stationIndex];

            // Move
            yield return StartCoroutine(MoveToStation(stationId));

            // Pre-station events
            if (stationIndex == 1)
            {
                order.orderStarted?.Invoke();
            }

            // Prep text
            string prepText = null;
            if (order.dish.stationPrepText != null && stationIndex < order.dish.stationPrepText.Count)
            {
                prepText = order.dish.stationPrepText[stationIndex];
            }

            Station station = KitchenManager.instance?.GetStation(stationId);
            float baseTime = station != null ? station.stationTime : 0f;

            // Wait for car at checkout before doing the final work
            if (stationId == StationId.CheckOut && order.assignedCar != null)
            {
                yield return new WaitUntil(() => order.assignedCar.reachedPickupPoint);
            }

            // Execute work
            TriggerPrepStarted(prepText);
            float efficiencyMultiplier = 1 + CurrentStationEfficiency;
            float workTime = Mathf.Max(0.01f, baseTime / efficiencyMultiplier);
            yield return StartCoroutine(CountdownCoroutine(workTime));
            TriggerPrepFinished();

            // Post-station events
            if (stationId == StationId.CheckIn)
            {
                // Signal that the order has been fully taken at CheckIn
                order.orderRequested?.Invoke();
            }
            if (stationIndex == requiredStationsCount - 2)
            {
                order.orderPrepared?.Invoke();
            }

            // If this was checkout, complete and exit
            if (stationId == StationId.CheckOut)
            {
                CompleteOrder(order);
                yield break;
            }
        }
    }

    private void CompleteOrder(Order order)
    {
        if (order == null) return;

        order.status = OrderStatus.Completed;
        order.orderHanded?.Invoke();
        CurrencyManager.instance?.addFunds(order.dish.itemPrice);

        SetCurrentDish(null);
        currentOrder = null;

        OnOrderCompleted();
    }
    #endregion

    #region BaseWorker Implementation
    public override bool IsFree()
    {
        return CurrentStationId == StationId.Rest;
    }

    public override void Rest()
    {
        StartCoroutine(MoveToStation(StationId.Rest));
    }
    #endregion

    #region Item Management
    internal void SetCurrentDish(DishSo dish)
    {
        currentDish = dish;
    }

    public bool EquipItem(string itemId)
    {
        return itemsHandler?.EquipItem(itemId) ?? false;
    }

    public bool HasItem(string itemId)
    {
        return itemsHandler?.HasItem(itemId) ?? false;
    }

    public Item GetItem(string itemId)
    {
        return itemsHandler?.GetItem(itemId);
    }

    public bool UseItem(string itemId)
    {
        return itemsHandler?.UseItem(itemId) ?? false;
    }
    #endregion
}
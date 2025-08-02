using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public enum WorkerStatus
{
    Running, AtStation
}

public static class WorkerBaseStats
{
    public static float movementSpeed = 2;
    public static float stationEfficiency = 0.1f;
}

public class Worker : MonoBehaviour
{

    //WORKER STATS
    public float movementSpeed;
    public float stationEfficiency;
    public List<Utensil> equippedUtensils;
    public int FinishedOrdersCount;

    //WORKER TRACKERS
    public StationId currentStationId;//when running this value is the destination. When at station this stores the station where the worker is working
    public WorkerStatus currentStatus;
    private Order currentOrder;
    private int stationsCompletedCount;//this keeps track of which station the worker is in, out of all the statiosn that he has to go to
    private Station currentWorkStation;
    private int requiredStationsCount;

    //MOVEMENT VARS
    [NonSerialized]
    public bool interruptMovementFlag = false;
    public Action postInterruptAction;


    [Header("UI")]
    //UI
    public Image stationProgress;
    public float fillInterval = 1.0f;

    private float bonusReduction = 0f;
    private WokerVFXManager vfxManager;



    void Start()
    {
        KitchenManager.instance.addWorker(this);
        vfxManager = GetComponent<WokerVFXManager>();
        movementSpeed = WorkerBaseStats.movementSpeed;
        stationEfficiency = WorkerBaseStats.stationEfficiency;
    }

    public void startOrder(Order order)
    {
        //worker was going to rest UNIQUE SCENARIO
        if(currentStationId == StationId.Rest && currentStatus == WorkerStatus.Running)
        {

            Debug.Log("interrupted");
            interruptMovementFlag = true;
            postInterruptAction = () =>
            {
                PreProcessOrder(order);
                StartCoroutine(workLoop(order));
            };
        }
        else
        {
            PreProcessOrder(order);
            StartCoroutine(workLoop(order));
        }


    }

    private void PreProcessOrder(Order order)
    {
        currentOrder = order;
        //currentOrder.dish.requiredStations.Insert(0,StationId.CheckIn);
        //currentOrder.dish.requiredStations.Add(StationId.CheckOut);
        currentOrder.status = OrderStatus.InProgress;
        currentOrder.orderStartTime = Time.time;
        stationsCompletedCount = 0;
        requiredStationsCount = order.dish.requiredStations.Count;
    }
    private void PreStationChecks()
    {

        if (stationsCompletedCount == 1)
        {
            currentOrder.orderStarted?.Invoke();
        }
    }

    private void PostStationChecks()
    {
        if (currentStationId == StationId.CheckIn)
        {
            currentOrder.orderRequested?.Invoke();
        }
        else if (stationsCompletedCount == requiredStationsCount - 2)
        {
            currentOrder.orderPrepared?.Invoke();
        }
        else if (stationsCompletedCount == requiredStationsCount - 1)
        {
            currentOrder.orderHanded?.Invoke();

        }
    }

    private void OrderComplete()
    {
        currentOrder.status = OrderStatus.Completed;
        currentOrder.orderHanded?.Invoke();
        FinishedOrdersCount++;
        if (!KitchenManager.instance.GiveMeOrder(this))
        {
            Rest();
        }
    }

    private IEnumerator barFill(float seconds, bool reverse = false)
    {
        float percentComplete = 0;
        float elapsedTime = 0;

        while (percentComplete < 1.0f)
        {
            elapsedTime += Time.deltaTime;
            percentComplete = elapsedTime / seconds;
            stationProgress.fillAmount = percentComplete;

            yield return null; // Wait for next frame
        }

        // Ensure it's exactly 1.0 at the end
        stationProgress.fillAmount = 1.0f;
    }
    private IEnumerator workLoop(Order order)
    {

        //Ensure that we start at check in counter
        if (stationsCompletedCount == 0)
        {
            yield return StartCoroutine(MoveFromCurrentPlaceToStation(StationId.CheckIn));

        }

        if (stationsCompletedCount == order.dish.requiredStations.Count)
        {
            yield return StartCoroutine(MoveFromCurrentPlaceToStation(StationId.CheckOut));

        }

        currentWorkStation = KitchenManager.instance.GetStation(order.dish.requiredStations[stationsCompletedCount]);

        float waitTime = currentWorkStation.stationTime;

        PreStationChecks();

        yield return StartCoroutine(CountdownCoroutine(Mathf.Lerp(waitTime, 0.1f, stationEfficiency)));

        PostStationChecks();

        //ORDER FINISHED
        if (currentWorkStation.StationId == StationId.CheckOut) 
        {
            OrderComplete();
            yield return null;
        }
        else
        {
            
            stationsCompletedCount++;
            yield return StartCoroutine(MoveFromCurrentPlaceToStation(order.dish.requiredStations[stationsCompletedCount]));
            StartCoroutine(workLoop(order));
        }


        //go to next station
        yield return null;
    }

    //bonus is in seconds
    public void OnWorkerClicked(float bonus = 0.5f)
    {
        if(currentStatus == WorkerStatus.AtStation && currentStationId != StationId.Rest)
        {
            bonusReduction = bonus;
            
        }
    }

    private IEnumerator CountdownCoroutine(float waitTime)
    {
        float currentTime = waitTime;
        stationProgress.fillAmount = 0;


        while (currentTime > 0f)
        {
            // Calculate the reduction for this frame
            float frameReduction = Time.deltaTime;

            // Add any bonus reduction that was triggered
            if (bonusReduction > 0f)
            {
                frameReduction += bonusReduction;
                bonusReduction = 0f; // Reset after using it
                vfxManager.onClicked();
            }
            currentTime -= frameReduction;
            currentTime = Mathf.Max(0f, currentTime);

            float percentComplete = (waitTime - currentTime) / waitTime;
            stationProgress.fillAmount = percentComplete;
            yield return null; // Wait for next frame
        }
        stationProgress.fillAmount = 1;
    }

    //THESE MOVEMENT FUNCTIONS ASSUME THAT YOU HAVE CHECKED FOR FREE SLOTS
    private IEnumerator MoveFromCurrentPlaceToStation(StationId destinationStationId)
    {
        if(destinationStationId == currentStationId)
        {
            yield return null;
        }
        // Release current station if at one
        if (currentStatus == WorkerStatus.AtStation)
        {
            KitchenManager.instance.GetStation(currentStationId).ReleaseStandingLocation(this);
        }

        Vector3 startingPos = transform.position;
        Station destinationStation = KitchenManager.instance.GetStation(destinationStationId);
        Transform slotTransform = destinationStation.ReserveAvailableStandingLocation(this);
        currentStationId = destinationStationId;

        if (slotTransform != null)
        {
            // Direct movement to available slot
            yield return StartCoroutine(MoveDirectlyToSlot(startingPos, slotTransform.position));
        }
        else
        {
            // Movement with waiting for slot to become available
            yield return StartCoroutine(MoveWithWaitingForSlot(startingPos, destinationStation));
        }
    }

    private IEnumerator MoveDirectlyToSlot(Vector3 startPos, Vector3 endPos)
    {
        float distance = (startPos - endPos).magnitude;
        float timeToDestination = distance / movementSpeed;
        float percentCompleted = 0.0f;
        float startTime = Time.time;

        while (percentCompleted < 1.0f && !interruptMovementFlag)
        {
            float elapsedTime = Time.time - startTime;
            percentCompleted = elapsedTime / timeToDestination;
            percentCompleted = Mathf.Clamp01(percentCompleted);
            transform.position = Vector3.Lerp(startPos, endPos, percentCompleted);
            currentStatus = WorkerStatus.Running;
            yield return null;
        }

        if (interruptMovementFlag)
        {
            interruptMovementFlag = false;
            postInterruptAction?.Invoke();
            postInterruptAction = null;
        }
        else
        {
            currentStatus = WorkerStatus.AtStation;
        }
    }

    private IEnumerator MoveWithWaitingForSlot(Vector3 startPos, Station destinationStation)
    {
        Vector3 defaultSlotPos = destinationStation.GetDefaultSlot().position;
        float distance = (startPos - defaultSlotPos).magnitude;
        float timeToDestination = distance / movementSpeed;
        float percentCompleted = 0.0f;
        float startTime = Time.time;

        // Set up waiting mechanism for slot
        Transform freedSlot = null;
        Action<Transform> onSlotFreed = (Transform slot) => {
            freedSlot = slot;
        };
        destinationStation.ReserveUnavailableStandingLocation(onSlotFreed);

        // Move halfway to default slot
        while (percentCompleted <= 0.5f)
        {
            float elapsedTime = Time.time - startTime;
            percentCompleted = elapsedTime / timeToDestination;
            percentCompleted = Mathf.Clamp01(percentCompleted);
            transform.position = Vector3.Lerp(startPos, defaultSlotPos, percentCompleted);
            currentStatus = WorkerStatus.Running;
            yield return null;
        }

        // Wait for slot to become available
        yield return new WaitUntil(() => freedSlot != null);

        // Move from current position to the freed slot
        Vector3 currentPos = transform.position;
        Vector3 endPos = freedSlot.position;
        float remainingDistance = (currentPos - endPos).magnitude;
        float remainingTime = remainingDistance / movementSpeed;
        float secondPhasePercent = 0.0f;
        float secondPhaseStartTime = Time.time;

        while (secondPhasePercent < 1.0f && !interruptMovementFlag)
        {
            float elapsedTime = Time.time - secondPhaseStartTime;
            secondPhasePercent = elapsedTime / remainingTime;
            secondPhasePercent = Mathf.Clamp01(secondPhasePercent);
            transform.position = Vector3.Lerp(currentPos, endPos, secondPhasePercent);
            currentStatus = WorkerStatus.Running;
            yield return null;
        }

        if (interruptMovementFlag)
        {
            interruptMovementFlag = false;
            postInterruptAction?.Invoke();
            postInterruptAction = null;
        }
        else
        {
            currentStatus = WorkerStatus.AtStation;
        }
    }


    public void Rest()
    {
        StartCoroutine(MoveFromCurrentPlaceToStation(StationId.Rest));
    }

    public bool isFree()
    {
        if(currentStationId == StationId.Rest)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //PROGRAMMING/RDITOR HELPERS 

    public void InitializeWorker()
    {
        currentStatus = WorkerStatus.AtStation;
        currentStationId = StationId.Rest;
    }

}
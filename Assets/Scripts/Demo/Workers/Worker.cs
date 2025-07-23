using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using UnityEditor.UIElements;
using System.Threading;
using System;

public enum WorkerStatus
{
    Running, AtStation
}

public class Worker : MonoBehaviour
{

    //WORKER STATS
    public float movementSpeed;
    public float stationEfficiency;
    public List<Utensil> equippedUtensils;

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




    void Start()
    {
        KitchenManager.instance.addWorker(this);
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
                StartCoroutine(workLoop(currentOrder,false));
            };
        }
        else
        {
            PreProcessOrder(order);
            StartCoroutine(workLoop(currentOrder));
        }


    }

    private void PreProcessOrder(Order order)
    {
        currentOrder = order;
        currentOrder.status = OrderStatus.InProgress;
        currentOrder.orderStartTime = Time.time;
        stationsCompletedCount = 0;
        requiredStationsCount = order.requiredStations.Length;
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
    private IEnumerator workLoop(Order order, bool startingFromStation = true)
    {
        //Ensure that we start at check in counter
        if (stationsCompletedCount == 0 && currentStationId != StationId.CheckIn)
        {
            //IDEALLY YOU WANT ONE FUNCTION TO FACILITATE THE MOVEMENT 
            //BUT FOR NOW WE SHALL USE WORKAROUNDS
            if (startingFromStation)
            {
                yield return StartCoroutine(MoveFromStationToStation(StationId.CheckIn));                
            }
            else
            {
                yield return StartCoroutine(MoveFromCurrentPlaceToStation(StationId.CheckIn));
            }
        }


        currentWorkStation = KitchenManager.instance.GetStation(order.requiredStations[stationsCompletedCount]);

        float waitTime = currentWorkStation.stationTime;

        PreStationChecks();

        StartCoroutine(barFill(waitTime));
        yield return new WaitForSeconds(waitTime);

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
            yield return StartCoroutine(MoveFromStationToStation(order.requiredStations[stationsCompletedCount]));
            StartCoroutine(workLoop(order));
        }


        //go to next station
        yield return null;
    }

    //THESE MOVEMENT FUNCTIONS ASSUME THAT YOU HAVE CHECKED FOR FREE SLOTS
    private IEnumerator MoveFromStationToStation(StationId destinationStationId)
    {
        bool reversed = false;
        GameObject path = MovementManager.instance.GetPathObject(currentStationId, destinationStationId, ref reversed);

        // Get the SplineContainer component, then access its Spline
        SplineContainer splineContainer = path.GetComponent<SplineContainer>();
        Spline pathSpline = splineContainer.Spline;

        float distance = pathSpline.GetLength();
        float timeToDestination = distance / movementSpeed;
        float percentCompleted = 0.0f;
        float startTime = Time.time;

        ///START HERE
        //MOVEMENT FLOW
        currentStationId = destinationStationId;
        if (!reversed)
        {
            // Start at spline position 0 and move to position 1
            transform.position = pathSpline.EvaluatePosition(0.0f);
            while (percentCompleted < 1.0f && !interruptMovementFlag)
            {
                float elapsedTime = Time.time - startTime;
                percentCompleted = elapsedTime / timeToDestination;
                // Clamp to ensure we don't go past 1.0
                percentCompleted = Mathf.Clamp01(percentCompleted);
                // Move along the spline
                transform.position = pathSpline.EvaluatePosition(percentCompleted);
                currentStatus = WorkerStatus.Running;
                yield return null; // Wait for next frame
            }
        }
        else
        {
            // Start at spline position 1 and move to position 0
            transform.position = pathSpline.EvaluatePosition(1.0f);
            while (percentCompleted < 1.0f && !interruptMovementFlag)
            {
                float elapsedTime = Time.time - startTime;
                percentCompleted = elapsedTime / timeToDestination;
                // Clamp to ensure we don't go past 1.0
                percentCompleted = Mathf.Clamp01(percentCompleted);
                // Move along the spline in reverse (1.0 to 0.0)
                float splinePosition = 1.0f - percentCompleted;
                transform.position = pathSpline.EvaluatePosition(splinePosition);
                currentStatus = WorkerStatus.Running;
                yield return null; // Wait for next frame
            }

        }

        if (interruptMovementFlag) 
        {
            //reset the flag
            interruptMovementFlag = false;
            postInterruptAction?.Invoke();
            postInterruptAction = null;
        }
        else
        {
            currentStatus = WorkerStatus.AtStation;            
        }
    }

    private IEnumerator MoveFromCurrentPlaceToStation(StationId destinationStationId)
    {
        Vector3 startingPos = transform.position;
        Vector3 endPos = KitchenManager.instance.GetStation(destinationStationId).GetAvailableStandingLocation().position;
        float distance = (startingPos - endPos).magnitude;
        float timeToDestination = distance / movementSpeed;
        float percentCompleted = 0.0f;
        float startTime = Time.time;

        ///START HERE
        //MOVEMENT FLOW
        currentStationId = destinationStationId;
        while (percentCompleted < 1.0f && !interruptMovementFlag)
        {
            float elapsedTime = Time.time - startTime;
            percentCompleted = elapsedTime / timeToDestination;
            // Clamp to ensure we don't go past 1.0
            percentCompleted = Mathf.Clamp01(percentCompleted);
            //TODO
            // Move along the path using above information
            transform.position = Vector3.Lerp(startingPos, endPos, percentCompleted);
            currentStatus = WorkerStatus.Running;
            yield return null; // Wait for next frame
        }

        if (interruptMovementFlag)
        {
            //reset the flag
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
        StartCoroutine(MoveFromStationToStation(StationId.Rest));
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
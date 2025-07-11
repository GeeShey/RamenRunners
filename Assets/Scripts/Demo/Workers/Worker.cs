using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public enum WorkerStatus
{
    Running, AtStation, Free
}

public class Worker : MonoBehaviour
{
    public float movementSpeed;
    public float stationEfficiency;
    public StationId currentStation;
    public WorkerStatus currentStatus;
    public List<Utensil> equippedUtensils;

    private Order currentOrder;
    private int currentStationIndex;//this keeps track of which station the worker is in, out of all the statiosn that he has to go to
    private Transform targetPosition;
    private Station currentWorkStation;
    private float workTimer;
    private bool isWorking;

    private int requiredStationsCount;

    [Header("USE THIS ONLY FOR EDITIOR")]
    public GameObject checkInStation;


    void Start()
    {
        KitchenManager.instance.addWorker(this);
        currentStatus = WorkerStatus.Free;
    }

    public void startOrder(Order order)
    {
        currentOrder = order;
        currentOrder.status = OrderStatus.InProgress;
        currentStationIndex = 0;
        //STARTING THE ORDER
        requiredStationsCount = order.requiredStations.Length;

        StartCoroutine(workLoop(currentOrder));
    }

    private IEnumerator MoveTo(StationId destinationStationId)
    {
        bool reversed = false;
        GameObject path = MovementManager.instance.GetPathObject(currentStation, destinationStationId, ref reversed);

        // Get the SplineContainer component, then access its Spline
        SplineContainer splineContainer = path.GetComponent<SplineContainer>();
        Spline pathSpline = splineContainer.Spline;

        float distance = pathSpline.GetLength();
        float timeToDestination = distance / movementSpeed;
        float percentCompleted = 0.0f;
        float startTime = Time.time;

        if (!reversed)
        {
            // Start at spline position 0 and move to position 1
            transform.position = pathSpline.EvaluatePosition(0.0f);
            while (percentCompleted < 1.0f)
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
            while (percentCompleted < 1.0f)
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

        // Update current station after movement is complete
        currentStation = destinationStationId;
        currentStatus = WorkerStatus.AtStation;
    }
    private IEnumerator workLoop(Order order)
    {
        //Ensure that we start at check in counter
        if (currentStationIndex == 0 && currentStation != StationId.CheckIn)
        {
            yield return StartCoroutine(MoveTo(StationId.CheckIn));
        }

        Station currentWorkStation = KitchenManager.instance.GetStation(order.requiredStations[currentStationIndex]);


        //INVOKING THE RIGHT EVENTS
        if (currentWorkStation.StationId == StationId.CheckIn)
        {
            currentOrder.orderRequested?.Invoke();
        }
        else if (currentWorkStation.StationId == StationId.CheckOut)
        {
            currentOrder.orderHanded?.Invoke();
        }
        else if(currentStationIndex == 1)
        {
            currentOrder.orderStarted?.Invoke();
        }

        float waitTime = currentWorkStation.stationTime;

        Debug.Log("Started work at station "+ currentWorkStation.name);
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Ended work at station " + currentWorkStation.name);

        if (currentStationIndex == requiredStationsCount - 1)
        {
            currentOrder.orderPrepared?.Invoke();
        }

        //ORDER FINISHED
        if (currentWorkStation.StationId == StationId.CheckOut) 
        {
            currentOrder.status = OrderStatus.Completed;
            currentOrder.orderHanded?.Invoke();
            currentStatus = WorkerStatus.Free;

            yield return null;
        }
        else
        {
            
            currentStationIndex++;
            yield return StartCoroutine(MoveTo(order.requiredStations[currentStationIndex]));
            StartCoroutine(workLoop(order));
        }


        //go to next station
        yield return null;
    }


    //PROGRAMMING/RDITOR HELPERS 

    public void InitializeWorker()
    {

        transform.position = checkInStation.transform.position;
        currentStatus = WorkerStatus.Free;
        currentStation = StationId.CheckIn;
    }

}
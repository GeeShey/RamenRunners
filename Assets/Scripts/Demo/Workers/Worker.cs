using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEditor.Experimental.GraphView;

public enum WorkerStatus
{
    Running, AtStation, Waiting
}

public static class WorkerBaseStats
{
    public static float movementSpeed = 2;
    public static float stationEfficiency = 0.1f;
}

public class Worker : MonoBehaviour
{
    #region Worker Stats
    //WORKER STATS
    public float CurrentMovementSpeed;
    public float CurrentStationEfficiency;
    public List<Utensil> equippedUtensils;
    public int FinishedOrdersCount;
    #endregion

    #region Worker Trackers
    //WORKER TRACKERS
    public StationId currentStationId; //when running this value is the destination. When at station this stores the station where the ItemOwner is working
    public WorkerStatus currentStatus;
    private Order currentOrder;
    private int stationsCompletedCount; //this keeps track of which station the ItemOwner is in, out of all the stations that he has to go to
    public Station currentWorkStation;
    private int requiredStationsCount;
    #endregion

    #region Movement Variables
    //MOVEMENT VARS
    [NonSerialized]
    public bool interruptMovementFlag = false;
    public Action postInterruptAction;
    public delegate IEnumerator MovementMethod(StationId stationId);
    public MovementMethod CurrentMovementMethod;
    #endregion

    #region UI Components
    [Header("UI")]
    //UI
    public Image stationProgress;
    public float fillInterval = 1.0f;
    #endregion

    #region Private Fields
    private float bonusReduction = 0f;
    private WokerVFXManager vfxManager;
    #endregion

    #region Events
    //HELPERS FOR OTHER CLASSES
    public Action<string> onPrepStarted;
    public Action onPrepFinished;

    public Action<string> onMovementStarted;
    //FUNC IS BASICALLY AN ACTION BUT RETURNS A VALUE WHEN YOU CALL INVOKE()
    public Func<IEnumerator> initializeMovementMethod;

    #endregion

    #region Debug
    //DEBUG
    public DishSo currentDish;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        InitializeWorkerComponents();
        InitializeWorkerStats();
        InitializeExternalSystems();
    }
    #endregion

    #region Initialization
    private void InitializeWorkerComponents()
    {
        KitchenManager.instance.addWorker(this);
        vfxManager = GetComponent<WokerVFXManager>();
    }

    private void InitializeWorkerStats()
    {
        CurrentMovementSpeed = WorkerBaseStats.movementSpeed;
        CurrentStationEfficiency = WorkerBaseStats.stationEfficiency;
        CurrentMovementMethod = NormalMove;
    }

    private void InitializeExternalSystems()
    {
        //CarManager.instance.InitializeNewCar();
    }
    #endregion

    #region Order Management
    public void startOrder(Order orderToStart)
    {
        orderToStart.assignedWorker = this;
        currentDish = orderToStart.dish;

        if (ShouldInterruptCurrentMovement())
        {
            HandleMovementInterruption(orderToStart);
        }
        else
        {
            StartOrderDirectly(orderToStart);
        }
    }

    private bool ShouldInterruptCurrentMovement()
    {
        return currentStationId == StationId.Rest && currentStatus == WorkerStatus.Running;
    }

    private void HandleMovementInterruption(Order orderToStart)
    {
        Debug.Log("interrupted");
        interruptMovementFlag = true;
        postInterruptAction = () =>
        {
            PreProcessOrder(orderToStart);
            StartCoroutine(workLoop(orderToStart));
        };
    }

    private void StartOrderDirectly(Order orderToStart)
    {
        PreProcessOrder(orderToStart);
        StartCoroutine(workLoop(orderToStart));
    }

    private void PreProcessOrder(Order orderToProcess)
    {
        currentOrder = orderToProcess;
        SetOrderInProgress();
        ResetOrderCounters();
    }

    private void SetOrderInProgress()
    {
        currentOrder.status = OrderStatus.InProgress;
        currentOrder.orderStartTime = Time.time;
    }

    private void ResetOrderCounters()
    {
        stationsCompletedCount = 0;
        requiredStationsCount = currentOrder.dish.requiredStations.Count;
    }

    private void OrderComplete()
    {
        FinalizeOrderStatus();
        ProcessOrderPayment();
        UpdateWorkerStats();
        AssignNextOrderOrRest();
    }

    private void FinalizeOrderStatus()
    {
        currentOrder.status = OrderStatus.Completed;
        currentOrder.orderHanded?.Invoke();
    }

    private void ProcessOrderPayment()
    {
        CurrencyManager.instance.addFunds(currentOrder.dish.itemPrice);
    }

    private void UpdateWorkerStats()
    {
        FinishedOrdersCount++;
    }

    private void AssignNextOrderOrRest()
    {
        if (!KitchenManager.instance.GiveMeOrder(this))
        {
            Rest();
        }
        //CurrencyFallingEffectController.instance.activateEffect();
    }
    #endregion

    #region Station Processing
    private void PreStationChecks()
    {
        if (IsFirstWorkingStation())
        {
            currentOrder.orderStarted?.Invoke();
        }
    }

    private bool IsFirstWorkingStation()
    {
        return stationsCompletedCount == 1;
    }

    private void PostStationChecks()
    {
        if (IsCheckInStation())
        {
            currentOrder.orderRequested?.Invoke();
        }
        else if (IsOrderAlmostComplete())
        {
            currentOrder.orderPrepared?.Invoke();
        }
    }

    private bool IsCheckInStation()
    {
        return currentStationId == StationId.CheckIn;
    }

    private bool IsOrderAlmostComplete()
    {
        return stationsCompletedCount == requiredStationsCount - 2;
    }
    #endregion

    #region Work Loop
    private IEnumerator workLoop(Order orderToProcess)
    {
        SetCurrentWorkStation(orderToProcess);
        yield return StartCoroutine(MoveToCurrentStation());

        float stationWaitTime = CalculateStationWaitTime();

        PreStationChecks();
        TriggerPrepStartedEvent(orderToProcess);

        yield return StartCoroutine(CountdownCoroutine(stationWaitTime));

        PostStationChecks();

        if (IsCheckoutStation())
        {
            OrderComplete();
            yield return null;
        }
        else
        {
            AdvanceToNextStation(orderToProcess);
        }

        yield return null;
    }

    private void SetCurrentWorkStation(Order orderToProcess)
    {
        currentWorkStation = KitchenManager.instance.GetStation(orderToProcess.dish.requiredStations[stationsCompletedCount]);
    }

    private IEnumerator MoveToCurrentStation()
    {
        //BroadcastMessage("AboutToStartMoving", SendMessageOptions.DontRequireReceiver);

        if (initializeMovementMethod!= null)
        {
            foreach (Func<IEnumerator> action in initializeMovementMethod.GetInvocationList())
            {
                // wadafak going on here? u may ask lemme explain
                // so basically, if there are multiple methods subscribed to the initializeMovementMethod func,
                // we want to call them one by one and wait for each to finish before proceeding to the next
                yield return StartCoroutine(action?.Invoke());
            }
        }
        yield return StartCoroutine(CurrentMovementMethod(currentWorkStation.StationId));
    }

    private float CalculateStationWaitTime()
    {
        float baseTime = currentWorkStation.stationTime;
        float efficiencyMultiplier = 1 + CurrentStationEfficiency;
        return Mathf.Max(0.01f, baseTime / efficiencyMultiplier);
    }

    private void TriggerPrepStartedEvent(Order orderToProcess)
    {
        string prepText = orderToProcess.dish.stationPrepText[stationsCompletedCount];
        onPrepStarted?.Invoke(prepText);
    }

    private bool IsCheckoutStation()
    {
        return currentWorkStation.StationId == StationId.CheckOut;
    }

    private void AdvanceToNextStation(Order orderToProcess)
    {
        stationsCompletedCount++;
        StartCoroutine(workLoop(orderToProcess));
    }
    #endregion

    #region Worker Interaction
    //bonus is in seconds
    public void RecieveBonusReduction(float bonusTimeReduction = 0.5f)
    {
        if (CanReceiveClickBonus())
        {
            bonusReduction += bonusTimeReduction;
        }
    }

    private bool CanReceiveClickBonus()
    {
        return currentStatus == WorkerStatus.AtStation && currentStationId != StationId.Rest;
    }
    #endregion

    #region Timing and Progress
    private IEnumerator barFill(float durationInSeconds, bool shouldReverse = false)
    {
        float completionPercentage = 0;
        float timeElapsed = 0;

        while (completionPercentage < 1.0f)
        {
            timeElapsed += Time.deltaTime;
            completionPercentage = timeElapsed / durationInSeconds;
            stationProgress.fillAmount = completionPercentage;

            yield return null; // Wait for next frame
        }

        // Ensure it's exactly 1.0 at the end
        stationProgress.fillAmount = 1.0f;
    }

    private IEnumerator CountdownCoroutine(float totalWaitTime)
    {
        if (IsCheckoutStation())
        {
            yield return WaitForCarArrival();
        }

        currentWorkStation.stationWorkStarted?.Invoke(this);

        float remainingTime = totalWaitTime;
        ResetProgressBar();

        while (remainingTime > 0f)
        {
            float timeReductionThisFrame = CalculateTimeReduction();
            remainingTime = UpdateRemainingTime(remainingTime, timeReductionThisFrame);
            UpdateProgressBar(totalWaitTime, remainingTime);

            yield return null; // Wait for next frame
        }

        CompleteProgressBar();
    }

    private IEnumerator WaitForCarArrival()
    {
        yield return new WaitUntil(() => currentOrder.assignedCar.reachedPickupPoint == true);
        Debug.Log("car has reached pickup point");
    }

    private void ResetProgressBar()
    {
        stationProgress.fillAmount = 0;
    }

    private float CalculateTimeReduction()
    {
        float frameTimeReduction = Time.deltaTime;

        // Add any bonus reduction that was triggered
        if (HasBonusReduction())
        {
            frameTimeReduction += bonusReduction;
            ConsumeBonusReduction();
        }

        return frameTimeReduction;
    }

    private bool HasBonusReduction()
    {
        return bonusReduction > 0f;
    }

    private void ConsumeBonusReduction()
    {
        bonusReduction = 0f; // Reset after using it
        vfxManager.onClicked();
    }

    private float UpdateRemainingTime(float currentRemainingTime, float reductionAmount)
    {
        currentRemainingTime -= reductionAmount;
        return Mathf.Max(0f, currentRemainingTime);
    }

    private void UpdateProgressBar(float totalTime, float timeRemaining)
    {
        float completionPercentage = (totalTime - timeRemaining) / totalTime;
        stationProgress.fillAmount = completionPercentage;
    }

    private void CompleteProgressBar()
    {
        stationProgress.fillAmount = 1;
    }
    #endregion

    #region Movement System
    //THESE MOVEMENT FUNCTIONS ASSUME THAT YOU HAVE CHECKED FOR FREE SLOTS
    private IEnumerator NormalMove(StationId destinationStationId)
    {
        if (IsAlreadyAtDestination(destinationStationId))
        {
            yield return null;
        }

        ReleaseCurrentStationIfOccupied();

        Vector3 startingPosition = transform.position;
        Station destinationStation = KitchenManager.instance.GetStation(destinationStationId);
        Transform availableSlot = destinationStation.ReserveAvailableStandingLocation(this);
        currentStationId = destinationStationId;

        if (HasAvailableSlot(availableSlot))
        {
            yield return StartCoroutine(MoveDirectlyToSlot(startingPosition, availableSlot.position));
        }
        else
        {
            yield return StartCoroutine(MoveWithWaitingForSlot(startingPosition, destinationStation));
        }
    }

    private bool IsAlreadyAtDestination(StationId destination)
    {
        return destination == currentStationId;
    }

    private void ReleaseCurrentStationIfOccupied()
    {
        if (currentStatus == WorkerStatus.AtStation)
        {
            KitchenManager.instance.GetStation(currentStationId).ReleaseStandingLocation(this);
        }
    }

    private bool HasAvailableSlot(Transform slotTransform)
    {
        return slotTransform != null;
    }

    private IEnumerator MoveDirectlyToSlot(Vector3 startPosition, Vector3 targetPosition)
    {
        float totalDistance = (startPosition - targetPosition).magnitude;
        float totalTravelTime = totalDistance / CurrentMovementSpeed;
        float movementProgress = 0.0f;
        float movementStartTime = Time.time;

        TriggerMovementStartedEvent();

        while (movementProgress < 1.0f && !interruptMovementFlag)
        {
            movementProgress = CalculateMovementProgress(movementStartTime, totalTravelTime);
            UpdateWorkerPosition(startPosition, targetPosition, movementProgress);
            currentStatus = WorkerStatus.Running;
            yield return null;
        }

        HandleMovementCompletion();
    }

    private void TriggerMovementStartedEvent()
    {
        string stationName = Enum.GetName(typeof(StationId), currentStationId);
        onMovementStarted?.Invoke(stationName);
    }

    private float CalculateMovementProgress(float startTime, float totalTime)
    {
        float elapsedTime = Time.time - startTime;
        float progress = elapsedTime / totalTime;
        return Mathf.Clamp01(progress);
    }

    private void UpdateWorkerPosition(Vector3 start, Vector3 end, float progress)
    {
        transform.position = Vector3.Lerp(start, end, progress);
    }

    private void HandleMovementCompletion()
    {
        if (interruptMovementFlag)
        {
            ProcessMovementInterruption();
        }
        else
        {
            currentStatus = WorkerStatus.AtStation;
        }
    }

    private void ProcessMovementInterruption()
    {
        interruptMovementFlag = false;
        postInterruptAction?.Invoke();
        postInterruptAction = null;
    }

    private IEnumerator MoveWithWaitingForSlot(Vector3 startPosition, Station targetStation)
    {
        Vector3 defaultSlotPosition = targetStation.GetDefaultSlot().position;
        float totalDistance = (startPosition - defaultSlotPosition).magnitude;
        float totalTravelTime = totalDistance / CurrentMovementSpeed;

        Transform freedSlotTransform = null;
        Action<Transform> onSlotFreed = (Transform slot) => {
            freedSlotTransform = slot;
        };
        targetStation.ReserveUnavailableStandingLocation(onSlotFreed);

        // Move halfway to default slot
        yield return StartCoroutine(MoveToWaitingPosition(startPosition, defaultSlotPosition, totalTravelTime));

        currentStatus = WorkerStatus.Waiting;

        // Wait for slot to become available
        yield return new WaitUntil(() => freedSlotTransform != null);

        // Move from current position to the freed slot
        yield return StartCoroutine(MoveToFreedSlot(freedSlotTransform));
    }

    private IEnumerator MoveToWaitingPosition(Vector3 start, Vector3 defaultSlot, float totalTime)
    {
        float movementProgress = 0.0f;
        float startTime = Time.time;

        while (movementProgress <= 0.5f)
        {
            movementProgress = CalculateMovementProgress(startTime, totalTime);
            movementProgress = Mathf.Clamp01(movementProgress);
            UpdateWorkerPosition(start, defaultSlot, movementProgress);
            currentStatus = WorkerStatus.Running;
            yield return null;
        }
    }

    private IEnumerator MoveToFreedSlot(Transform freedSlot)
    {
        Vector3 currentPosition = transform.position;
        Vector3 finalPosition = freedSlot.position;
        float remainingDistance = (currentPosition - finalPosition).magnitude;
        float remainingTravelTime = remainingDistance / CurrentMovementSpeed;
        float secondPhaseProgress = 0.0f;
        float secondPhaseStartTime = Time.time;

        while (secondPhaseProgress < 1.0f && !interruptMovementFlag)
        {
            secondPhaseProgress = CalculateMovementProgress(secondPhaseStartTime, remainingTravelTime);
            UpdateWorkerPosition(currentPosition, finalPosition, secondPhaseProgress);
            currentStatus = WorkerStatus.Running;
            yield return null;
        }

        HandleMovementCompletion();
    }
    #endregion

    #region Worker State Management
    public void Rest()
    {
        StartCoroutine(CurrentMovementMethod(StationId.Rest));
    }

    public bool isFree()
    {
        return currentStationId == StationId.Rest;
    }
    #endregion

    #region Editor Helpers
    //PROGRAMMING/EDITOR HELPERS 
    public void InitializeWorker()
    {
        currentStatus = WorkerStatus.AtStation;
        currentStationId = StationId.Rest;
    }
    #endregion
}
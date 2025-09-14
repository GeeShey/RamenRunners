using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

[System.Serializable]
public class TestTask
{
    public StationId stationId;
    public float workDuration = 2.0f;
    public string taskDescription = "Working at station";
}

[System.Serializable]
public class TestItemConfig
{
    public GameObject itemPrefab;
    public int stackCount = 1;
}

public class TestWorker : BaseWorker
{
    #region Test Configuration
    [Header("Test Configuration")]
    [SerializeField] private bool enableTestMode = true;
    [SerializeField] private List<TestTask> testTasks = new List<TestTask>();
    [SerializeField] private List<TestItemConfig> testItems = new List<TestItemConfig>();
    [SerializeField] private bool loopTasks = true;
    [SerializeField] private float delayBetweenTasks = 1.0f;
    [SerializeField] private bool autoStartOnPlay = true;
    #endregion

    #region Runtime Status Display (Inspector Only)
    [Header("Runtime Status (Read Only)")]
    [SerializeField, HideInInspector] private string _statusDisplay;
    [SerializeField, HideInInspector] private string _currentStationDisplay;
    [SerializeField, HideInInspector] private string _taskProgressDisplay;
    [SerializeField, HideInInspector] private bool _isTestRunningDisplay;

    // Properties that will show in inspector
    [SerializeField] private string StatusDisplay => $"Status: {currentStatus}";
    [SerializeField] private string CurrentStationDisplay => $"Current Station: {currentStationId}";
    [SerializeField] private string TaskProgressDisplay => $"Task Progress: {currentTaskIndex + 1}/{testTasks.Count}";
    [SerializeField] private bool IsTestRunningDisplay => isTestRunning;
    #endregion

    #region Test State
    private int currentTaskIndex = 0;
    private bool isTestRunning = false;
    private Coroutine testCoroutine;
    #endregion

    #region Unity Lifecycle
    protected override void Start()
    {
        base.Start(); // Call BaseWorker's Start method
        InitializeTestWorker();

        if (enableTestMode && autoStartOnPlay)
        {
            StartTest();
        }
    }

    void Update()
    {
        // Update inspector display values in editor
#if UNITY_EDITOR
        UpdateInspectorDisplay();
#endif
    }

    void OnValidate()
    {
        // Ensure we have at least one task
        if (testTasks.Count == 0)
        {
            testTasks.Add(new TestTask { stationId = StationId.Rest, workDuration = 2.0f });
        }
    }
    #endregion

    #region Inspector Display Updates
#if UNITY_EDITOR
    private void UpdateInspectorDisplay()
    {
        _statusDisplay = $"Status: {currentStatus}";
        _currentStationDisplay = $"Current Station: {currentStationId}";
        _taskProgressDisplay = $"Task Progress: {currentTaskIndex + 1}/{testTasks.Count}";
        _isTestRunningDisplay = isTestRunning;
    }
#endif
    #endregion

    #region Initialization
    private void InitializeTestWorker()
    {
        // Set initial state
        currentStatus = WorkerStatus.AtStation;
        currentStationId = StationId.Rest;

        // Register with KitchenManager if it exists
        if (KitchenManager.instance != null)
        {
            KitchenManager.instance.addWorker(this);
        }

        // Initialize VFX manager
        vfxManager = GetComponent<WokerVFXManager>();
    }

    #endregion

    #region Test Control Methods
    [ContextMenu("Start Test")]
    public void StartTest()
    {
        if (!enableTestMode)
        {
            Debug.LogWarning("Test mode is disabled!");
            return;
        }

        if (isTestRunning)
        {
            Debug.LogWarning("Test is already running!");
            return;
        }

        if (testTasks.Count == 0)
        {
            Debug.LogWarning("No test tasks configured!");
            return;
        }

        currentTaskIndex = 0;
        isTestRunning = true;
        testCoroutine = StartCoroutine(RunTestSequence());

        Debug.Log($"Starting test with {testTasks.Count} tasks. Loop: {loopTasks}");
    }

    [ContextMenu("Stop Test")]
    public void StopTest()
    {
        if (testCoroutine != null)
        {
            StopCoroutine(testCoroutine);
            testCoroutine = null;
        }

        isTestRunning = false;
        Debug.Log("Test stopped");
    }

    [ContextMenu("Next Task")]
    public void NextTask()
    {
        if (!isTestRunning) return;

        interruptMovementFlag = true;
        postInterruptAction = () => {
            currentTaskIndex = (currentTaskIndex + 1) % testTasks.Count;
        };
    }

    #endregion

    #region Test Execution
    private IEnumerator RunTestSequence()
    {
        while (isTestRunning)
        {
            if (testTasks.Count == 0) break;
            TestTask currentTask = testTasks[currentTaskIndex];
            //Debug.Log($"Executing task {currentTaskIndex + 1}/{testTasks.Count}: {currentTask.taskDescription} at {currentTask.stationId}");
            yield return StartCoroutine(ExecuteTestTask(currentTask));

            // Wait between tasks
            if (delayBetweenTasks > 0)
            {
                yield return new WaitForSeconds(delayBetweenTasks);
            }

            // Move to next task
            currentTaskIndex++;

            // Check if we should loop or stop
            if (currentTaskIndex >= testTasks.Count)
            {
                if (loopTasks)
                {
                    currentTaskIndex = 0;
                    //Debug.Log("Looping back to first task");
                }
                else
                {
                    break;
                }
            }
        }

        isTestRunning = false;
        Debug.Log("Test sequence completed");
    }

    private IEnumerator ExecuteTestTask(TestTask task)
    {
        // Set current work station (like Worker script does)
        SetCurrentWorkStation(task.stationId);

        // Move to the station with proper slot reservation
        yield return StartCoroutine(MoveToCurrentStation());

        // Calculate station wait time with efficiency (like Worker script)
        float stationWaitTime = CalculateStationWaitTime(task.workDuration);

        // Trigger prep started event
        onPrepStarted?.Invoke(task.taskDescription);

        // Use the same countdown logic as Worker script
        yield return StartCoroutine(CountdownCoroutine(stationWaitTime));

        // Trigger prep finished event
        onPrepFinished?.Invoke();

        //Debug.Log($"Completed task: {task.taskDescription}");
    }

    private void SetCurrentWorkStation(StationId stationId)
    {
        if (KitchenManager.instance != null)
        {
            currentWorkStation = KitchenManager.instance.GetStation(stationId);
        }
    }

    private float CalculateStationWaitTime(float baseTime)
    {
        // Same logic as Worker script
        float efficiencyMultiplier = 1 + CurrentStationEfficiency;
        return Mathf.Max(0.01f, baseTime / efficiencyMultiplier);
    }
    #endregion

    #region Movement System - TestWorker-specific implementation
    private IEnumerator MoveToCurrentStation()
    {
        // Call initialize movement method (for items like Sandevistan)
        if (initializeMovementMethod != null)
        {
            foreach (Func<IEnumerator> action in initializeMovementMethod.GetInvocationList())
            {
                yield return StartCoroutine(action?.Invoke());
            }
        }

        // Use the enhanced movement method with slot reservation
        yield return StartCoroutine(CurrentMovementMethod(currentWorkStation.StationId));
    }

    protected override IEnumerator NormalMove(StationId destinationStationId)
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
        return destination == currentStationId && currentStatus == WorkerStatus.AtStation;
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

    #region Timing and Progress
    private IEnumerator CountdownCoroutine(float totalWaitTime)
    {
        // Trigger station work started event if we have a current work station
        if (currentWorkStation != null)
        {
            currentWorkStation.SomeoneStartedWorkAtStation?.Invoke(this);
        }

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

    private void ResetProgressBar()
    {
        if (stationProgress != null)
        {
            stationProgress.fillAmount = 0;
        }
    }

    private float UpdateRemainingTime(float currentRemainingTime, float reductionAmount)
    {
        currentRemainingTime -= reductionAmount;
        return Mathf.Max(0f, currentRemainingTime);
    }

    private void UpdateProgressBar(float totalTime, float timeRemaining)
    {
        if (stationProgress != null)
        {
            float completionPercentage = (totalTime - timeRemaining) / totalTime;
            stationProgress.fillAmount = completionPercentage;
        }
    }

    private void CompleteProgressBar()
    {
        if (stationProgress != null)
        {
            stationProgress.fillAmount = 1;
        }
    }
    #endregion

    #region BaseWorker Implementation
    public override bool isFree()
    {
        return !isTestRunning && currentStationId == StationId.Rest;
    }

    public override void Rest()
    {
        StartCoroutine(CurrentMovementMethod(StationId.Rest));
    }
    #endregion

    #region Public Interface
    public void AddTestTask(StationId stationId, float duration, string description = "Test Task")
    {
        testTasks.Add(new TestTask
        {
            stationId = stationId,
            workDuration = duration,
            taskDescription = description
        });
    }

    public void ClearTestTasks()
    {
        testTasks.Clear();
    }

    public void AddTestItem(GameObject itemPrefab, int stackCount = 1)
    {
        testItems.Add(new TestItemConfig
        {
            itemPrefab = itemPrefab,
            stackCount = stackCount
        });
    }
    #endregion
}
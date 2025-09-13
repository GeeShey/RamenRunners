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

public class TestWorker : MonoBehaviour
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

    #region Worker Components (Similar to original Worker)
    [Header("Worker Stats")]
    public float CurrentMovementSpeed;
    public float CurrentStationEfficiency;
    public List<Item> equippedItems = new List<Item>();
    public WorkerStatus currentStatus;
    public StationId currentStationId;
    public Station currentWorkStation;
    #endregion

    #region Movement Variables
    [NonSerialized]
    public bool interruptMovementFlag = false;
    public Action postInterruptAction;
    public Worker.MovementMethod CurrentMovementMethod;
    #endregion

    #region UI Components
    [Header("UI")]
    public Image stationProgress;
    public float fillInterval = 1.0f;
    #endregion

    #region Events (Compatible with items)
    public Action<string> onPrepStarted;
    public Action onPrepFinished;
    public Action<string> onMovementStarted;
    public Func<IEnumerator> initializeMovementMethod;
    #endregion

    #region Test State
    private int currentTaskIndex = 0;
    private bool isTestRunning = false;
    private Coroutine testCoroutine;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        InitializeTestWorker();

        if (enableTestMode && autoStartOnPlay)
        {
            StartTest();
        }
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

    #region Initialization
    private void InitializeTestWorker()
    {
        // Initialize base stats
        CurrentMovementSpeed = WorkerBaseStats.movementSpeed;
        CurrentStationEfficiency = WorkerBaseStats.stationEfficiency;
        CurrentMovementMethod = NormalMove;

        // Set initial state
        currentStatus = WorkerStatus.AtStation;
        currentStationId = StationId.Rest;

        // Register with KitchenManager if it exists
        if (KitchenManager.instance != null)
        {
            KitchenManager.instance.addWorker(ConvertToWorker());
        }

        // Initialize test items
        InitializeTestItems();
    }

    private void InitializeTestItems()
    {
        foreach (var itemConfig in testItems)
        {
            if (itemConfig.itemPrefab != null)
            {
                GameObject itemObj = Instantiate(itemConfig.itemPrefab, transform);
                Item item = itemObj.GetComponent<Item>();

                if (item != null)
                {
                    // Convert this TestWorker to Worker for item compatibility
                    Worker workerComponent = ConvertToWorker();
                    item.PreLoad(workerComponent);
                    item.OnStackModified(itemConfig.stackCount);
                    equippedItems.Add(item);

                    Debug.Log($"Equipped {item.ItemBasicDetails?.name ?? "Unknown Item"} with stack count {itemConfig.stackCount}");
                }
            }
        }
    }

    // Create a Worker component dynamically for item compatibility
    private Worker ConvertToWorker()
    {
        Worker worker = gameObject.GetComponent<Worker>();
        if (worker == null)
        {
            worker = gameObject.AddComponent<Worker>();
        }

        // Copy relevant properties
        worker.CurrentMovementSpeed = CurrentMovementSpeed;
        worker.CurrentStationEfficiency = CurrentStationEfficiency;
        worker.currentStatus = currentStatus;
        worker.currentStationId = currentStationId;
        worker.CurrentMovementMethod = CurrentMovementMethod;
        worker.stationProgress = stationProgress;
        worker.onPrepStarted = onPrepStarted;
        worker.onPrepFinished = onPrepFinished;
        worker.onMovementStarted = onMovementStarted;
        worker.initializeMovementMethod = initializeMovementMethod;

        return worker;
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

    [ContextMenu("Reset Test Items")]
    public void ResetTestItems()
    {
        // Clear existing items
        foreach (var item in equippedItems)
        {
            if (item != null)
            {
                DestroyImmediate(item.gameObject);
            }
        }
        equippedItems.Clear();

        // Reinitialize items
        InitializeTestItems();
        Debug.Log("Test items reset and reinitialized");
    }
    #endregion

    #region Test Execution
    private IEnumerator RunTestSequence()
    {
        while (isTestRunning)
        {
            if (testTasks.Count == 0) break;

            TestTask currentTask = testTasks[currentTaskIndex];
            Debug.Log($"Executing task {currentTaskIndex + 1}/{testTasks.Count}: {currentTask.taskDescription} at {currentTask.stationId}");

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
                    Debug.Log("Looping back to first task");
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
        // Move to the station
        yield return StartCoroutine(MoveToStation(task.stationId));

        // Trigger prep started event
        onPrepStarted?.Invoke(task.taskDescription);

        // Work at the station
        yield return StartCoroutine(WorkAtStation(task.workDuration));

        // Trigger prep finished event
        onPrepFinished?.Invoke();

        Debug.Log($"Completed task: {task.taskDescription}");
    }

    private IEnumerator WorkAtStation(float workTime)
    {
        float remainingTime = workTime;

        // Reset progress bar
        if (stationProgress != null)
        {
            stationProgress.fillAmount = 0;
        }

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            // Update progress bar
            if (stationProgress != null)
            {
                float progress = (workTime - remainingTime) / workTime;
                stationProgress.fillAmount = Mathf.Clamp01(progress);
            }

            yield return null;
        }

        // Complete progress bar
        if (stationProgress != null)
        {
            stationProgress.fillAmount = 1.0f;
        }
    }
    #endregion

    #region Movement System
    private IEnumerator MoveToStation(StationId destinationStationId)
    {
        if (destinationStationId == currentStationId && currentStatus == WorkerStatus.AtStation)
        {
            yield break;
        }

        // Call initialize movement method (for items like Sandevistan)
        if (initializeMovementMethod != null)
        {
            foreach (Func<IEnumerator> action in initializeMovementMethod.GetInvocationList())
            {
                yield return StartCoroutine(action?.Invoke());
            }
        }

        // Use current movement method
        yield return StartCoroutine(CurrentMovementMethod(destinationStationId));
    }

    private IEnumerator NormalMove(StationId destinationStationId)
    {
        if (destinationStationId == currentStationId)
        {
            yield break;
        }

        // Release current station if occupied
        if (currentStatus == WorkerStatus.AtStation && KitchenManager.instance != null)
        {
            Station currentStation = KitchenManager.instance.GetStation(currentStationId);
            currentStation?.ReleaseStandingLocation(ConvertToWorker());
        }

        if (KitchenManager.instance == null)
        {
            Debug.LogWarning("KitchenManager not found, using simplified movement");
            yield return StartCoroutine(SimpleMove(destinationStationId));
            yield break;
        }

        Station destinationStation = KitchenManager.instance.GetStation(destinationStationId);
        if (destinationStation == null)
        {
            Debug.LogWarning($"Station {destinationStationId} not found!");
            yield break;
        }

        Transform availableSlot = destinationStation.ReserveAvailableStandingLocation(ConvertToWorker());
        currentStationId = destinationStationId;
        currentWorkStation = destinationStation;

        if (availableSlot != null)
        {
            yield return StartCoroutine(MoveToPosition(availableSlot.position));
            currentStatus = WorkerStatus.AtStation;
        }
        else
        {
            Debug.Log("No available slots, waiting...");
            yield return StartCoroutine(WaitForSlot(destinationStation));
        }

        // Trigger movement started event
        onMovementStarted?.Invoke(Enum.GetName(typeof(StationId), destinationStationId));
    }

    private IEnumerator SimpleMove(StationId destinationStationId)
    {
        // Fallback movement when no KitchenManager
        Vector3 targetPosition = transform.position + Vector3.right * 2f; // Simple offset
        yield return StartCoroutine(MoveToPosition(targetPosition));

        currentStationId = destinationStationId;
        currentStatus = WorkerStatus.AtStation;
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float travelTime = distance / CurrentMovementSpeed;
        float elapsed = 0f;

        currentStatus = WorkerStatus.Running;

        while (elapsed < travelTime && !interruptMovementFlag)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
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
            transform.position = targetPosition;
            currentStatus = WorkerStatus.AtStation;
        }
    }

    private IEnumerator WaitForSlot(Station station)
    {
        Transform freedSlot = null;
        Action<Transform> onSlotFreed = (Transform slot) => {
            freedSlot = slot;
        };

        station.ReserveUnavailableStandingLocation(onSlotFreed);
        currentStatus = WorkerStatus.Waiting;

        yield return new WaitUntil(() => freedSlot != null);

        yield return StartCoroutine(MoveToPosition(freedSlot.position));
        currentStatus = WorkerStatus.AtStation;
    }
    #endregion

    #region Public Interface
    public bool isFree()
    {
        return !isTestRunning && currentStationId == StationId.Rest;
    }

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

    #region Debug Info
    void OnGUI()
    {
        if (!enableTestMode) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Test Worker Status: {currentStatus}");
        GUILayout.Label($"Current Station: {currentStationId}");
        GUILayout.Label($"Task: {currentTaskIndex + 1}/{testTasks.Count}");
        GUILayout.Label($"Items Equipped: {equippedItems.Count}");

        if (GUILayout.Button("Start Test"))
            StartTest();

        if (GUILayout.Button("Stop Test"))
            StopTest();

        if (GUILayout.Button("Next Task"))
            NextTask();

        GUILayout.EndArea();
    }
    #endregion
}
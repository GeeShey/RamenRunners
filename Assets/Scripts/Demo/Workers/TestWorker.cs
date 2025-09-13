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
        // Set initial state
        currentStatus = WorkerStatus.AtStation;
        currentStationId = StationId.Rest;

        // Register with KitchenManager if it exists
        if (KitchenManager.instance != null)
        {
            KitchenManager.instance.addWorker(this);
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
                    // Now we can use this TestWorker directly with items (no conversion needed)
                    item.PreLoad(this);
                    item.OnStackModified(itemConfig.stackCount);
                    equippedItems.Add(item);

                    Debug.Log($"Equipped {item.ItemBasicDetails?.name ?? "Unknown Item"} with stack count {itemConfig.stackCount}");
                }
            }
        }
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
        // Set current work station (like Worker script does)
        SetCurrentWorkStation(task.stationId);

        // Move to the station
        yield return StartCoroutine(MoveToStation(task.stationId));

        // Calculate station wait time with efficiency (like Worker script)
        float stationWaitTime = CalculateStationWaitTime(task.workDuration);

        // Trigger prep started event
        onPrepStarted?.Invoke(task.taskDescription);

        // Use the same countdown logic as Worker script
        yield return StartCoroutine(CountdownCoroutine(stationWaitTime));

        // Trigger prep finished event
        onPrepFinished?.Invoke();

        Debug.Log($"Completed task: {task.taskDescription}");
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

    #region Timing and Progress (Copied from Worker)
    private IEnumerator CountdownCoroutine(float totalWaitTime)
    {
        // Trigger station work started event if we have a current work station
        currentWorkStation?.stationWorkStarted?.Invoke(this);

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

        // Use current movement method (inherited from BaseWorker)
        yield return StartCoroutine(CurrentMovementMethod(destinationStationId));
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

    #region Debug Info
    void OnGUI()
    {
        if (!enableTestMode) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Test Worker Status: {currentStatus}");
        GUILayout.Label($"Current Station: {currentStationId}");
        GUILayout.Label($"Task: {currentTaskIndex + 1}/{testTasks.Count}");

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
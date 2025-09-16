using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TestTask
{
    [SerializeField] public StationId stationId;
    [SerializeField] public float workDuration = 2.0f;
    [SerializeField] public string taskDescription = "Working at station";

    public TestTask(StationId station, float duration, string description = "Working at station")
    {
        stationId = station;
        workDuration = duration;
        taskDescription = description;
    }
}

[Serializable]
public class TestItemConfig
{
    [SerializeField] public GameObject itemPrefab;
    [SerializeField] public int stackCount = 1;
}

/// <summary>
/// Test worker for debugging and testing station workflows without full order system
/// </summary>
public class TestWorker : BaseWorker
{
    #region Inspector Configuration
    [Header("Test Configuration")]
    [SerializeField] private bool enableTestMode = true;
    [SerializeField] private List<TestTask> testTasks = new List<TestTask>();
    [SerializeField] private List<TestItemConfig> testItems = new List<TestItemConfig>();
    [SerializeField] private bool loopTasks = true;
    [SerializeField] private float delayBetweenTasks = 1.0f;
    [SerializeField] private bool autoStartOnPlay = true;

    [Header("Runtime Status (Read Only)")]
    [SerializeField, HideInInspector] private string statusDisplay;
    [SerializeField, HideInInspector] private string currentStationDisplay;
    [SerializeField, HideInInspector] private string taskProgressDisplay;
    [SerializeField, HideInInspector] private bool isTestRunningDisplay;
    #endregion

    #region Private Fields
    private TestSequenceManager testManager;
    #endregion

    #region Properties for Inspector Display
    public string StatusDisplay => $"Status: {Status}";
    public string CurrentStationDisplay => $"Current Station: {CurrentStationId}";
    public string TaskProgressDisplay => testManager?.GetProgressDisplay() ?? "Not Started";
    public bool IsTestRunningDisplay => testManager?.IsRunning ?? false;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        testManager = new TestSequenceManager(this, testTasks);
    }

    protected override void Start()
    {
        base.Start();
        InitializeTestWorker();

        if (enableTestMode && autoStartOnPlay)
        {
            StartTest();
        }
    }

    private void Update()
    {
        UpdateInspectorDisplay();
    }

    private void OnValidate()
    {
        ValidateTestConfiguration();
    }
    #endregion

    #region Initialization
    private void InitializeTestWorker()
    {
        RegisterWithKitchenManager();
        ValidateTestConfiguration();
    }

    private void RegisterWithKitchenManager()
    {
        KitchenManager.instance?.AddWorker(this);
    }

    private void ValidateTestConfiguration()
    {
        if (testTasks.Count == 0)
        {
            testTasks.Add(new TestTask(StationId.Rest, 2.0f, "Default Rest Task"));
        }
    }

    private void UpdateInspectorDisplay()
    {
#if UNITY_EDITOR
        statusDisplay = StatusDisplay;
        currentStationDisplay = CurrentStationDisplay;
        taskProgressDisplay = TaskProgressDisplay;
        isTestRunningDisplay = IsTestRunningDisplay;
#endif
    }
    #endregion

    #region Test Control Interface
    [ContextMenu("Start Test")]
    public void StartTest()
    {
        if (!enableTestMode)
        {
            Debug.LogWarning("Test mode is disabled!");
            return;
        }

        var config = new TestConfiguration
        {
            Tasks = testTasks,
            LoopTasks = loopTasks,
            DelayBetweenTasks = delayBetweenTasks
        };

        testManager.StartTest(config);
    }

    [ContextMenu("Stop Test")]
    public void StopTest() => testManager.StopTest();

    [ContextMenu("Next Task")]
    public void NextTask() => testManager.SkipToNextTask();

    [ContextMenu("Add Random Task")]
    public void AddRandomTask()
    {
        var randomStations = Enum.GetValues(typeof(StationId));
        var randomStation = (StationId)randomStations.GetValue(UnityEngine.Random.Range(0, randomStations.Length));
        var randomDuration = UnityEngine.Random.Range(1f, 5f);

        AddTestTask(randomStation, randomDuration, $"Random task at {randomStation}");
    }
    #endregion

    #region Public API
    public void AddTestTask(StationId stationId, float duration, string description = "Test Task")
    {
        testTasks.Add(new TestTask(stationId, duration, description));
        testManager.UpdateTaskList(testTasks);
    }

    public void ClearTestTasks()
    {
        testTasks.Clear();
        testManager.UpdateTaskList(testTasks);
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

    #region Task Execution
    internal IEnumerator ExecuteTestTask(TestTask task)
    {
        // Move to station
        yield return StartCoroutine(MoveToStation(task.stationId));

        // Calculate work time with efficiency
        float workTime = CalculateStationWorkTime(task.workDuration);

        // Execute work at station
        TriggerPrepStarted(task.taskDescription);
        yield return StartCoroutine(CountdownCoroutine(workTime));
        TriggerPrepFinished();
    }

    private float CalculateStationWorkTime(float baseTime)
    {
        float efficiencyMultiplier = 1 + CurrentStationEfficiency;
        return Mathf.Max(0.01f, baseTime / efficiencyMultiplier);
    }
    #endregion

    #region BaseWorker Implementation
    public override bool IsFree()
    {
        return !(testManager?.IsRunning ?? false) && CurrentStationId == StationId.Rest;
    }

    public override void Rest()
    {
        StartCoroutine(MoveToStation(StationId.Rest));
    }
    #endregion
}

#region Supporting Classes
/// <summary>
/// Configuration for test sequences
/// </summary>
[Serializable]
public class TestConfiguration
{
    public List<TestTask> Tasks { get; set; }
    public bool LoopTasks { get; set; }
    public float DelayBetweenTasks { get; set; }
}

/// <summary>
/// Manages test sequence execution
/// </summary>
public class TestSequenceManager
{
    private readonly TestWorker worker;
    private List<TestTask> tasks;
    private int currentTaskIndex;
    private bool isRunning;
    private Coroutine testCoroutine;

    public bool IsRunning => isRunning;

    public TestSequenceManager(TestWorker worker, List<TestTask> initialTasks)
    {
        this.worker = worker;
        this.tasks = initialTasks ?? new List<TestTask>();
    }

    public string GetProgressDisplay()
    {
        if (!isRunning) return "Not Running";
        return $"Task {currentTaskIndex + 1}/{tasks.Count}";
    }

    public void UpdateTaskList(List<TestTask> newTasks)
    {
        tasks = newTasks ?? new List<TestTask>();
    }

    public void StartTest(TestConfiguration config)
    {
        if (isRunning)
        {
            Debug.LogWarning("Test is already running!");
            return;
        }

        if (config.Tasks.Count == 0)
        {
            Debug.LogWarning("No test tasks configured!");
            return;
        }

        tasks = config.Tasks;
        currentTaskIndex = 0;
        isRunning = true;

        testCoroutine = worker.StartCoroutine(RunTestSequence(config));
        Debug.Log($"Starting test with {tasks.Count} tasks. Loop: {config.LoopTasks}");
    }

    public void StopTest()
    {
        if (testCoroutine != null)
        {
            worker.StopCoroutine(testCoroutine);
            testCoroutine = null;
        }

        isRunning = false;
        Debug.Log("Test stopped");
    }

    public void SkipToNextTask()
    {
        if (!isRunning) return;

        // This would require more complex implementation to properly interrupt current task
        currentTaskIndex = (currentTaskIndex + 1) % tasks.Count;
        Debug.Log($"Skipping to task {currentTaskIndex + 1}");
    }

    private IEnumerator RunTestSequence(TestConfiguration config)
    {
        while (isRunning)
        {
            if (tasks.Count == 0) break;

            TestTask currentTask = tasks[currentTaskIndex];
            yield return worker.StartCoroutine(worker.ExecuteTestTask(currentTask));

            // Delay between tasks
            if (config.DelayBetweenTasks > 0)
            {
                yield return new WaitForSeconds(config.DelayBetweenTasks);
            }

            // Advance to next task
            currentTaskIndex++;

            // Handle looping or completion
            if (currentTaskIndex >= tasks.Count)
            {
                if (config.LoopTasks)
                {
                    currentTaskIndex = 0;
                }
                else
                {
                    break;
                }
            }
        }

        isRunning = false;
        Debug.Log("Test sequence completed");
    }
}
#endregion
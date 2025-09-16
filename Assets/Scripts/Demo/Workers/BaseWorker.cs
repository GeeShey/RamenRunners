using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum WorkerStatus
{
    Running, AtStation, Waiting
}

public static class WorkerBaseStats
{
    public static float MovementSpeed = 2f;
    public static float StationEfficiency = 0.1f;
}

/// <summary>
/// Abstract base class that defines the common interface for all worker types.
/// Handles movement, station interaction, and progress tracking.
/// </summary>
public abstract class BaseWorker : MonoBehaviour
{
    #region Inspector Fields
    [Header("Worker Stats")]
    [SerializeField] protected float currentMovementSpeed;
    [SerializeField] protected float currentStationEfficiency;
    [SerializeField] protected WorkerStatus currentStatus;
    [SerializeField] protected StationId currentStationId;

    [Header("UI Components")]
    [SerializeField] protected Image stationProgress;
    [SerializeField] protected float fillInterval = 1.0f;
    #endregion

    #region Protected Properties
    protected Station CurrentWorkStation { get; set; }
    protected WokerVFXManager VfxManager { get; private set; }
    #endregion

    #region Movement System
    private MovementController movementController;
    private BonusSystem bonusSystem;
    #endregion

    #region Events
    public Action OnPrepFinished;
    public Action<string> OnMovementStarted;
    public Action<string> OnPrepStarted;
    public MovementMethod InitializeMovementMethod;
    #endregion

    #region Movement Method Type
    public delegate IEnumerator MovementMethod(StationId destinationStationId);
    #endregion

    #region Properties
    public float CurrentMovementSpeed 
    { 
        get => currentMovementSpeed; 
        set => currentMovementSpeed = value; 
    }
    public float CurrentStationEfficiency 
    { 
        get => currentStationEfficiency; 
        set => currentStationEfficiency = value; 
    }
    public WorkerStatus Status => currentStatus;
    public StationId CurrentStationId => currentStationId;
    public Station WorkStation => CurrentWorkStation;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        movementController = new MovementController(this);
        bonusSystem = new BonusSystem();
        VfxManager = GetComponent<WokerVFXManager>();
    }

    protected virtual void Start()
    {
        InitializeBaseStats();
        InitializeWorker();
    }
    #endregion

    #region Initialization
    protected virtual void InitializeBaseStats()
    {
        currentMovementSpeed = WorkerBaseStats.MovementSpeed;
        currentStationEfficiency = WorkerBaseStats.StationEfficiency;
    }

    public void InitializeWorker()
    {
        currentStatus = WorkerStatus.AtStation;
        currentStationId = StationId.Rest;
    }
    #endregion

    #region Abstract Methods
    public abstract bool IsFree();
    public abstract void Rest();
    #endregion

    #region Movement System
    public IEnumerator MoveToStation(StationId destinationStationId)
    {
        // Check if there's a custom movement method (like Sandevistan teleport)
        if (InitializeMovementMethod != null)
        {
            Debug.Log($"[{name}] Using custom movement method to {destinationStationId}");
            yield return StartCoroutine(InitializeMovementMethod(destinationStationId));
            // After custom movement completes, ensure we are marked as AtStation
            UpdateStatus(WorkerStatus.AtStation);
        }
        else
        {
            Debug.Log($"[{name}] Using normal movement to {destinationStationId}");
            yield return StartCoroutine(movementController.MoveToStation(destinationStationId));
        }
    }

    internal void UpdateStatus(WorkerStatus newStatus)
    {
        currentStatus = newStatus;
    }

    internal void UpdateCurrentStation(StationId stationId, Station station)
    {
        currentStationId = stationId;
        CurrentWorkStation = station;
    }

    protected void InterruptMovement(Action callback)
    {
        movementController?.InterruptMovement(callback);
    }
    #endregion

    #region Bonus System
    public void ReceiveBonusReduction(float bonusTimeReduction = 0.5f)
    {
        if (CanReceiveClickBonus())
        {
            bonusSystem.AddBonus(bonusTimeReduction);
        }
    }

    public float CalculateTimeReduction()
    {
        float frameTimeReduction = Time.deltaTime;

        if (bonusSystem.HasBonus())
        {
            frameTimeReduction += bonusSystem.ConsumeBonus();
            VfxManager?.onClicked();
        }

        return frameTimeReduction;
    }

    private bool CanReceiveClickBonus()
    {
        return currentStatus == WorkerStatus.AtStation && currentStationId != StationId.Rest;
    }
    #endregion

    #region Progress System
    public IEnumerator CountdownCoroutine(float totalWaitTime)
    {
        var progressTracker = new ProgressTracker(stationProgress, totalWaitTime);

        CurrentWorkStation?.SomeoneStartedWorkAtStation?.Invoke(this);

        yield return StartCoroutine(progressTracker.RunCountdown(() => CalculateTimeReduction()));
    }
    #endregion

    #region Event Triggers
    public void TriggerMovementStarted(StationId stationId)
    {
        OnMovementStarted?.Invoke(Enum.GetName(typeof(StationId), stationId));
    }

    public void TriggerPrepStarted(string prepText)
    {
        OnPrepStarted?.Invoke(prepText);
    }

    public void TriggerPrepFinished()
    {
        OnPrepFinished?.Invoke();
    }
    #endregion
}

#region Helper Classes
/// <summary>
/// Handles movement logic for workers
/// </summary>
public class MovementController
{
    private readonly BaseWorker worker;
    private bool interruptMovementFlag;
    private Action postInterruptAction;

    public MovementController(BaseWorker worker)
    {
        this.worker = worker;
    }

    public IEnumerator MoveToStation(StationId destinationStationId)
    {
        if (IsAlreadyAtDestination(destinationStationId))
        {
            yield break;
        }

        ReleaseCurrentStationIfOccupied();

        if (KitchenManager.instance == null)
        {
            yield return worker.StartCoroutine(SimpleFallbackMove(destinationStationId));
            yield break;
        }

        Station destinationStation = KitchenManager.instance.GetStation(destinationStationId);
        if (destinationStation == null)
        {
            Debug.LogWarning($"Station {destinationStationId} not found!");
            yield break;
        }

        worker.UpdateCurrentStation(destinationStationId, destinationStation);
        worker.TriggerMovementStarted(destinationStationId);

        Transform availableSlot = destinationStation.ReserveAvailableStandingLocation(worker as Worker);

        if (availableSlot != null)
        {
            yield return worker.StartCoroutine(MoveToPosition(availableSlot.position));
        }
        else
        {
            yield return worker.StartCoroutine(WaitForSlot(destinationStation));
        }
    }

    private bool IsAlreadyAtDestination(StationId destination)
    {
        return destination == worker.CurrentStationId && worker.Status == WorkerStatus.AtStation;
    }

    private void ReleaseCurrentStationIfOccupied()
    {
        if (worker.Status == WorkerStatus.AtStation && KitchenManager.instance != null)
        {
            Station currentStation = KitchenManager.instance.GetStation(worker.CurrentStationId);
            currentStation?.ReleaseStandingLocation(worker as Worker);
        }
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        Vector3 startPosition = worker.transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float travelTime = distance / worker.CurrentMovementSpeed;
        float elapsed = 0f;

        worker.UpdateStatus(WorkerStatus.Running);

        while (elapsed < travelTime && !interruptMovementFlag)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;
            worker.transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        HandleMovementCompletion(targetPosition);
    }

    private void HandleMovementCompletion(Vector3 targetPosition)
    {
        if (interruptMovementFlag)
        {
            interruptMovementFlag = false;
            postInterruptAction?.Invoke();
            postInterruptAction = null;
        }
        else
        {
            worker.transform.position = targetPosition;
            worker.UpdateStatus(WorkerStatus.AtStation);
        }
    }

    private IEnumerator WaitForSlot(Station station)
    {
        Transform freedSlot = null;
        Action<Transform> onSlotFreed = (slot) => freedSlot = slot;

        station.ReserveUnavailableStandingLocation(onSlotFreed);
        worker.UpdateStatus(WorkerStatus.Waiting);

        yield return new WaitUntil(() => freedSlot != null);
        yield return worker.StartCoroutine(MoveToPosition(freedSlot.position));
    }

    private IEnumerator SimpleFallbackMove(StationId destinationStationId)
    {
        Debug.LogWarning("KitchenManager not found, using simplified movement");
        Vector3 targetPosition = worker.transform.position + Vector3.right * 2f;
        yield return worker.StartCoroutine(MoveToPosition(targetPosition));
        worker.UpdateCurrentStation(destinationStationId, null);
    }

    public void InterruptMovement(Action callback)
    {
        interruptMovementFlag = true;
        postInterruptAction = callback;
    }
}

/// <summary>
/// Handles bonus time reduction system
/// </summary>
public class BonusSystem
{
    private float bonusReduction;

    public bool HasBonus() => bonusReduction > 0f;

    public void AddBonus(float bonus)
    {
        bonusReduction += bonus;
    }

    public float ConsumeBonus()
    {
        float bonus = bonusReduction;
        bonusReduction = 0f;
        return bonus;
    }
}

/// <summary>
/// Handles progress bar updates and timing
/// </summary>
public class ProgressTracker
{
    private readonly Image progressBar;
    private readonly float totalTime;

    public ProgressTracker(Image progressBar, float totalTime)
    {
        this.progressBar = progressBar;
        this.totalTime = totalTime;
    }

    public IEnumerator RunCountdown(Func<float> getTimeReduction)
    {
        ResetProgressBar();
        float remainingTime = totalTime;

        while (remainingTime > 0f)
        {
            float reduction = getTimeReduction();
            remainingTime = Mathf.Max(0f, remainingTime - reduction);
            UpdateProgressBar(remainingTime);
            yield return null;
        }

        CompleteProgressBar();
    }

    private void ResetProgressBar()
    {
        if (progressBar != null)
            progressBar.fillAmount = 0f;
    }

    private void UpdateProgressBar(float remainingTime)
    {
        if (progressBar != null)
        {
            float completionPercentage = (totalTime - remainingTime) / totalTime;
            progressBar.fillAmount = completionPercentage;
        }
    }

    private void CompleteProgressBar()
    {
        if (progressBar != null)
            progressBar.fillAmount = 1f;
    }
}
#endregion
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.VFX;

public enum WorkerStatus
{
    Running, AtStation, Waiting
}

public static class WorkerBaseStats
{
    public static float movementSpeed = 2;
    public static float stationEfficiency = 0.1f;
}

// Abstract base class that defines the common interface for all worker types
public abstract class BaseWorker : MonoBehaviour
{
    #region Common Worker Stats
    [Header("Worker Stats")]
    public float CurrentMovementSpeed;
    public float CurrentStationEfficiency;
    public WorkerStatus currentStatus;
    public StationId currentStationId;
    public Station currentWorkStation;
    private float bonusReduction = 0f;

    #endregion

    #region Movement Variables
    [NonSerialized]
    public bool interruptMovementFlag = false;
    public Action postInterruptAction;

    // Define the movement method delegate here so items can use it
    public delegate IEnumerator MovementMethod(StationId stationId);
    public MovementMethod CurrentMovementMethod;
    #endregion

    #region UI Components
    [Header("UI")]
    public Image stationProgress;
    public float fillInterval = 1.0f;
    #endregion

    public WokerVFXManager vfxManager;


    #region Events (For Item Compatibility)
    public Action<string> onPrepStarted;
    public Action onPrepFinished;
    public Action<string> onMovementStarted;
    public Func<IEnumerator> initializeMovementMethod;
    #endregion

    #region Abstract Methods
    // These must be implemented by derived classes
    public abstract bool isFree();
    public abstract void Rest();

    // Virtual methods that can be overridden if needed
    public virtual void InitializeBaseStats()
    {
        CurrentMovementSpeed = WorkerBaseStats.movementSpeed;
        CurrentStationEfficiency = WorkerBaseStats.stationEfficiency;
    }
    #endregion

    #region Worker Interaction
    //bonus is in seconds
    public float CalculateTimeReduction()
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

    #region Common Movement System
    // Shared movement logic that both Worker and TestWorker can use
    protected IEnumerator MoveToPosition(Vector3 targetPosition)
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

    protected IEnumerator WaitForSlot(Station station)
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

    #region Unity Lifecycle
    protected virtual void Start()
    {
        InitializeBaseStats();
        InitializeMovementMethod();
    }

    protected virtual void InitializeMovementMethod()
    {
        CurrentMovementMethod = NormalMove;
    }
    #endregion

    #region Base Movement Implementation
    protected virtual IEnumerator NormalMove(StationId destinationStationId)
    {
        if (destinationStationId == currentStationId && currentStatus == WorkerStatus.AtStation)
        {
            yield break;
        }

        // Release current station if occupied
        if (currentStatus == WorkerStatus.AtStation && KitchenManager.instance != null)
        {
            Station currentStation = KitchenManager.instance.GetStation(currentStationId);
            currentStation?.ReleaseStandingLocation(this as Worker);
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

        Transform availableSlot = destinationStation.ReserveAvailableStandingLocation(this as Worker);
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
        Vector3 targetPosition = transform.position + Vector3.right * 2f;
        yield return StartCoroutine(MoveToPosition(targetPosition));

        currentStationId = destinationStationId;
        currentStatus = WorkerStatus.AtStation;
    }
    #endregion
}
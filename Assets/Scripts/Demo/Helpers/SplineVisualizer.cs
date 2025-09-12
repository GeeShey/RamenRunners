using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SplineVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] public GameObject visualizerObject;
    [SerializeField] private float speed = 5f;
    [SerializeField] private bool autoStart = false;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool lookForward = true;

    [Header("Object Pooling")]
    [SerializeField] private int poolSize = 5;
    [SerializeField] private bool allowPoolExpansion = true;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private int gizmoResolution = 100;

    private SplineContainer splineContainer;
    private float currentT = 0f;
    private bool isMoving = false;
    private Coroutine currentAnimation = null;

    // Object pooling system
    private Queue<GameObject> visualizerPool = new Queue<GameObject>();
    private List<VisualizerInstance> activeVisualizers = new List<VisualizerInstance>();

    public class VisualizerInstance
    {
        public GameObject gameObject;
        public Coroutine animation;
        public bool isActive;

        public VisualizerInstance(GameObject obj)
        {
            gameObject = obj;
            animation = null;
            isActive = false;
        }
    }

    private IEnumerator AnimatePathCoroutineMain(float duration, System.Action onComplete = null, float startProgress = 0f, float endProgress = 1f)
    {
        // Animation for main visualizer (used in edit mode)
        float elapsedTime = 0f;
        currentT = startProgress;

        // Make main visualizer visible
        if (visualizerObject != null)
        {
            visualizerObject.SetActive(true);
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration;

            float smoothTime = SmoothStep(normalizedTime);
            currentT = Mathf.Lerp(startProgress, endProgress, smoothTime);

            SetVisualizerPosition(visualizerObject, currentT);
            yield return null;
        }

        currentT = endProgress;
        SetVisualizerPosition(visualizerObject, currentT);
        currentAnimation = null;

        // In edit mode, don't do the collection point animation
        if (Application.isPlaying && TransformManager.instance != null)
        {
            StartCoroutine(LerpToCollectionPointMain());
        }
    }

    public IEnumerator LerpToCollectionPointMain(System.Action onComplete = null)
    {
        Vector3 startingPos = visualizerObject.transform.position;
        Vector3 targetPos = TransformManager.instance.transformMap["EffectCollectionPoint"].position;
        float timeToCollectionPoint = 2.0f;

        float timeElapsed = 0.0f;

        while (timeElapsed < timeToCollectionPoint)
        {
            float t = timeElapsed / timeToCollectionPoint;
            visualizerObject.transform.position = Vector3.Lerp(startingPos, targetPos, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        visualizerObject.transform.position = targetPos;
        visualizerObject.SetActive(false);
        onComplete?.Invoke();

    }

    private void Awake()
    {
        Initialize();
        if (Application.isPlaying)
        {
            InitializePool();
        }
    }

    private void OnEnable()
    {
        Initialize();
        if (Application.isPlaying && visualizerPool.Count == 0)
        {
            InitializePool();
        }
    }

    private void Initialize()
    {
        splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null)
        {
            Debug.LogError("SplineVisualizer requires a SplineContainer component!");
            return;
        }

        // Hide the main visualizer object by default
        if (visualizerObject != null)
        {
            visualizerObject.SetActive(false);
        }

        if (Application.isPlaying && autoStart)
        {
            StartVisualization();
        }
    }

    private void InitializePool()
    {
        if (visualizerObject == null || !Application.isPlaying) return;

        // Only initialize if pool is empty to avoid duplicates
        if (visualizerPool.Count > 0) return;

        // Clear existing pool (safety check)
        while (visualizerPool.Count > 0)
        {
            GameObject pooledObj = visualizerPool.Dequeue();
            if (pooledObj != null)
            {
                Destroy(pooledObj);
            }
        }

        activeVisualizers.Clear();

        // Create pool objects
        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledVisualizer();
        }
    }

    private GameObject CreatePooledVisualizer()
    {
        GameObject pooledObj = Instantiate(visualizerObject, transform);
        pooledObj.SetActive(false);
        pooledObj.name = visualizerObject.name + "_Pooled";
        visualizerPool.Enqueue(pooledObj);
        return pooledObj;
    }

    private VisualizerInstance GetPooledVisualizer()
    {
        GameObject pooledObj = null;

        // Try to get from pool
        if (visualizerPool.Count > 0)
        {
            pooledObj = visualizerPool.Dequeue();
        }
        // Create new one if pool is empty and expansion is allowed
        else if (allowPoolExpansion)
        {
            pooledObj = CreatePooledVisualizer();
            visualizerPool.Dequeue(); // Remove it from queue since we're using it immediately
        }

        if (pooledObj != null)
        {
            pooledObj.SetActive(true);
            VisualizerInstance instance = new VisualizerInstance(pooledObj);
            activeVisualizers.Add(instance);
            return instance;
        }

        Debug.LogWarning("No available visualizers in pool and expansion is disabled!");
        return null;
    }

    private void ReturnToPool(VisualizerInstance instance)
    {
        if (instance == null || instance.gameObject == null) return;

        // Stop any running animation
        if (instance.animation != null)
        {
            StopCoroutine(instance.animation);
            instance.animation = null;
        }

        // Reset state
        instance.isActive = false;
        instance.gameObject.SetActive(false);

        // Return to pool
        visualizerPool.Enqueue(instance.gameObject);
        activeVisualizers.Remove(instance);
    }

    public void StartVisualization()
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0) return;

        isMoving = true;
        currentT = 0f;

        // Make the main visualizer visible when starting visualization
        if (visualizerObject != null)
        {
            visualizerObject.SetActive(true);
        }

        // In edit mode, we need to handle updates differently
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorUpdate;
#endif
        }
    }

    public void StopVisualization()
    {
        isMoving = false;

        // Hide the main visualizer when stopping
        if (visualizerObject != null)
        {
            visualizerObject.SetActive(false);
        }

        // Stop any running timed animation on the main visualizer
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        // Remove editor update callback
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
        }
    }

    public void StopAllAnimations()
    {
        StopVisualization();

        // Stop all pooled visualizers
        for (int i = activeVisualizers.Count - 1; i >= 0; i--)
        {
            ReturnToPool(activeVisualizers[i]);
        }
    }

    public void ResetVisualization()
    {
        currentT = 0f;
        SetVisualizerPosition(visualizerObject, 0f);
        isMoving = false;

        // Hide the main visualizer when resetting
        if (visualizerObject != null)
        {
            visualizerObject.SetActive(false);
        }
    }

    public void SetProgress(float t)
    {
        currentT = Mathf.Clamp01(t);

        // Make visualizer visible when setting progress manually
        if (visualizerObject != null)
        {
            visualizerObject.SetActive(true);
        }

        SetVisualizerPosition(visualizerObject, currentT);
    }

    public void AnimateAlongPath(float duration, System.Action onComplete = null)
    {
        AnimateAlongPath(duration, 0f, 1f, onComplete);
    }

    public void AnimateAlongPath(float duration, float startProgress, float endProgress, System.Action onComplete = null)
    {
        // Only use pooling in play mode, fallback to main visualizer in edit mode
        if (!Application.isPlaying)
        {
            // In edit mode, use the main visualizer (no pooling)
            StopVisualization();
            currentAnimation = StartCoroutine(AnimatePathCoroutineMain(duration, onComplete, startProgress, endProgress));
            return;
        }

        startProgress = Mathf.Clamp01(startProgress);
        endProgress = Mathf.Clamp01(endProgress);

        // Get a pooled visualizer instead of stopping the current one
        VisualizerInstance instance = GetPooledVisualizer();
        if (instance == null)
        {
            Debug.LogWarning("Could not get pooled visualizer for animation!");
            return;
        }

        // Start the animation on the pooled instance
        instance.isActive = true;
        instance.animation = StartCoroutine(AnimatePathCoroutine(instance, duration, onComplete, startProgress, endProgress));
    }

    private IEnumerator AnimatePathCoroutine(VisualizerInstance instance, float duration, System.Action onComplete = null, float startProgress = 0f, float endProgress = 1f)
    {
        float elapsedTime = 0f;
        float instanceT = startProgress;

        while (elapsedTime < duration && instance.isActive)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration;

            // Use smooth interpolation (ease in/out) for more natural movement
            float smoothTime = SmoothStep(normalizedTime);
            instanceT = Mathf.Lerp(startProgress, endProgress, smoothTime);

            SetVisualizerPosition(instance.gameObject, instanceT);
            yield return null;
        }

        if (instance.isActive)
        {
            // Ensure we end exactly at the target position
            instanceT = endProgress;
            SetVisualizerPosition(instance.gameObject, instanceT);

            instance.animation = null;
            onComplete?.Invoke();

            StartCoroutine(LerpToCollectionPoint(instance));
        }
    }

    public IEnumerator LerpToCollectionPoint(VisualizerInstance instance)
    {
        if (instance == null || !instance.isActive) yield break;

        Vector3 startingPos = instance.gameObject.transform.position;
        Vector3 targetPos = TransformManager.instance.transformMap["EffectCollectionPoint"].position;
        float timeToCollectionPoint = 2.0f;

        float timeElapsed = 0.0f;

        while (timeElapsed < timeToCollectionPoint && instance.isActive)
        {
            float t = timeElapsed / timeToCollectionPoint;
            instance.gameObject.transform.position = Vector3.Lerp(startingPos, targetPos, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        if (instance.isActive)
        {
            // Ensure final position is exactly at the target
            instance.gameObject.transform.position = targetPos;

            // Return to pool instead of deactivating the main object
            ReturnToPool(instance);
        }
    }

    /// <summary>
    /// Smooth step function for natural easing animation
    /// </summary>
    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            HandleMovement();
        }
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (!Application.isPlaying)
        {
            HandleMovement();
            UnityEditor.SceneView.RepaintAll(); // Refresh scene view
        }
    }

    private void OnDestroy()
    {
        // Clean up editor update callback
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
        }

        // Clean up pool
        StopAllAnimations();
    }

    private void OnDisable()
    {
        // Clean up editor update callback
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
        }
    }
#endif

    private void HandleMovement()
    {
        if (!isMoving || splineContainer == null || splineContainer.Splines.Count == 0)
            return;

        // Get delta time (different for editor vs runtime)
        float deltaTime = Application.isPlaying ? Time.deltaTime : 0.016f; // ~60fps in editor

        // Move along the spline
        currentT += speed * deltaTime / GetSplineLength();

        if (currentT >= 1f)
        {
            if (loop)
            {
                currentT = 0f;
            }
            else
            {
                currentT = 1f;
                StopVisualization();
            }
        }

        SetVisualizerPosition(visualizerObject, currentT);
    }

    private void SetVisualizerPosition(GameObject targetObject, float t)
    {
        if (targetObject == null || splineContainer == null || splineContainer.Splines.Count == 0)
            return;

        var spline = splineContainer.Splines[0];

        // Get position on spline
        spline.Evaluate(t, out float3 position, out float3 tangent, out float3 upVector);

        // Convert to world space
        Vector3 worldPos = transform.TransformPoint(position);
        targetObject.transform.position = worldPos;

        // Optionally orient the object to face forward along the spline
        if (lookForward && tangent.Equals(float3.zero) == false)
        {
            Vector3 worldTangent = transform.TransformDirection(tangent);
            Vector3 worldUp = transform.TransformDirection(upVector);
            targetObject.transform.rotation = Quaternion.LookRotation(worldTangent, worldUp);
        }
    }

    private void SetVisualizerPosition(Vector3 position)
    {
        visualizerObject.transform.position = position;
    }

    private float GetSplineLength()
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0)
            return 1f;

        return splineContainer.Splines[0].GetLength();
    }

    // Public methods for external control
    public bool IsMoving => isMoving || currentAnimation != null;
    public bool IsAnimating => currentAnimation != null || activeVisualizers.Count > 0;
    public float Progress => currentT;
    public float Speed { get => speed; set => speed = value; }
    public bool Loop { get => loop; set => loop = value; }
    public bool LookForward { get => lookForward; set => lookForward = value; }
    public int ActiveVisualizerCount => activeVisualizers.Count;
    public int AvailableVisualizerCount => visualizerPool.Count;

    private void OnDrawGizmos()
    {
        if (!showGizmos || splineContainer == null || splineContainer.Splines.Count == 0)
            return;

        var spline = splineContainer.Splines[0];
        Gizmos.color = gizmoColor;

        Vector3 previousPoint = transform.TransformPoint(spline.EvaluatePosition(0f));

        for (int i = 1; i <= gizmoResolution; i++)
        {
            float t = (float)i / gizmoResolution;
            Vector3 currentPoint = transform.TransformPoint(spline.EvaluatePosition(t));
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        // Draw direction arrows
        for (int i = 0; i < gizmoResolution; i += gizmoResolution / 10)
        {
            float t = (float)i / gizmoResolution;
            spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);

            Vector3 worldPos = transform.TransformPoint(pos);
            Vector3 worldTangent = transform.TransformDirection(tangent).normalized * 0.5f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(worldPos, worldTangent);
        }
    }
}
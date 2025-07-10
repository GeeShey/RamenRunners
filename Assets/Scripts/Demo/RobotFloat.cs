using UnityEngine;
using DG.Tweening;

public class RobotFloat : MonoBehaviour
{
    [Header("Float Settings")]
    public float floatHeight = 0.5f;     // How high/low the robot moves
    public float floatDuration = 2f;     // Duration for one up/down cycle
    public Ease floatEase = Ease.InOutSine; // Easing for smooth motion

    [Header("Optional Settings")]
    public bool startOnAwake = true;
    public bool randomStartOffset = true; // Prevents multiple robots from syncing

    private Vector3 startPos,endPos;
    private Tween floatTween;

    void Start()
    {
        // Store the initial position
        startPos = transform.position;

        if (startOnAwake)
        {
            StartFloating();
        }
    }

    public void StartFloating()
    {
        // Kill any existing tween
        if (floatTween != null)
        {
            floatTween.Kill();
        }

        // Reset to start position
        //transform.position = startPos;
        endPos = startPos + Vector3.up * floatHeight;

        transform.DOMove(endPos, floatDuration)
            .SetLoops(-1, LoopType.Yoyo) // Infinite yoyo loop
            .SetEase(Ease.InOutSine);
    }

    public void StopFloating()
    {
        if (floatTween != null)
        {
            floatTween.Kill();
            floatTween = null;
        }

        // Optionally return to start position
        transform.DOMoveY(startPos.y, 0.5f).SetEase(Ease.OutQuad);
    }

    void OnDestroy()
    {
        // Clean up tween when object is destroyed
        if (floatTween != null)
        {
            floatTween.Kill();
        }
    }

    void OnDisable()
    {
        // Pause tween when object is disabled
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Pause();
        }
    }

    void OnEnable()
    {
        // Resume tween when object is re-enabled
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Play();
        }
    }
}
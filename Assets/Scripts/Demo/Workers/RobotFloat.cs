using UnityEngine;
using DG.Tweening;

public class RobotFloat : MonoBehaviour
{
    [Header("Float Settings")]
    public float floatHeight = 0.5f;          // Vertical float distance
    public float floatDuration = 2f;          // Duration of one up/down cycle
    public Ease floatEase = Ease.InOutSine;   // Easing for float motion

    [Header("Optional Settings")]
    public bool startOnAwake = true;
    public bool randomStartOffset = true;     // Prevents sync among robots

    private float startY;
    private Tween floatTween;

    void Start()
    {
        startY = transform.position.y;

        if (startOnAwake)
        {
            StartFloating();
        }
    }

    public void StartFloating()
    {
        // Kill existing tween
        if (floatTween != null)
        {
            floatTween.Kill();
        }

        float targetY = startY + floatHeight;

        // Apply random delay to desync floating robots
        float delay = randomStartOffset ? Random.Range(0f, floatDuration) : 0f;

        floatTween = transform.DOMoveY(targetY, floatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(floatEase)
            .SetDelay(delay);
    }

    public void StopFloating()
    {
        if (floatTween != null)
        {
            floatTween.Kill();
            floatTween = null;
        }

        // Return to original Y position
        transform.DOMoveY(startY, 0.5f).SetEase(Ease.OutQuad);
    }

    void OnDestroy()
    {
        if (floatTween != null)
        {
            floatTween.Kill();
        }
    }

    void OnDisable()
    {
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Pause();
        }
    }

    void OnEnable()
    {
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Play();
        }
    }
}

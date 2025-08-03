using UnityEngine;
using DG.Tweening;

public class TutorialArrow : MonoBehaviour
{
    [Header("Popup Settings")]
    public float scaleUpDuration = 0.3f;
    public float settleDuration = 0.15f;
    private float displayDuration = 9.5f;
    public bool shake = false;
    public bool destroyAfter = true;

    [Header("Oscillation Settings")]
    public float floatDistance = 20f;
    public float floatDuration = 1f;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Tween floatTween;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        TriggerPopup();
    }

    public void TriggerPopup()
    {
        transform.localScale = Vector3.zero;
        transform.position = originalPosition;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * 1.05f, scaleUpDuration).SetEase(Ease.OutBack));
        seq.Append(transform.DOScale(originalScale, settleDuration).SetEase(Ease.OutCubic));

        if (shake)
        {
            seq.Append(transform.DOShakeRotation(
                duration: 0.4f,
                strength: new Vector3(0f, 0f, 20f),
                vibrato: 10,
                randomness: 90,
                fadeOut: true
            ));
        }

        // Start floating when the scale finishes
        seq.AppendCallback(StartFloating);
        seq.AppendInterval(displayDuration);

        if (destroyAfter)
        {
            seq.AppendCallback(() =>
            {
                StopFloating();
                Destroy(gameObject);
            });
        }
        else
        {
            seq.AppendCallback(() =>
            {
                StopFloating();
                gameObject.SetActive(false);
            });
        }
    }

    private void StartFloating()
    {
        floatTween = transform.DOMoveY(originalPosition.y + floatDistance, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopFloating()
    {
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Kill();
            transform.position = originalPosition;
        }
    }
}

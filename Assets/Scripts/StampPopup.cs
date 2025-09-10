using UnityEngine;
using DG.Tweening;

public class StampPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;                    // Target canvas
    [SerializeField] private RectTransform stampPrefab;        // Prefab that already contains your TMP text (and styling)
    [SerializeField] private RectTransform optionalParent;     // Optional parent under the canvas (panel). Leave null to use canvas

    [Header("Arrival (from above)")]
    [SerializeField, Min(1f)] private float startScale = 1.6f; // starts big
    [SerializeField] private float dropYOffset = 64f;          // px above cursor to start
    [SerializeField] private float dropDuration = 0.18f;       // fast drop
    [SerializeField] private Ease dropEase = Ease.OutCubic;

    [Header("Tilt Wobble")]
    [SerializeField] private float tiltAngle = 14f;            // degrees (peak tilt)
    [SerializeField] private float tiltDuration = 0.35f;       // total wobble time

    [Header("Exit")]
    [SerializeField] private float exitDelay = 0.05f;
    [SerializeField] private float exitDuration = 0.2f;
    [SerializeField] private Ease exitEase = Ease.InBack;

    // Hook this to a UI Button (no parameters needed)
    public void ShowStampAtMouse() => ShowStampAtScreenPoint(Input.mousePosition);

    // If you want to trigger from code with an explicit screen point
    public void ShowStampAtScreenPoint(Vector3 screenPoint)
    {
        if (!canvas || !stampPrefab) { Debug.LogWarning("StampPopup not configured."); return; }

        Transform parent = optionalParent ? (Transform)optionalParent : canvas.transform;
        RectTransform rt = Instantiate(stampPrefab, parent);
        rt.SetAsLastSibling();                // draw on top
        rt.localScale = Vector3.one * startScale;
        rt.localRotation = Quaternion.identity;

        bool worldSpace = canvas.renderMode == RenderMode.WorldSpace;
        RectTransform parentRect = parent as RectTransform;

        if (worldSpace)
        {
            // Convert screen to world, start above, land at cursor
            Vector3 targetWorld;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPoint, canvas.worldCamera, out targetWorld);

            Vector3 startWorld;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPoint + new Vector3(0f, dropYOffset, 0f), canvas.worldCamera, out startWorld);

            rt.position = startWorld;

            Sequence seq = DOTween.Sequence();

            // Drop + shrink to 1.0
            seq.Append(rt.DOMove(targetWorld, dropDuration).SetEase(dropEase))
               .Join(rt.DOScale(1f, dropDuration));

            // Damped tilt wobble
            float t = tiltDuration;
            seq.Append(rt.DOLocalRotate(new Vector3(0, 0, tiltAngle), t * 0.25f).SetEase(Ease.InOutSine));
            seq.Append(rt.DOLocalRotate(new Vector3(0, 0, -tiltAngle * 0.66f), t * 0.25f).SetEase(Ease.InOutSine));
            seq.Append(rt.DOLocalRotate(new Vector3(0, 0, tiltAngle * 0.33f), t * 0.20f).SetEase(Ease.InOutSine));
            seq.Append(rt.DOLocalRotate(Vector3.zero, t * 0.15f).SetEase(Ease.InOutSine));

            // Exit
            seq.AppendInterval(exitDelay);
            seq.Append(rt.DOScale(0f, exitDuration).SetEase(exitEase))
               .OnComplete(() => Destroy(rt.gameObject));

            return;
        }

        // Screen Space (Overlay / Camera)
        Vector2 targetLocal;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, null, out targetLocal);
        else // ScreenSpaceCamera
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, canvas.worldCamera, out targetLocal);

        Vector2 startLocal = targetLocal + new Vector2(0f, dropYOffset);
        rt.anchoredPosition = startLocal;

        Sequence seq2 = DOTween.Sequence();

        // Drop + shrink to 1.0
        seq2.Append(rt.DOAnchorPos(targetLocal, dropDuration).SetEase(dropEase))
            .Join(rt.DOScale(1f, dropDuration));

        // Damped tilt wobble
        float tt = tiltDuration;
        seq2.Append(rt.DOLocalRotate(new Vector3(0, 0, tiltAngle), tt * 0.25f).SetEase(Ease.InOutSine));
        seq2.Append(rt.DOLocalRotate(new Vector3(0, 0, -tiltAngle * 0.66f), tt * 0.25f).SetEase(Ease.InOutSine));
        seq2.Append(rt.DOLocalRotate(new Vector3(0, 0, tiltAngle * 0.33f), tt * 0.20f).SetEase(Ease.InOutSine));
        seq2.Append(rt.DOLocalRotate(Vector3.zero, tt * 0.15f).SetEase(Ease.InOutSine));

        // Exit
        seq2.AppendInterval(exitDelay);
        seq2.Append(rt.DOScale(0f, exitDuration).SetEase(exitEase))
            .OnComplete(() => Destroy(rt.gameObject));
    }
}

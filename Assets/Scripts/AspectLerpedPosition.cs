using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class AspectTriLerpedPosition : MonoBehaviour
{
    public enum ApplyMode
    {
        WorldPosition,
        LocalPosition,
        AnchoredPosition // RectTransform.anchoredPosition (x,y)
    }

    [Header("Aspect breakpoints (W/H)")]
    public float minAspect = 3f / 2f;   // 1.5
    public float baseAspect = 16f / 9f; // 1.7777... your design aspect
    public float maxAspect = 18f / 9f;  // 2.0

    [Header("Positions at breakpoints")]
    public Vector3 positionAtMin = new Vector3(-5f, 3f, 0f);
    public Vector3 positionAtBase = new Vector3(0f, 3f, 0f);
    public Vector3 positionAtMax = new Vector3(5f, 3f, 0f);

    [Header("How to apply the position")]
    public ApplyMode applyMode = ApplyMode.WorldPosition;

    [Header("Reference camera (for true viewport aspect)")]
    public Camera referenceCamera;

    [Header("Smoothing (optional)")]
    public bool smooth = false;
    [Tooltip("Time constant in seconds. Smaller = faster.")]
    public float smoothTime = 0.12f;

    [Header("Custom easing (optional)")]
    [Tooltip("Easing for min -> base segment (t in 0..1). Leave null for linear.")]
    public AnimationCurve easeLeft = null;
    [Tooltip("Easing for base -> max segment (t in 0..1). Leave null for linear.")]
    public AnimationCurve easeRight = null;

    [Header("Behavior")]
    [Tooltip("Clamp aspect to [minAspect, maxAspect] before evaluating.")]
    public bool clampAspectToRange = true;

    // Internals
    RectTransform rt;
    Vector3 velWorldOrLocal; // SmoothDamp velocity
    Vector2 velAnchored;

    Rect lastCamPixelRect;
    int lastScreenW, lastScreenH;

    void Reset()
    {
        referenceCamera = Camera.main;
    }

    void OnEnable()
    {
        rt = transform as RectTransform;
        ApplyNow(true);
    }

    void Update()
    {
        if (!referenceCamera) referenceCamera = Camera.main;
        if (!referenceCamera) return;

        bool sizeChanged =
            Screen.width != lastScreenW ||
            Screen.height != lastScreenH ||
            referenceCamera.pixelRect != lastCamPixelRect;

        if (sizeChanged || !Application.isPlaying)
        {
            ApplyNow(false);
        }
        else if (smooth)
        {
            // keep converging during play when smoothing is enabled
            ApplyNow(false);
        }
    }

    public void ApplyNow(bool instant)
    {
        if (!referenceCamera) return;

        // Ensure extremes are ordered for math; swap positions if needed so min<=max.
        float aMin = minAspect;
        float aMax = maxAspect;
        Vector3 pMin = positionAtMin;
        Vector3 pMax = positionAtMax;

        if (aMin > aMax)
        {
            // swap ends so math is monotonic
            (aMin, aMax) = (aMax, aMin);
            (pMin, pMax) = (pMax, pMin);
        }

        // Clamp base to [aMin, aMax] so piecewise lerp behaves well.
        float aBase = Mathf.Clamp(baseAspect, aMin, aMax);
        Vector3 pBase = positionAtBase;

        // Current aspect based on actual viewport
        float aCam = GetCameraAspect(referenceCamera);
        float a = clampAspectToRange ? Mathf.Clamp(aCam, aMin, aMax) : aCam;

        // Choose segment and t
        Vector3 target;
        if (a <= aBase)
        {
            float t01 = SafeInverseLerp(aMin, aBase, a);
            if (easeLeft != null && easeLeft.length > 0) t01 = Mathf.Clamp01(easeLeft.Evaluate(t01));
            target = Vector3.LerpUnclamped(pMin, pBase, t01);
        }
        else
        {
            float t12 = SafeInverseLerp(aBase, aMax, a);
            if (easeRight != null && easeRight.length > 0) t12 = Mathf.Clamp01(easeRight.Evaluate(t12));
            target = Vector3.LerpUnclamped(pBase, pMax, t12);
        }

        // Apply in chosen space
        switch (applyMode)
        {
            case ApplyMode.WorldPosition:
                if (smooth && Application.isPlaying && !instant)
                    transform.position = Vector3.SmoothDamp(transform.position, target, ref velWorldOrLocal, Mathf.Max(0.0001f, smoothTime));
                else
                    transform.position = target;
                break;

            case ApplyMode.LocalPosition:
                if (smooth && Application.isPlaying && !instant)
                    transform.localPosition = Vector3.SmoothDamp(transform.localPosition, target, ref velWorldOrLocal, Mathf.Max(0.0001f, smoothTime));
                else
                    transform.localPosition = target;
                break;

            case ApplyMode.AnchoredPosition:
                if (!rt) rt = transform as RectTransform;
                if (!rt)
                {
                    Debug.LogWarning("AspectTriLerpedPosition: AnchoredPosition mode requires a RectTransform.");
                    return;
                }
                Vector2 t2 = new Vector2(target.x, target.y);
                if (smooth && Application.isPlaying && !instant)
                    rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, t2, ref velAnchored, Mathf.Max(0.0001f, smoothTime));
                else
                    rt.anchoredPosition = t2;
                break;
        }

        // Track viewport for change detection
        lastCamPixelRect = referenceCamera.pixelRect;
        lastScreenW = Screen.width;
        lastScreenH = Screen.height;
    }

    static float GetCameraAspect(Camera cam)
    {
        if (cam && cam.pixelHeight > 0)
            return (float)cam.pixelWidth / cam.pixelHeight;
        return (float)Screen.width / Mathf.Max(1, Screen.height);
    }

    static float SafeInverseLerp(float a, float b, float v)
    {
        if (Mathf.Approximately(a, b)) return 0f;
        return Mathf.InverseLerp(a, b, v);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ApplyNow(true);
    }
#endif
}

using UnityEngine;

[ExecuteAlways]
public class AspectClampView : MonoBehaviour
{
    [Header("Allowed aspect range (W/H)")]
    public float minAspect = 4f / 3f;   // 1.3333
    public float maxAspect = 18f / 9f;  // 2.0

    [Header("Camera to control")]
    public Camera playCamera;

    [Header("Lock horizontal world width")]
    public bool lockWorldWidth = true;

    // If 0, we auto-calibrate from current camera on first Apply().
    public float referenceWorldWidth = 0f;
    public bool autoCalibrateIfZero = true;

    [Header("Optional smoothing for ortho size")]
    public bool smoothOrthoResize = false;
    [Tooltip("Time constant (seconds) for smoothing. Smaller = faster.")]
    public float orthoLerpTime = 0.15f;

    // internals
    int lastScreenW, lastScreenH;
    int lastPixelW, lastPixelH;
    Rect lastRect;
    float lastTargetOrtho;

    void OnEnable()
    {
        Apply(true);
    }

    void OnDisable()
    {
        if (playCamera) playCamera.rect = new Rect(0f, 0f, 1f, 1f);
    }

    void Update()
    {
        if (!playCamera) return;

        bool sizeChanged =
            Screen.width != lastScreenW ||
            Screen.height != lastScreenH ||
            playCamera.pixelWidth != lastPixelW ||
            playCamera.pixelHeight != lastPixelH;

        if (sizeChanged)
        {
            Apply(false);
        }
        else if (smoothOrthoResize && lockWorldWidth && playCamera.orthographic)
        {
            // Asymptotic lerp toward target to handle live resize smoothly
            if (!Mathf.Approximately(playCamera.orthographicSize, lastTargetOrtho))
            {
                float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(1e-4f, orthoLerpTime));
                playCamera.orthographicSize = Mathf.Lerp(playCamera.orthographicSize, lastTargetOrtho, k);
            }
        }
    }

    public void Apply(bool firstCall)
    {
        if (!playCamera || Screen.height <= 0) return;

        // 1) Clamp viewport to [minAspect, maxAspect] using Screen aspect
        float aScreen = (float)Screen.width / Screen.height;
        Rect targetRect;

        if (aScreen > maxAspect)
        {
            // pillarbox
            float w = maxAspect / aScreen;
            float x = (1f - w) * 0.5f;
            targetRect = new Rect(x, 0f, w, 1f);
        }
        else if (aScreen < minAspect)
        {
            // letterbox
            float h = aScreen / minAspect;
            float y = (1f - h) * 0.5f;
            targetRect = new Rect(0f, y, 1f, h);
        }
        else
        {
            // inside window: full viewport
            targetRect = new Rect(0f, 0f, 1f, 1f);
        }

        if (!ApproximatelyRect(playCamera.rect, targetRect))
        {
            playCamera.rect = targetRect;
            lastRect = targetRect;
        }

        // 2) Lock horizontal world width (orthographic only)
        if (lockWorldWidth && playCamera.orthographic)
        {
            // Use the camera's actual pixel rect (after we set camera.rect)
            float aCam = GetCameraAspect(playCamera);

            if (autoCalibrateIfZero && (referenceWorldWidth <= 0f))
            {
                // Record current width as the reference once
                referenceWorldWidth = 2f * playCamera.orthographicSize * aCam;
            }

            float targetOrtho = referenceWorldWidth / (2f * Mathf.Max(0.0001f, aCam));
            lastTargetOrtho = targetOrtho;

            if (!smoothOrthoResize || !Application.isPlaying)
            {
                playCamera.orthographicSize = targetOrtho;
            }
            // else: Update() will smooth toward lastTargetOrtho
        }

        // 3) Track sizes for change detection
        lastScreenW = Screen.width;
        lastScreenH = Screen.height;
        lastPixelW = playCamera.pixelWidth;
        lastPixelH = playCamera.pixelHeight;
    }

    // Aspect of the visible camera viewport in pixels
    static float GetCameraAspect(Camera cam)
    {
        if (cam && cam.pixelHeight > 0)
            return (float)cam.pixelWidth / cam.pixelHeight;
        return (float)Screen.width / Mathf.Max(1, Screen.height);
    }

    static bool ApproximatelyRect(Rect a, Rect b)
    {
        const float eps = 1e-6f;
        return Mathf.Abs(a.x - b.x) < eps &&
               Mathf.Abs(a.y - b.y) < eps &&
               Mathf.Abs(a.width - b.width) < eps &&
               Mathf.Abs(a.height - b.height) < eps;
    }

#if UNITY_EDITOR
    [ContextMenu("Calibrate reference width from current")]
    void CalibrateReferenceWidthFromCurrent()
    {
        if (!playCamera) return;
        float aCam = GetCameraAspect(playCamera);
        referenceWorldWidth = 2f * playCamera.orthographicSize * aCam;
        Debug.Log("Calibrated referenceWorldWidth = " + referenceWorldWidth);
    }
#endif
}
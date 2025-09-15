using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;

[DisallowMultipleComponent]
public class CameraController : MonoBehaviour
{
    [Header("Timings")]
    public float moveDuration = 1.0f;
    public float zoomDuration = 1.0f;
    public float postMoveDelay = 0f;

    [Header("Framing")]
    public float paddingWorldUnits = 0f;

    [Header("Integration with AspectClampView")]
    public bool temporarilyDisableAspectClamp = true;
    [Tooltip("If true, after framing we set AspectClampView.referenceWorldWidth to the current world width so the clamp holds this zoom.")]
    public bool handOffWidthToClamp = true;

    private Camera mainCamera;
    private AspectClampView clamp; // optional

    private Vector3 initialPosition;
    private float initialOrthographicSize;

    private Coroutine activeRoutine;

    void Awake()
    {
        mainCamera = Camera.main;
        if (!mainCamera)
        {
            Debug.LogError("CameraController: No Camera.main found.");
            enabled = false;
            return;
        }
        clamp = mainCamera.GetComponent<AspectClampView>();
    }

    void Start()
    {
        if (!mainCamera.orthographic)
            Debug.LogWarning("CameraController expects an orthographic camera.");

        initialPosition = transform.position;
        initialOrthographicSize = mainCamera.orthographicSize;
    }

    // Public API ---------------------------------------------------------------

    public void AnimateToRect(RectTransform targetRect, float delay = 0f, Action onZoomMidpoint = null, Action onZoomEndpoint = null)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(AnimateToRectRoutine(targetRect, delay, onZoomMidpoint, onZoomEndpoint));
    }

    public void AnimateToBounds(Bounds worldBounds, float delay = 0f, Action onZoomMidpoint = null, Action onZoomEndpoint = null)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(AnimateToBoundsRoutine(worldBounds, delay, onZoomMidpoint, onZoomEndpoint));
    }

    public void AnimateToWorldPoints(Vector3[] worldPoints, float delay = 0f, Action onZoomMidpoint = null, Action onZoomEndpoint = null)
    {
        if (worldPoints == null || worldPoints.Length == 0) return;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(AnimateToPointsRoutine(worldPoints, delay, onZoomMidpoint, onZoomEndpoint));
    }

    public void ResetToStartPosition(float delay = 0f)
    {
        if (!mainCamera) return;
        if (activeRoutine != null) StopCoroutine(activeRoutine);

        bool hadClamp = (clamp != null);
        bool prevLock = false;
        bool prevSmooth = false;

        if (hadClamp && temporarilyDisableAspectClamp)
        {
            prevLock = clamp.lockWorldWidth;
            prevSmooth = clamp.smoothOrthoResize;
            clamp.lockWorldWidth = false;
            clamp.smoothOrthoResize = false;
            clamp.Apply(true);
        }

        DOTween.Kill(transform);
        DOTween.Kill(mainCamera);

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(Mathf.Max(0f, delay));

        Tween moveTween = transform.DOMove(initialPosition, moveDuration).SetEase(Ease.InOutSine);
        Tween zoomTween = mainCamera.DOOrthoSize(initialOrthographicSize, zoomDuration).SetEase(Ease.InOutSine);

        seq.Join(moveTween);
        seq.Join(zoomTween);

        seq.OnComplete(() =>
        {
            if (hadClamp && temporarilyDisableAspectClamp)
            {
                // Hand back control without changing the current width.
                if (handOffWidthToClamp)
                {
                    float widthNow = 2f * mainCamera.orthographicSize * Mathf.Max(0.0001f, mainCamera.aspect);
                    clamp.referenceWorldWidth = widthNow;
                }
                clamp.lockWorldWidth = prevLock;
                // avoid any smoothing-induced nudge on restore
                clamp.smoothOrthoResize = false;
                clamp.Apply(true);
                // restore previous smoothing next frame
                if (prevSmooth) StartCoroutine(RestoreClampSmoothingNextFrame(true));
            }
        });
    }

    // Coroutines ---------------------------------------------------------------

    private IEnumerator AnimateToRectRoutine(RectTransform targetRect, float delay, Action onZoomMidpoint, Action onZoomEndpoint)
    {
        if (!targetRect || !mainCamera) yield break;

        Canvas c = targetRect.GetComponentInParent<Canvas>();
        if (c && c.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogWarning("AnimateToRect: Target is on a Screen Space Overlay Canvas; scene camera cannot frame overlay UI.");
            yield break;
        }

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        yield return AnimateToPointsRoutine(corners, delay, onZoomMidpoint, onZoomEndpoint);
    }

    private IEnumerator AnimateToBoundsRoutine(Bounds worldBounds, float delay, Action onZoomMidpoint, Action onZoomEndpoint)
    {
        if (!mainCamera) yield break;

        Vector3 c = worldBounds.center;
        Vector3 e = worldBounds.extents;
        Vector3[] pts = new Vector3[8]
        {
            c + new Vector3(-e.x, -e.y, -e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z),
            c + new Vector3(-e.x,  e.y,  e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3( e.x,  e.y, -e.z),
            c + new Vector3( e.x,  e.y,  e.z),
        };

        yield return AnimateToPointsRoutine(pts, delay, onZoomMidpoint, onZoomEndpoint);
    }

    private IEnumerator AnimateToPointsRoutine(Vector3[] worldPoints, float delay, Action onZoomMidpoint, Action onZoomEndpoint)
    {
        if (!mainCamera) yield break;
        if (delay > 0f) yield return new WaitForSeconds(delay);

        DOTween.Kill(transform);
        DOTween.Kill(mainCamera);

        bool hadClamp = (clamp != null);
        bool prevLock = false;
        bool prevSmooth = false;

        if (hadClamp && temporarilyDisableAspectClamp)
        {
            prevLock = clamp.lockWorldWidth;
            prevSmooth = clamp.smoothOrthoResize;
            clamp.lockWorldWidth = false;
            clamp.smoothOrthoResize = false;
            clamp.Apply(true);
        }

        // 1) Camera-space AABB from points
        Vector2 sizeCam;
        Vector3 worldCenter;
        GetCameraSpaceAABBFromPoints(worldPoints, out sizeCam, out worldCenter);

        // 2) Move
        Vector3 moveTarget = new Vector3(worldCenter.x, worldCenter.y, transform.position.z);
        Tween moveTween = transform.DOMove(moveTarget, moveDuration).SetEase(Ease.InOutSine);
        yield return moveTween.WaitForCompletion();

        if (postMoveDelay > 0f) yield return new WaitForSeconds(postMoveDelay);

        // 3) Recompute size (not strictly necessary for ortho, but harmless)
        GetCameraSpaceAABBFromPoints(worldPoints, out sizeCam, out worldCenter);

        // 4) Fit with actual viewport aspect
        float aspect = mainCamera.aspect;

        float width = sizeCam.x + paddingWorldUnits * 2f;
        float height = sizeCam.y + paddingWorldUnits * 2f;

        float halfHeightToFitHeight = height * 0.5f;
        float halfHeightToFitWidth = (width * 0.5f) / Mathf.Max(0.0001f, aspect);

        float targetOrtho = Mathf.Max(halfHeightToFitHeight, halfHeightToFitWidth);

        Tween zoomTween = mainCamera.DOOrthoSize(targetOrtho, zoomDuration).SetEase(Ease.InOutSine);

        // 5) Callbacks on the zoom timeline
        yield return InvokeZoomCallbacks(zoomDuration, onZoomMidpoint, onZoomEndpoint);
        yield return zoomTween.WaitForCompletion();

        // 6) Hand control back to AspectClampView without snapping away
        if (hadClamp && temporarilyDisableAspectClamp)
        {
            if (handOffWidthToClamp)
            {
                float widthNow = 2f * mainCamera.orthographicSize * Mathf.Max(0.0001f, mainCamera.aspect);
                clamp.referenceWorldWidth = widthNow; // this is the key line
            }

            clamp.lockWorldWidth = prevLock;
            // avoid any smoothing induced drift on the same frame
            clamp.smoothOrthoResize = false;
            clamp.Apply(true);

            // restore smoothing setting next frame
            if (prevSmooth) StartCoroutine(RestoreClampSmoothingNextFrame(true));
        }

        activeRoutine = null;
    }

    // Helpers -----------------------------------------------------------------

    private IEnumerator InvokeZoomCallbacks(float duration, Action onMid, Action onEnd)
    {
        float tMid = duration * 0.3f;
        float tEnd = duration * 0.8f;

        if (tMid > 0f)
        {
            yield return new WaitForSeconds(tMid);
            onMid?.Invoke();
        }
        if (tEnd - tMid > 0f)
        {
            yield return new WaitForSeconds(tEnd - tMid);
            onEnd?.Invoke();
        }
    }

    private IEnumerator RestoreClampSmoothingNextFrame(bool smooth)
    {
        yield return null; // wait one frame so Apply has taken effect
        if (clamp) clamp.smoothOrthoResize = smooth;
    }

    // Build camera-space AABB from arbitrary world points
    private void GetCameraSpaceAABBFromPoints(Vector3[] worldPoints, out Vector2 sizeCamXY, out Vector3 worldCenter)
    {
        Matrix4x4 w2c = mainCamera.worldToCameraMatrix;

        Vector3 p0 = w2c.MultiplyPoint3x4(worldPoints[0]);
        Vector3 min = p0;
        Vector3 max = p0;

        for (int i = 1; i < worldPoints.Length; i++)
        {
            Vector3 p = w2c.MultiplyPoint3x4(worldPoints[i]);
            if (p.x < min.x) min.x = p.x;
            if (p.y < min.y) min.y = p.y;
            if (p.z < min.z) min.z = p.z;

            if (p.x > max.x) max.x = p.x;
            if (p.y > max.y) max.y = p.y;
            if (p.z > max.z) max.z = p.z;
        }

        Vector3 camCenter = (min + max) * 0.5f;
        sizeCamXY = new Vector2(max.x - min.x, max.y - min.y);
        worldCenter = mainCamera.cameraToWorldMatrix.MultiplyPoint3x4(camCenter);
    }
}

using UnityEngine;
using DG.Tweening;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public float moveDuration = 2f;
    public float zoomDuration = 2f;
    public float targetOrthographicSize = 3.5f;

    private Camera mainCamera;

    private Vector3 initialPosition;
    private float initialOrthographicSize;

    void Start()
    {
        mainCamera = Camera.main;
        initialPosition = transform.position;
        initialOrthographicSize = mainCamera.orthographicSize;
    }

    void Awake()
    {
        mainCamera = Camera.main;
    }

    public void AnimateToTarget(Transform target, float delay = 0f, System.Action onZoomMidpoint = null, System.Action onZoomEndpoint = null)
    {
        StartCoroutine(AnimateRoutine(target, delay, onZoomMidpoint, onZoomEndpoint));
    }

    private IEnumerator AnimateRoutine(Transform target, float delay, System.Action onZoomMidpoint, System.Action onZoomEndPoint)
    {
        yield return new WaitForSeconds(delay);

        Vector3 targetPos = target.position + new Vector3(0, 0, -10f);

        // Move camera first
        Tween moveTween = transform.DOMove(targetPos, 1f).SetEase(Ease.InOutSine);
        yield return moveTween.WaitForCompletion();

        // Start zoom tween
        Tween zoomTween = Camera.main.DOOrthoSize(targetOrthographicSize, zoomDuration).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(zoomDuration * 0.3f);
        onZoomMidpoint?.Invoke();

        yield return new WaitForSeconds(zoomDuration * 0.5f);
        onZoomEndPoint?.Invoke();

        // Wait for zoom to finish
        yield return zoomTween.WaitForCompletion();
    }

    public void ResetToStartPosition(float delay = 0f)
    {
        if (mainCamera == null) return;

        Sequence resetSequence = DOTween.Sequence();
        resetSequence.AppendInterval(delay);

        // Move and zoom simultaneously
        Tween moveTween = transform.DOMove(initialPosition, moveDuration).SetEase(Ease.InOutSine);
        Tween zoomTween = DOTween.To(() => mainCamera.orthographicSize, x => mainCamera.orthographicSize = x, initialOrthographicSize, zoomDuration)
                                 .SetEase(Ease.InOutSine);

        resetSequence.Append(moveTween);
        resetSequence.Join(zoomTween); // This works because we're appending moveTween to the sequence, then joining zoomTween to it
    }

}

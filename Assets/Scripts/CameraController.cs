using UnityEngine;
using DG.Tweening;

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

    public void AnimateToTarget(Transform target, float delay)
    {
        if (target == null || mainCamera == null)
            return;

        Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);

        Sequence cameraSequence = DOTween.Sequence();
        cameraSequence.AppendInterval(delay); // Wait before starting
        cameraSequence.Append(transform.DOMove(targetPos, moveDuration).SetEase(Ease.InOutSine)); // Move
        cameraSequence.Append(mainCamera.DOOrthoSize(targetOrthographicSize, zoomDuration).SetEase(Ease.InOutSine)); // Zoom after move
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

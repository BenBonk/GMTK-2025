using UnityEngine;

public class Butterfly : MonoBehaviour
{
    public float attractionRadius;
    private const float WAVE_FREQUENCY = 1.5f;
    private const float WAVE_AMPLITUDE = 0.8f;
    private const float MIN_FLY_TIME = 2f;
    private const float MAX_FLY_TIME = 5f;
    private const float EXIT_SPEED = 2.5f;
    private const float EXIT_TRANSITION_TIME = 0.5f;

    private float waveProgress = 0f;
    private float flyTimer = 0f;
    private float exitTime = 0f;
    private bool isExiting = false;
    private float exitDirection = 0f;
    private float currentExitSpeed = 0f;
    private float exitSpeedVelocity = 0f;

    public Sprite[] sprites;

    // pulled from Animal usage
    public float tiltFrequency = 3f;
    public float maxTiltAmplitude = 20f;
    public float speed = 2f;
    public float currentSpeed = 2f;
    public float topLimitY;
    public float bottomLimitY;
    private float tiltProgress = 0f;
    private Vector3 previousPosition;
    public float actualSpeed { get; private set; }
    public bool isExitingRound = false; // if you need forceExit, wire it up

    public bool CanBeLassoed => false; // kept for parity with the old override
    private float leftEdgeX;

    public void Start()
    {
        // init like your Animal.Start() did for spawn and limits
        SetVerticalLimits(GameController.gameManager ? GameController.gameManager.playArea : null);

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 rightEdge = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, cam.nearClipPlane));
            Vector3 leftEdge = cam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, cam.nearClipPlane));
            leftEdgeX = leftEdge.x - 1f;

            Vector3 startPos = new Vector3(rightEdge.x + 1f, transform.position.y, transform.position.z);
            transform.position = startPos;
        }

        currentSpeed = speed;

        waveProgress = Random.Range(0f, Mathf.PI * 2f);
        exitTime = Random.Range(MIN_FLY_TIME, MAX_FLY_TIME);
        GetComponent<SpriteRenderer>().sprite = sprites[Random.Range(0, sprites.Length)];
    }

    private void Update()
    {
        Vector3 nextPos = ComputeMove();
        nextPos.y = !isExiting ? Mathf.Clamp(nextPos.y, bottomLimitY, topLimitY) : nextPos.y;

        transform.position = nextPos;

        if (nextPos.x < leftEdgeX - 5f)
            Destroy(gameObject);
    }

    private void LateUpdate()
    {
        actualSpeed = (transform.position - previousPosition).magnitude / Mathf.Max(Time.deltaTime, 0.000001f);
        previousPosition = transform.position;
        ApplyRunTilt();
    }

    private Vector3 ComputeMove()
    {
        BroadcastPredatorAttraction();

        if (!isExiting)
        {
            flyTimer += Time.deltaTime;

            if (flyTimer >= exitTime)
            {
                isExiting = true;
                currentExitSpeed = 0f;

                float centerY = (topLimitY + bottomLimitY) / 2f;
                exitDirection = transform.position.y >= centerY ? 1f : -1f;
            }
        }

        if (isExiting)
        {
            currentExitSpeed = Mathf.SmoothDamp(currentExitSpeed, EXIT_SPEED, ref exitSpeedVelocity, EXIT_TRANSITION_TIME);

            Vector3 nextPos = transform.position + Vector3.left * currentSpeed * Time.deltaTime;
            nextPos.y += exitDirection * currentExitSpeed * Time.deltaTime;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 viewportPos = cam.WorldToViewportPoint(nextPos);
                if (viewportPos.y > 1.3f || viewportPos.y < -0.3f)
                {
                    Destroy(gameObject, 1f);
                }
            }

            return nextPos;
        }
        else
        {
            waveProgress += Time.deltaTime * WAVE_FREQUENCY;
            float waveOffset = Mathf.Sin(waveProgress) * WAVE_AMPLITUDE * Time.deltaTime;

            Vector3 nextPos = transform.position + Vector3.left * currentSpeed * Time.deltaTime;
            nextPos.y += waveOffset;

            return nextPos;
        }
    }

    private void ApplyRunTilt()
    {
        tiltProgress += Time.deltaTime * tiltFrequency * 2f;

        float denom = Mathf.Max(0.0001f, Mathf.Abs(speed));
        float speedFactor = Mathf.Clamp01(actualSpeed / denom);

        float amplitude = maxTiltAmplitude * speedFactor * 0.5f;
        float angle = Mathf.Sin(tiltProgress * Mathf.PI * 2f) * amplitude;

        if (float.IsNaN(angle) || float.IsInfinity(angle)) angle = 0f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void BroadcastPredatorAttraction()
    {
        // Predators still do FindObjectsOfType<Animal>() and TryPredatorAttractionOverride.
        // Now they can target any GameObject (this.gameObject).
        Animal[] all = FindObjectsOfType<Animal>();
        Vector3 myPos = transform.position;
        float r2 = attractionRadius * attractionRadius;

        for (int i = 0; i < all.Length; i++)
        {
            var a = all[i];
            if (a == null || !a.isPredator) continue;
            if (a.isLassoed) continue;

            if ((a.transform.position - myPos).sqrMagnitude <= r2)
            {
                a.SetAttractTarget(gameObject);
                a.ModifySpeed("chase", 1.5f);
            }
        }
    }

    private void OnDestroy()
    {
        // keep your prior cleanup: any predator targeting THIS butterfly should be destroyed
        Animal[] all = FindObjectsOfType<Animal>();
        for (int i = 0; i < all.Length; i++)
        {
            var a = all[i];
            if (!a || !a.isPredator) continue;

            if (a.AttractTarget == gameObject)
            {
                Destroy(a.gameObject);
            }
        }
    }

    // condensed vertical limits copied from your Animal.SetVerticalLimits
    public void SetVerticalLimits(RectTransform rect, Camera worldCam = null)
    {
        if (!worldCam)
        {
            if (rect)
            {
                var root = rect.GetComponentInParent<Canvas>()?.rootCanvas;
                if (root && root.renderMode != RenderMode.ScreenSpaceOverlay && root.worldCamera)
                    worldCam = root.worldCamera;
            }
            if (!worldCam) worldCam = Camera.main;
        }
        if (!worldCam) return;

        float zDist = Mathf.Abs(Vector3.Dot(transform.position - worldCam.transform.position, worldCam.transform.forward));

        float halfHeight = 0f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr) halfHeight = sr.bounds.extents.y;

        if (rect)
        {
            Vector3[] wc = new Vector3[4];
            rect.GetWorldCorners(wc);

            var root = rect.GetComponentInParent<Canvas>()?.rootCanvas;
            bool overlay = root && root.renderMode == RenderMode.ScreenSpaceOverlay;

            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(overlay ? null : worldCam, wc[i]);
                if (sp.y < minY) minY = sp.y;
                if (sp.y > maxY) maxY = sp.y;
            }

            Rect pr = worldCam.pixelRect;
            minY = Mathf.Max(minY, pr.yMin);
            maxY = Mathf.Min(maxY, pr.yMax);

            if (minY > maxY)
            {
                Vector3 topV = worldCam.ViewportToWorldPoint(new Vector3(0.5f, 1f, zDist));
                Vector3 botV = worldCam.ViewportToWorldPoint(new Vector3(0.5f, 0f, zDist));
                topLimitY = topV.y - halfHeight;
                bottomLimitY = botV.y + halfHeight;
                return;
            }

            float centerXPix = Mathf.Clamp(0.5f * (pr.xMin + pr.xMax), pr.xMin, pr.xMax);
            Vector3 topWorld = worldCam.ScreenToWorldPoint(new Vector3(centerXPix, maxY, zDist));
            Vector3 botWorld = worldCam.ScreenToWorldPoint(new Vector3(centerXPix, minY, zDist));

            topLimitY = topWorld.y - halfHeight;
            bottomLimitY = botWorld.y + halfHeight;
        }
        else
        {
            Vector3 topWorld = worldCam.ViewportToWorldPoint(new Vector3(0.5f, 1f, zDist));
            Vector3 botWorld = worldCam.ViewportToWorldPoint(new Vector3(0.5f, 0f, zDist));
            topLimitY = topWorld.y - halfHeight;
            bottomLimitY = botWorld.y + halfHeight;
        }
    }
}


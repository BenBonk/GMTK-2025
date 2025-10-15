using UnityEngine;

public class Butterfly : Animal
{
    public float attractionRadius;
    private const float WAVE_FREQUENCY = 1.5f;
    private const float WAVE_AMPLITUDE = 0.8f;
    private const float MIN_FLY_TIME = 5f;
    private const float MAX_FLY_TIME = 8f;
    private const float EXIT_SPEED = 2.5f;
    private const float EXIT_TRANSITION_TIME = 0.5f;

    private float waveProgress = 0f;
    private float flyTimer = 0f;
    private float exitTime = 0f;
    private bool isExiting = false;
    private float exitDirection = 0f;
    private float currentExitSpeed = 0f;
    private float exitSpeedVelocity = 0f;

    public override bool CanBeLassoed => false;
    protected override bool ShouldClampY => !isExiting;
    public Sprite[] sprites;

    public override void Start()
    {
        base.Start();
        waveProgress = Random.Range(0f, Mathf.PI * 2f);
        exitTime = Random.Range(MIN_FLY_TIME, MAX_FLY_TIME);
        GetComponent<SpriteRenderer>().sprite = sprites[Random.Range(0, sprites.Length)];
    }

    protected override Vector3 ComputeMove()
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
                    Destroy(gameObject,1);
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

    protected override void ApplyRunTilt()
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
        Animal[] all = FindObjectsOfType<Animal>();
        Vector3 myPos = transform.position;
        float r2 = attractionRadius * attractionRadius;

        for (int i = 0; i < all.Length; i++)
        {
            var a = all[i];
            if (a == null || a == this) continue;
            if (!a.isPredator) continue;   // only predators get attracted
            if (a.isLassoed) continue;

            // inside radius? tag with an attraction target for a short time
            if ((a.transform.position - myPos).sqrMagnitude <= r2)
            {
                a.SetAttractTarget(this);
            }
        }
    }

    private void OnDestroy()
    {
        Animal[] all = FindObjectsOfType<Animal>();
        for (int i = 0; i < all.Length; i++)
        {
            var a = all[i];
            if (a == null || a == this) continue;
            if (!a.isPredator) continue;

            if (a.AttractTarget == this)
            {
                Destroy(a.gameObject);
            }
        }
    }
}

using UnityEngine;

public class Cow : Animal
{
    public float minMoveDuration = 2f;
    public float maxMoveDuration = 4f;
    public float minPauseDuration = 0.5f;
    public float maxPauseDuration = 1.5f;
    public float pauseFadeDuration = 0.7f;

    public float stopThreshold = 0.05f;

    private float speedTarget;
    private float speedVelocity = 0f;
    private float stateTimer = 0f;
    private bool isPaused = false;

    // baselines for scaling
    private float baseSpeed;
    private float basePauseFadeDuration;
    private float baseMinMove, baseMaxMove;
    private float baseMinPause, baseMaxPause;

    // track last applied scale for mid-phase timing
    private float _lastScale = 1f;

    protected override void Awake()
    {
        base.Awake();
        baseSpeed = speed;
        basePauseFadeDuration = pauseFadeDuration;
        baseMinMove = minMoveDuration;
        baseMaxMove = maxMoveDuration;
        baseMinPause = minPauseDuration;
        baseMaxPause = maxPauseDuration;
    }

    protected override void ApplyEffectiveSpeedScale(float scale)
    {
        const float EXP_ACCEL = 0.5f;
        const float EXP_TIME = 0.6f;

        speed = baseSpeed * scale;
        pauseFadeDuration = basePauseFadeDuration / Mathf.Pow(scale, EXP_ACCEL);

        minMoveDuration = baseMinMove / Mathf.Pow(scale, EXP_TIME);
        maxMoveDuration = baseMaxMove / Mathf.Pow(scale, EXP_TIME);
        minPauseDuration = baseMinPause / Mathf.Pow(scale, EXP_TIME);
        maxPauseDuration = baseMaxPause / Mathf.Pow(scale, EXP_TIME);

        if (_lastScale > 0f && !Mathf.Approximately(scale, _lastScale))
        {
            float k = scale / _lastScale;
            stateTimer /= k;

            if (!isPaused) speedTarget = speed;
        }

        _lastScale = scale;
    }

    public override void Start()
    {
        base.Start();
        currentSpeed = speed;    // already scaled by OnEnable -> ApplyEffectiveSpeedScale
        speedTarget = speed;
        stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
    }

    protected override Vector3 ComputeMove()
    {
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, pauseFadeDuration);
        stateTimer -= Time.deltaTime;

        if (isPaused)
        {
            if (stateTimer <= 0f)
            {
                isPaused = false;
                speedTarget = speed; // resume moving
                stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
            }
        }
        else
        {
            if (stateTimer <= 0f)
            {
                // begin stopping
                speedTarget = 0f;

                if (Mathf.Abs(currentSpeed) < stopThreshold)
                {
                    isPaused = true;
                    stateTimer = Random.Range(minPauseDuration, maxPauseDuration);
                    // reset damping for snappy stop/start
                    speedVelocity = 0f;
                    currentSpeed = 0f;
                }
            }
        }

        return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
    }
}


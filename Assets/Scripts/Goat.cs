using System.Collections.Generic;
using UnityEngine;

public class Goat : Animal
{
    public float minMoveDuration = 2f;
    public float maxMoveDuration = 4f;
    public float minPauseDuration = 0.5f;
    public float maxPauseDuration = 1.5f;
    public float pauseFadeDuration = 0.7f;

    public float stopThreshold = 0.05f;

    public float maxAngleFromLeft = 45f; // Limit direction spread

    public float predatorSlowRadius = 2.5f;
    public float predatorSpeedMultiplier = 0.5f;
    public float slowCheckInterval = 0.2f;
    private readonly HashSet<Animal> _slowedNow = new HashSet<Animal>();
    private float _slowCheckTimer = 0f;


    private Vector3 moveDirection;
    private float stateTimer = 0f;
    private float speedTarget;
    private float speedVelocity = 0f;
    private bool isPaused = false;

    [Header("Predator Attraction")]
    public float attractionRadius = 6f;     // how far the goat attracts predators
    public float attractStickTime = 3f; //how long a predator is attracted

    private float baseSpeed;
    private float basePauseFadeDuration;
    private float baseMinMove, baseMaxMove;
    private float baseMinPause, baseMaxPause;
    // optional: if you want radii to scale a bit with game speed
    private float basePredatorSlowRadius;
    private float baseAttractionRadius;

    private float _lastScale = 1f; // for mid-phase timer rescale

    protected override void Awake()
    {
        base.Awake();
        baseSpeed = speed;
        basePauseFadeDuration = pauseFadeDuration;
        baseMinMove = minMoveDuration;
        baseMaxMove = maxMoveDuration;
        baseMinPause = minPauseDuration;
        baseMaxPause = maxPauseDuration;

        basePredatorSlowRadius = predatorSlowRadius;
        baseAttractionRadius = attractionRadius;
    }

    public override void Start()
    {
        base.Start();
        PickNewDirection();
        speedTarget = speed;
        currentSpeed = speed;
        stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
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

        predatorSlowRadius = basePredatorSlowRadius * Mathf.Pow(scale, 0.25f);
        attractionRadius = baseAttractionRadius * Mathf.Pow(scale, 0.25f);

        if (_lastScale > 0f && !Mathf.Approximately(scale, _lastScale))
        {
            float k = scale / _lastScale;   
            stateTimer /= k;             
            if (!isPaused) speedTarget = speed;
        }

        _lastScale = scale;
    }

    protected override Vector3 ComputeMove()
    {
        SlowNearbyPredators();
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref speedVelocity, pauseFadeDuration);
        stateTimer -= Time.deltaTime;
        edgeRedirectTimer -= Time.deltaTime;
        if (isPaused)
        {
            if (stateTimer <= 0f)
            {
                isPaused = false;
                speedTarget = speed;
                PickNewDirection();
                stateTimer = Random.Range(minMoveDuration, maxMoveDuration);
            }
        }
        else
        {
            if (stateTimer <= 0f)
            {
                speedTarget = 0f;
                if (Mathf.Abs(currentSpeed) < stopThreshold)
                {
                    isPaused = true;
                    BroadcastPredatorAttraction();
                    stateTimer = Random.Range(minPauseDuration, maxPauseDuration);
                }
            }
        }

        if (!isPaused)
        {
            EdgeGuardRedirect();
        }

        // Move in the chosen direction
        Vector3 nextPos = transform.position + moveDirection * currentSpeed * Time.deltaTime;

        return nextPos;
    }


    public float edgeGuardMargin = 0.5f;
    public float edgeRedirectCooldown = 0.5f;
    private float edgeRedirectTimer = 0f;
    private void PickNewDirection()
    {
        // Angle is relative to left. Positive = up-left, negative = down-left.
        float y = transform.position.y;

        float minAngle = -maxAngleFromLeft;
        float maxAngle = maxAngleFromLeft;

        // If near or touching bottom, do not pick a downward angle.
        if (y <= bottomLimitY + edgeGuardMargin)
        {
            minAngle = 0f; // only flat or up-left
        }
        // If near or touching top, do not pick an upward angle.
        else if (y >= topLimitY - edgeGuardMargin)
        {
            maxAngle = 0f; // only flat or down-left
        }

        float angle = Random.Range(minAngle, maxAngle);
        float radians = angle * Mathf.Deg2Rad;

        moveDirection = new Vector3(-Mathf.Cos(radians), Mathf.Sin(radians), 0f).normalized;
    }

    private void SlowNearbyPredators()
    {
        _slowCheckTimer -= Time.deltaTime;
        if (_slowCheckTimer > 0f) return;
        _slowCheckTimer = slowCheckInterval;

        float r2 = predatorSlowRadius * predatorSlowRadius;

        // Build the current-in-range set
        var inRange = new HashSet<Animal>();
        var all = FindObjectsOfType<Animal>();
        foreach (var other in all)
        {
            if (other == null || other == this || other.isLassoed || !other.isPredator) continue;

            if ((other.transform.position - transform.position).sqrMagnitude <= r2)
                inRange.Add(other);
        }

        foreach (var a in inRange)
        {
            if (_slowedNow.Add(a))
                a.ModifySpeed("goat", predatorSpeedMultiplier); // enable once on enter
        }

        var toRemove = new List<Animal>();
        foreach (var a in _slowedNow)
        {
            if (a == null || a.isLassoed || !inRange.Contains(a))
            {
                a?.RevertSpeed("goat"); // revert once on exit
                toRemove.Add(a);
            }
        }
        foreach (var a in toRemove) _slowedNow.Remove(a);
    }

    private void BroadcastPredatorAttraction()
    {
        if (!legendary) return;

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
                a.SetAttractTarget(this.gameObject);
            }
        }
    }

    private void EdgeGuardRedirect()
    {
        if (edgeRedirectTimer > 0f) return;

        float y = transform.position.y;

        if ((y <= bottomLimitY + edgeGuardMargin && moveDirection.y < 0f) ||
            (y >= topLimitY - edgeGuardMargin && moveDirection.y > 0f))
        {
            PickNewDirection(); // your edge-aware version
            edgeRedirectTimer = edgeRedirectCooldown;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        foreach (var a in _slowedNow) a?.RevertSpeed("goat");
        _slowedNow.Clear();
    }
}

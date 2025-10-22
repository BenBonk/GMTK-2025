using System;
using System.Collections;
using UnityEngine;

public class Bee : Animal
{
    [Header("Wave patrol")]
    public float waveAmplitude = 1f;
    public float waveFrequency = 2f;
    private bool initialized = false;
    private float waveProgress = 0f;
    private float startY;

    [Header("Targeting")]
    public float detectRadius = 3.0f;     
    public float minDetectRadius = 0.75f;   
    public LayerMask animalMask;
    public bool includePredators = true;
    public float retargetCooldown = 0.6f;
    // Only target animals in front of the bee
    [Range(0f, 180f)]
    public float frontConeAngle = 60f; // degrees of vision cone

    [Header("Attack geometry")]
    public float hoverHeight = 1.25f;
    public float hoverSpeed = 6f;
    public float alignRotSpeed = 720f;
    public float diveSpeed = 12f;
    public float hitInset = 0.05f;

    [Header("Stinger orientation")]
    public Vector2 localStingerDir = new Vector2(-1f, 0f);

    [Header("After sting")]
    public bool destroyAfterSting = false;
    public float recoverUpKick = 4f;

    [Header("Dive timing")]
    public Vector2 diveDelayRange = new Vector2(0.25f, 0.6f);
    public bool useAnticipation = true;
    public float anticipateDip = 0.15f;
    public float anticipateTime = 0.12f;

    [Header("Attack motion")]
    public float pullBackDistance = 0.5f;
    public float pullBackTime = 0.15f;
    public float diveAccel = 35f;

    [Header("Spawn delay")]
    public float targetSearchDelay = 2f;

    private bool facingRight = true;


    // private state
    Vector3 hoverPoint;
    float diveDelayTimer = 0f;
    float anticipateT = 0f;
    float diveVy = 0f;
    float diveVel = 0f;

    Vector3 pullStart, pullEnd;
    Vector2 pullDir;
    float pullTimer = 0f;

    enum BeeState { Patrol, Stalking, PreDive, PullBack, Diving, Recover }
    BeeState state = BeeState.Patrol;

    [Serializable]
    public class StingEvent : UnityEngine.Events.UnityEvent<Animal> { }
    public StingEvent OnSting;

    Animal target;
    float retargetTimer = 0f;
    Vector3 smoothVel = Vector3.zero;
    float spawnTimer = 0f;

    // -------------------------------------------------------------------------
    public override void Start()
    {
        base.Start();

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 rightEdge = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, cam.nearClipPlane));
            Vector3 leftEdge = cam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, cam.nearClipPlane));
            leftEdgeX = leftEdge.x - 0.5f;

            startPos = new Vector3(leftEdge.x - 1f, transform.position.y, transform.position.z);
            transform.position = startPos;
        }

        currentSpeed = speed;
        spawnTimer = targetSearchDelay;
    }

    void UpdateFacing(float moveX)
    {
        // Only flip sprite if moving horizontally enough
        if (Mathf.Abs(moveX) > 0.01f)
        {
            bool shouldFaceRight = moveX > 0f;

            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;

                // flip the local scale.x, but preserve rotation and y scale
                Vector3 sc = transform.localScale;
                sc.x = Mathf.Abs(sc.x) * (facingRight ? 1f : -1f);
                transform.localScale = sc;
            }
        }
    }

    // -------------------------------------------------------------------------
    protected override Vector3 ComputeMove()
    {
        if (spawnTimer > 0f) spawnTimer -= Time.deltaTime;
        if (retargetTimer > 0f) retargetTimer -= Time.deltaTime;

        switch (state)
        {
            case BeeState.Patrol:
                PatrolTick();
                TryAcquireTarget();
                return PatrolMove();

            case BeeState.Stalking:
                if (TargetIsInvalidOrLassoed()) { LoseTarget(); return PatrolMove(); }
                return StalkMove();

            case BeeState.PreDive:
                if (TargetIsInvalidOrLassoed()) { LoseTarget(); return PatrolMove(); }
                return PreDiveMove();

            case BeeState.PullBack:
                if (TargetIsInvalidOrLassoed()) { LoseTarget(); return PatrolMove(); }
                return PullBackMove();

            case BeeState.Diving:
                if (TargetIsInvalidOrLassoed()) { LoseTarget(); return PatrolMove(); }
                return DiveMove();

            case BeeState.Recover:
                return RecoverMove();
        }

        return transform.position;
    }

    // -------------------------------------------------------------------------
    private Vector3 PatrolMove()
    {
        if (!initialized)
        {
            AdjustStartYToFitWave();
            initialized = true;
        }

        waveProgress += Time.deltaTime;
        float yOffset = Mathf.Sin(waveProgress * Mathf.PI * 2f * waveFrequency) * waveAmplitude;

        Vector3 next = transform.position + Vector3.right * currentSpeed * Time.deltaTime;
        float verticalOffset = externalOffset.y;
        next.y = startY + yOffset + verticalOffset;
        externalOffset.y = 0f;

        float moveX = next.x - transform.position.x;
        UpdateFacing(moveX);

        return next;
    }

    private void PatrolTick() { }

    // -------------------------------------------------------------------------
    void TryAcquireTarget()
    {
        if (spawnTimer > 0f) return;
        if (retargetTimer > 0f) return;

        Animal best = null;
        float bestSqr = float.PositiveInfinity;

        // Forward direction based on stinger or sprite orientation
        Vector2 forward = GetWorldStingerDir();
        float maxSqr = detectRadius * detectRadius;
        float minSqr = minDetectRadius * minDetectRadius;

        if (animalMask.value != 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius, animalMask);
            foreach (var h in hits)
            {
                var a = h.GetComponentInParent<Animal>();
                if (!ValidVictim(a)) continue;

                Vector2 toTarget = (a.transform.position - transform.position);
                float d2 = toTarget.sqrMagnitude;
                if (d2 > maxSqr || d2 < minSqr) continue; // skip too far or too close

                // check if target is in front
                float angle = Vector2.Angle(forward, toTarget);
                if (angle > frontConeAngle * 0.5f) continue;

                if (d2 < bestSqr)
                {
                    best = a;
                    bestSqr = d2;
                }
            }
        }
        else
        {
            var all = FindObjectsOfType<Animal>();
            foreach (var a in all)
            {
                if (a == this || !ValidVictim(a)) continue;

                Vector2 toTarget = (a.transform.position - transform.position);
                float d2 = toTarget.sqrMagnitude;
                if (d2 > maxSqr || d2 < minSqr) continue;

                float angle = Vector2.Angle(forward, toTarget);
                if (angle > frontConeAngle * 0.5f) continue;

                if (d2 < bestSqr)
                {
                    best = a;
                    bestSqr = d2;
                }
            }
        }

        if (best != null)
        {
            target = best;
            state = BeeState.Stalking;
            smoothVel = Vector3.zero;
        }
    }

    bool ValidVictim(Animal a)
    {
        if (!a || a == this) return false;
        if (!includePredators && a.isPredator) return false;
        if (!a.gameObject.activeInHierarchy) return false;
        return true;
    }

    bool ValidateTarget()
    {
        return ValidVictim(target) && (target != null);
    }

    bool TargetIsInvalidOrLassoed()
    {
        return !ValidateTarget() || (target != null && target.isLassoed);
    }

    // -------------------------------------------------------------------------
    void LoseTarget()
    {
        target = null;
        state = BeeState.Patrol;
        retargetTimer = retargetCooldown;
        diveVy = 0f;
        diveVel = 0f;

        // smooth visual reentry: re-anchor wave and fade amplitude back in
        startY = ClampY(transform.position.y);
        waveProgress = 0f;
        StartCoroutine(RestoreWaveAmplitude());
    }

    IEnumerator RestoreWaveAmplitude()
    {
        float t = 0f;
        float originalAmp = waveAmplitude;
        waveAmplitude = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            waveAmplitude = Mathf.Lerp(0f, originalAmp, t / 0.5f);
            yield return null;
        }
        waveAmplitude = originalAmp;
    }

    // -------------------------------------------------------------------------
    Vector3 StalkMove()
    {
        Vector3 p = transform.position;
        Vector3 tp = target.transform.position;
        float targetTopY = GetTopY(target);
        Vector3 desired = new Vector3(tp.x, targetTopY + hoverHeight, p.z);

        p = Vector3.MoveTowards(p, desired, hoverSpeed * Time.deltaTime);
        //AlignStingerToTarget(Time.deltaTime);

        bool nearX = Mathf.Abs(p.x - desired.x) <= 0.05f;
        bool nearY = Mathf.Abs(p.y - desired.y) <= 0.05f;
        if (nearX && nearY)
        {
            hoverPoint = desired;
            state = BeeState.PreDive;
            diveDelayTimer = UnityEngine.Random.Range(diveDelayRange.x, diveDelayRange.y);
            anticipateT = 0f;
            //AlignStingerToTarget(Time.deltaTime, snap: true);
        }
        UpdateFacing(desired.x - transform.position.x);
        return p;
    }

    // -------------------------------------------------------------------------
    Vector3 PreDiveMove()
    {
        Vector3 p = hoverPoint;
        AlignStingerToTarget(Time.deltaTime);

        if (useAnticipation && anticipateTime > 0f)
        {
            anticipateT = Mathf.Min(anticipateT + Time.deltaTime, anticipateTime);
            float k = Mathf.SmoothStep(0f, 1f, anticipateT / anticipateTime);
            float dip = Mathf.Lerp(0f, -anticipateDip, k);
            p.y = hoverPoint.y + dip;
        }

        diveDelayTimer -= Time.deltaTime;
        if (diveDelayTimer <= 0f)
        {
            state = BeeState.PullBack;
            pullTimer = pullBackTime;
            pullDir = -(target.transform.position - transform.position).normalized;
            pullStart = transform.position;
            pullEnd = pullStart + (Vector3)pullDir * pullBackDistance;
            AlignStingerToTarget(Time.deltaTime, snap: true);
        }

        return p;
    }

    // -------------------------------------------------------------------------
    Vector3 PullBackMove()
    {
        AlignStingerToTarget(Time.deltaTime);

        pullTimer -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(pullTimer / pullBackTime);
        Vector3 p = Vector3.Lerp(pullStart, pullEnd, t);

        if (pullTimer <= 0f)
        {
            state = BeeState.Diving;
            diveVel = 0f;
        }

        return p;
    }

    // -------------------------------------------------------------------------
    Vector3 DiveMove()
    {
        AlignStingerToTarget(Time.deltaTime);

        diveVel += diveAccel * Time.deltaTime;
        float speedNow = Mathf.Min(diveVel, diveSpeed);

        Vector3 p = transform.position;
        Vector3 tp = target.transform.position;
        Vector2 aimDir = (tp - p).normalized;
        p += (Vector3)aimDir * (speedNow * Time.deltaTime);

        bool hit = false;
        var col = target.GetComponent<Collider2D>();
        if (col)
        {
            Vector2 closest = col.ClosestPoint(p);
            hit = ((Vector2)p - closest).sqrMagnitude <= 1e-8f;
        }
        else
        {
            var sr = target.GetComponent<SpriteRenderer>();
            if (sr) hit = sr.bounds.Contains(p);
        }

        if (hit)
        {
            OnSting?.Invoke(target);

            if (destroyAfterSting)
            {
                Destroy(gameObject);
                return p;
            }
            else
            {
                state = BeeState.Recover;
                diveVy = recoverUpKick;
            }
        }

        return p;
    }

    // -------------------------------------------------------------------------
    Vector3 RecoverMove()
    {
        Vector3 p = transform.position;
        p.y += diveVy * Time.deltaTime;
        diveVy += -12f * Time.deltaTime;

        if (diveVy <= 0f)
        {
            state = BeeState.Patrol;
            retargetTimer = retargetCooldown;
            startY = ClampY(p.y);
            waveProgress = 0f;
            StartCoroutine(RestoreWaveAmplitude());
        }

        return p;
    }

    // -------------------------------------------------------------------------
    void AlignStingerToTarget(float dt, bool snap = false)
    {
        if (!target) return;

        Vector2 aimDir = ((Vector2)(target.transform.position - transform.position)).normalized;
        if (aimDir.sqrMagnitude < 1e-6f) return;

        Vector2 stingerWorld = GetWorldStingerDir();
        Quaternion want = Quaternion.FromToRotation(stingerWorld, aimDir) * transform.rotation;

        transform.rotation = snap
            ? want
            : Quaternion.RotateTowards(transform.rotation, want, alignRotSpeed * dt);
    }

    protected override void ApplyRunTilt()
    {
        switch (state)
        {
            case BeeState.Patrol:
            case BeeState.Stalking:
                // Use the base "running" tilt while patrolling and while moving to the hover point
                base.ApplyRunTilt();
                break;

            case BeeState.PreDive:
            case BeeState.PullBack:
            case BeeState.Diving:
                // Once we are hovering or attacking, keep the stinger aimed at the target
                AlignStingerToTarget(Time.deltaTime);
                break;

            case BeeState.Recover:
                base.ApplyRunTilt();
                break;
        }
    }


    // -------------------------------------------------------------------------
    private float GetTopY(Animal a)
    {
        var sr = a.GetComponent<SpriteRenderer>();
        if (sr) return sr.bounds.max.y;
        return a.transform.position.y;
    }

    Vector2 GetAdjustedLocalStingerDir()
    {
        Vector2 d = localStingerDir.normalized;  // e.g., (-1,0) if stinger points left in the art
        if (transform.localScale.x < 0f) d.x = -d.x;
        return d;
    }

    Vector2 GetWorldStingerDir()
    {
        // rotation-only transform (scale is already baked into d above if needed)
        Vector2 dLocal = GetAdjustedLocalStingerDir();
        return ((Vector2)(transform.rotation * (Vector3)dLocal)).normalized;
    }

    private void AdjustStartYToFitWave()
    {
        float z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        Vector3 bottomWorld = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0f, z));
        Vector3 topWorld = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));

        float halfHeight = GetComponent<SpriteRenderer>().bounds.extents.y;
        float topLimit = topWorld.y - halfHeight;
        float bottomLimit = bottomWorld.y + halfHeight;

        float maxY = topLimit - waveAmplitude;
        float minY = bottomLimit + waveAmplitude;

        startY = Mathf.Clamp(transform.position.y, minY, maxY);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
#endif
}

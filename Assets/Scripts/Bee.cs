using System;
using UnityEngine;

public class Bee : Animal
{
    [Header("Patrol")]
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
    [Range(0f, 180f)] public float frontConeAngle = 60f;

    [Header("Hover & Attack")]
    public float hoverHeight = 1.25f;
    public float hoverSpeed = 6f;
    public float alignRotSpeed = 720f;
    public Vector2 diveDelayRange = new Vector2(0.25f, 0.6f);
    public float pullBackDistance = 0.35f;
    public float pullBackTime = 0.25f;
    public float diveSpeed = 12f;

    [Header("Stinger Orientation")]
    public Vector2 localStingerDir = new Vector2(-1f, 0f);

    [Header("After Sting")]
    public bool destroyAfterSting = false;
    public float recoverUpKick = 4f;

    [Serializable]
    public class StingEvent : UnityEngine.Events.UnityEvent<Animal> { }
    public StingEvent OnSting;
    public GameObject beePoof;
    
    private enum BeeState { Patrol, Stalking, PreDive, PullBack, Diving, Recover }
    private BeeState state = BeeState.Patrol;

    private Animal target;
    private float retargetTimer = 0f;
    private float diveVy = 0f;
    private float diveVel = 0f;
    private Vector3 hoverPoint;
    private Vector3 pullStart, pullEnd, pullDir;
    private float pullTimer = 0f;

    private bool facingRight = true;
    public float spawnTimer = 3f; // initial delay before acquiring targets

    public override void Start()
    {
        base.Start();
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 rightEdge = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, cam.nearClipPlane));
            Vector3 leftEdge = cam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, cam.nearClipPlane));
            leftEdgeX = leftEdge.x - 1f;
            startPos = new Vector3(leftEdge.x - 1f, transform.position.y, transform.position.z);
            transform.position = startPos;
        }
        currentSpeed = speed;
    }

    public void Update()
    {
        if (spawnTimer > 0f) spawnTimer -= Time.deltaTime;
        base.Update();
    }

    protected override Vector3 ComputeMove()
    {
        if (retargetTimer > 0f) retargetTimer -= Time.deltaTime;

        switch (state)
        {
            case BeeState.Patrol:
                TryAcquireTarget();
                return PatrolMove();

            case BeeState.Stalking:
                if (!ValidateTarget()) { LoseTarget(); return PatrolMove(); }
                return StalkMove();

            case BeeState.PreDive:
                if (!ValidateTarget()) { LoseTarget(); return PatrolMove(); }
                return PreDiveMove();

            case BeeState.PullBack:
                if (!ValidateTarget()) { LoseTarget(); return PatrolMove(); }
                return PullBackMove();

            case BeeState.Diving:
                if (!ValidateTarget()) { LoseTarget(); return PatrolMove(); }
                return DiveMove();

            case BeeState.Recover:
                return RecoverMove();
        }

        return transform.position;
    }

    // ----------------- Patrol -----------------
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
        next.y = startY + yOffset;
        return next;
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

    // ----------------- Targeting -----------------
    void TryAcquireTarget()
    {
        if (spawnTimer > 0f) return;
        if (retargetTimer > 0f) return;

        Animal best = null;
        float bestScore = float.PositiveInfinity;  // smaller is better

        Vector2 forward = GetWorldStingerDir();
        float maxSqr = detectRadius * detectRadius;
        float minSqr = minDetectRadius * minDetectRadius;

        Animal[] all = FindObjectsOfType<Animal>();
        foreach (var a in all)
        {
            if (a == this || !ValidVictim(a)) continue;

            Vector2 toTarget = a.transform.position - transform.position;
            float d2 = toTarget.sqrMagnitude;
            if (d2 > maxSqr || d2 < minSqr) continue;

            float angle = Vector2.Angle(forward, toTarget);
            if (angle > frontConeAngle * 0.5f) continue;

            // --- new part: prioritize closer Y-level alignment ---
            float yDiff = Mathf.Abs(a.transform.position.y - transform.position.y);

            // compute a score favoring same Y-level and proximity
            // weight Y-difference slightly higher so the bee stays level
            float score = d2 + (yDiff * yDiff * 2f); // tweak multiplier to taste

            if (score < bestScore)
            {
                best = a;
                bestScore = score;
            }
        }

        if (best != null)
        {
            target = best;
            state = BeeState.Stalking;
        }
    }


    bool ValidVictim(Animal a)
    {
        if (!a || a == this) return false;
        if (!includePredators && a.isPredator) return false;
        if (!a.gameObject.activeInHierarchy) return false;
        if (a.isLassoed) return false;
        return true;
    }

    bool ValidateTarget()
    {
        return target && ValidVictim(target);
    }

    void LoseTarget()
    {
        target = null;
        state = BeeState.Patrol;
        retargetTimer = retargetCooldown;
        diveVy = 0f;
        diveVel = 0f;

        // Face right again
        SetFacingRight(true);

        // Re-anchor patrol
        startY = ClampY(transform.position.y);
        waveProgress = 0f;
    }

    // ----------------- Movement States -----------------
    Vector3 StalkMove()
    {
        Vector3 p = transform.position;
        Vector3 tp = target.transform.position;
        float targetTopY = GetTopY(target);
        Vector3 desired = new Vector3(tp.x, targetTopY + hoverHeight, p.z);

        p = Vector3.MoveTowards(p, desired, hoverSpeed * Time.deltaTime);

        // Flip if target is behind
        float xToTarget = target.transform.position.x - transform.position.x;
        bool targetIsRight = xToTarget > 0f;
        bool currentlyRight = transform.localScale.x > 0f;
        if (targetIsRight != currentlyRight)
            SetFacingRight(targetIsRight);

        bool nearX = Mathf.Abs(p.x - desired.x) <= 0.05f;
        bool nearY = Mathf.Abs(p.y - desired.y) <= 0.05f;
        if (nearX && nearY)
        {
            hoverPoint = desired;
            state = BeeState.PreDive;
            diveVy = 0f;
            diveVel = 0f;
            SetFacingRight(true); // flip back before hover
            diveVy = UnityEngine.Random.Range(diveDelayRange.x, diveDelayRange.y);
        }

        return p;
    }

    Vector3 PreDiveMove()
    {
        Vector3 p = hoverPoint;
        AlignStingerToTarget(Time.deltaTime);
        diveVy -= Time.deltaTime;
        if (diveVy <= 0f)
        {
            state = BeeState.PullBack;
            pullTimer = pullBackTime;
            pullDir = -(target.transform.position - transform.position).normalized;
            pullStart = transform.position;
            pullEnd = pullStart + pullDir * pullBackDistance;
            SetFacingRight(true);
        }
        return p;
    }

    Vector3 PullBackMove()
    {
        pullTimer -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(pullTimer / pullBackTime);
        Vector3 p = Vector3.Lerp(pullStart, pullEnd, t);
        AlignStingerToTarget(Time.deltaTime);
        if (pullTimer <= 0f)
        {
            state = BeeState.Diving;
            diveVel = 0f;
            SetFacingRight(true);
        }
        return p;
    }

    Vector3 DiveMove()
    {
        AlignStingerToTarget(Time.deltaTime);
        Vector3 p = transform.position;
        Vector3 tp = target.transform.position;
        Vector2 aimDir = (tp - p).normalized;
        p += (Vector3)aimDir * (diveSpeed * Time.deltaTime);

        // hit test
        bool hit = false;
        var col = target.GetComponent<Collider2D>();
        if (col)
        {
            Vector2 closest = col.ClosestPoint(p);
            hit = ((Vector2)p - closest).sqrMagnitude <= 1e-6f;
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
                Instantiate(beePoof, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
            else
            {
                state = BeeState.Recover;
                diveVy = recoverUpKick;
            }
        }
        return p;
    }

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
            SetFacingRight(true);
        }
        return p;
    }

    // ----------------- Helpers -----------------
    private float GetTopY(Animal a)
    {
        var sr = a.GetComponent<SpriteRenderer>();
        if (sr) return sr.bounds.max.y;
        return a.transform.position.y;
    }

    void SetFacingRight(bool right)
    {
        Vector3 sc = transform.localScale;
        sc.x = Mathf.Abs(sc.x) * (right ? 1f : -1f);
        transform.localScale = sc;
        facingRight = right;
    }

    Vector2 GetWorldStingerDir()
    {
        Vector2 dir = localStingerDir.normalized;
        if (transform.localScale.x < 0f) dir.x = -dir.x;
        return ((Vector2)(transform.rotation * (Vector3)dir)).normalized;
    }

    void AlignStingerToTarget(float dt)
    {
        if (!target) return;
        Vector2 aimDir = ((Vector2)(target.transform.position - transform.position)).normalized;
        if (aimDir.sqrMagnitude < 1e-6f) return;
        Vector2 stingerWorld = GetWorldStingerDir();
        Quaternion want = Quaternion.FromToRotation(stingerWorld, aimDir) * transform.rotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, alignRotSpeed * dt);
    }

    protected override void ApplyRunTilt()
    {
        switch (state)
        {
            case BeeState.Patrol:
            case BeeState.Stalking:
                base.ApplyRunTilt();
                break;

            case BeeState.PreDive:
            case BeeState.PullBack:
            case BeeState.Diving:
                AlignStingerToTarget(Time.deltaTime);
                break;

            case BeeState.Recover:
                base.ApplyRunTilt();
                break;
        }
    }
}
